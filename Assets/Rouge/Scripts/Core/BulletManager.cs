using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    public class BulletManager : MonoBehaviour
    {
        public enum BulletType { Straight, Orbital, Ricochet, Shotgun, Chain }
        public static readonly int TypeCount = System.Enum.GetValues(typeof(BulletType)).Length;

        public static BulletManager Instance { get; private set; }

        [Header("Config")]
        public GameConfig config;

        public Transform playerTransform;

        private HashSet<BulletType> activeTypes = new HashSet<BulletType>();
        private float[] cooldowns;
        private float[] lastFireTimes = new float[TypeCount];

        private GameObject[] orbitalBullets;
        private int orbitalCount;
        private float orbitalRadius;
        private float orbitalSpeed;
        private float orbitalAngleOffset;

        private int ricochetMaxBounces;
        private int shotgunCount;
        private float shotgunSpread;
        private int chainMaxHops;
        private float chainRange;

        // Global upgrade modifiers (set by WaveManager)
        private float damageMult = 1f;
        private float cooldownMult = 1f;
        private int countBonus;
        private float speedMult = 1f;

        // Per-type upgrade modifiers
        private int ricochetBounceBonus;
        private float orbitalSpeedBonus;
        private int chainHopsBonus;
        private float chainRangeBonus;
        private float shotgunSpreadReduction;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ApplyConfig();
            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
            for (int i = 0; i < lastFireTimes.Length; i++)
                lastFireTimes[i] = -999f;

            // All types active by default
            for (int i = 0; i < TypeCount; i++)
                activeTypes.Add((BulletType)i);
            CreateOrbital();
        }

        private void ApplyConfig()
        {
            if (config != null)
            {
                cooldowns = new float[] { config.straight.cooldown, config.orbital.cooldown,
                    config.ricochet.cooldown, config.shotgun.cooldown, config.chain.cooldown };
                orbitalCount = config.orbital.count;
                orbitalRadius = config.orbitalRadius;
                orbitalSpeed = config.orbital.speed;
                ricochetMaxBounces = config.ricochetMaxBounces;
                shotgunCount = config.shotgun.count;
                shotgunSpread = config.shotgunSpreadAngle;
                chainMaxHops = config.chainMaxHops;
                chainRange = config.chainRange;
            }
            else
            {
                cooldowns = new float[] { 0.3f, 0f, 0.5f, 0.8f, 0.6f };
                orbitalCount = 3; orbitalRadius = 1.5f; orbitalSpeed = 360f;
                ricochetMaxBounces = 5; shotgunCount = 5; shotgunSpread = 60f;
                chainMaxHops = 3; chainRange = 5f;
            }
            for (int i = 0; i < cooldowns.Length; i++)
                cooldowns[i] *= cooldownMult;
            orbitalBullets = new GameObject[orbitalCount + countBonus];
        }

        private BulletTypeConfig GetConfig(BulletType type) => config != null ? type switch
        {
            BulletType.Straight => config.straight, BulletType.Orbital => config.orbital,
            BulletType.Ricochet => config.ricochet, BulletType.Shotgun => config.shotgun,
            BulletType.Chain => config.chain, _ => config.straight,
        } : null;

        private void Update()
        {
            if (playerTransform == null) return;
            if (GameManager.IsPaused) return;

            // Orbital always updates when active
            if (activeTypes.Contains(BulletType.Orbital))
                UpdateOrbital();

            // Fire all active non-orbital types independently
            for (int i = 0; i < TypeCount; i++)
            {
                var type = (BulletType)i;
                if (type == BulletType.Orbital || !activeTypes.Contains(type)) continue;
                if (Time.time - lastFireTimes[i] >= cooldowns[i])
                {
                    FireType(type);
                    lastFireTimes[i] = Time.time;
                }
            }
        }

        public void ToggleBulletType(BulletType type)
        {
            if (activeTypes.Contains(type))
            {
                activeTypes.Remove(type);
                if (type == BulletType.Orbital) DestroyOrbital();
            }
            else
            {
                activeTypes.Add(type);
                if (type == BulletType.Orbital) CreateOrbital();
            }
        }

        public bool IsBulletTypeActive(BulletType type) => activeTypes.Contains(type);

        public void SwitchBulletType(BulletType newType) => ToggleBulletType(newType);

        public void ApplyUpgrades(float dmgMult, float cdMult, int cntBonus, float spdMult)
        {
            damageMult = dmgMult;
            cooldownMult = cdMult;
            countBonus = cntBonus;
            speedMult = spdMult;
            bool hadOrbital = activeTypes.Contains(BulletType.Orbital);
            if (hadOrbital) DestroyOrbital();
            ApplyConfig();
            for (int i = 0; i < cooldowns.Length; i++)
                cooldowns[i] *= cooldownMult;
            if (hadOrbital) CreateOrbital();
        }

        public void AddRicochetBounce(int bonus)
        {
            ricochetBounceBonus += bonus;
        }

        public void AddOrbitalSpeed(float percentBonus)
        {
            orbitalSpeedBonus += orbitalSpeed * (percentBonus / 100f);
        }

        public void AddChainHops(int bonus)
        {
            chainHopsBonus += bonus;
        }

        public void AddChainRange(float percentBonus)
        {
            chainRangeBonus += chainRange * (percentBonus / 100f);
        }

        public void AddShotgunSpreadReduction(float percentReduction)
        {
            shotgunSpreadReduction += shotgunSpread * (percentReduction / 100f);
        }

        public float GetCooldownProgress()
        {
            float maxProgress = 0f;
            for (int i = 0; i < TypeCount; i++)
            {
                var type = (BulletType)i;
                if (type == BulletType.Orbital) continue;
                if (!activeTypes.Contains(type)) continue;
                float progress = Mathf.Clamp01((Time.time - lastFireTimes[i]) / cooldowns[i]);
                if (progress > maxProgress) maxProgress = progress;
            }
            // If nothing active (except maybe orbital), show full
            return activeTypes.Count == 0 || (activeTypes.Count == 1 && activeTypes.Contains(BulletType.Orbital))
                ? 1f : maxProgress;
        }

        private void FireType(BulletType type)
        {
            switch (type)
            {
                case BulletType.Straight: FireStraight(); break;
                case BulletType.Ricochet: FireRicochet(); break;
                case BulletType.Shotgun: FireShotgun(); break;
                case BulletType.Chain: FireChain(); break;
            }
        }

        private GameObject FindNearestEnemy()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length == 0) return null;
            GameObject nearest = null;
            float minDist = float.MaxValue;
            Vector3 origin = playerTransform != null ? playerTransform.position : Vector3.zero;
            foreach (var e in enemies)
            {
                float d = Vector3.Distance(origin, e.transform.position);
                if (d < minDist) { minDist = d; nearest = e; }
            }
            return nearest;
        }

        // --- Straight ---
        private void FireStraight()
        {
            var target = FindNearestEnemy();
            if (target == null) return;
            var cfg = GetConfig(BulletType.Straight);
            var go = MeshGenerator.CreateSwordBullet("StraightSword",
                cfg?.color ?? new Color(1f, 0.85f, 0.3f), 0.7f);
            go.transform.position = playerTransform.position;
            var sb = go.AddComponent<StraightBullet>();
            sb.damage = Mathf.RoundToInt((cfg?.damage ?? 10) * damageMult);
            sb.speed = (cfg?.speed ?? 10f) * speedMult;
            sb.impactColor = cfg?.color ?? Color.yellow;
            sb.Init((target.transform.position - playerTransform.position).normalized);
        }

        // --- Ricochet ---
        private void FireRicochet()
        {
            var target = FindNearestEnemy();
            Vector3 dir = target != null
                ? (target.transform.position - playerTransform.position).normalized
                : Random.insideUnitCircle.normalized;
            var cfg = GetConfig(BulletType.Ricochet);
            var go = MeshGenerator.CreateSwordBullet("RicochetSword",
                cfg?.color ?? new Color(0.3f, 1f, 0.4f), 0.6f);
            go.transform.position = playerTransform.position;
            var rb = go.AddComponent<RicochetBullet>();
            rb.damage = Mathf.RoundToInt((cfg?.damage ?? 8) * damageMult);
            rb.speed = (cfg?.speed ?? 8f) * speedMult;
            rb.impactColor = cfg?.color ?? Color.green;
            rb.Init(dir, ricochetMaxBounces + ricochetBounceBonus);
        }

        // --- Shotgun ---
        private void FireShotgun()
        {
            var target = FindNearestEnemy();
            Vector3 baseDir = target != null
                ? (target.transform.position - playerTransform.position).normalized
                : Vector3.up;
            var cfg = GetConfig(BulletType.Shotgun);
            int count = (cfg?.count ?? shotgunCount) + countBonus;
            float spread = Mathf.Max(10f, shotgunSpread - shotgunSpreadReduction);
            float startAngle = -spread / 2f;
            float step = spread / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                Vector3 dir = Quaternion.Euler(0, 0, angle) * baseDir;
                var go = MeshGenerator.CreateSwordBullet("ShotgunSword",
                    cfg?.color ?? new Color(1f, 0.3f, 0.9f), 0.5f);
                go.transform.position = playerTransform.position;
                var sb = go.AddComponent<StraightBullet>();
                sb.damage = Mathf.RoundToInt((cfg?.damage ?? 6) * damageMult);
                sb.speed = (cfg?.speed ?? 10f) * speedMult;
                sb.impactColor = cfg?.color ?? Color.magenta;
                sb.Init(dir);
            }
        }

        // --- Chain ---
        private void FireChain()
        {
            var target = FindNearestEnemy();
            if (target == null) return;
            var cfg = GetConfig(BulletType.Chain);
            var go = MeshGenerator.CreateSwordBullet("ChainSword",
                cfg?.color ?? new Color(0.3f, 0.7f, 1f), 0.65f);
            go.transform.position = playerTransform.position;
            var cb = go.AddComponent<ChainBullet>();
            cb.damage = Mathf.RoundToInt((cfg?.damage ?? 12) * damageMult);
            cb.impactColor = cfg?.color ?? Color.cyan;
            cb.Init(target.transform, chainMaxHops + chainHopsBonus, chainRange + chainRangeBonus);
        }

        // --- Orbital ---
        private void CreateOrbital()
        {
            var cfg = GetConfig(BulletType.Orbital);
            int count = (cfg?.count ?? orbitalCount) + countBonus;
            if (orbitalBullets.Length != count)
                orbitalBullets = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                var go = MeshGenerator.CreateSwordBullet("OrbitalSword",
                    cfg?.color ?? new Color(1f, 0.5f, 0f), 0.55f);
                go.transform.position = playerTransform.position;
                var ob = go.AddComponent<OrbitalBullet>();
                ob.damage = Mathf.RoundToInt((cfg?.damage ?? 10) * damageMult);
                ob.impactColor = cfg?.color ?? new Color(1f, 0.5f, 0f);
                ob.Init(i, count, orbitalRadius, orbitalSpeed);
                orbitalBullets[i] = go;
            }
            orbitalCount = count;
        }

        private void UpdateOrbital()
        {
            if (playerTransform == null) return;
            orbitalAngleOffset += (orbitalSpeed + orbitalSpeedBonus) * Time.deltaTime;
            for (int i = 0; i < orbitalBullets.Length; i++)
            {
                if (orbitalBullets[i] == null) continue;
                float angle = orbitalAngleOffset + (360f / orbitalBullets.Length) * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 pos = playerTransform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * orbitalRadius;
                orbitalBullets[i].transform.position = pos;
                // Face tangential to orbit
                Vector3 tangent = new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0);
                float swordAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f;
                orbitalBullets[i].transform.rotation = Quaternion.Euler(0, 0, swordAngle);
            }
        }

        private void DestroyOrbital()
        {
            for (int i = 0; i < orbitalBullets.Length; i++)
            {
                if (orbitalBullets[i] != null) Destroy(orbitalBullets[i]);
                orbitalBullets[i] = null;
            }
        }
    }
}

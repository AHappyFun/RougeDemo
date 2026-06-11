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
        public BulletConfig bulletConfig;

        public Transform playerTransform;
        public Transform bulletParent;

        private HashSet<BulletType> activeTypes = new HashSet<BulletType>();
        private float[] cooldowns;
        private float[] lastFireTimes = new float[TypeCount];
        private PlayerStats cachedStats;

        private GameObject[] orbitalBullets;
        private int orbitalCount;
        private float orbitalRadius;
        private float orbitalSpeed;
        private float orbitalDuration;
        private float orbitalAngleOffset;
        private float orbitalActiveEndTime;
        private float orbitalCooldownEndTime;
        private bool orbitalIsOnCooldown;

        private int ricochetMaxBounces;
        private int shotgunCount;
        private float shotgunSpread;
        private int chainMaxHops;
        private float chainRange;

        // Global upgrade modifiers (set by WaveManager)
        private float damageMult = 1f;
        private float cooldownMult = 1f;      // 攻速倍率（Attack 类型）
        private float skillCooldownMult = 1f; // 冷却缩减倍率（Skill 类型）
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
            cooldowns = new float[TypeCount];
        }

        /// <summary>由 GameBootstrap 调用，仅存引用不触发其他逻辑</summary>
        public void InitStats(PlayerStats stats) { cachedStats = stats; }

        private void Start()
        {
            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
            for (int i = 0; i < lastFireTimes.Length; i++)
                lastFireTimes[i] = -999f;

            // 如果 GameBootstrap 没设置 cachedStats，自己找
            if (cachedStats == null)
                cachedStats = FindObjectOfType<PlayerStats>();

            // 从 PlayerStats 全量同步（数据流：Config → PlayerStats → BulletManager）
            if (cachedStats != null) SyncFromPlayerStats(cachedStats);

            // All types active by default
            for (int i = 0; i < TypeCount; i++)
                activeTypes.Add((BulletType)i);
            CreateOrbital();
        }

        // =========================================================
        //  数据流核心：PlayerStats 是唯一数据源
        // =========================================================

        /// <summary>全量同步——从 PlayerStats 读取所有值，应用增益倍率后写入运行时字段</summary>
        public void SyncFromPlayerStats(PlayerStats stats)
        {
            cachedStats = stats;
            if (stats == null) return;

            // 先设置轨道参数（注意：此处不能调 SyncTogglesFromPlayerStats——原因见下文）
            orbitalCount = stats.orbitalCount + countBonus;
            orbitalRadius = stats.orbitalRadius;
            orbitalSpeed = stats.orbitalSpeed + orbitalSpeedBonus;
            orbitalDuration = stats.orbitalDuration;

            // 子弹专属参数
            ricochetMaxBounces = stats.ricochetBounces + ricochetBounceBonus;
            shotgunCount = stats.shotgunCount + countBonus;
            shotgunSpread = Mathf.Max(10f, stats.shotgunSpread - shotgunSpreadReduction);
            chainMaxHops = stats.chainHops + chainHopsBonus;
            chainRange = stats.chainRange + chainRangeBonus;

            // 注意：不能在 SyncFromPlayerStats 里调 SyncTogglesFromPlayerStats——
            // 因为 ToggleBulletType(Orbital) 会触发 CreateOrbital()，但此时
            // orbitalBullets 数组还未分配（在下一段分配），导致创建的子弹成为孤儿。
            // SyncTogglesFromPlayerStats 由 Update() 每帧调用，无需在此重复。

            // 冷却/攻速 = 基础值 × 累积倍率
            cooldowns[(int)BulletType.Straight]  = stats.straightCooldown * cooldownMult;
            cooldowns[(int)BulletType.Ricochet]  = stats.ricochetCooldown * skillCooldownMult;
            cooldowns[(int)BulletType.Shotgun]   = stats.shotgunCooldown * skillCooldownMult;
            cooldowns[(int)BulletType.Chain]     = stats.chainCooldown * skillCooldownMult;
            cooldowns[(int)BulletType.Orbital]   = stats.orbitalCooldown * skillCooldownMult;

            orbitalBullets = new GameObject[orbitalCount];
        }

        /// <summary>轻量同步——每帧仅同步武器开关，不覆盖冷却/升级值</summary>
        private void SyncTogglesFromPlayerStats(PlayerStats stats)
        {
            if (stats == null) return;
            SetActive(BulletType.Straight, stats.weaponStraight);
            SetActive(BulletType.Orbital,  stats.weaponOrbital);
            SetActive(BulletType.Ricochet, stats.weaponRicochet);
            SetActive(BulletType.Shotgun,  stats.weaponShotgun);
            SetActive(BulletType.Chain,    stats.weaponChain);
            cachedStats = stats;
        }

        private void SetActive(BulletType type, bool active)
        {
            if (active == activeTypes.Contains(type)) return;
            ToggleBulletType(type);
        }

        private BulletTypeConfig GetConfig(BulletType type) => bulletConfig != null ? type switch
        {
            BulletType.Straight => bulletConfig.straight, BulletType.Orbital => bulletConfig.orbital,
            BulletType.Ricochet => bulletConfig.ricochet, BulletType.Shotgun => bulletConfig.shotgun,
            BulletType.Chain => bulletConfig.chain, _ => bulletConfig.straight,
        } : null;

        public BulletCategory GetCategory(BulletType type)
        {
            if (cachedStats != null)
            {
                return type switch
                {
                    BulletType.Straight => cachedStats.straightCategory,
                    BulletType.Orbital => cachedStats.orbitalCategory,
                    BulletType.Ricochet => cachedStats.ricochetCategory,
                    BulletType.Shotgun => cachedStats.shotgunCategory,
                    BulletType.Chain => cachedStats.chainCategory,
                    _ => BulletCategory.Attack,
                };
            }
            var cfg = GetConfig(type);
            return cfg != null ? cfg.category : BulletCategory.Attack;
        }

        // =========================================================
        //  主循环
        // =========================================================

        private void Update()
        {
            if (playerTransform == null) return;
            if (GameManager.IsPaused) return;

            // 每帧仅同步武器开关（不覆盖冷却，保留升级效果）
            var ps = FindObjectOfType<PlayerStats>();
            if (ps != null) SyncTogglesFromPlayerStats(ps);

            // Orbital: 持续时长→冷却→自动激活 循环
            if (activeTypes.Contains(BulletType.Orbital))
            {
                if (!orbitalIsOnCooldown)
                {
                    UpdateOrbital();
                    if (orbitalDuration > 0 && Time.time >= orbitalActiveEndTime)
                    {
                        DestroyOrbital();
                        orbitalIsOnCooldown = true;
                        orbitalCooldownEndTime = Time.time + cooldowns[1];
                    }
                }
                else
                {
                    if (Time.time >= orbitalCooldownEndTime)
                        CreateOrbital();
                }
            }

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
                if (type == BulletType.Orbital)
                {
                    DestroyOrbital();
                    orbitalIsOnCooldown = false;
                }
            }
            else
            {
                activeTypes.Add(type);
                if (type == BulletType.Orbital) CreateOrbital();
            }
        }

        public bool IsBulletTypeActive(BulletType type) => activeTypes.Contains(type);
        public void SwitchBulletType(BulletType newType) => ToggleBulletType(newType);

        // =========================================================
        //  增益接口（由 WaveManager 调用）
        // =========================================================

        public void ApplyUpgrades(float dmgMult, float cdMult, int cntBonus, float spdMult, float skillCdMult = 1f)
        {
            damageMult = dmgMult;
            cooldownMult = cdMult;
            skillCooldownMult = skillCdMult;
            countBonus = cntBonus;
            speedMult = spdMult;

            bool hadOrbital = activeTypes.Contains(BulletType.Orbital);
            if (hadOrbital) DestroyOrbital();

            // 从 PlayerStats 重新同步（含新倍率）
            if (cachedStats != null)
            {
                SyncFromPlayerStats(cachedStats);
                SyncRuntimeToPlayerStats(); // 回写运行时值让 Inspector 实时显示
            }

            // Reset cooldown timers so shorter cooldowns don't trigger instant re-fire
            float now = Time.time;
            for (int i = 0; i < lastFireTimes.Length; i++)
                lastFireTimes[i] = now;
            if (hadOrbital) CreateOrbital();
        }

        /// <summary>将运行时值写入 PlayerStats 的 runtime 字段（不影响基础值，避免 buff 叠加）</summary>
        private void SyncRuntimeToPlayerStats()
        {
            if (cachedStats == null) return;
            cachedStats.runtimeStraightCD    = cooldowns[(int)BulletType.Straight];
            cachedStats.runtimeStraightDamage = (cachedStats.straightDamage * damageMult);
            cachedStats.runtimeRicochetCD    = cooldowns[(int)BulletType.Ricochet];
            cachedStats.runtimeRicochetDamage = (cachedStats.ricochetDamage * damageMult);
            cachedStats.runtimeShotgunCD     = cooldowns[(int)BulletType.Shotgun];
            cachedStats.runtimeShotgunDamage = (cachedStats.shotgunDamage * damageMult);
            cachedStats.runtimeChainCD       = cooldowns[(int)BulletType.Chain];
            cachedStats.runtimeChainDamage   = (cachedStats.chainDamage * damageMult);
            cachedStats.runtimeOrbitalCD     = cooldowns[(int)BulletType.Orbital];
            cachedStats.runtimeOrbitalDamage = (cachedStats.orbitalDamage * damageMult);
            cachedStats.runtimeOrbitalDuration = orbitalDuration;
            cachedStats.runtimeOrbitalCount  = orbitalCount;
            cachedStats.runtimeOrbitalSpeed  = orbitalSpeed;
            cachedStats.runtimeRicochetBounces = ricochetMaxBounces;
            cachedStats.runtimeShotgunCount  = shotgunCount;
            cachedStats.runtimeShotgunSpread = shotgunSpread;
            cachedStats.runtimeChainHops     = chainMaxHops;
            cachedStats.runtimeChainRange    = chainRange;
        }

        public void AddRicochetBounce(int bonus)
        {
            ricochetBounceBonus += bonus;
            if (cachedStats != null) { ricochetMaxBounces = cachedStats.ricochetBounces + ricochetBounceBonus; SyncRuntimeToPlayerStats(); }
        }

        public void AddOrbitalSpeed(float percentBonus)
        {
            orbitalSpeedBonus += (cachedStats != null ? cachedStats.orbitalSpeed : orbitalSpeed) * (percentBonus / 100f);
            if (cachedStats != null) { orbitalSpeed = cachedStats.orbitalSpeed + orbitalSpeedBonus; SyncRuntimeToPlayerStats(); }
        }

        public void AddChainHops(int bonus)
        {
            chainHopsBonus += bonus;
            if (cachedStats != null) { chainMaxHops = cachedStats.chainHops + chainHopsBonus; SyncRuntimeToPlayerStats(); }
        }

        public void AddChainRange(float percentBonus)
        {
            chainRangeBonus += (cachedStats != null ? cachedStats.chainRange : chainRange) * (percentBonus / 100f);
            if (cachedStats != null) { chainRange = cachedStats.chainRange + chainRangeBonus; SyncRuntimeToPlayerStats(); }
        }

        public void AddShotgunSpreadReduction(float percentReduction)
        {
            shotgunSpreadReduction += (cachedStats != null ? cachedStats.shotgunSpread : shotgunSpread) * (percentReduction / 100f);
            if (cachedStats != null) { shotgunSpread = Mathf.Max(10f, cachedStats.shotgunSpread - shotgunSpreadReduction); SyncRuntimeToPlayerStats(); }
        }

        // =========================================================
        //  UI 进度
        // =========================================================

        // =========================================================
        //  调试接口（供 SkillDebugPanel 使用）
        // =========================================================

        public float GetCooldownForType(BulletType type) => cooldowns[(int)type];
        public float GetDamageMultiplier() => damageMult;
        public float GetSpeedMultiplier() => speedMult;
        public int GetOrbitalCount() => orbitalCount;
        public float GetOrbitalDuration() => orbitalDuration;
        public int GetRicochetBounces() => ricochetMaxBounces;
        public int GetShotgunCount() => shotgunCount;
        public float GetShotgunSpread() => shotgunSpread;
        public int GetChainHops() => chainMaxHops;
        public float GetChainRange() => chainRange;

        public float GetCooldownProgress()
        {
            return Mathf.Max(GetAttackProgress(), GetSkillCooldownProgress());
        }

        public float GetAttackProgress()
        {
            float maxProgress = 0f;
            for (int i = 0; i < TypeCount; i++)
            {
                var type = (BulletType)i;
                if (type == BulletType.Orbital || !activeTypes.Contains(type)) continue;
                if (GetCategory(type) != BulletCategory.Attack) continue;
                float progress = Mathf.Clamp01((Time.time - lastFireTimes[i]) / cooldowns[i]);
                if (progress > maxProgress) maxProgress = progress;
            }
            return activeTypes.Count == 0 ? 1f : Mathf.Max(maxProgress, 0.01f);
        }

        public float GetSkillCooldownProgress()
        {
            float maxProgress = 0f;
            for (int i = 0; i < TypeCount; i++)
            {
                var type = (BulletType)i;
                if (!activeTypes.Contains(type)) continue;
                if (GetCategory(type) != BulletCategory.Skill) continue;

                float progress;
                if (type == BulletType.Orbital)
                {
                    if (!orbitalIsOnCooldown)
                        progress = 1f;
                    else
                        progress = 1f - Mathf.Clamp01((orbitalCooldownEndTime - Time.time) / cooldowns[1]);
                }
                else
                {
                    progress = Mathf.Clamp01((Time.time - lastFireTimes[i]) / cooldowns[i]);
                }
                if (progress > maxProgress) maxProgress = progress;
            }
            return activeTypes.Count == 0 ? 1f : Mathf.Max(maxProgress, 0.01f);
        }

        // =========================================================
        //  发射逻辑
        // =========================================================

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
            int dmg = cachedStats != null ? cachedStats.straightDamage : (cfg?.damage ?? 10);
            float spd = cachedStats != null ? cachedStats.straightSpeed : (cfg?.speed ?? 10f);
            var go = MeshGenerator.CreateSwordBullet("StraightSword",
                cachedStats?.straightColor ?? cfg?.color ?? new Color(1f, 0.85f, 0.3f), 0.7f);
            go.transform.position = playerTransform.position;
            if (bulletParent != null) go.transform.SetParent(bulletParent);
            var sb = go.AddComponent<StraightBullet>();
            sb.damage = Mathf.RoundToInt(dmg * damageMult);
            sb.speed = spd * speedMult;
            sb.impactColor = cachedStats?.straightColor ?? cfg?.color ?? Color.yellow;
            sb.Init((target.transform.position - playerTransform.position).normalized);
        }

        // --- Ricochet ---
        private void FireRicochet()
        {
            var target = FindNearestEnemy();
            Vector3 dir = target != null
                ? (target.transform.position - playerTransform.position).normalized
                : new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            var cfg = GetConfig(BulletType.Ricochet);
            int dmg = cachedStats != null ? cachedStats.ricochetDamage : (cfg?.damage ?? 8);
            float spd = cachedStats != null ? cachedStats.ricochetSpeed : (cfg?.speed ?? 8f);
            var go = MeshGenerator.CreateSwordBullet("RicochetSword",
                cachedStats?.ricochetColor ?? cfg?.color ?? new Color(0.3f, 1f, 0.4f), 1.0f);
            go.transform.position = playerTransform.position;
            if (bulletParent != null) go.transform.SetParent(bulletParent);
            var rb = go.AddComponent<RicochetBullet>();
            rb.damage = Mathf.RoundToInt(dmg * damageMult);
            rb.speed = spd * speedMult;
            rb.impactColor = cachedStats?.ricochetColor ?? cfg?.color ?? Color.green;
            rb.Init(dir, ricochetMaxBounces);
        }

        // --- Shotgun ---
        private void FireShotgun()
        {
            var target = FindNearestEnemy();
            Vector3 baseDir = target != null
                ? (target.transform.position - playerTransform.position).normalized
                : Vector3.forward;
            var cfg = GetConfig(BulletType.Shotgun);
            int count = Mathf.Max(1, shotgunCount);
            float startAngle = -shotgunSpread / 2f;
            float step = count > 1 ? shotgunSpread / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * baseDir;
                var go = MeshGenerator.CreateSwordBullet("ShotgunSword",
                    cachedStats?.shotgunColor ?? cfg?.color ?? new Color(1f, 0.3f, 0.9f), 0.5f);
                go.transform.position = playerTransform.position;
                if (bulletParent != null) go.transform.SetParent(bulletParent);
                var sb = go.AddComponent<StraightBullet>();
                int sd = cachedStats != null ? cachedStats.shotgunDamage : (cfg?.damage ?? 6);
                float ss = cachedStats != null ? cachedStats.shotgunSpeed : (cfg?.speed ?? 10f);
                sb.damage = Mathf.RoundToInt(sd * damageMult);
                sb.speed = ss * speedMult;
                sb.impactColor = cachedStats?.shotgunColor ?? cfg?.color ?? Color.magenta;
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
                cachedStats?.chainColor ?? cfg?.color ?? new Color(0.3f, 0.7f, 1f), 0.65f);
            go.transform.position = playerTransform.position;
            if (bulletParent != null) go.transform.SetParent(bulletParent);
            var cb = go.AddComponent<ChainBullet>();
            int cd = cachedStats != null ? cachedStats.chainDamage : (cfg?.damage ?? 12);
            cb.damage = Mathf.RoundToInt(cd * damageMult);
            cb.impactColor = cachedStats?.chainColor ?? cfg?.color ?? Color.cyan;
            cb.Init(target.transform, chainMaxHops, chainRange);
        }

        // --- Orbital ---
        private void CreateOrbital()
        {
            var cfg = GetConfig(BulletType.Orbital);
            int count = orbitalCount;
            if (orbitalBullets == null || orbitalBullets.Length != count)
                orbitalBullets = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                var go = MeshGenerator.CreateSwordBullet("OrbitalSword",
                    cachedStats?.orbitalColor ?? cfg?.color ?? new Color(1f, 0.5f, 0f), 1.2f);
                go.transform.position = playerTransform.position;
                if (bulletParent != null) go.transform.SetParent(bulletParent);
                var ob = go.AddComponent<OrbitalBullet>();
                int od = cachedStats != null ? cachedStats.orbitalDamage : (cfg?.damage ?? 10);
                ob.damage = Mathf.RoundToInt(od * damageMult);
                ob.impactColor = cachedStats?.orbitalColor ?? cfg?.color ?? new Color(1f, 0.5f, 0f);
                ob.Init(i, count, orbitalRadius, orbitalSpeed);
                orbitalBullets[i] = go;
            }
            orbitalCount = count;
            orbitalIsOnCooldown = false;
            orbitalActiveEndTime = Time.time + orbitalDuration;
        }

        private void UpdateOrbital()
        {
            if (playerTransform == null) return;
            orbitalAngleOffset += orbitalSpeed * Time.deltaTime;
            for (int i = 0; i < orbitalBullets.Length; i++)
            {
                if (orbitalBullets[i] == null) continue;
                float angle = orbitalAngleOffset + (360f / orbitalBullets.Length) * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 pos = playerTransform.position + new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitalRadius;
                orbitalBullets[i].transform.position = pos;
                // 大剑侧面砍——剑身指向径向（朝外），剑刃划过敌人
                Vector3 radial = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));
                float swordAngle = Mathf.Atan2(radial.x, radial.z) * Mathf.Rad2Deg;
                orbitalBullets[i].transform.rotation = Quaternion.Euler(0, swordAngle, 0);
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

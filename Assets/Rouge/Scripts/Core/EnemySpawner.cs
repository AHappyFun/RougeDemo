using UnityEngine;

namespace Rouge
{
    [System.Serializable]
    public class EnemyTypeDef
    {
        public string typeName;
        public int hp;
        public float speed;
        public int damage;
        public float scale;
        public Color color;
        public int xpReward;
        /// <summary>权重：值越大越容易出现</summary>
        public float weight;
    }

    public class EnemySpawner : MonoBehaviour
    {
        [Header("Config")]
        public GameConfig gameConfig;

        public int maxEnemies = 40;
        public float spawnInterval = 0.35f;

        private int baseMaxEnemies;
        private float baseSpawnInterval;

        private float nextSpawnTime;
        private Camera cam;
        private int currentWave = 1;

        // 全部7种敌人 —— 权重随波次动态调整
        private static readonly EnemyTypeDef[] EnemyTypes = new EnemyTypeDef[]
        {
            //        typeName      HP  spd  dmg scale  color               xp   weight
            new EnemyTypeDef { typeName = "Scout",   hp = 8,   speed = 3.5f, damage = 5,  scale = 0.7f, color = new Color(0.2f, 0.7f, 0.5f), xpReward = 10,  weight = 1f },
            new EnemyTypeDef { typeName = "Bat",     hp = 6,   speed = 4.0f, damage = 3,  scale = 0.6f, color = new Color(0.6f, 0.3f, 0.8f), xpReward = 8,   weight = 0.8f },
            new EnemyTypeDef { typeName = "MonkeyE", hp = 12,  speed = 3.0f, damage = 5,  scale = 0.7f, color = new Color(0.6f, 0.4f, 0.2f), xpReward = 12,  weight = 0.7f },
            new EnemyTypeDef { typeName = "Tank",    hp = 40,  speed = 1.2f, damage = 4,  scale = 1.0f, color = new Color(0.5f, 0.15f, 0.15f), xpReward = 25, weight = 0.5f },
            new EnemyTypeDef { typeName = "MonkeyW", hp = 30,  speed = 1.8f, damage = 8,  scale = 0.9f, color = new Color(0.7f, 0.25f, 0.1f), xpReward = 20,  weight = 0.4f },
            new EnemyTypeDef { typeName = "MonkeyG", hp = 80,  speed = 1.2f, damage = 10, scale = 1.2f, color = new Color(0.3f, 0.2f, 0.1f), xpReward = 35,  weight = 0.3f },
            new EnemyTypeDef { typeName = "Elite",   hp = 120, speed = 1.0f, damage = 12, scale = 0.3f, color = new Color(0.9f, 0.7f, 0.1f), xpReward = 50,  weight = 0.2f },
        };

        private void Start()
        {
            cam = Camera.main;
            ApplyConfig();
            baseMaxEnemies = maxEnemies;
            baseSpawnInterval = spawnInterval;
        }

        private void ApplyConfig()
        {
            if (gameConfig != null)
            {
                maxEnemies = gameConfig.maxEnemies;
                spawnInterval = gameConfig.spawnInterval;
            }
        }

        private float _hpMult = 1f;
        private float _dmgBonus;

        public void ApplyWaveScaling(float hpMult, float speedMult, float spawnRateMult, int maxBonus, int damageBonus)
        {
            _hpMult = hpMult;
            _dmgBonus = damageBonus;
            spawnInterval = baseSpawnInterval * spawnRateMult;
            maxEnemies = baseMaxEnemies + maxBonus;
            currentWave++;
        }

        public void AddKill() { }

        private void Update()
        {
            if (GameManager.IsPaused) return;
            if (Time.time >= nextSpawnTime)
            {
                TrySpawn();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void TrySpawn()
        {
            if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies) return;
            var type = PickType();
            BuildEnemy(cam.RandomPointOnScreenEdge(0.5f), type);
        }

        private EnemyTypeDef PickType()
        {
            // 根据波次调整权重：越往后，强敌出现概率越高
            float waveBonus = currentWave * 0.15f;
            float totalWeight = 0f;
            foreach (var t in EnemyTypes)
            {
                float w = t.weight * (1f + waveBonus);
                if (t == EnemyTypes[0] || t == EnemyTypes[1] || t == EnemyTypes[2])
                    w *= Mathf.Max(0.3f, 1f - currentWave * 0.05f); // 低级怪随波次递减
                totalWeight += w;
            }

            float roll = Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var t in EnemyTypes)
            {
                float w = t.weight * (1f + waveBonus);
                if (t == EnemyTypes[0] || t == EnemyTypes[1] || t == EnemyTypes[2])
                    w *= Mathf.Max(0.3f, 1f - currentWave * 0.05f);
                cumulative += w;
                if (roll < cumulative) return t;
            }
            return EnemyTypes[0];
        }

        private void BuildEnemy(Vector3 position, EnemyTypeDef type)
        {
            position.y = 0.5f;

            var go = ObjectPool.Get(type.typeName, GetEnemyPrefab(type.typeName));
            go.transform.position = position;
            go.transform.localScale = Vector3.one * type.scale;
            go.name = type.typeName;

            var enemiesRoot = GameObject.Find("Enemies");
            if (enemiesRoot != null) go.transform.SetParent(enemiesRoot.transform);

            var be = go.GetComponent<BaseEnemy>();
            if (be != null) { be.moveSpeed = type.speed; be.contactDamage = type.damage + Mathf.RoundToInt(_dmgBonus); be.xpReward = type.xpReward; }

            var health = go.GetComponent<EnemyHealth>();
            if (health != null) { health.maxHealth = Mathf.RoundToInt(type.hp * _hpMult); health.ResetHealth(); }

            if (go.GetComponent<HealthBar3D>() == null)
                go.AddComponent<HealthBar3D>();

            go.SetActive(true);
        }

        private GameObject GetEnemyPrefab(string typeName)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Enemies/" + typeName);
            if (prefab == null) Debug.LogError("[EnemySpawner] Missing prefab: Prefabs/Enemies/" + typeName);
            return prefab;
        }
    }
}

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
    }

    public class EnemySpawner : MonoBehaviour
    {
        [Header("Config")]
        public GameConfig gameConfig;

        public int maxEnemies = 40;
        public float spawnInterval = 0.35f;

        // Base config values (before wave scaling)
        private int baseMaxEnemies;
        private float baseSpawnInterval;

        private float nextSpawnTime;
        private Camera cam;
        private int currentWave = 1;

        private static readonly EnemyTypeDef[] EnemyTypes = new EnemyTypeDef[]
        {
            new EnemyTypeDef { typeName = "Scout", hp = 15,  speed = 4.5f, damage = 7,  scale = 0.6f, color = new Color(0.2f, 0.7f, 0.5f), xpReward = 10 },
            new EnemyTypeDef { typeName = "Tank",  hp = 100, speed = 1.2f, damage = 5,  scale = 1.2f, color = new Color(0.5f, 0.15f, 0.15f), xpReward = 25 },
            new EnemyTypeDef { typeName = "Elite", hp = 300, speed = 1.0f, damage = 20, scale = 1.8f, color = new Color(0.9f, 0.7f, 0.1f), xpReward = 50 },
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
            // Weighted random: early waves favour Scouts, later waves mix in more Tanks/Elites
            float scoutW = 0.6f;
            float tankW = 0.3f + currentWave * 0.05f;
            float eliteW = 0.1f + currentWave * 0.03f;
            float total = scoutW + tankW + eliteW;
            float roll = Random.value * total;
            if (roll < scoutW) return EnemyTypes[0];
            if (roll < scoutW + tankW) return EnemyTypes[1];
            return EnemyTypes[2];
        }

        private void BuildEnemy(Vector3 position, EnemyTypeDef type)
        {
            position.y = 0.5f;
            var go = MeshGenerator.CreateEnemy(type.typeName, type.color);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * type.scale;
            go.transform.SetParent(GameObject.Find("Enemies")?.transform);

            // Override default component values with type stats
            var be = go.GetComponent<BaseEnemy>();
            if (be != null) { be.moveSpeed = type.speed; be.contactDamage = type.damage + Mathf.RoundToInt(_dmgBonus); be.xpReward = type.xpReward; }

            var health = go.GetComponent<EnemyHealth>();
            if (health != null) { health.maxHealth = Mathf.RoundToInt(type.hp * _hpMult); }

            go.AddComponent<HealthBar3D>();
            go.AddComponent<Billboard>();
        }
    }
}

using UnityEngine;

namespace Rouge
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Config")]
        public GameConfig config;

        public int maxEnemies = 40;
        public float spawnInterval = 0.35f;
        public int enemyHP = 30;
        public float enemySpeed = 2.5f;
        public Color enemyColor = Color.red;
        public int contactDamage = 10;

        // Base config values (before wave scaling)
        private int baseMaxEnemies;
        private float baseSpawnInterval;
        private int baseEnemyHP;
        private float baseEnemySpeed;
        private int baseContactDamage;

        private float nextSpawnTime;
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
            ApplyConfig();
            // Save base values
            baseMaxEnemies = maxEnemies;
            baseSpawnInterval = spawnInterval;
            baseEnemyHP = enemyHP;
            baseEnemySpeed = enemySpeed;
            baseContactDamage = contactDamage;
        }

        private void ApplyConfig()
        {
            if (config != null)
            {
                maxEnemies = config.maxEnemies;
                spawnInterval = config.spawnInterval;
                enemyHP = config.enemyHP;
                enemySpeed = config.enemySpeed;
                enemyColor = config.enemyColor;
                contactDamage = config.enemyContactDamage;
            }
        }

        public void ApplyWaveScaling(float hpMult, float speedMult, float spawnRateMult, int maxBonus, int damageBonus)
        {
            enemyHP = Mathf.RoundToInt(baseEnemyHP * hpMult);
            enemySpeed = baseEnemySpeed * speedMult;
            spawnInterval = baseSpawnInterval * spawnRateMult;
            maxEnemies = baseMaxEnemies + maxBonus;
            contactDamage = baseContactDamage + damageBonus;
        }

        private void Update()
        {
            if (Time.time >= nextSpawnTime)
            {
                TrySpawn();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void TrySpawn()
        {
            if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies) return;
            BuildEnemy(cam.RandomPointOnScreenEdge(0.5f));
        }

        private void BuildEnemy(Vector3 position)
        {
            var go = MeshGenerator.CreateEnemy(enemyColor);
            go.transform.position = position;

            var be = go.AddComponent<BaseEnemy>();
            be.moveSpeed = enemySpeed;
            be.contactDamage = contactDamage;

            var health = go.AddComponent<EnemyHealth>();
            health.maxHealth = enemyHP;
            health.deathColor = enemyColor;

            var (bg, fill) = MeshGenerator.CreateHealthBar(go.transform, 0.6f, 0.06f);
            health.SetHealthBar(fill.GetComponent<MeshRenderer>(), fill);
        }
    }
}

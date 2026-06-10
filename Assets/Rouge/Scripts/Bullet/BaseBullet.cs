using UnityEngine;

namespace Rouge
{
    public class BaseBullet : MonoBehaviour
    {
        public int damage = 10;
        public float speed = 10f;
        public float lifetime = 5f;
        public Color impactColor = Color.white;

        protected float spawnTime;

        protected virtual void OnEnable()
        {
            spawnTime = Time.time;
        }

        protected virtual void Update()
        {
            if (GameManager.IsPaused) return;
            if (Time.time - spawnTime > lifetime) { Destroy(gameObject); return; }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                var health = other.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    MeshGenerator.SpawnHitParticles(transform.position, impactColor);
                }
                OnHitEnemy(other);
            }
        }

        protected virtual void OnHitEnemy(Collider enemy) { Destroy(gameObject); }
    }
}

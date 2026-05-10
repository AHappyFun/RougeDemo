using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    public class OrbitalBullet : BaseBullet
    {
        private Dictionary<EnemyHealth, float> lastDamageTimes = new Dictionary<EnemyHealth, float>();
        private const float DamageCD = 1f;

        public void Init(int index, int total, float radius, float rotateSpeed)
        {
            damage = 10;
            lifetime = float.MaxValue;
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy")) return;
            var health = other.GetComponent<EnemyHealth>();
            if (health == null) return;

            if (lastDamageTimes.TryGetValue(health, out float lastTime))
            {
                if (Time.time - lastTime < DamageCD) return;
            }
            lastDamageTimes[health] = Time.time;

            health.TakeDamage(damage);
            OnHitEnemy(other);
        }
    }
}

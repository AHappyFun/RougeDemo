using UnityEngine;

namespace Rouge
{
    public class PlayerHealth : MonoBehaviour
    {
        public int maxHP = 100;
        private int currentHP;

        public float HPPercent => (float)currentHP / maxHP;
        public bool IsDead => currentHP <= 0;

        private float lastDamageTime;
        private const float DamageCooldown = 0.5f;

        private void Start()
        {
            currentHP = maxHP;
        }

        public void Heal(float percent)
        {
            currentHP = Mathf.Min(maxHP, currentHP + Mathf.RoundToInt(maxHP * percent / 100f));
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;
            if (Time.time - lastDamageTime < DamageCooldown) return;

            lastDamageTime = Time.time;
            currentHP -= damage;
            if (currentHP <= 0)
            {
                currentHP = 0;
                GameManager.Instance?.TriggerGameOver();
            }
        }
    }
}

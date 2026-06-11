using UnityEngine;

namespace Rouge
{
    /// <summary>Controls HP logic — writes to PlayerStats model.</summary>
    public class PlayerHealth : MonoBehaviour
    {
        public PlayerStats stats;

        private float lastDamageTime;
        private const float DamageCooldown = 0.5f;
        private PlayerAnimController animController;

        private void Start()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (stats != null) stats.currentHP = stats.maxHP;
            animController = GetComponent<PlayerAnimController>();
        }

        public bool IsDead => stats != null && stats.currentHP <= 0;

        public void Heal(float percent)
        {
            if (stats == null) return;
            stats.currentHP = Mathf.Min(stats.maxHP, stats.currentHP + Mathf.RoundToInt(stats.maxHP * percent));
        }

        public void TakeDamage(int damage)
        {
            if (stats == null || IsDead) return;
            if (Time.time - lastDamageTime < DamageCooldown) return;

            lastDamageTime = Time.time;
            stats.currentHP -= damage;
            if (stats.currentHP <= 0)
            {
                stats.currentHP = 0;
                if (animController != null) animController.PlayDead();
                GameManager.Instance?.TriggerGameOver();
            }
            else
            {
                if (animController != null) animController.PlayHurt();
            }
        }
    }
}

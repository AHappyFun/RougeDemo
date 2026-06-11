using System.Collections;
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
        private Renderer rend;
        private MaterialPropertyBlock mpb;

        private void Start()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (stats != null) stats.currentHP = stats.maxHP;
            animController = GetComponent<PlayerAnimController>();
            rend = GetComponentInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
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

            // 材质闪红（利用 shader 的 _HitColor + _HitAmount）
            if (rend != null)
            {
                rend.GetPropertyBlock(mpb);
                mpb.SetColor("_HitColor", Color.red);
                mpb.SetFloat("_HitAmount", 1f);
                rend.SetPropertyBlock(mpb);
                StartCoroutine(FadeHit());
            }

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

        private IEnumerator FadeHit()
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                if (rend != null)
                {
                    rend.GetPropertyBlock(mpb);
                    mpb.SetFloat("_HitAmount", 1f - t / 0.3f);
                    rend.SetPropertyBlock(mpb);
                }
                yield return null;
            }
            if (rend != null)
            {
                rend.GetPropertyBlock(mpb);
                mpb.SetFloat("_HitAmount", 0f);
                rend.SetPropertyBlock(mpb);
            }
        }
    }
}


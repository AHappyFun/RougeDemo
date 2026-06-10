using System.Collections;
using UnityEngine;

namespace Rouge
{
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Stats")]
        public int maxHealth = 30;
        public int currentHealth;
        public Color deathColor = Color.red;

        [Header("Hit Flash")]
        [Tooltip("受击闪白持续时间（秒）")]
        public float flashDuration = 0.12f;
        [Tooltip("受击闪白颜色")]
        public Color flashColor = Color.white;

        private Renderer rend;
        private MaterialPropertyBlock mpb;
        private Coroutine flashRoutine;

        private void Start()
        {
            currentHealth = maxHealth;
            rend = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            Flash();
            if (currentHealth <= 0)
            {
                int xp = GetComponent<BaseEnemy>()?.xpReward ?? 10;
                MeshGenerator.SpawnDeathParticles(transform.position, deathColor);
                GameManager.Instance?.AddKill(xp);
                Destroy(gameObject);
            }
        }

        private void Flash()
        {
            if (rend == null) return;

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            // 闪白
            rend.GetPropertyBlock(mpb);
            mpb.SetFloat("_HitAmount", 1f);
            rend.SetPropertyBlock(mpb);

            yield return new WaitForSeconds(flashDuration);

            // 恢复
            if (rend != null)
            {
                rend.GetPropertyBlock(mpb);
                mpb.SetFloat("_HitAmount", 0f);
                rend.SetPropertyBlock(mpb);
            }

            flashRoutine = null;
        }
    }
}

using UnityEngine;

namespace Rouge
{
    public class EnemyHealth : MonoBehaviour
    {
        public int maxHealth = 30;
        public Color deathColor = Color.red;
        private int currentHealth;

        private MeshRenderer fillRenderer;
        private GameObject fillObject;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void SetHealthBar(MeshRenderer fillMr, GameObject fillObj)
        {
            fillRenderer = fillMr;
            fillObject = fillObj;
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            UpdateHealthBar();
            if (currentHealth <= 0)
            {
                MeshGenerator.SpawnDeathParticles(transform.position, deathColor);
                GameManager.Instance?.AddKill();
                Destroy(gameObject);
            }
        }

        private void UpdateHealthBar()
        {
            if (fillRenderer != null && fillObject != null)
            {
                float ratio = (float)currentHealth / maxHealth;
                fillObject.transform.localScale = new Vector3(ratio, 1f, 1f);
                fillObject.transform.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
                fillRenderer.material.color = ratio > 0.5f ? Color.green
                    : (ratio > 0.25f ? Color.yellow : Color.red);
            }
        }
    }
}

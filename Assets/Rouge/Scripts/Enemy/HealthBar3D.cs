using UnityEngine;
using UnityEngine.UI;

namespace Rouge
{
    public class HealthBar3D : MonoBehaviour
    {
        public float heightOffset = 1.2f;
        public float barWidth = 1.2f;
        public float barHeight = 0.12f;
        public Color highHealthColor = Color.green;
        public Color midHealthColor = Color.yellow;
        public Color lowHealthColor = Color.red;

        private EnemyHealth health;
        private Image fillImage;
        private Transform barRoot;

        private void Start()
        {
            health = GetComponent<EnemyHealth>();
            if (health == null) { enabled = false; return; }

            CreateCanvas();
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("HealthBarCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = new Vector3(0, heightOffset, 0);
            canvasGO.transform.localRotation = Quaternion.identity;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            var bgGO = new GameObject("BG");
            bgGO.transform.SetParent(canvasGO.transform);
            bgGO.transform.localPosition = Vector3.zero;
            bgGO.transform.localRotation = Quaternion.identity;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            var bgRt = bgImg.rectTransform;
            bgRt.sizeDelta = new Vector2(barWidth, barHeight);
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(canvasGO.transform);
            fillGO.transform.localPosition = Vector3.zero;
            fillGO.transform.localRotation = Quaternion.identity;
            fillImage = fillGO.AddComponent<Image>();
            fillImage.color = highHealthColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            var fillRt = fillImage.rectTransform;
            fillRt.sizeDelta = new Vector2(barWidth, barHeight);
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0.5f, 0.5f);

            barRoot = canvasGO.transform;
        }

        private void LateUpdate()
        {
            if (health == null || fillImage == null) return;

            float ratio = (float)health.currentHealth / Mathf.Max(1, health.maxHealth);
            ratio = Mathf.Clamp01(ratio);
            fillImage.fillAmount = ratio;

            fillImage.color = ratio > 0.5f ? highHealthColor
                : (ratio > 0.25f ? midHealthColor : lowHealthColor);
        }
    }
}

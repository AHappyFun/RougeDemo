using UnityEngine;

namespace Rouge
{
    public class RicochetBullet : BaseBullet
    {
        private Vector3 direction;
        private int maxBounces = 5;
        private int bounceCount;
        private Camera cam;

        public void Init(Vector3 dir, int maxBouncesOverride = -1)
        {
            direction = dir.normalized;
            direction.y = 0f;
            bounceCount = 0;
            if (maxBouncesOverride > 0) maxBounces = maxBouncesOverride;
            cam = Camera.main;
            UpdateFacing();
        }

        private void UpdateFacing()
        {
            direction.y = 0f;
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        protected override void Update()
        {
            if (GameManager.IsPaused) return;
            base.Update();
            transform.position += direction * speed * Time.deltaTime;

            Vector3 viewport = cam.WorldToViewportPoint(transform.position);
            bool bounced = false;

            // 只在窗口边界反弹，穿过敌人不转向
            if (viewport.x < 0f) { direction.x = Mathf.Abs(direction.x); bounced = true; }
            else if (viewport.x > 1f) { direction.x = -Mathf.Abs(direction.x); bounced = true; }
            if (viewport.y < 0f) { direction.z = Mathf.Abs(direction.z); bounced = true; }
            else if (viewport.y > 1f) { direction.z = -Mathf.Abs(direction.z); bounced = true; }

            if (bounced)
            {
                UpdateFacing();
                bounceCount++;
                if (bounceCount >= maxBounces) Destroy(gameObject);
            }
        }

        // 穿透敌人，不反弹
        protected override void OnHitEnemy(Collider enemy) { }
    }
}

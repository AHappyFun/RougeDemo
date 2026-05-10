using UnityEngine;

namespace Rouge
{
    public class RicochetBullet : BaseBullet
    {
        private Vector3 direction;
        private int bounceCount;
        private int maxBounces = 5;
        private Camera cam;

        public void Init(Vector3 dir, int maxBouncesOverride = -1)
        {
            direction = dir.normalized;
            bounceCount = 0;
            if (maxBouncesOverride > 0) maxBounces = maxBouncesOverride;
            cam = Camera.main;
            UpdateFacing();
        }

        private void UpdateFacing()
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        protected override void Update()
        {
            base.Update();
            transform.position += direction * speed * Time.deltaTime;

            Vector3 viewport = cam.WorldToViewportPoint(transform.position);
            bool bounced = false;

            if (viewport.x < 0f) { direction.x = Mathf.Abs(direction.x); bounced = true; }
            else if (viewport.x > 1f) { direction.x = -Mathf.Abs(direction.x); bounced = true; }
            if (viewport.y < 0f) { direction.y = Mathf.Abs(direction.y); bounced = true; }
            else if (viewport.y > 1f) { direction.y = -Mathf.Abs(direction.y); bounced = true; }

            if (bounced)
            {
                UpdateFacing();
                bounceCount++;
                if (bounceCount >= maxBounces) Destroy(gameObject);
            }
        }

        protected override void OnHitEnemy(Collider enemy)
        {
            direction = Random.insideUnitCircle.normalized;
            UpdateFacing();
            bounceCount++;
            if (bounceCount >= maxBounces) Destroy(gameObject);
        }
    }
}

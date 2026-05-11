using UnityEngine;

namespace Rouge
{
    public class StraightBullet : BaseBullet
    {
        private Vector3 direction;

        public void Init(Vector3 dir)
        {
            direction = dir.normalized;
            direction.y = 0f;
            // Face the blade toward movement direction (XZ plane, Y-up)
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        protected override void Update()
        {
            if (GameManager.IsPaused) return;
            base.Update();
            transform.position += direction * speed * Time.deltaTime;
        }
    }
}

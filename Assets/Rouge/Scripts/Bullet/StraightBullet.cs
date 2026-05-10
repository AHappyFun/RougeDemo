using UnityEngine;

namespace Rouge
{
    public class StraightBullet : BaseBullet
    {
        private Vector3 direction;

        public void Init(Vector3 dir)
        {
            direction = dir.normalized;
            // Face the blade toward movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        protected override void Update()
        {
            base.Update();
            transform.position += direction * speed * Time.deltaTime;
        }
    }
}

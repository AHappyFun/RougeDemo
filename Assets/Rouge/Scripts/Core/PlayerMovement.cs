using UnityEngine;

namespace Rouge
{
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 6f;
        public float bounds = 23f;

        private void Update()
        {
            if (GameManager.IsPaused) return;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 move = new Vector3(h, 0, v).normalized * speed * Time.deltaTime;
            transform.position += move;

            // Clamp within floor bounds
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -bounds, bounds);
            pos.z = Mathf.Clamp(pos.z, -bounds, bounds);
            transform.position = pos;
        }
    }
}

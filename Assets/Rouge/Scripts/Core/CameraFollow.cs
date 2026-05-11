using UnityEngine;

namespace Rouge
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float height = 18f;
        public float pitchAngle = 70f;
        public float smoothSpeed = 4f;

        private void LateUpdate()
        {
            if (target == null) return;

            // Compensate for forward offset caused by the downward pitch
            // so the player stays at screen center
            float zOffset = height * Mathf.Tan((90f - pitchAngle) * Mathf.Deg2Rad);

            Vector3 targetPos = new Vector3(
                target.position.x,
                height,
                target.position.z - zOffset
            );
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        }
    }
}

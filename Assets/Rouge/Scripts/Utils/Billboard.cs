using UnityEngine;

namespace Rouge
{
    public class Billboard : MonoBehaviour
    {
        private Transform cam;

        private void Start()
        {
            var c = Camera.main;
            if (c != null) cam = c.transform;
        }

        private void LateUpdate()
        {
            if (cam != null)
                transform.forward = cam.forward;
        }
    }
}

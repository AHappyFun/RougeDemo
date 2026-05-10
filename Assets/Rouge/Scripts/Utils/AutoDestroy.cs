using UnityEngine;

namespace Rouge
{
    public class AutoDestroy : MonoBehaviour
    {
        public float lifetime = 1f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}

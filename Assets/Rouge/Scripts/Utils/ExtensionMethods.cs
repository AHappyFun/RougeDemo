using UnityEngine;

namespace Rouge
{
    public static class ExtensionMethods
    {
        public static Vector3 RandomPointOnScreenEdge(this Camera cam, float margin = 0.5f)
        {
            int side = Random.Range(0, 4);
            Vector3 viewport = side switch
            {
                0 => new Vector3(Random.value, 0f, 0.5f),
                1 => new Vector3(Random.value, 1f, 0.5f),
                2 => new Vector3(0f, Random.value, 0.5f),
                _ => new Vector3(1f, Random.value, 0.5f),
            };
            Vector3 world = cam.ViewportToWorldPoint(viewport);
            world.z = 0f;
            Vector3 camPos = cam.transform.position;
            camPos.z = 0f;
            Vector3 dir = (world - camPos).normalized;
            return world + dir * margin;
        }
    }
}

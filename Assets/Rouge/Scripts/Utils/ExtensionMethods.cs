using UnityEngine;

namespace Rouge
{
    public static class ExtensionMethods
    {
        /// <summary>Random point on the visible ground plane at one of four screen edges.</summary>
        public static Vector3 RandomPointOnScreenEdge(this Camera cam, float margin = 0.5f)
        {
            int side = Random.Range(0, 4);
            Vector3 viewport = side switch
            {
                0 => new Vector3(Random.value, 0f, 0f),
                1 => new Vector3(Random.value, 1f, 0f),
                2 => new Vector3(0f, Random.value, 0f),
                _ => new Vector3(1f, Random.value, 0f),
            };

            Ray ray = cam.ViewportPointToRay(viewport);
            Plane ground = new Plane(Vector3.up, 0f);
            if (ground.Raycast(ray, out float enter))
                return ray.GetPoint(enter) + ray.direction.normalized * margin;

            // Fallback: camera forward at distance
            return cam.transform.position + cam.transform.forward * 20f;
        }
    }
}

using UnityEngine;
using Unity.AI.Navigation;

namespace Rouge
{
    public static class SceneBuilder
    {
        private static readonly Color FloorColor = new Color(0.25f, 0.25f, 0.3f);
        private static readonly Color WallColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color ObstacleColor = new Color(0.4f, 0.4f, 0.45f);

        public static void Build()
        {
            // ── Floor ──
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(50, 1, 50);
            SetColor(floor, FloorColor);
            Object.Destroy(floor.GetComponent<BoxCollider>());

            // ── Boundary walls ──
            BuildWall("Wall_N", new Vector3(0, 2.5f, -25), new Vector3(50, 5, 1));
            BuildWall("Wall_S", new Vector3(0, 2.5f, 25), new Vector3(50, 5, 1));
            BuildWall("Wall_W", new Vector3(-25, 2.5f, 0), new Vector3(1, 5, 50));
            BuildWall("Wall_E", new Vector3(25, 2.5f, 0), new Vector3(1, 5, 50));

            // ── Obstacles ──
            var obstaclePositions = new Vector3[]
            {
                new Vector3(-8, 0, -6),   new Vector3(10, 0, -8),
                new Vector3(-12, 0, 7),   new Vector3(6, 0, 10),
                new Vector3(-5, 0, -12),  new Vector3(14, 0, 4),
                new Vector3(-14, 0, -3),  new Vector3(0, 0, -15),
                new Vector3(8, 0, -14),   new Vector3(-9, 0, 13),
                new Vector3(13, 0, -11),  new Vector3(-6, 0, -9),
            };
            foreach (var pos in obstaclePositions)
            {
                float w = Random.Range(0.8f, 2.5f);
                float h = Random.Range(0.5f, 3f);
                float d = Random.Range(0.8f, 2.5f);
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Obstacle";
                box.transform.position = pos + new Vector3(0, h * 0.5f, 0);
                box.transform.localScale = new Vector3(w, h, d);
                box.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 90f), 0);
                SetColor(box, ObstacleColor);
                // Mark as NavMesh obstacle (area 1 = Not Walkable)
                var mod = box.AddComponent<NavMeshModifier>();
                mod.overrideArea = true;
                mod.area = 1;
            }

            // ── Bake NavMesh ──
            BakeNavMesh();
        }

        private static void BuildWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.tag = "Wall";
            wall.transform.position = position;
            wall.transform.localScale = scale;
            SetColor(wall, WallColor);
            // Mark as NavMesh obstacle (area 1 = Not Walkable)
            var mod = wall.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1;
        }

        private static void BakeNavMesh()
        {
            var navGO = new GameObject("NavMeshSurface");
            var surface = navGO.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
        }

        private static void SetColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = color;
        }
    }
}

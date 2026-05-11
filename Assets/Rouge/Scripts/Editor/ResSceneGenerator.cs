using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Rouge.EditorTools
{
    /// <summary>
    /// Generates the main game scene with environment + GameBootstrap.
    /// Run once via Rouge -> Generate Scene.
    /// </summary>
    public static class ResSceneGenerator
    {
        private const string ScenePath = "Assets/Rouge/Scene/Game.unity";

        [MenuItem("Rouge/Generate Scene", priority = 11)]
        private static void GenerateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Directional Light ──
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.8f);
            light.intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
            lightGO.tag = "Untagged";

            // ── Floor ──
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(50, 1, 50);
            var floorMr = floor.GetComponent<MeshRenderer>();
            floorMr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMr.material.color = new Color(0.25f, 0.25f, 0.3f);
            Object.DestroyImmediate(floor.GetComponent<BoxCollider>());

            // ── Walls ──
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
            var obstacleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            obstacleMat.color = new Color(0.4f, 0.4f, 0.45f);
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
                box.GetComponent<MeshRenderer>().material = obstacleMat;
            }

            // ── GameBootstrap ──
            var bootstrapGO = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGO.AddComponent<GameBootstrap>();
            bootstrap.config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Rouge/Resources/GameConfig.asset");
            bootstrap.waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>("Assets/Rouge/Resources/WaveConfig.asset");

            // ── Save ──
            System.IO.Directory.CreateDirectory("Assets/Rouge/Scene");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[ResSceneGenerator] Scene saved to " + ScenePath);
        }

        private static void BuildWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            var wallMr = wall.GetComponent<MeshRenderer>();
            wallMr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMr.material.color = new Color(0.35f, 0.35f, 0.4f);
        }
    }
}

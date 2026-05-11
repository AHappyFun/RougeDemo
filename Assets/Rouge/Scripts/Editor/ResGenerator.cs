using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

namespace Rouge.EditorTools
{
    [InitializeOnLoad]
    public static class ResGenerator
    {
        private const string Root = "Assets/Rouge/Resources";
        private const string FlagKey = "Rouge_AssetsGenerated";

        // Auto-run once after first script compile
        static ResGenerator()
        {
            if (!EditorPrefs.GetBool(FlagKey, false))
            {
                EditorApplication.delayCall += GenerateAll;
                EditorPrefs.SetBool(FlagKey, true);
            }
        }

        [MenuItem("Rouge/Generate Assets", priority = 10)]
        public static void GenerateAll()
        {
            Debug.Log("[ResGenerator] Generating prefabs & materials...");

            // ── 1. Create directory structure on disk ────────────────
            var dirs = new List<string>
            {
                Root + "/Prefabs/Enemies",
                Root + "/Prefabs/Bullets",
                Root + "/Prefabs/VFX",
                Root + "/Materials/Bullets",
                Root + "/Materials/VFX",
            };
            foreach (var d in dirs)
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);

            // ── 2. Make Unity recognize the new directories ──────────
            AssetDatabase.Refresh();

            // ── 3. Create material assets ────────────────────────────
            var particleMat = CreateVFXMat("ParticleDefault", Color.white);
            CreateCharMat("Player", Color.white);
            CreateCharMat("EnemyDefault", new Color(1f, 0.25f, 0.25f));
            CreateCharMat("Straight", Color.yellow);
            CreateCharMat("Orbital", new Color(1f, 0.5f, 0f));
            CreateCharMat("Ricochet", Color.green);
            CreateCharMat("Shotgun", Color.magenta);
            CreateCharMat("Chain", Color.cyan);
            CreateCharMat("SwordBullet", Color.white);
            CreateVFXMat("HealthBarBG", Color.gray);
            CreateVFXMat("HealthBarFill", Color.green);

            // ── 4. Create prefabs ────────────────────────────────────
            SavePrefab(BuildPlayer(),                   Root + "/Prefabs/Player.prefab");
            SavePrefab(BuildEnemy(),                    Root + "/Prefabs/Enemies/BasicEnemy.prefab");
            // Per-type bullet prefabs with baked-in colors
            SavePrefab(BuildBullet("StraightBullet",  Color.yellow,             particleMat), Root + "/Prefabs/Bullets/StraightBullet.prefab");
            SavePrefab(BuildBullet("OrbitalBullet",   new Color(1f, 0.5f, 0f), particleMat), Root + "/Prefabs/Bullets/OrbitalBullet.prefab");
            SavePrefab(BuildBullet("RicochetBullet",  Color.green,              particleMat), Root + "/Prefabs/Bullets/RicochetBullet.prefab");
            SavePrefab(BuildBullet("ShotgunBullet",   Color.magenta,            particleMat), Root + "/Prefabs/Bullets/ShotgunBullet.prefab");
            SavePrefab(BuildBullet("ChainBullet",     Color.cyan,               particleMat), Root + "/Prefabs/Bullets/ChainBullet.prefab");
            // Generic fallback (white)
            SavePrefab(BuildBullet("SwordBullet",     Color.white,              particleMat), Root + "/Prefabs/Bullets/SwordBullet.prefab");
            SavePrefab(BuildHitVFX(particleMat),   Root + "/Prefabs/VFX/HitParticles.prefab");
            SavePrefab(BuildDeathVFX(particleMat), Root + "/Prefabs/VFX/DeathParticles.prefab");

            // ── 5. Cleanup orphaned runtime materials ────────────────
            foreach (var o in Resources.FindObjectsOfTypeAll<Material>())
                if (!AssetDatabase.Contains(o) && o.name != "Default-Material")
                    Object.DestroyImmediate(o);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ResGenerator] Done! All assets in Resources/");
        }

        // ═══════════════════════════════════════════════════════════════
        //  Prefab builders
        // ═══════════════════════════════════════════════════════════════

        private static GameObject BuildPlayer()
        {
            var mat = LoadMat("Player");
            var go = CreatePrim(PrimitiveType.Sphere, "Player", mat);
            go.tag = "Player";
            go.transform.localScale = Vector3.one;
            go.GetComponent<SphereCollider>().radius = 0.5f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            return go;
        }

        private static GameObject BuildEnemy()
        {
            var mat = LoadMat("EnemyDefault");
            var go = CreatePrim(PrimitiveType.Sphere, "Enemy", mat);
            go.tag = "Enemy";
            go.transform.localScale = Vector3.one * 0.8f;
            go.GetComponent<SphereCollider>().isTrigger = true;
            go.GetComponent<SphereCollider>().radius = 0.5f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            var hbBgMat = LoadMat("HealthBarBG");
            var bg = CreatePrim(PrimitiveType.Quad, "HealthBarBG", hbBgMat);
            bg.transform.SetParent(go.transform);
            bg.transform.localPosition = new Vector3(0, 0.8f, 0);
            bg.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            bg.transform.localScale = new Vector3(0.6f, 0.06f, 1f);
            Object.DestroyImmediate(bg.GetComponent<Collider>());

            var hbFillMat = LoadMat("HealthBarFill");
            var fill = CreatePrim(PrimitiveType.Quad, "HealthBarFill", hbFillMat);
            fill.transform.SetParent(bg.transform);
            fill.transform.localPosition = new Vector3(-0.5f, 0, 0);
            fill.transform.localRotation = Quaternion.identity;
            fill.transform.localScale = Vector3.one;
            Object.DestroyImmediate(fill.GetComponent<Collider>());

            return go;
        }

        private static GameObject BuildBullet(string prefabName, Color color, Material particleMat)
        {
            // Material name is prefabName minus "Bullet" suffix, e.g. "StraightBullet" -> "Straight"
            string matName = prefabName == "SwordBullet" ? "SwordBullet" : prefabName.Replace("Bullet", "");
            var mat = LoadMat(matName);
            if (mat == null) mat = LoadMat("SwordBullet"); // fallback

            var go = new GameObject(prefabName);
            go.tag = "Bullet";

            // Blade body
            var blade = CreatePrim(PrimitiveType.Cube, "Blade", mat);
            blade.transform.SetParent(go.transform);
            blade.transform.localPosition = new Vector3(0, 0, 0.4f);
            blade.transform.localRotation = Quaternion.identity;
            blade.transform.localScale = new Vector3(0.05f, 0.02f, 0.6f);
            Object.DestroyImmediate(blade.GetComponent<Collider>());

            // Blade tip
            var tip = CreatePrim(PrimitiveType.Cube, "Tip", mat);
            tip.transform.SetParent(go.transform);
            tip.transform.localPosition = new Vector3(0, 0, 0.75f);
            tip.transform.localRotation = Quaternion.identity;
            tip.transform.localScale = new Vector3(0.03f, 0.015f, 0.15f);
            Object.DestroyImmediate(tip.GetComponent<Collider>());

            // Cross-guard
            var guard = CreatePrim(PrimitiveType.Cube, "Guard", mat);
            guard.transform.SetParent(go.transform);
            guard.transform.localPosition = new Vector3(0, 0, -0.05f);
            guard.transform.localRotation = Quaternion.identity;
            guard.transform.localScale = new Vector3(0.3f, 0.03f, 0.05f);
            Object.DestroyImmediate(guard.GetComponent<Collider>());

            // Handle
            var handle = CreatePrim(PrimitiveType.Cube, "Handle", CreateTempMat(Color.white * 0.6f));
            handle.transform.SetParent(go.transform);
            handle.transform.localPosition = new Vector3(0, 0, -0.2f);
            handle.transform.localRotation = Quaternion.identity;
            handle.transform.localScale = new Vector3(0.04f, 0.02f, 0.2f);
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            // Pommel
            var pommel = CreatePrim(PrimitiveType.Cube, "Pommel", CreateTempMat(Color.white * 0.4f));
            pommel.transform.SetParent(go.transform);
            pommel.transform.localPosition = new Vector3(0, 0, -0.32f);
            pommel.transform.localRotation = Quaternion.identity;
            pommel.transform.localScale = new Vector3(0.08f, 0.06f, 0.06f);
            Object.DestroyImmediate(pommel.GetComponent<Collider>());

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.3f, 0.3f, 1.0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.15f;
            tr.startWidth = 0.1f;
            tr.endWidth = 0f;
            var tc = color; tc.a = 0.6f;
            tr.startColor = tc;
            tr.endColor = new Color(tc.r, tc.g, tc.b, 0f);
            tr.material = particleMat;
            tr.minVertexDistance = 0.05f;

            return go;
        }

        private static GameObject BuildHitVFX(Material mat)
        {
            var go = new GameObject("HitParticles");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = Color.white;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emit = ps.emission;
            emit.rateOverTime = 0;
            emit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)8) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            go.AddComponent<AutoDestroy>().lifetime = 1f;
            return go;
        }

        private static GameObject BuildDeathVFX(Material mat)
        {
            var go = new GameObject("DeathParticles");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startColor = Color.white;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emit = ps.emission;
            emit.rateOverTime = 0;
            emit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)16) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            go.AddComponent<AutoDestroy>().lifetime = 1.5f;
            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════════

        private static Material LoadMat(string name)
        {
            // Try each subfolder — root, Bullets, VFX
            var prefixes = new string[] { "", "/Bullets", "/VFX" };
            foreach (var p in prefixes)
            {
                var path = Root + "/Materials" + p + "/" + name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null) return m;
            }
            return null;
        }

        private static GameObject CreatePrim(PrimitiveType type, string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.GetComponent<MeshRenderer>().material = mat;
            return go;
        }

        private static Material CreateCharMat(string name, Color color)
        {
            var shader = Shader.Find("Rouge/Character");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = name, color = color };
            SaveMat(mat, name);
            return mat;
        }

        private static Material CreateTempMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return new Material(shader) { color = color };
        }

        private static Material CreateVFXMat(string name, Color color)
        {
            var shader = Shader.Find("Rouge/VFX");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = name, color = color };
            SaveMat(mat, name);
            return mat;
        }

        private static void SaveMat(Material mat, string name)
        {
            // Determine subfolder: "Player"/"EnemyDefault" -> root;  "Straight"/"Orbital"/etc -> Bullets;  "Particle"/"HealthBar" -> VFX
            string sub = "";
            if (name == "Player" || name == "EnemyDefault") sub = "";
            else if (name == "ParticleDefault" || name.StartsWith("HealthBar")) sub = "/VFX";
            else sub = "/Bullets"; // Straight, Orbital, Ricochet, Shotgun, Chain, SwordBullet
            AssetDatabase.CreateAsset(mat, Root + "/Materials" + sub + "/" + name + ".mat");
        }

        private static void SavePrefab(GameObject source, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            if (prefab != null)
                Debug.Log("  V " + path);
            else
                Debug.LogError("  X " + path);
            Object.DestroyImmediate(source);
        }
    }
}

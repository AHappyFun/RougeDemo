using UnityEngine;

namespace Rouge
{
    public static class MeshGenerator
    {
        private static Material unlitMat;
        private static Material particleMat;

        private static Material GetUnlitMaterial()
        {
            if (unlitMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard");
                unlitMat = new Material(shader);
            }
            return unlitMat;
        }

        private static Material GetParticleMaterial()
        {
            if (particleMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                particleMat = new Material(shader);
            }
            return particleMat;
        }

        // ===== Flying Sword Bullets =====

        /// <summary>Creates a flying-sword style bullet: elongated blade with trail.</summary>
        public static GameObject CreateSwordBullet(string name, Color color, float scale)
        {
            var go = new GameObject(name);
            go.tag = "Bullet";

            // Blade: long thin cube
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(go.transform);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localRotation = Quaternion.Euler(0, 0, 90); // align Z-up with forward
            blade.transform.localScale = new Vector3(0.06f, 0.8f, 0.03f);
            var bladeMr = blade.GetComponent<MeshRenderer>();
            bladeMr.material = new Material(GetUnlitMaterial());
            bladeMr.material.color = color;

            // Guard: tiny cross cube
            var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Guard";
            guard.transform.SetParent(go.transform);
            guard.transform.localPosition = new Vector3(0, -0.25f, 0);
            guard.transform.localRotation = Quaternion.identity;
            guard.transform.localScale = new Vector3(0.2f, 0.04f, 0.03f);
            var guardMr = guard.GetComponent<MeshRenderer>();
            guardMr.material = new Material(GetUnlitMaterial());
            guardMr.material.color = Color.Lerp(color, Color.white, 0.5f);

            // Sword-specific scale
            go.transform.localScale = Vector3.one * scale;

            // Collider
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.3f, 1.2f, 0.3f);

            // Rigidbody
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // TrailRenderer for sword trail
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.15f;
            tr.startWidth = 0.08f * scale;
            tr.endWidth = 0f;
            tr.material = new Material(GetUnlitMaterial());
            Color trailCol = color;
            trailCol.a = 0.6f;
            tr.startColor = trailCol;
            tr.endColor = new Color(trailCol.r, trailCol.g, trailCol.b, 0f);
            tr.minVertexDistance = 0.05f;

            return go;
        }

        // ===== Player =====

        public static GameObject CreatePlayer(Color color, float scale)
        {
            // Player is a larger glowing sphere (cultivation core)
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Player";
            go.tag = "Player";
            go.transform.localScale = Vector3.one * scale;

            var col = go.GetComponent<SphereCollider>();
            if (col != null)
            {
                col.isTrigger = false;
                col.radius = 0.5f;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(GetUnlitMaterial());
            mr.material.color = color;

            return go;
        }

        // ===== Enemy =====

        public static GameObject CreateEnemy(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Enemy";
            go.tag = "Enemy";
            go.transform.localScale = Vector3.one * 0.8f;

            var col = go.GetComponent<SphereCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.5f;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(GetUnlitMaterial());
            mr.material.color = color;

            return go;
        }

        // ===== Health Bar =====

        public static (GameObject background, GameObject fill) CreateHealthBar(Transform parent, float width, float height)
        {
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "HealthBarBG";
            bg.transform.SetParent(parent);
            bg.transform.localPosition = new Vector3(0, 0.8f, 0);
            bg.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            bg.transform.localScale = new Vector3(width, height, 1f);
            bg.GetComponent<MeshRenderer>().material.color = Color.gray;
            if (bg.GetComponent<Collider>() != null)
                Object.Destroy(bg.GetComponent<Collider>());

            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "HealthBarFill";
            fill.transform.SetParent(bg.transform);
            fill.transform.localPosition = new Vector3(-0.5f, 0, 0);
            fill.transform.localRotation = Quaternion.identity;
            fill.transform.localScale = Vector3.one;
            fill.GetComponent<MeshRenderer>().material.color = Color.green;
            if (fill.GetComponent<Collider>() != null)
                Object.Destroy(fill.GetComponent<Collider>());

            return (bg, fill);
        }

        // ===== Particle Effects =====

        public static GameObject SpawnHitParticles(Vector3 position, Color color, int count = 8)
        {
            var go = new GameObject("HitParticles");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = color;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, (short)count)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            go.AddComponent<AutoDestroy>().lifetime = 1f;
            return go;
        }

        public static GameObject SpawnDeathParticles(Vector3 position, Color color)
        {
            var go = new GameObject("DeathParticles");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startColor = color;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 16)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            go.AddComponent<AutoDestroy>().lifetime = 1.5f;
            return go;
        }
    }
}

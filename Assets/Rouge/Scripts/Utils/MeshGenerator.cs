using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    public static class MeshGenerator
    {
        // ── Cached prefab refs ──────────────────────────────────
        private static GameObject _playerPrefab;
        private static GameObject _enemyPrefab;
        private static Dictionary<string, GameObject> _bulletPrefabs = new Dictionary<string, GameObject>();
        private static GameObject _hitParticlePrefab;
        private static GameObject _deathParticlePrefab;

        // ── Fallback materials ──────────────────────────────────
        private static Material _unlitMat;
        private static Material _particleMat;

        private static Material GetUnlit()
        {
            if (_unlitMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard");
                _unlitMat = new Material(shader);
            }
            return _unlitMat;
        }

        private static Material GetParticle()
        {
            if (_particleMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                _particleMat = new Material(shader);
            }
            return _particleMat;
        }

        // =====================================================
        //  Flying Sword Bullet
        // =====================================================

        public static GameObject CreateSwordBullet(string name, Color color, float scale)
        {
            var go = CreateBulletFromPrefab(name, color, scale);
            return go ?? CreateBulletProcedural(name, color, scale);
        }

        private static GameObject CreateBulletFromPrefab(string name, Color color, float scale)
        {
            string key = "SwordBullet";
            if (name.EndsWith("Sword"))
                key = name.Substring(0, name.Length - 5) + "Bullet";

            GameObject prefab;
            if (!_bulletPrefabs.TryGetValue(key, out prefab))
            {
                prefab = Resources.Load<GameObject>("Prefabs/Bullets/" + key);
                _bulletPrefabs[key] = prefab;
            }
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab);
            go.name = name;
            SetPartColor(go.transform, "Blade",  color);
            SetPartColor(go.transform, "Tip",    color);
            SetPartColor(go.transform, "Guard",  Color.Lerp(color, Color.white, 0.5f));
            SetPartColor(go.transform, "Handle", color * 0.6f);
            SetPartColor(go.transform, "Pommel", color * 0.4f);

            var tr = go.GetComponent<TrailRenderer>();
            if (tr != null)
            {
                var c = color; c.a = 0.6f;
                tr.startColor = c;
                tr.endColor = new Color(c.r, c.g, c.b, 0f);
                tr.startWidth = 0.1f * scale;
            }
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static GameObject CreateBulletProcedural(string name, Color color, float scale)
        {
            var go = new GameObject(name);
            go.tag = "Bullet";

            AddSwordPart(go.transform, "Blade",  color,      new Vector3(0, 0, 0.4f),  Quaternion.identity, new Vector3(0.05f, 0.02f, 0.6f));
            AddSwordPart(go.transform, "Tip",    color,      new Vector3(0, 0, 0.75f), Quaternion.identity, new Vector3(0.03f, 0.015f, 0.15f));
            AddSwordPart(go.transform, "Guard",  Color.Lerp(color, Color.white, 0.5f), new Vector3(0, 0, -0.05f), Quaternion.identity, new Vector3(0.3f, 0.03f, 0.05f));
            AddSwordPart(go.transform, "Handle", color * 0.6f, new Vector3(0, 0, -0.2f), Quaternion.identity, new Vector3(0.04f, 0.02f, 0.2f));
            AddSwordPart(go.transform, "Pommel", color * 0.4f, new Vector3(0, 0, -0.32f), Quaternion.identity, new Vector3(0.08f, 0.06f, 0.06f));

            go.transform.localScale = Vector3.one * scale;

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.3f, 0.3f, 1f);

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.15f;
            tr.startWidth = 0.1f * scale;
            tr.endWidth = 0f;
            tr.material = new Material(GetUnlit());
            var tc = color; tc.a = 0.6f;
            tr.startColor = tc;
            tr.endColor = new Color(tc.r, tc.g, tc.b, 0f);
            tr.minVertexDistance = 0.05f;
            return go;
        }

        // =====================================================
        //  Player
        // =====================================================

        public static GameObject CreatePlayer(Color color, float scale)
        {
            var go = CreatePlayerFromPrefab(color, scale);
            return go ?? CreatePlayerProcedural(color, scale);
        }

        private static GameObject CreatePlayerFromPrefab(Color color, float scale)
        {
            if (_playerPrefab == null)
                _playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
            if (_playerPrefab == null) return null;

            var go = Object.Instantiate(_playerPrefab);
            go.name = "Player";
            go.transform.localScale = Vector3.one * scale;

            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null) smr.material.color = color;
            else
            {
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material.color = color;
            }
            return go;
        }

        private static GameObject CreatePlayerProcedural(Color color, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Player";
            go.tag = "Player";
            go.transform.localScale = Vector3.one * scale;

            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = false; col.radius = 0.5f;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            go.GetComponent<MeshRenderer>().material = new Material(GetUnlit());
            go.GetComponent<MeshRenderer>().material.color = color;
            return go;
        }

        // =====================================================
        //  Enemy
        // =====================================================

        public static GameObject CreateEnemy(Color color)
        {
            var go = CreateEnemyFromPrefab(color);
            return go ?? CreateEnemyProcedural(color);
        }

        private static GameObject CreateEnemyFromPrefab(Color color)
        {
            if (_enemyPrefab == null)
                _enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemies/BasicEnemy");
            if (_enemyPrefab == null) return null;

            var go = Object.Instantiate(_enemyPrefab);
            go.name = "Enemy";

            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null) smr.material.color = color;
            else
            {
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material.color = color;
            }
            return go;
        }

        private static GameObject CreateEnemyProcedural(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Enemy";
            go.tag = "Enemy";
            go.transform.localScale = Vector3.one * 0.8f;

            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.5f;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            go.GetComponent<MeshRenderer>().material = new Material(GetUnlit());
            go.GetComponent<MeshRenderer>().material.color = color;
            return go;
        }

        // =====================================================
        //  Health Bar
        // =====================================================

        public static (GameObject background, GameObject fill) CreateHealthBar(
            Transform parent, float width, float height)
        {
            var bgT = parent.Find("HealthBarBG");
            if (bgT != null)
            {
                var bg = bgT.gameObject;
                var fillT = bgT.Find("HealthBarFill");
                if (fillT != null) return (bg, fillT.gameObject);
            }

            var bgFb = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgFb.name = "HealthBarBG";
            bgFb.transform.SetParent(parent);
            bgFb.transform.localPosition = new Vector3(0, 0.8f, 0);
            bgFb.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            bgFb.transform.localScale = new Vector3(width, height, 1f);
            bgFb.GetComponent<MeshRenderer>().material.color = Color.gray;
            if (bgFb.GetComponent<Collider>() != null)
                Object.Destroy(bgFb.GetComponent<Collider>());

            var fillFb = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillFb.name = "HealthBarFill";
            fillFb.transform.SetParent(bgFb.transform);
            fillFb.transform.localPosition = new Vector3(-0.5f, 0, 0);
            fillFb.transform.localRotation = Quaternion.identity;
            fillFb.transform.localScale = Vector3.one;
            fillFb.GetComponent<MeshRenderer>().material.color = Color.green;
            if (fillFb.GetComponent<Collider>() != null)
                Object.Destroy(fillFb.GetComponent<Collider>());

            return (bgFb, fillFb);
        }

        // =====================================================
        //  Particle Effects
        // =====================================================

        public static GameObject SpawnHitParticles(Vector3 position, Color color, int count = 8)
        {
            if (_hitParticlePrefab == null)
                _hitParticlePrefab = Resources.Load<GameObject>("Prefabs/VFX/HitParticles");

            if (_hitParticlePrefab != null)
            {
                var go = Object.Instantiate(_hitParticlePrefab, position, Quaternion.identity);
                go.name = "HitParticles";
                var ps = go.GetComponent<ParticleSystem>();
                var m = ps.main; m.startColor = color;
                ps.Play();
                return go;
            }
            return BuildHitVFXProcedural(position, color, count);
        }

        public static GameObject SpawnDeathParticles(Vector3 position, Color color)
        {
            if (_deathParticlePrefab == null)
                _deathParticlePrefab = Resources.Load<GameObject>("Prefabs/VFX/DeathParticles");

            if (_deathParticlePrefab != null)
            {
                var go = Object.Instantiate(_deathParticlePrefab, position, Quaternion.identity);
                go.name = "DeathParticles";
                var ps = go.GetComponent<ParticleSystem>();
                var m = ps.main; m.startColor = color;
                ps.Play();
                return go;
            }
            return BuildDeathVFXProcedural(position, color);
        }

        // =====================================================
        //  Procedural fallbacks (particle VFX)
        // =====================================================

        private static GameObject BuildHitVFXProcedural(Vector3 position, Color color, int count)
        {
            var go = new GameObject("HitParticles");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var m = ps.main;
            m.playOnAwake = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            m.startColor = color;
            m.loop = false;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0;
            e.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.3f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.material = GetParticle();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            go.AddComponent<AutoDestroy>().lifetime = 1f;
            return go;
        }

        private static GameObject BuildDeathVFXProcedural(Vector3 position, Color color)
        {
            var go = new GameObject("DeathParticles");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var m = ps.main;
            m.playOnAwake = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            m.startColor = color;
            m.loop = false;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0;
            e.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)16) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.5f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.material = GetParticle();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            go.AddComponent<AutoDestroy>().lifetime = 1.5f;
            return go;
        }

        // =====================================================
        //  Helpers
        // =====================================================

        private static void SetPartColor(Transform root, string name, Color color)
        {
            var t = root.Find(name);
            if (t == null) return;
            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = color;
        }

        private static void AddSwordPart(Transform parent, string name, Color color,
            Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().material = new Material(GetUnlit());
            go.GetComponent<MeshRenderer>().material.color = color;
        }
    }
}

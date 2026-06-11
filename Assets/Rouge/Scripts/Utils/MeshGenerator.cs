using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    public static class MeshGenerator
    {
        // ── Cached prefab refs ──────────────────────────────────
        private static GameObject _playerPrefab;
        private static Dictionary<string, GameObject> _enemyPrefabs = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _bulletPrefabs = new Dictionary<string, GameObject>();
        private static GameObject _hitParticlePrefab;
        private static GameObject _deathParticlePrefab;

        // =====================================================
        //  Flying Sword Bullet
        // =====================================================

        public static GameObject CreateSwordBullet(string name, Color color, float scale)
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
            if (prefab == null)
            {
                Debug.LogError("[MeshGenerator] Missing bullet prefab: Prefabs/Bullets/" + key);
                return new GameObject(name);
            }

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

        // =====================================================
        //  Player
        // =====================================================

        public static GameObject CreatePlayer(Color color, float scale)
        {
            if (_playerPrefab == null)
                _playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
            if (_playerPrefab == null)
            {
                Debug.LogError("[MeshGenerator] Missing Prefabs/Player");
                return new GameObject("Player") { tag = "Player" };
            }

            var go = Object.Instantiate(_playerPrefab);
            go.name = "Player";
            go.tag = "Player";
            go.transform.localScale = Vector3.one * scale;

            return go;
        }

        // =====================================================
        //  Enemy
        // =====================================================

        public static GameObject CreateEnemy(string type, Color color)
        {
            string prefabPath = "Prefabs/Enemies/" + type;
            GameObject prefab;
            if (!_enemyPrefabs.TryGetValue(type, out prefab))
            {
                prefab = Resources.Load<GameObject>(prefabPath);
                _enemyPrefabs[type] = prefab;
            }
            if (prefab == null)
            {
                Debug.LogError("[MeshGenerator] Missing enemy prefab: " + prefabPath);
                return new GameObject("Enemy") { tag = "Enemy" };
            }

            var go = Object.Instantiate(prefab);
            go.name = type;

            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null) smr.material.SetColor("_BaseColor", color);
            else
            {
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material.SetColor("_BaseColor", color);
            }
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
            bgFb.transform.localPosition = new Vector3(0, 1.2f, 0);
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
            if (_hitParticlePrefab == null)
            {
                Debug.LogError("[MeshGenerator] Missing Prefabs/VFX/HitParticles");
                return null;
            }

            var go = ObjectPool.Get("HitParticles", _hitParticlePrefab);
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            go.name = "HitParticles";

            var vfxGo = GameObject.Find("VFX");
            if (vfxGo != null) go.transform.SetParent(vfxGo.transform);

            // 确保有 PooledParticle 组件（第一次添加后永久保留）
            if (go.GetComponent<PooledParticle>() == null)
                go.AddComponent<PooledParticle>();

            var ps = go.GetComponent<ParticleSystem>();
            var m = ps.main; m.startColor = color;

            go.SetActive(true);
            ps.Play();
            return go;
        }

        public static GameObject SpawnDeathParticles(Vector3 position, Color color)
        {
            if (_deathParticlePrefab == null)
                _deathParticlePrefab = Resources.Load<GameObject>("Prefabs/VFX/DeathParticles");
            if (_deathParticlePrefab == null)
            {
                Debug.LogError("[MeshGenerator] Missing Prefabs/VFX/DeathParticles");
                return null;
            }

            var go = ObjectPool.Get("DeathParticles", _deathParticlePrefab);
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            go.name = "DeathParticles";

            var vfxGo = GameObject.Find("VFX");
            if (vfxGo != null) go.transform.SetParent(vfxGo.transform);

            if (go.GetComponent<PooledParticle>() == null)
                go.AddComponent<PooledParticle>();

            var ps = go.GetComponent<ParticleSystem>();
            var m = ps.main; m.startColor = color;

            go.SetActive(true);
            ps.Play();
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
            if (mr != null) mr.material.SetColor("_BaseColor", color);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    /// <summary>通用对象池，按 key 管理 GameObject 的复用</summary>
    public static class ObjectPool
    {
        private static Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private static Transform poolRoot;

        private static void EnsureRoot()
        {
            if (poolRoot != null) return;
            var go = new GameObject("_ObjectPool");
            Object.DontDestroyOnLoad(go);
            poolRoot = go.transform;
        }

        /// <summary>从池中取出一个对象（返回时处于非激活状态，调用方设置完毕后自行 Activate）</summary>
        public static GameObject Get(string key, GameObject prefab)
        {
            EnsureRoot();

            if (pools.TryGetValue(key, out var queue) && queue.Count > 0)
            {
                var obj = queue.Dequeue();
                obj.transform.SetParent(null);
                return obj; // 保持非激活，调用方配置后再激活
            }

            // 池空 → 新实例化，也保持非激活
            var newObj = Object.Instantiate(prefab);
            newObj.name = key;
            newObj.SetActive(false);
            return newObj;
        }

        /// <summary>将对象归还池中（自动 SetActive(false)）</summary>
        public static void Return(GameObject obj)
        {
            EnsureRoot();
            obj.SetActive(false);
            obj.transform.SetParent(poolRoot);
            string key = obj.name;
            if (!pools.TryGetValue(key, out var queue))
            {
                queue = new Queue<GameObject>();
                pools[key] = queue;
            }
            queue.Enqueue(obj);
        }

        /// <summary>清空指定 key 的池</summary>
        public static void Clear(string key)
        {
            if (pools.TryGetValue(key, out var queue))
            {
                foreach (var obj in queue)
                    Object.Destroy(obj);
                queue.Clear();
            }
        }

        /// <summary>清空所有池</summary>
        public static void ClearAll()
        {
            foreach (var kv in pools)
            {
                foreach (var obj in kv.Value)
                    Object.Destroy(obj);
                kv.Value.Clear();
            }
            pools.Clear();
        }
    }
}

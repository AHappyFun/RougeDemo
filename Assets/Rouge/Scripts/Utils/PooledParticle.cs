using System.Collections;
using UnityEngine;

namespace Rouge
{
    /// <summary>挂载在对象池管理的粒子特效上，播放完毕后自动归还池中</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticle : MonoBehaviour
    {
        private ParticleSystem ps;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            if (!gameObject.activeInHierarchy) return;

            // 阻止 prefab 自带的 stopAction=Destroy（我们要用对象池归还）
            var m = ps.main;
            m.stopAction = ParticleSystemStopAction.None;

            StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            // 等待粒子全部播放完（duration + 最长 lifetime + 余量）
            float total = ps.main.duration + ps.main.startLifetime.constantMax + 0.5f;
            yield return new WaitForSeconds(total);

            if (gameObject.activeInHierarchy)
                ObjectPool.Return(gameObject);
        }
    }
}

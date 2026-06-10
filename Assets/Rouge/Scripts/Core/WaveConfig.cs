using UnityEngine;

namespace Rouge
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Rouge/Wave Config", order = 2)]
    public class WaveConfig : ScriptableObject
    {
        [Header("波次触发条件")]
        [Tooltip("升到下一波所需的击杀数")]
        public int killsPerWave = 100;
        [Tooltip("每波额外增加的需求击杀数（0 = 固定需求）")]
        public int killsPerWaveGrowth = 20;

        [Header("每波敌人强化幅度")]
        [Tooltip("生命值缩放：HP += 基础HP × hpScale × (波次-1)")]
        [Range(0f, 2f)] public float hpScale = 0.5f;
        [Tooltip("移速缩放：速度 += 基础速度 × speedScale × (波次-1)")]
        [Range(0f, 1f)] public float speedScale = 0.15f;
        [Tooltip("生成间隔缩放：间隔 × (1 - spawnRateScale)^(波次-1)")]
        [Range(0f, 0.5f)] public float spawnRateScale = 0.15f;
        [Tooltip("每波额外增加的最大敌人数")]
        public int maxEnemiesPerWave = 5;
        [Tooltip("每波额外增加的敌人伤害")]
        public int damagePerWave = 3;
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Rouge/Wave Config", order = 2)]
    public class WaveConfig : ScriptableObject
    {
        [Header("Wave Trigger")]
        [Tooltip("Kills needed to trigger a wave-up")]
        public int killsPerWave = 100;
        [Tooltip("Extra kills added to requirement each wave (0 = fixed)")]
        public int killsPerWaveGrowth = 20;

        [Header("Enemy Scaling Per Wave")]
        [Tooltip("Multiplier: HP += HP * hpScale * (wave-1)")]
        [Range(0f, 2f)] public float hpScale = 0.5f;
        [Tooltip("Multiplier: Speed += Speed * speedScale * (wave-1)")]
        [Range(0f, 1f)] public float speedScale = 0.15f;
        [Tooltip("Spawn interval *= (1 - spawnRateScale)^(wave-1)")]
        [Range(0f, 0.5f)] public float spawnRateScale = 0.15f;
        [Tooltip("Extra max enemies per wave")]
        public int maxEnemiesPerWave = 5;
        [Tooltip("Extra enemy damage per wave")]
        public int damagePerWave = 3;

        [Header("Available Upgrades")]
        public List<UpgradeDef> upgrades = new List<UpgradeDef>
        {
            // Global upgrades
            new UpgradeDef { type = UpgradeType.DamageUp,    displayName = "剑气强化", desc = "飞剑伤害 +30%",  value = 30f, color = new Color(1f, 0.3f, 0.2f) },
            new UpgradeDef { type = UpgradeType.CooldownDown, displayName = "御剑如风", desc = "攻击冷却 -20%",  value = 20f, color = new Color(0.3f, 0.6f, 1f) },
            new UpgradeDef { type = UpgradeType.BulletCount,  displayName = "万剑归宗", desc = "子弹数量 +1",    value = 1f,  color = new Color(1f, 0.7f, 0.1f) },
            new UpgradeDef { type = UpgradeType.Heal,         displayName = "灵气复苏", desc = "恢复 30% 生命",  value = 30f, color = new Color(0.2f, 1f, 0.3f) },
            new UpgradeDef { type = UpgradeType.BulletSpeed,  displayName = "流星赶月", desc = "飞剑速度 +25%",  value = 25f, color = new Color(0.8f, 0.4f, 1f) },
            // Per-type upgrades
            new UpgradeDef { type = UpgradeType.RicochetBounce, displayName = "弹射强化", desc = "弹射飞剑反弹次数 +2", value = 2f, color = new Color(0.3f, 1f, 0.4f), targetBulletType = BulletManager.BulletType.Ricochet },
            new UpgradeDef { type = UpgradeType.OrbitalSpeed,  displayName = "旋转加速", desc = "环绕飞剑转速 +40%",   value = 40f, color = new Color(1f, 0.5f, 0f), targetBulletType = BulletManager.BulletType.Orbital },
            new UpgradeDef { type = UpgradeType.ChainHops,    displayName = "连锁增强", desc = "链式飞剑弹射次数 +1",  value = 1f, color = new Color(0.3f, 0.7f, 1f), targetBulletType = BulletManager.BulletType.Chain },
            new UpgradeDef { type = UpgradeType.ChainRange,   displayName = "链式延伸", desc = "链式飞剑弹射范围 +30%", value = 30f, color = new Color(0.3f, 0.7f, 1f), targetBulletType = BulletManager.BulletType.Chain },
            new UpgradeDef { type = UpgradeType.ShotgunSpread, displayName = "散射聚焦", desc = "散射角度 -20%",     value = 20f, color = new Color(1f, 0.3f, 0.9f), targetBulletType = BulletManager.BulletType.Shotgun },
        };
    }

    public enum UpgradeType
    {
        // Global — affect all bullet types
        DamageUp,
        CooldownDown,
        BulletCount,
        Heal,
        BulletSpeed,
        // Per-type — affect a specific bullet type
        RicochetBounce,
        OrbitalSpeed,
        ChainHops,
        ChainRange,
        ShotgunSpread,
    }

    [System.Serializable]
    public class UpgradeDef
    {
        public UpgradeType type;
        public string displayName;
        public string desc;
        [Range(1f, 100f)] public float value;
        public Color color = Color.white;
        [Tooltip("If set, this upgrade only affects the specified bullet type. Leave empty for global.")]
        public BulletManager.BulletType? targetBulletType;
    }
}

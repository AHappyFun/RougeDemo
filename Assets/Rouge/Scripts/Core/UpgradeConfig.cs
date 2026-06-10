using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    [CreateAssetMenu(fileName = "UpgradeConfig", menuName = "Rouge/Upgrade Config", order = 4)]
    public class UpgradeConfig : ScriptableObject
    {
        [Header("可用增益列表")]
        public List<UpgradeDef> upgrades = new List<UpgradeDef>
        {
            // Global upgrades
            new UpgradeDef { type = UpgradeType.DamageUp,    displayName = "剑气强化", desc = "飞剑伤害 +30%",  value = 30f, color = new Color(1f, 0.3f, 0.2f) },
            new UpgradeDef { type = UpgradeType.CooldownDown, displayName = "御剑如风", desc = "所有技能冷却 -20%",  value = 20f, color = new Color(0.3f, 0.6f, 1f) },
            new UpgradeDef { type = UpgradeType.AttackSpeedUp, displayName = "疾风剑意", desc = "普攻攻速 +25%",    value = 20f, color = new Color(0.2f, 0.9f, 0.8f) },
            new UpgradeDef { type = UpgradeType.BulletCount,  displayName = "万剑归宗", desc = "子弹数量 +1",    value = 1f,  color = new Color(1f, 0.7f, 0.1f) },
            new UpgradeDef { type = UpgradeType.Heal,         displayName = "灵气复苏", desc = "恢复 30% 生命",  value = 30f, color = new Color(0.2f, 1f, 0.3f) },
            new UpgradeDef { type = UpgradeType.BulletSpeed,  displayName = "流星赶月", desc = "飞剑速度 +25%",  value = 25f, color = new Color(0.8f, 0.4f, 1f) },
            // Per-type upgrades
            new UpgradeDef { type = UpgradeType.SkillCooldownDown, displayName = "玄门心法", desc = "技能冷却缩减 -15%", value = 15f, color = new Color(0.4f, 0.5f, 0.9f) },
            new UpgradeDef { type = UpgradeType.RicochetBounce, displayName = "弹射强化", desc = "弹射飞剑反弹次数 +2", value = 2f, color = new Color(0.3f, 1f, 0.4f), targetBulletType = BulletManager.BulletType.Ricochet },
            new UpgradeDef { type = UpgradeType.OrbitalSpeed,  displayName = "旋转加速", desc = "环绕飞剑转速 +40%",   value = 40f, color = new Color(1f, 0.5f, 0f), targetBulletType = BulletManager.BulletType.Orbital },
            new UpgradeDef { type = UpgradeType.ChainHops,    displayName = "连锁增强", desc = "链式飞剑弹射次数 +1",  value = 1f, color = new Color(0.3f, 0.7f, 1f), targetBulletType = BulletManager.BulletType.Chain },
            new UpgradeDef { type = UpgradeType.ChainRange,   displayName = "链式延伸", desc = "链式飞剑弹射范围 +30%", value = 30f, color = new Color(0.3f, 0.7f, 1f), targetBulletType = BulletManager.BulletType.Chain },
            new UpgradeDef { type = UpgradeType.ShotgunSpread, displayName = "散射聚焦", desc = "散射角度 -20%",     value = 20f, color = new Color(1f, 0.3f, 0.9f), targetBulletType = BulletManager.BulletType.Shotgun },
        };
    }

    [System.Serializable]
    public class UpgradeDef
    {
        [Tooltip("增益类型（决定实际效果）")]
        public UpgradeType type;
        [Tooltip("增益名称（UI 显示）")]
        public string displayName;
        [Tooltip("增益描述（UI 显示）")]
        public string desc;
        [Range(1f, 100f)]
        [Tooltip("增益数值（百分比或绝对值，取决于类型）")]
        public float value;
        [Tooltip("增益颜色（UI 显示）")]
        public Color color = Color.white;
        [Tooltip("仅对指定子弹类型生效。留空 = 全局增益")]
        public BulletManager.BulletType? targetBulletType;
    }

    public enum UpgradeType
    {
        [Tooltip("全子弹伤害 +X%")]
        DamageUp,
        [Tooltip("全局冷却缩减（影响所有攻击和技能）")]
        CooldownDown,
        [Tooltip("普攻攻速提升（仅 Attack 类型）")]
        AttackSpeedUp,
        [Tooltip("技能冷却缩减（仅 Skill 类型）")]
        SkillCooldownDown,
        [Tooltip("全子弹数量 +X")]
        BulletCount,
        [Tooltip("恢复 X% 生命")]
        Heal,
        [Tooltip("全子弹速度 +X%")]
        BulletSpeed,
        [Tooltip("弹射飞剑反弹次数 +X")]
        RicochetBounce,
        [Tooltip("环绕飞剑转速 +X%")]
        OrbitalSpeed,
        [Tooltip("链式飞剑跳数 +X")]
        ChainHops,
        [Tooltip("链式飞剑范围 +X%")]
        ChainRange,
        [Tooltip("散射飞剑角度 -X%（收缩）")]
        ShotgunSpread,
    }
}

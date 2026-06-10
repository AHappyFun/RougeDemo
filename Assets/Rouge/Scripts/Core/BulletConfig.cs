using UnityEngine;

namespace Rouge
{
    public enum BulletCategory
    {
        Attack,  // 常驻攻击——受攻速影响
        Skill,   // 技能——有冷却 CD
    }

    [CreateAssetMenu(fileName = "BulletConfig", menuName = "Rouge/Bullet Config", order = 3)]
    public class BulletConfig : ScriptableObject
    {
        [Header("子弹类型配置")]
        [Tooltip("直线飞剑——基础普攻，沿直线飞行")]
        public BulletTypeConfig straight = new BulletTypeConfig
        {
            typeName = "Straight",
            category = BulletCategory.Attack,
            cooldown = 0.3f,
            damage = 10,
            speed = 10f,
            count = 1,
            color = Color.yellow
        };

        [Tooltip("环绕飞剑——技能，围绕玩家旋转，自动攻击范围内的敌人")]
        public BulletTypeConfig orbital = new BulletTypeConfig
        {
            typeName = "Orbital",
            category = BulletCategory.Skill,
            cooldown = 3f,
            damage = 10,
            speed = 360f,
            count = 3,
            radius = 4f,
            duration = 5f,
            color = new Color(1f, 0.5f, 0f)
        };

        [Tooltip("弹射飞剑——技能，命中敌人后弹射到附近下一个敌人")]
        public BulletTypeConfig ricochet = new BulletTypeConfig
        {
            typeName = "Ricochet",
            category = BulletCategory.Skill,
            cooldown = 0.5f,
            damage = 8,
            speed = 8f,
            count = 1,
            color = Color.green,
            maxBounces = 5
        };

        [Tooltip("散射飞剑——技能，扇面射出多发子弹")]
        public BulletTypeConfig shotgun = new BulletTypeConfig
        {
            typeName = "Shotgun",
            category = BulletCategory.Skill,
            cooldown = 0.8f,
            damage = 6,
            speed = 10f,
            count = 5,
            color = Color.magenta,
            spreadAngle = 60f
        };

        [Tooltip("链式飞剑——技能，命中后在敌人之间连锁传导")]
        public BulletTypeConfig chain = new BulletTypeConfig
        {
            typeName = "Chain",
            category = BulletCategory.Skill,
            cooldown = 0.6f,
            damage = 12,
            speed = 20f,
            count = 1,
            color = Color.cyan,
            maxHops = 3,
            chainRange = 5f
        };

    }

    [System.Serializable]
    public class BulletTypeConfig
    {
        [Tooltip("子弹类型名称（用于代码逻辑，不要随意修改）")]
        public string typeName;
        [Tooltip("攻击/技能分类——Attack=常驻攻速射击，Skill=技能冷却")]
        public BulletCategory category = BulletCategory.Attack;
        [Tooltip("攻击间隔（秒）。Attack 类型=攻速间隔，Skill 类型=冷却时间")]
        public float cooldown;
        [Tooltip("单发伤害")]
        public int damage;
        [Tooltip("子弹飞行速度")]
        public float speed;
        [Tooltip("每次射击的子弹数量（散射的弹片数、环绕的球数等）")]
        public int count;
        [Tooltip("环绕半径（仅环绕飞剑有效）")]
        public float radius = 1.5f;
        [Tooltip("持续时长（秒）。仅 Orbital 类技能有效——激活后持续多久才进入冷却")]
        public float duration;

        // ── 子弹专属参数 ──
        [Tooltip("子弹颜色")]
        public Color color;

        // ── 子弹专属参数 ──
        [Tooltip("弹射次数（仅弹射飞剑有效）")]
        public int maxBounces = 3;
        [Tooltip("散射扇形角度（仅散射飞剑有效）")]
        public float spreadAngle = 60f;
        [Tooltip("连锁跳转次数（仅链式飞剑有效）")]
        public int maxHops = 3;
        [Tooltip("连锁搜索范围（仅链式飞剑有效）")]
        public float chainRange = 5f;
    }
}

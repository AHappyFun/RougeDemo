using UnityEngine;

namespace Rouge
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Rouge/Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("玩家配置")]
        [Tooltip("玩家最大生命值")]
        public int playerMaxHP = 100;
        [Tooltip("敌人碰撞对玩家造成的伤害")]
        public int enemyContactDamage = 10;

        [Header("敌人生成配置")]
        [Tooltip("场上同时存在的最大敌人数")]
        public int maxEnemies = 40;
        [Tooltip("敌人生成间隔（秒），越小生成越快")]
        public float spawnInterval = 0.35f;
        [Tooltip("敌人基础生命值")]
        public int enemyHP = 30;
        [Tooltip("敌人基础移动速度")]
        public float enemySpeed = 2.5f;
        [Tooltip("敌人默认颜色")]
        public Color enemyColor = Color.red;

        [Header("画面")]
        [Range(4f, 20f)]
        [Tooltip("正交相机视口大小（数值越大看到越广）")]
        public float cameraOrthoSize = 8f;
        [Tooltip("摄像机背景色")]
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f);
    }
}

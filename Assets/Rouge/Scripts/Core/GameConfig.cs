using UnityEngine;

namespace Rouge
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Rouge/Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("Bullet Type Configs")]
        public BulletTypeConfig straight = new BulletTypeConfig
        {
            typeName = "Straight",
            cooldown = 0.3f,
            damage = 10,
            speed = 10f,
            count = 1,
            color = Color.yellow
        };

        public BulletTypeConfig orbital = new BulletTypeConfig
        {
            typeName = "Orbital",
            cooldown = 0f,
            damage = 10,
            speed = 360f,
            count = 3,
            radius = 4f,
            color = new Color(1f, 0.5f, 0f)
        };

        public BulletTypeConfig ricochet = new BulletTypeConfig
        {
            typeName = "Ricochet",
            cooldown = 0.5f,
            damage = 8,
            speed = 8f,
            count = 1,
            color = Color.green
        };

        public BulletTypeConfig shotgun = new BulletTypeConfig
        {
            typeName = "Shotgun",
            cooldown = 0.8f,
            damage = 6,
            speed = 10f,
            count = 5,
            color = Color.magenta
        };

        public BulletTypeConfig chain = new BulletTypeConfig
        {
            typeName = "Chain",
            cooldown = 0.6f,
            damage = 12,
            speed = 20f,
            count = 1,
            color = Color.cyan
        };

        [Header("Player Config")]
        public int playerMaxHP = 100;
        public int enemyContactDamage = 10;

        [Header("Enemy Config")]
        public int maxEnemies = 40;
        public float spawnInterval = 0.35f;
        public int enemyHP = 30;
        public float enemySpeed = 2.5f;
        public Color enemyColor = Color.red;

        [Header("Ricochet Config")]
        public int ricochetMaxBounces = 5;

        [Header("Shotgun Config")]
        public float shotgunSpreadAngle = 60f;

        [Header("Chain Config")]
        public int chainMaxHops = 3;
        public float chainRange = 5f;

        [Header("Screen")]
        [Range(4f, 20f)] public float cameraOrthoSize = 8f;
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f);
    }

    [System.Serializable]
    public class BulletTypeConfig
    {
        public string typeName;
        [Tooltip("Seconds between shots (0 = no auto-fire, e.g. orbital)")]
        public float cooldown;
        public int damage;
        public float speed;
        [Tooltip("Bullets per shot (shotgun pellets, orbital count, etc.)")]
        public int count;
        [Tooltip("Orbit radius (orbital only)")]
        public float radius = 1.5f;
        public Color color;
    }
}

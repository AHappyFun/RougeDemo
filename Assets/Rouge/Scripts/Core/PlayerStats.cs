using UnityEngine;

namespace Rouge
{
    /// <summary>Player data model — Inspector-exposed fields, UI reads from here.
    /// 基础值从 Config 同步，永不修改。运行时值由 BulletManager 更新。</summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        public int maxHP = 100;
        public int currentHP;

        [Header("Movement")]
        public float moveSpeed = 6f;

        [Header("Weapon Toggles")]
        public bool weaponStraight  = true;
        public bool weaponOrbital   = true;
        public bool weaponRicochet  = true;
        public bool weaponShotgun   = true;
        public bool weaponChain     = true;

        [Header("Straight")]
        public BulletCategory straightCategory = BulletCategory.Attack;
        public int straightDamage = 10;
        public float straightCooldown = 0.3f;
        public float straightSpeed = 10f;
        public Color straightColor = Color.yellow;

        [Header("Orbital")]
        public BulletCategory orbitalCategory = BulletCategory.Skill;
        public int orbitalDamage = 10;
        public float orbitalCooldown = 3f;
        public float orbitalDuration = 5f;
        public float orbitalSpeed = 360f;
        public int orbitalCount = 3;
        public float orbitalRadius = 4f;
        public Color orbitalColor = new Color(1f, 0.5f, 0f);

        [Header("Ricochet")]
        public BulletCategory ricochetCategory = BulletCategory.Skill;
        public int ricochetDamage = 8;
        public float ricochetCooldown = 1.5f;
        public float ricochetSpeed = 8f;
        public int ricochetBounces = 5;
        public Color ricochetColor = Color.green;

        [Header("Shotgun")]
        public BulletCategory shotgunCategory = BulletCategory.Skill;
        public int shotgunDamage = 6;
        public float shotgunCooldown = 0.8f;
        public float shotgunSpeed = 10f;
        public int shotgunCount = 5;
        public float shotgunSpread = 60f;
        public Color shotgunColor = Color.magenta;

        [Header("Chain")]
        public BulletCategory chainCategory = BulletCategory.Skill;
        public int chainDamage = 12;
        public float chainCooldown = 0.6f;
        public float chainSpeed = 20f;
        public int chainHops = 3;
        public float chainRange = 5f;
        public Color chainColor = Color.cyan;

        // ── 运行时值（只读，由 BulletManager 在升级后更新）──
        [Header("⚡ Runtime (read-only, after upgrades)")]
        public float runtimeStraightCD;
        public float runtimeStraightDamage;
        public float runtimeRicochetCD;
        public float runtimeRicochetDamage;
        public float runtimeShotgunCD;
        public float runtimeShotgunDamage;
        public float runtimeChainCD;
        public float runtimeChainDamage;
        public float runtimeOrbitalCD;
        public float runtimeOrbitalDamage;
        public float runtimeOrbitalDuration;
        public int runtimeOrbitalCount;
        public float runtimeOrbitalSpeed;
        public int runtimeRicochetBounces;
        public int runtimeShotgunCount;
        public float runtimeShotgunSpread;
        public int runtimeChainHops;
        public float runtimeChainRange;

        public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    }
}

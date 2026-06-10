using UnityEngine;

namespace Rouge
{
    /// <summary>
    /// 运行时技能调试面板——挂到任意 GameObject 上，在 Inspector 中查看子弹最终属性。
    /// </summary>
    public class SkillDebugPanel : MonoBehaviour
    {
        [Header("选择要查看的子弹类型")]
        public BulletManager.BulletType selectedType = BulletManager.BulletType.Straight;

        // ── 由 SkillDebugPanelEditor 填充显示 ──
        [System.NonSerialized] public string displayText = "";

        private BulletManager bm;
        private PlayerStats stats;

        private void Update()
        {
            if (bm == null) bm = FindObjectOfType<BulletManager>();
            if (stats == null) stats = FindObjectOfType<PlayerStats>();
            if (bm == null || stats == null) return;

            displayText = BuildDebugText(selectedType);
        }

        private string BuildDebugText(BulletManager.BulletType type)
        {
            var cat = bm.GetCategory(type);
            string catStr = cat == BulletCategory.Attack ? "普攻" : "技能";
            float cd = 0;
            int dmg = 0;
            float spd = 0;
            int count = 0;
            string extras = "";

            switch (type)
            {
                case BulletManager.BulletType.Straight:
                    cd = bm.GetCooldownForType(BulletManager.BulletType.Straight);
                    dmg = Mathf.RoundToInt(stats.straightDamage * bm.GetDamageMultiplier());
                    spd = stats.straightSpeed * bm.GetSpeedMultiplier();
                    count = 1;
                    break;
                case BulletManager.BulletType.Orbital:
                    cd = bm.GetCooldownForType(BulletManager.BulletType.Orbital);
                    dmg = Mathf.RoundToInt(stats.orbitalDamage * bm.GetDamageMultiplier());
                    spd = stats.orbitalSpeed;
                    count = bm.GetOrbitalCount();
                    extras = $" 持续: {bm.GetOrbitalDuration():F1}s  半径: {stats.orbitalRadius}";
                    break;
                case BulletManager.BulletType.Ricochet:
                    cd = bm.GetCooldownForType(BulletManager.BulletType.Ricochet);
                    dmg = Mathf.RoundToInt(stats.ricochetDamage * bm.GetDamageMultiplier());
                    spd = stats.ricochetSpeed * bm.GetSpeedMultiplier();
                    count = 1;
                    extras = $" 弹射: {bm.GetRicochetBounces()}次";
                    break;
                case BulletManager.BulletType.Shotgun:
                    cd = bm.GetCooldownForType(BulletManager.BulletType.Shotgun);
                    dmg = Mathf.RoundToInt(stats.shotgunDamage * bm.GetDamageMultiplier());
                    spd = stats.shotgunSpeed * bm.GetSpeedMultiplier();
                    count = bm.GetShotgunCount();
                    extras = $" 散射: {bm.GetShotgunSpread():F0}°";
                    break;
                case BulletManager.BulletType.Chain:
                    cd = bm.GetCooldownForType(BulletManager.BulletType.Chain);
                    dmg = Mathf.RoundToInt(stats.chainDamage * bm.GetDamageMultiplier());
                    spd = stats.chainSpeed * bm.GetSpeedMultiplier();
                    count = 1;
                    extras = $" 链跳: {bm.GetChainHops()}次  范围: {bm.GetChainRange():F1}";
                    break;
            }

            return $"{catStr} | 伤害:{dmg}  CD:{cd:F2}s  速度:{spd:F1}  数量:{count}{extras}";
        }
    }
}

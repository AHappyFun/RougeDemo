using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge
{
    public class WaveManager : MonoBehaviour
    {
        [Header("Config")]
        public WaveConfig config;

        // Runtime state
        public int CurrentWave { get; private set; } = 1;
        public int KillsThisWave { get; private set; }
        public int KillsNeeded { get; private set; } = 100;

        // Multipliers applied from upgrades (cumulative)
        public float DamageMultiplier { get; private set; } = 1f;
        public float CooldownMultiplier { get; private set; } = 1f;
        public int BulletCountBonus { get; private set; }
        public float BulletSpeedMultiplier { get; private set; } = 1f;

        // Enemy scaling per wave
        public float EnemyHPMultiplier { get; private set; } = 1f;
        public float EnemySpeedMultiplier { get; private set; } = 1f;
        public float EnemySpawnRateMultiplier { get; private set; } = 1f;
        public int EnemyMaxBonus { get; private set; }
        public int EnemyDamageBonus { get; private set; }

        // UI references (created by us)
        private GameObject upgradePanel;
        private const int ChoicesPerWave = 3;

        // Dependencies
        private BulletManager bulletManager;
        private EnemySpawner enemySpawner;
        private GameManager gameManager;
        private Canvas canvas;

        private bool isUpgrading;

        public void Init(BulletManager bm, EnemySpawner es, GameManager gm, Canvas cv)
        {
            bulletManager = bm;
            enemySpawner = es;
            gameManager = gm;
            canvas = cv;
            ApplyConfig();
            CreateUpgradePanel();
        }

        private void ApplyConfig()
        {
            if (config != null)
                KillsNeeded = config.killsPerWave;
        }

        public void OnKill()
        {
            if (isUpgrading) return;
            KillsThisWave++;
            if (KillsThisWave >= KillsNeeded)
                TriggerWaveUp();
        }

        private void TriggerWaveUp()
        {
            isUpgrading = true;
            GameManager.IsPaused = true;
            ShowUpgradeChoices();
        }

        private void ShowUpgradeChoices()
        {
            upgradePanel.SetActive(true);

            // Pick 3 random upgrades
            var pool = config != null && config.upgrades.Count > 0
                ? config.upgrades
                : GetDefaultUpgrades();

            var shuffled = new List<UpgradeDef>(pool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
            }

            int choiceCount = Mathf.Min(ChoicesPerWave, shuffled.Count);
            var buttons = upgradePanel.GetComponentsInChildren<Button>();
            var texts = upgradePanel.GetComponentsInChildren<Text>();

            for (int i = 0; i < buttons.Length && i < choiceCount; i++)
            {
                var def = shuffled[i];
                buttons[i].gameObject.SetActive(true);
                buttons[i].onClick.RemoveAllListeners();
                var capturedDef = def;
                buttons[i].onClick.AddListener(() => OnUpgradeChosen(capturedDef));

                // Find text children of this button
                var childTexts = buttons[i].GetComponentsInChildren<Text>();
                if (childTexts.Length >= 2)
                {
                    childTexts[0].text = def.displayName;
                    childTexts[0].color = def.color;
                    childTexts[1].text = def.desc;
                }
            }

            for (int i = choiceCount; i < buttons.Length; i++)
                buttons[i].gameObject.SetActive(false);
        }

        private void OnUpgradeChosen(UpgradeDef def)
        {
            switch (def.type)
            {
                case UpgradeType.DamageUp:
                    DamageMultiplier *= 1f + def.value / 100f;
                    break;
                case UpgradeType.CooldownDown:
                    CooldownMultiplier *= 1f - def.value / 100f;
                    break;
                case UpgradeType.BulletCount:
                    BulletCountBonus += (int)def.value;
                    break;
                case UpgradeType.Heal:
                    var ph = FindObjectOfType<PlayerHealth>();
                    if (ph != null) ph.Heal(def.value / 100f);
                    break;
                case UpgradeType.BulletSpeed:
                    BulletSpeedMultiplier *= 1f + def.value / 100f;
                    break;
                case UpgradeType.RicochetBounce:
                    bulletManager?.AddRicochetBounce((int)def.value);
                    break;
                case UpgradeType.OrbitalSpeed:
                    bulletManager?.AddOrbitalSpeed(def.value);
                    break;
                case UpgradeType.ChainHops:
                    bulletManager?.AddChainHops((int)def.value);
                    break;
                case UpgradeType.ChainRange:
                    bulletManager?.AddChainRange(def.value);
                    break;
                case UpgradeType.ShotgunSpread:
                    bulletManager?.AddShotgunSpreadReduction(def.value);
                    break;
            }

            upgradePanel.SetActive(false);
            AdvanceWave();
            // Resume gameplay
            isUpgrading = false;
            GameManager.IsPaused = false;
        }

        private void AdvanceWave()
        {
            CurrentWave++;
            KillsThisWave = 0;

            if (config != null)
            {
                KillsNeeded = config.killsPerWave + config.killsPerWaveGrowth * (CurrentWave - 1);
                EnemyHPMultiplier = 1f + config.hpScale * (CurrentWave - 1);
                EnemySpeedMultiplier = 1f + config.speedScale * (CurrentWave - 1);
                EnemySpawnRateMultiplier = Mathf.Pow(1f - config.spawnRateScale, CurrentWave - 1);
                EnemyMaxBonus = config.maxEnemiesPerWave * (CurrentWave - 1);
                EnemyDamageBonus = config.damagePerWave * (CurrentWave - 1);
            }

            // Push scaling to spawner
            if (enemySpawner != null)
                enemySpawner.ApplyWaveScaling(EnemyHPMultiplier, EnemySpeedMultiplier,
                    EnemySpawnRateMultiplier, EnemyMaxBonus, EnemyDamageBonus);

            // Push upgrade multipliers to bullet manager
            if (bulletManager != null)
                bulletManager.ApplyUpgrades(DamageMultiplier, CooldownMultiplier,
                    BulletCountBonus, BulletSpeedMultiplier);
        }

        private List<UpgradeDef> GetDefaultUpgrades()
        {
            return new List<UpgradeDef>
            {
                new UpgradeDef { type = UpgradeType.DamageUp,    displayName = "剑气强化", desc = "飞剑伤害 +30%",  value = 30f, color = new Color(1f, 0.3f, 0.2f) },
                new UpgradeDef { type = UpgradeType.CooldownDown, displayName = "御剑如风", desc = "攻击冷却 -20%",  value = 20f, color = new Color(0.3f, 0.6f, 1f) },
                new UpgradeDef { type = UpgradeType.BulletCount,  displayName = "万剑归宗", desc = "子弹数量 +1",    value = 1f,  color = new Color(1f, 0.7f, 0.1f) },
                new UpgradeDef { type = UpgradeType.Heal,         displayName = "灵气复苏", desc = "恢复 30% 生命",  value = 30f, color = new Color(0.2f, 1f, 0.3f) },
                new UpgradeDef { type = UpgradeType.BulletSpeed,  displayName = "流星赶月", desc = "飞剑速度 +25%",  value = 25f, color = new Color(0.8f, 0.4f, 1f) },
                new UpgradeDef { type = UpgradeType.RicochetBounce, displayName = "弹射强化", desc = "弹射飞剑反弹次数 +2", value = 2f, color = new Color(0.3f, 1f, 0.4f), targetBulletType = BulletManager.BulletType.Ricochet },
                new UpgradeDef { type = UpgradeType.OrbitalSpeed,  displayName = "旋转加速", desc = "环绕飞剑转速 +40%",   value = 40f, color = new Color(1f, 0.5f, 0f), targetBulletType = BulletManager.BulletType.Orbital },
                new UpgradeDef { type = UpgradeType.ChainHops,    displayName = "连锁增强", desc = "链式飞剑弹射次数 +1",  value = 1f, color = new Color(0.3f, 0.7f, 1f), targetBulletType = BulletManager.BulletType.Chain },
                new UpgradeDef { type = UpgradeType.ChainRange,   displayName = "链式延伸", desc = "链式飞剑弹射范围 +30%", value = 30f, color = new Color(0.3f, 0.7f, 1f), targetBulletType = BulletManager.BulletType.Chain },
                new UpgradeDef { type = UpgradeType.ShotgunSpread, displayName = "散射聚焦", desc = "散射角度 -20%",     value = 20f, color = new Color(1f, 0.3f, 0.9f), targetBulletType = BulletManager.BulletType.Shotgun },
            };
        }

        // --- Upgrade Panel UI ---

        private void CreateUpgradePanel()
        {
            if (canvas == null) return;

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 12);

            // Panel background
            var panelGO = new GameObject("UpgradePanel");
            panelGO.transform.SetParent(canvas.transform, false);
            var panelRt = panelGO.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.75f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRt = titleGO.AddComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.8f);
            titleRt.sizeDelta = new Vector2(400, 50);
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "突破！选择一项强化";
            titleText.font = font;
            titleText.fontSize = 32;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.85f, 0.3f);

            // 3 choice buttons stacked vertically
            for (int i = 0; i < 3; i++)
            {
                var btnGO = new GameObject("UpgradeBtn" + i);
                btnGO.transform.SetParent(panelGO.transform, false);
                var btnRt = btnGO.AddComponent<RectTransform>();
                btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                btnRt.sizeDelta = new Vector2(350, 70);
                btnRt.anchoredPosition = new Vector2(0, 60 - i * 85);
                var btnImg = btnGO.AddComponent<Image>();
                btnImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
                var btn = btnGO.AddComponent<Button>();

                // Name text
                var nameGO = new GameObject("Name");
                nameGO.transform.SetParent(btnGO.transform, false);
                var nameRt = nameGO.AddComponent<RectTransform>();
                nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
                nameRt.pivot = new Vector2(0.5f, 1f);
                nameRt.sizeDelta = new Vector2(330, 36);
                nameRt.anchoredPosition = new Vector2(0, -5);
                var nameText = nameGO.AddComponent<Text>();
                nameText.font = font;
                nameText.fontSize = 24;
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = Color.white;

                // Desc text
                var descGO = new GameObject("Desc");
                descGO.transform.SetParent(btnGO.transform, false);
                var descRt = descGO.AddComponent<RectTransform>();
                descRt.anchorMin = descRt.anchorMax = new Vector2(0.5f, 0f);
                descRt.pivot = new Vector2(0.5f, 0f);
                descRt.sizeDelta = new Vector2(330, 28);
                descRt.anchoredPosition = new Vector2(0, 5);
                var descText = descGO.AddComponent<Text>();
                descText.font = font;
                descText.fontSize = 16;
                descText.alignment = TextAnchor.MiddleCenter;
                descText.color = new Color(0.8f, 0.8f, 0.8f);
            }

            upgradePanel = panelGO;
            upgradePanel.SetActive(false);
        }
    }
}

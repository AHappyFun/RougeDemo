using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Rouge
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Configs")]
        public GameConfig gameConfig;
        public BulletConfig bulletConfig;
        public WaveConfig waveConfig;
        public UpgradeConfig upgradeConfig;

        public float orthoSize = 8f;
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f);

        private void Awake()
        {
            // Load configs from Resources if Inspector refs are missing
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("GameConfig");
            if (bulletConfig == null) bulletConfig = Resources.Load<BulletConfig>("BulletConfig");
            if (waveConfig == null) waveConfig = Resources.Load<WaveConfig>("WaveConfig");
            if (upgradeConfig == null) upgradeConfig = Resources.Load<UpgradeConfig>("UpgradeConfig");

            if (gameConfig != null)
            {
                orthoSize = gameConfig.cameraOrthoSize;
                backgroundColor = gameConfig.backgroundColor;
            }
            SetupCamera();
            if (GameObject.Find("Floor") == null) SceneBuilder.Build();

            var bulletsRoot = new GameObject("Bullets").transform;
            var enemiesRoot = new GameObject("Enemies").transform;
            new GameObject("VFX");

            var player = CreatePlayer();
            // PlayerMovement 等组件已在 Prefab 中
            var stats = player.gameObject.GetComponent<PlayerStats>();
            SetupCamera3D(player);
            var bulletMgr = CreateBulletManager(player, bulletsRoot, stats);
            bulletMgr.bulletParent = bulletsRoot;
            var enemySpawner = CreateEnemySpawner(enemiesRoot);
            var gameMgr = CreateGameManager(bulletMgr, enemySpawner);
            var canvas = CreateUI(gameMgr);
            var waveMgr = CreateWaveManager(bulletMgr, enemySpawner, gameMgr, canvas);
            gameMgr.waveManager = waveMgr;
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.backgroundColor = backgroundColor;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        private void SetupCamera3D(Transform player)
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = false;
            cam.fieldOfView = 45f;
            cam.transform.position = new Vector3(player.position.x, 18f, player.position.z);
            cam.transform.rotation = Quaternion.Euler(70, 0, 0);

            // Enable URP post-processing
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;

            var cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.target = player;
            cf.height = 18f;
        }

        private Transform CreatePlayer()
        {
            var go = MeshGenerator.CreatePlayer(Color.white, 1.2f);
            go.transform.position = new Vector3(0, 0.5f, 0);

            var stats = go.GetComponent<PlayerStats>();
            if (stats == null) stats = go.AddComponent<PlayerStats>();
            stats.maxHP = gameConfig != null ? gameConfig.playerMaxHP : 100;
            stats.currentHP = stats.maxHP;

            // 从 BulletConfig 同步子弹属性到 PlayerStats（Inspector 可查看/调试）
            if (bulletConfig != null)
            {
                stats.straightCategory = bulletConfig.straight.category;
                stats.straightDamage = bulletConfig.straight.damage;
                stats.straightCooldown = bulletConfig.straight.cooldown;
                stats.straightSpeed = bulletConfig.straight.speed;
                stats.straightColor = bulletConfig.straight.color;

                stats.orbitalCategory = bulletConfig.orbital.category;
                stats.orbitalDamage = bulletConfig.orbital.damage;
                stats.orbitalCooldown = bulletConfig.orbital.cooldown;
                stats.orbitalDuration = bulletConfig.orbital.duration;
                stats.orbitalSpeed = bulletConfig.orbital.speed;
                stats.orbitalCount = bulletConfig.orbital.count;
                stats.orbitalRadius = bulletConfig.orbital.radius;
                stats.orbitalColor = bulletConfig.orbital.color;

                stats.ricochetCategory = bulletConfig.ricochet.category;
                stats.ricochetDamage = bulletConfig.ricochet.damage;
                stats.ricochetCooldown = bulletConfig.ricochet.cooldown;
                stats.ricochetSpeed = bulletConfig.ricochet.speed;
                stats.ricochetBounces = bulletConfig.ricochet.maxBounces;
                stats.ricochetColor = bulletConfig.ricochet.color;

                stats.shotgunCategory = bulletConfig.shotgun.category;
                stats.shotgunDamage = bulletConfig.shotgun.damage;
                stats.shotgunCooldown = bulletConfig.shotgun.cooldown;
                stats.shotgunSpeed = bulletConfig.shotgun.speed;
                stats.shotgunCount = bulletConfig.shotgun.count;
                stats.shotgunSpread = bulletConfig.shotgun.spreadAngle;
                stats.shotgunColor = bulletConfig.shotgun.color;

                stats.chainCategory = bulletConfig.chain.category;
                stats.chainDamage = bulletConfig.chain.damage;
                stats.chainCooldown = bulletConfig.chain.cooldown;
                stats.chainSpeed = bulletConfig.chain.speed;
                stats.chainHops = bulletConfig.chain.maxHops;
                stats.chainRange = bulletConfig.chain.chainRange;
                stats.chainColor = bulletConfig.chain.color;
            }

            var ph = go.AddComponent<PlayerHealth>();
            ph.stats = stats;

            return go.transform;
        }

        private BulletManager CreateBulletManager(Transform player, Transform parent, PlayerStats stats)
        {
            var go = new GameObject("BulletManager");
            go.transform.SetParent(parent);
            var bm = go.AddComponent<BulletManager>();
            bm.playerTransform = player;
            bm.bulletConfig = bulletConfig;
            bm.InitStats(stats);
            return bm;
        }

        private EnemySpawner CreateEnemySpawner(Transform parent)
        {
            var go = new GameObject("EnemySpawner");
            go.transform.SetParent(parent);
            var es = go.AddComponent<EnemySpawner>();
            es.gameConfig = gameConfig;
            return es;
        }

        private GameManager CreateGameManager(BulletManager bm, EnemySpawner es)
        {
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();
            gm.bulletManager = bm;
            gm.enemySpawner = es;
            return gm;
        }

        private WaveManager CreateWaveManager(BulletManager bm, EnemySpawner es, GameManager gm, Canvas canvas)
        {
            var go = new GameObject("WaveManager");
            var wm = go.AddComponent<WaveManager>();
            wm.waveConfig = waveConfig;
            wm.upgradeConfig = upgradeConfig;
            wm.Init(bm, es, gm, canvas);
            return wm;
        }

        private Canvas CreateUI(GameManager gm)
        {
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 12);

            // Stats (top-right)
            gm.statsText = MakeText("StatsText", canvasGO.transform,
                "Enemies: 0  Kills: 0", font, 18, TextAnchor.UpperRight,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-15, -10));

            // Player HP (top-left)
            gm.playerHPText = MakeText("PlayerHPText", canvasGO.transform,
                "HP: 100%", font, 22, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(15, -10));

            // Cooldown bar (below HP text)
            var barBG = MakeImage("CooldownBarBG", canvasGO.transform, new Color(0.2f, 0.2f, 0.2f));
            var barBGRT = barBG.GetComponent<RectTransform>();
            barBGRT.anchorMin = barBGRT.anchorMax = new Vector2(0.5f, 1f);
            barBGRT.pivot = new Vector2(0.5f, 1f);
            barBGRT.anchoredPosition = new Vector2(0, -70);
            barBGRT.sizeDelta = new Vector2(180, 12);

            var barFill = MakeImage("CooldownBarFill", barBG.transform, Color.green);
            var barFillRT = barFill.GetComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero;
            barFillRT.anchorMax = Vector2.one;
            barFillRT.pivot = new Vector2(0, 0.5f);
            barFillRT.anchoredPosition = Vector2.zero;
            barFillRT.sizeDelta = Vector2.zero;
            var fill = barFill.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            gm.cooldownBar = fill;

            // FPS
            gm.fpsText = MakeText("FPSText", canvasGO.transform,
                "FPS: 0", font, 16, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 10));

            // === GameOver Panel ===
            var panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f);
            panelImg.raycastTarget = true;

            var gameOverLabel = new GameObject("GameOverText");
            gameOverLabel.transform.SetParent(panel.transform, false);
            var labelRt = gameOverLabel.AddComponent<RectTransform>();
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.sizeDelta = new Vector2(500, 200);
            var labelText = gameOverLabel.AddComponent<Text>();
            labelText.text = "Game Over\nKills: 0\nPress R to Restart";
            labelText.font = font;
            labelText.fontSize = 36;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.raycastTarget = false;

            gm.gameOverPanel = panel;
            gm.gameOverText = labelText;

            return canvas;
        }

        private static Text MakeText(string name, Transform parent,
            string text, Font font, int size, TextAnchor align,
            Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 40);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        private static GameObject MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }
    }
}

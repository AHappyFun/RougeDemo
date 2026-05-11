using UnityEngine;
using UnityEngine.UI;

namespace Rouge
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Configs")]
        public GameConfig config;
        public WaveConfig waveConfig;

        public float orthoSize = 8f;
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f);

        private void Awake()
        {
            // Load configs from Resources if Inspector refs are missing
            if (config == null) config = Resources.Load<GameConfig>("GameConfig");
            if (waveConfig == null) waveConfig = Resources.Load<WaveConfig>("WaveConfig");

            if (config != null)
            {
                orthoSize = config.cameraOrthoSize;
                backgroundColor = config.backgroundColor;
            }
            SetupCamera();
            // Build scene if not loaded from .unity file
            if (GameObject.Find("Floor") == null) SceneBuilder.Build();
            var player = CreatePlayer();
            player.gameObject.AddComponent<PlayerMovement>();
            SetupCamera3D(player);
            var bulletMgr = CreateBulletManager(player);
            var enemySpawner = CreateEnemySpawner();
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
            var cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.target = player;
            cf.height = 18f;
        }

        private Transform CreatePlayer()
        {
            var go = MeshGenerator.CreatePlayer(Color.white, 1.2f);
            go.transform.position = new Vector3(0, 0.5f, 0);

            var ph = go.AddComponent<PlayerHealth>();
            ph.maxHP = config != null ? config.playerMaxHP : 100;

            return go.transform;
        }

        private BulletManager CreateBulletManager(Transform player)
        {
            var go = new GameObject("BulletManager");
            var bm = go.AddComponent<BulletManager>();
            bm.playerTransform = player;
            bm.config = config;
            return bm;
        }

        private EnemySpawner CreateEnemySpawner()
        {
            var go = new GameObject("EnemySpawner");
            var es = go.AddComponent<EnemySpawner>();
            es.config = config;
            es.contactDamage = config != null ? config.enemyContactDamage : 10;
            return es;
        }

        private GameManager CreateGameManager(BulletManager bm, EnemySpawner es)
        {
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();
            gm.bulletManager = bm;
            gm.enemySpawner = es;
            gm.config = config;
            return gm;
        }

        private WaveManager CreateWaveManager(BulletManager bm, EnemySpawner es, GameManager gm, Canvas canvas)
        {
            var go = new GameObject("WaveManager");
            var wm = go.AddComponent<WaveManager>();
            wm.config = waveConfig;
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

            // Bullet type (top-left)
            gm.bulletTypeText = MakeText("BulletTypeText", canvasGO.transform,
                "1:Straight[ON]  2:Orbital[ON]  3:Ricochet[ON]  4:Shotgun[ON]  5:Chain[ON]", font, 18, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10));

            // Stats (top-right)
            gm.statsText = MakeText("StatsText", canvasGO.transform,
                "Enemies: 0  Kills: 0", font, 20, TextAnchor.UpperRight,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10));

            // Player HP (top-center)
            gm.playerHPText = MakeText("PlayerHPText", canvasGO.transform,
                "HP: 100%", font, 22, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10));

            // Cooldown bar
            var barBG = MakeImage("CooldownBarBG", canvasGO.transform, new Color(0.2f, 0.2f, 0.2f));
            var barBGRT = barBG.GetComponent<RectTransform>();
            barBGRT.anchorMin = barBGRT.anchorMax = new Vector2(0.5f, 1f);
            barBGRT.pivot = new Vector2(0.5f, 1f);
            barBGRT.anchoredPosition = new Vector2(0, -60);
            barBGRT.sizeDelta = new Vector2(200, 16);

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

            // Help text
            MakeText("HelpText", canvasGO.transform,
                "Press 1-5 to toggle bullet types", font, 16, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10));

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

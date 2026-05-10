using UnityEngine;
using UnityEngine.UI;

namespace Rouge
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>When true, all gameplay entities (bullets, enemies) freeze.</summary>
        public static bool IsPaused { get; set; }

        public GameConfig config;
        public BulletManager bulletManager;
        public EnemySpawner enemySpawner;
        public WaveManager waveManager;

        [Header("UI (assigned by GameBootstrap)")]
        public Text bulletTypeText;
        public Text statsText;
        public Image cooldownBar;
        public Text fpsText;
        public Text playerHPText;
        public Text waveText;
        public GameObject gameOverPanel;
        public Text gameOverText;

        public BulletManager.BulletType CurrentBulletType
        {
            get
            {
                if (bulletManager == null) return BulletManager.BulletType.Straight;
                for (int i = 0; i < BulletManager.TypeCount; i++)
                {
                    var t = (BulletManager.BulletType)i;
                    if (bulletManager.IsBulletTypeActive(t)) return t;
                }
                return BulletManager.BulletType.Straight;
            }
        }

        private int killCount;
        private float[] fpsSamples = new float[30];
        private int fpsIndex;
        private bool isGameOver;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (bulletManager == null) bulletManager = FindObjectOfType<BulletManager>();
            if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>();
            UpdateBulletTypeUI();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void Update()
        {
            if (isGameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                return;
            }

            HandleInput();
            UpdateUI();
            UpdateFPS();
        }

        private void HandleInput()
        {
            if (bulletManager == null) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleBulletType(BulletManager.BulletType.Straight);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleBulletType(BulletManager.BulletType.Orbital);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleBulletType(BulletManager.BulletType.Ricochet);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleBulletType(BulletManager.BulletType.Shotgun);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleBulletType(BulletManager.BulletType.Chain);
        }

        private void ToggleBulletType(BulletManager.BulletType type)
        {
            bulletManager.ToggleBulletType(type);
            UpdateBulletTypeUI();
        }

        private static readonly string[] BulletTypeLabels = { "Straight", "Orbital", "Ricochet", "Shotgun", "Chain" };

        private void UpdateBulletTypeUI()
        {
            if (bulletTypeText != null && bulletManager != null)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < BulletManager.TypeCount; i++)
                {
                    var t = (BulletManager.BulletType)i;
                    bool on = bulletManager.IsBulletTypeActive(t);
                    if (i > 0) sb.Append("  ");
                    sb.Append(i + 1);
                    sb.Append(':');
                    sb.Append(BulletTypeLabels[i]);
                    sb.Append(on ? "[ON]" : "[OFF]");
                }
                bulletTypeText.text = sb.ToString();
            }
        }

        private void UpdateUI()
        {
            if (statsText != null)
            {
                int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
                int wave = waveManager != null ? waveManager.CurrentWave : 1;
                int killsNeeded = waveManager != null ? waveManager.KillsNeeded : 0;
                int killsWave = waveManager != null ? waveManager.KillsThisWave : 0;
                statsText.text = string.Format("Wave {0} | [{1}/{2}] | Enemies: {3}  Kills: {4}",
                    wave, killsWave, killsNeeded, enemyCount, killCount);
            }
            if (cooldownBar != null && bulletManager != null)
                cooldownBar.fillAmount = bulletManager.GetCooldownProgress();
            if (playerHPText != null)
            {
                var ph = FindObjectOfType<PlayerHealth>();
                if (ph != null)
                    playerHPText.text = string.Format("HP: {0:0}%", ph.HPPercent * 100f);
            }
        }

        private void UpdateFPS()
        {
            if (fpsText == null) return;
            fpsSamples[fpsIndex] = 1f / Time.unscaledDeltaTime;
            fpsIndex = (fpsIndex + 1) % fpsSamples.Length;
            float avg = 0f;
            foreach (float s in fpsSamples) avg += s;
            avg /= fpsSamples.Length;
            fpsText.text = string.Format("FPS: {0:0}", avg);
        }

        public void AddKill()
        {
            killCount++;
            waveManager?.OnKill();
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (gameOverText != null)
                    gameOverText.text = string.Format("Game Over\nKills: {0}\nPress R to Restart", killCount);
            }

            // Stop spawning
            if (enemySpawner != null) enemySpawner.enabled = false;
            if (bulletManager != null) bulletManager.enabled = false;
        }
    }
}

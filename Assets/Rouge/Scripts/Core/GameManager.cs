using UnityEngine;
using UnityEngine.UI;

namespace Rouge
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static bool IsPaused { get; set; }

        public BulletManager bulletManager;
        public EnemySpawner enemySpawner;
        public WaveManager waveManager;

        [Header("UI (assigned by GameBootstrap)")]
        public Text statsText;
        public Image cooldownBar;
        public Text fpsText;
        public Text playerHPText;
        public GameObject gameOverPanel;
        public Text gameOverText;
        private int killCount;
        private int totalXp;
        private float[] fpsSamples = new float[30];
        private int fpsIndex;
        private bool isGameOver;
        private PlayerStats playerStats;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (bulletManager == null) bulletManager = FindObjectOfType<BulletManager>();
            if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>();
            playerStats = FindObjectOfType<PlayerStats>();
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

            UpdateUI();
            UpdateFPS();
        }

        public void AddKill(int xp)
        {
            killCount++;
            totalXp += xp;
            waveManager?.OnXpGain(xp);
        }

        private void UpdateUI()
        {
            if (statsText != null)
            {
                int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
                int wave = waveManager != null ? waveManager.CurrentWave : 1;
                int xpNeeded = waveManager != null ? waveManager.XpNeeded : 0;
                int xpWave = waveManager != null ? waveManager.XpThisWave : 0;
                statsText.text = string.Format("Wave {0} | XP: {1}/{2} | Enemies: {3}  Kills: {4}",
                    wave, xpWave, xpNeeded, enemyCount, killCount);
            }
            if (cooldownBar != null && bulletManager != null)
                cooldownBar.fillAmount = bulletManager.GetCooldownProgress();
            if (playerHPText != null && playerStats != null)
                playerHPText.text = string.Format("HP: {0}/{1} ({2:0}%)", playerStats.currentHP, playerStats.maxHP, playerStats.HPPercent * 100f);
        }

        private void UpdateFPS()
        {
            if (fpsText == null) return;
            fpsSamples[fpsIndex] = 1f / Time.unscaledDeltaTime;
            fpsIndex = (fpsIndex + 1) % fpsSamples.Length;
            float avg = 0f;
            foreach (float s in fpsSamples) avg += s;
            fpsText.text = "FPS: " + Mathf.RoundToInt(avg / fpsSamples.Length);
        }

        public void TriggerGameOver()
        {
            UpdateUI(); // 死亡前刷新 UI，确保显示 0/100 而不是卡在上一次的值
            isGameOver = true;
            if (enemySpawner != null) enemySpawner.enabled = false;
            if (bulletManager != null) bulletManager.enabled = false;
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (gameOverText != null)
                gameOverText.text = "Game Over\nKills: " + killCount + "\nPress R to Restart";
        }
    }
}

using UnityEngine;
using TMPro;
using Platformer.Mechanics;

namespace Platformer.UI
{
    /// <summary>
    /// manages the main game HUD (health, timer, score).
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("UI References")]
        /// <summary>
        /// reference to the health UI component (auto-found if empty).
        /// </summary>
        public HealthUI healthUI;

        /// <summary>
        /// text display for the timer (assign in inspector).
        /// </summary>
        public TextMeshProUGUI timerText;

        /// <summary>
        /// text display for the score (assign in inspector).
        /// </summary>
        public TextMeshProUGUI scoreText;

        [Header("Positioning")]
        /// <summary>
        /// distance from the top of the screen in pixels.
        /// </summary>
        public float topPadding = 20f;

        /// <summary>
        /// distance from the sides of the screen in pixels.
        /// </summary>
        public float sidePadding = 40f;

        [Header("Game State")]
        private int currentScore = 0;
        private float elapsedTime = 0f;

        void Awake()
        {
            ValidateSetup();
        }

        void Start()
        {
            // find health UI if not assigned
            if (healthUI == null)
            {
                healthUI = FindFirstObjectByType<HealthUI>();
            }

            // initialize displays
            UpdateScoreDisplay();
            UpdateTimerDisplay();
        }

        void Update()
        {
            // only count time when game is not paused
            if (Time.timeScale > 0)
            {
                elapsedTime += Time.deltaTime;
                UpdateTimerDisplay();
            }
        }

        /// <summary>
        /// add points to the current score.
        /// </summary>
        /// <param name="amount">amount of points to add</param>
        public void AddScore(int amount)
        {
            currentScore += amount;
            UpdateScoreDisplay();
        }

        /// <summary>
        /// reset score to zero.
        /// </summary>
        public void ResetScore()
        {
            currentScore = 0;
            UpdateScoreDisplay();
        }

        /// <summary>
        /// reset timer to zero.
        /// </summary>
        public void ResetTimer()
        {
            elapsedTime = 0f;
            UpdateTimerDisplay();
        }

        /// <summary>
        /// update the timer text display.
        /// </summary>
        void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(elapsedTime / 60f);
                int seconds = Mathf.FloorToInt(elapsedTime % 60f);
                timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
            }
        }

        /// <summary>
        /// update the score text display.
        /// </summary>
        void UpdateScoreDisplay()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {currentScore}";
            }
        }

        /// <summary>
        /// validate that all UI references are assigned.
        /// </summary>
        void ValidateSetup()
        {
            // validation checks could be added here if needed
        }
    }
}

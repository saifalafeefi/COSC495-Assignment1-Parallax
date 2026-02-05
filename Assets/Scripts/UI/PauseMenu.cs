using UnityEngine;
using UnityEngine.UI;

namespace Platformer.UI
{
    /// <summary>
    /// handles pause menu toggling and button functionality.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI References")]
        /// <summary>
        /// the root game object of the pause menu panel.
        /// </summary>
        public GameObject pauseMenuPanel;

        /// <summary>
        /// button to resume the game.
        /// </summary>
        public Button resumeButton;

        /// <summary>
        /// button to restart the level (placeholder).
        /// </summary>
        public Button restartButton;

        /// <summary>
        /// button to quit the game (placeholder).
        /// </summary>
        public Button quitButton;

        private bool isPaused = false;

        void Awake()
        {
            ValidateSetup();
            WireUpButtons();

            // ensure menu starts hidden
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
        }

        void Update()
        {
            // listen for ESC key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        /// <summary>
        /// toggle pause menu on/off.
        /// </summary>
        public void TogglePause()
        {
            isPaused = !isPaused;

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(isPaused);
            }

            // freeze/unfreeze game
            Time.timeScale = isPaused ? 0f : 1f;
        }

        /// <summary>
        /// resume the game (close pause menu).
        /// </summary>
        public void Resume()
        {
            if (isPaused)
            {
                TogglePause();
            }
        }

        /// <summary>
        /// restart the level (placeholder).
        /// </summary>
        public void Restart()
        {
            // TODO: implement level restart logic
        }

        /// <summary>
        /// quit the game (placeholder).
        /// </summary>
        public void Quit()
        {
            // TODO: implement quit logic
        }

        /// <summary>
        /// wire up button click events.
        /// </summary>
        void WireUpButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(Resume);
            }
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(Quit);
            }
        }

        /// <summary>
        /// validate that all references are assigned.
        /// </summary>
        void ValidateSetup()
        {
            // validation checks could be added here if needed
        }
    }
}

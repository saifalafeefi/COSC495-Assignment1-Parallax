using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Platformer.Mechanics;

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

        /// <summary>
        /// check if the game is currently paused (used by other systems).
        /// </summary>
        public static bool IsPaused { get; private set; } = false;

        /// <summary>
        /// frame number when the game was last unpaused (used by KinematicObject to skip catch-up physics).
        /// </summary>
        public static int LastUnpauseFrame { get; private set; } = -2;
        /// <summary>
        /// how many rendered frames to skip FixedUpdate after unpause (prevents burst simulation).
        /// </summary>
        public static int FixedUpdateSkipFramesAfterUnpause = 2;

        private float storedTimeScale = 1f;
        private AnimatorUpdateMode storedAnimatorUpdateMode;
        private KinematicObject storedKinematicObject;
        private float storedMaxDeltaTime = 0.3333333f;
        private Coroutine restoreMaxDeltaTimeCoroutine;

        void Awake()
        {
            // reset static state (survives domain reload / scene reload)
            IsPaused = false;
            LastUnpauseFrame = -2;
            isPaused = false;

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
            IsPaused = isPaused; // update static property

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(isPaused);
            }

            var player = FindFirstObjectByType<Platformer.Mechanics.PlayerController>();

            if (isPaused)
            {
                // store current timeScale (could be 1.0 or time vial's slow speed)
                storedTimeScale = Time.timeScale;
                storedMaxDeltaTime = Time.maximumDeltaTime;

                // freeze game
                Time.timeScale = 0f;

                // if time vial is active, freeze player animator so animations don't play during pause
                // (UnscaledTime mode ignores timeScale=0, so we temporarily switch to Normal)
                if (player != null && player.HasTimeSlowActive)
                {
                    storedAnimatorUpdateMode = player.animator.updateMode;
                    player.animator.updateMode = AnimatorUpdateMode.Normal;
                }

                // freeze player kinematics immediately to avoid any drift
                if (player != null)
                {
                    storedKinematicObject = player.GetComponent<Platformer.Mechanics.KinematicObject>();
                    if (storedKinematicObject != null)
                    {
                        storedKinematicObject.Freeze();
                    }
                }

            }
            else
            {
                // mark unpause frame so KinematicObject can skip catch-up FixedUpdate calls
                LastUnpauseFrame = Time.frameCount;

                // unfreeze game - restore to stored timeScale (could be 1f or time vial's slow speed)
                Time.timeScale = storedTimeScale;
                // prevent large fixed-step bursts on the first unpaused frame
                Time.maximumDeltaTime = Time.fixedDeltaTime;

                // restore player animator mode if time vial was active
                if (player != null && player.HasTimeSlowActive)
                {
                    player.animator.updateMode = storedAnimatorUpdateMode;
                }

                // unfreeze player kinematics (restore exact pre-pause state)
                if (storedKinematicObject != null)
                {
                    storedKinematicObject.Unfreeze();
                    storedKinematicObject = null;
                }

                if (restoreMaxDeltaTimeCoroutine != null)
                {
                    StopCoroutine(restoreMaxDeltaTimeCoroutine);
                }
                restoreMaxDeltaTimeCoroutine = StartCoroutine(RestoreMaxDeltaTimeNextFrame());
            }
        }

        private IEnumerator RestoreMaxDeltaTimeNextFrame()
        {
            yield return null;
            Time.maximumDeltaTime = storedMaxDeltaTime;
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
        /// restart the current scene (fresh start).
        /// </summary>
        public void Restart()
        {
            // unpause game before reloading
            Time.timeScale = 1f;

            // reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// quit the game.
        /// </summary>
        public void Quit()
        {
            // unpause before quitting
            Time.timeScale = 1f;

#if UNITY_EDITOR
            // stop play mode in editor
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // quit application in build
            Application.Quit();
#endif
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

using System.Collections;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// time vial powerup that slows down time with ghost trail effect.
    /// </summary>
    public class TimeVial : MonoBehaviour
    {
        [Header("Time Slow Settings")]
        /// <summary>
        /// how long the time slow lasts (in seconds, real time).
        /// </summary>
        public float slowDuration = 5f;
        /// <summary>
        /// time scale multiplier (0.5 = half speed, 0.25 = quarter speed).
        /// </summary>
        public float timeScale = 0.5f;

        [Header("Visual Feedback")]
        /// <summary>
        /// enable purple tint on player sprite during slow.
        /// </summary>
        public bool enableTint = true;
        /// <summary>
        /// purple tint color to apply to player sprite.
        /// </summary>
        public Color tintColor = new Color(0.7f, 0.3f, 1f, 1f); // purple
        /// <summary>
        /// how many seconds before slow ends to start flashing warning.
        /// </summary>
        public float warningDuration = 2f;
        /// <summary>
        /// how fast the tint flashes during warning (flashes per second).
        /// </summary>
        public float warningFlashSpeed = 3f;

        [Header("Bloom Effect")]
        /// <summary>
        /// enable bloom tint flash on collection.
        /// </summary>
        public bool enableBloomTint = true;
        /// <summary>
        /// bloom tint color (applied to camera bloom).
        /// </summary>
        public Color bloomTintColor = new Color(0.7f, 0.3f, 1f, 1f); // purple
        /// <summary>
        /// how long the bloom tint stays before fading out (in seconds, real time).
        /// </summary>
        public float bloomTintDuration = 2f;

        [Header("Ghost Trail Effect")]
        /// <summary>
        /// enable ghost trail during time slow.
        /// </summary>
        public bool enableGhostTrail = true;

        [Header("Audio")]
        /// <summary>
        /// sound effect to play when collected.
        /// </summary>
        public AudioClip collectSound;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // check if player collected the vial
            var player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // play collection sound
                if (collectSound != null && player.audioSource != null)
                {
                    player.audioSource.PlayOneShot(collectSound);
                }

                // apply bloom tint effect (hold for full slow duration, then fade)
                if (enableBloomTint)
                {
                    var bloomController = FindFirstObjectByType<BloomTintController>();
                    if (bloomController != null)
                    {
                        bloomController.ApplyTintWithHold(bloomTintColor, slowDuration);
                    }
                }

                // start the time slow coroutine on the PLAYER (not this object!)
                player.StartCoroutine(ApplyTimeSlow(player));

                // destroy the vial AFTER starting the coroutine on player
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// apply time slow effect with visual feedback.
        /// </summary>
        private IEnumerator ApplyTimeSlow(PlayerController player)
        {
            // generate unique ID for this powerup instance
            string powerupID = "time_" + System.Guid.NewGuid().ToString();

            // store original values
            float originalTimeScale = Time.timeScale;
            PowerupColorManager colorManager = player.GetComponent<PowerupColorManager>();
            GhostTrail ghostTrail = player.GetComponent<GhostTrail>();
            CameraBackgroundController cameraBackgroundController = FindFirstObjectByType<CameraBackgroundController>();

            if (colorManager == null)
            {
                Debug.LogError("[TIME VIAL] No PowerupColorManager on player!");
                yield break;
            }

            // mark time slow as active (for i-frame piercing and death reset)
            player.HasTimeSlowActive = true;

            // apply time slow to world
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // make player immune to time slow (use unscaled time for movement)
            player.useUnscaledTime = true;

            // use UnscaledTime mode so transitions happen in REAL-TIME, not scaled time!
            // DON'T touch animator.speed - it's already correct (1x or 2x from speed boost)
            player.animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            // enable ghost trail effect
            if (enableGhostTrail && ghostTrail != null)
            {
                ghostTrail.EnableTrail();
            }

            // add color to blend (player sprite)
            if (enableTint)
            {
                colorManager.AddColor(powerupID, tintColor);
            }

            // add color to camera background blend
            if (enableBloomTint && cameraBackgroundController != null)
            {
                cameraBackgroundController.AddColor(powerupID, bloomTintColor);
            }

            // wait for normal duration (slow time - warning time)
            // use unscaledDeltaTime since we're slowing time
            float normalDuration = Mathf.Max(0, slowDuration - warningDuration);
            float elapsed = 0f;
            while (elapsed < normalDuration)
            {
                // early exit if player died (ResetVisualState already cleaned up)
                if (!player.HasTimeSlowActive)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // warning phase - flash the tint to indicate slow ending soon
            if (enableTint && warningDuration > 0)
            {
                elapsed = 0f;
                while (elapsed < warningDuration)
                {
                    // early exit if player died
                    if (!player.HasTimeSlowActive)
                    {
                        yield break;
                    }

                    elapsed += Time.unscaledDeltaTime;

                    // recalculate colors every frame (other powerups might finish during our warning!)
                    Color currentBlend = colorManager.GetCurrentBlend();
                    Color blendWithoutThis = colorManager.GetBlendWithout(powerupID);

                    // calculate flash using sine wave (smooth fade in/out)
                    float flashPhase = Mathf.Sin(elapsed * warningFlashSpeed * Mathf.PI * 2f);
                    // convert from -1...1 to 0...1 range
                    float alpha = (flashPhase + 1f) / 2f;

                    // pulse from current blend to what it would be without this powerup
                    colorManager.SetSpriteColor(Color.Lerp(currentBlend, blendWithoutThis, alpha));

                    yield return null;
                }

                // restore to CURRENT blend (recalculate fresh, don't use stale snapshot!)
                colorManager.SetSpriteColor(colorManager.GetCurrentBlend());
            }
            else
            {
                // no warning phase, just wait the remaining time
                elapsed = 0f;
                while (elapsed < warningDuration)
                {
                    // early exit if player died
                    if (!player.HasTimeSlowActive)
                    {
                        yield break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // mark time slow as inactive
            player.HasTimeSlowActive = false;

            // restore original time scale
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // restore player to normal time
            player.useUnscaledTime = false;

            // restore animator to Normal mode (animator.speed stays unchanged!)
            player.animator.updateMode = AnimatorUpdateMode.Normal;

            // disable ghost trail effect
            if (enableGhostTrail && ghostTrail != null)
            {
                ghostTrail.DisableTrail();
            }

            // remove color from blend (manager will auto-update sprite)
            if (enableTint)
            {
                colorManager.RemoveColor(powerupID);
            }

            // remove color from camera background blend
            if (enableBloomTint && cameraBackgroundController != null)
            {
                cameraBackgroundController.RemoveColor(powerupID);
            }
        }
    }
}

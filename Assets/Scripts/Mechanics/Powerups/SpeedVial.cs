using System.Collections;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// speed vial powerup that temporarily increases player movement and attack speed.
    /// </summary>
    public class SpeedVial : MonoBehaviour
    {
        [Header("Speed Boost Settings")]
        /// <summary>
        /// how long the speed boost lasts (in seconds).
        /// </summary>
        public float boostDuration = 5f;
        /// <summary>
        /// multiplier for player movement speed (1.5 = 50% faster).
        /// </summary>
        public float speedMultiplier = 1.5f;
        /// <summary>
        /// multiplier for attack animation speed (1.5 = 50% faster attacks).
        /// </summary>
        public float attackSpeedMultiplier = 1.5f;

        [Header("Visual Feedback")]
        /// <summary>
        /// particle effect to spawn on player when boost is active (optional).
        /// </summary>
        public GameObject speedEffectPrefab;
        /// <summary>
        /// color tint to apply to player sprite during boost (optional).
        /// </summary>
        public Color speedTintColor = new Color(0.5f, 0.8f, 1f, 1f); // light blue tint
        /// <summary>
        /// enable sprite tint during boost.
        /// </summary>
        public bool enableSpriteTint = true;
        /// <summary>
        /// how many seconds before boost ends to start flashing warning.
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
        public Color bloomTintColor = new Color(0.5f, 0.8f, 1f, 1f); // light blue

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

                // apply bloom tint effect (hold for boost duration, then fade)
                if (enableBloomTint)
                {
                    var bloomController = FindFirstObjectByType<BloomTintController>();
                    if (bloomController != null)
                    {
                        bloomController.ApplyTintWithHold(bloomTintColor, boostDuration);
                    }
                }

                // if speed boost already active, reset duration instead of starting new coroutine
                if (player.HasSpeedBoost)
                {
                    // find the SpeedBoostState component
                    SpeedBoostState speedBoostState = player.GetComponent<SpeedBoostState>();
                    if (speedBoostState != null)
                    {
                        // reset the duration back to full
                        speedBoostState.ResetDuration(boostDuration);
                    }
                }
                else
                {
                    // start the speed boost coroutine on the PLAYER (not this object!)
                    player.StartCoroutine(ApplySpeedBoost(player));
                }

                // destroy the vial AFTER starting the coroutine on player
                Destroy(gameObject);
            }
        }

        private IEnumerator ApplySpeedBoost(PlayerController player)
        {
            // generate unique ID for this powerup instance
            string powerupID = "speed_" + System.Guid.NewGuid().ToString();

            // get or create SpeedBoostState component
            SpeedBoostState speedBoostState = player.GetComponent<SpeedBoostState>();
            if (speedBoostState == null)
            {
                speedBoostState = player.gameObject.AddComponent<SpeedBoostState>();
            }

            // initialize shared duration
            speedBoostState.remainingDuration = boostDuration;

            // store multipliers (not snapshots!)
            float speedMult = speedMultiplier;
            float animMult = attackSpeedMultiplier;
            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            PowerupColorManager colorManager = player.GetComponent<PowerupColorManager>();
            CameraBackgroundController cameraBackgroundController = FindFirstObjectByType<CameraBackgroundController>();

            if (colorManager == null)
            {
                Debug.LogError("[SPEED VIAL] No PowerupColorManager on player!");
                yield break;
            }


            // spawn particle effect if provided
            GameObject effectInstance = null;
            if (speedEffectPrefab != null)
            {
                effectInstance = Instantiate(speedEffectPrefab, player.transform);
            }

            // apply speed boost using multiplication (works with time vial!)
            player.maxSpeed *= speedMult;
            player.animator.speed *= animMult;
            player.HasSpeedBoost = true; // enable i-frame piercing

            // add color to blend (player sprite)
            if (enableSpriteTint)
            {
                colorManager.AddColor(powerupID, speedTintColor);
            }
            else
            {
                Debug.LogWarning("[SPEED VIAL] Sprite tint is DISABLED! Check 'Enable Sprite Tint' checkbox on prefab!");
            }

            // add color to camera background blend
            if (enableBloomTint && cameraBackgroundController != null)
            {
                cameraBackgroundController.AddColor(powerupID, bloomTintColor);
            }

            // wait using SHARED duration (multiple vials can reset this!)
            // normal phase (no warning yet)
            while (speedBoostState.remainingDuration > warningDuration)
            {
                speedBoostState.remainingDuration -= Time.deltaTime;
                yield return null;
            }

            // warning phase - flash the tint to indicate boost ending soon
            if (enableSpriteTint && warningDuration > 0)
            {
                while (speedBoostState.remainingDuration > 0)
                {
                    speedBoostState.remainingDuration -= Time.deltaTime;

                    // recalculate colors every frame (other powerups might finish during our warning!)
                    Color currentBlend = colorManager.GetCurrentBlend();
                    Color blendWithoutThis = colorManager.GetBlendWithout(powerupID);

                    // calculate flash using sine wave (smooth fade in/out)
                    // use remaining time for phase calculation so it stays smooth
                    float flashPhase = Mathf.Sin(speedBoostState.remainingDuration * warningFlashSpeed * Mathf.PI * 2f);
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
                while (speedBoostState.remainingDuration > 0)
                {
                    speedBoostState.remainingDuration -= Time.deltaTime;
                    yield return null;
                }
            }

            // undo multipliers (divide to remove our contribution)
            player.maxSpeed /= speedMult;
            player.animator.speed /= animMult;
            player.HasSpeedBoost = false; // disable i-frame piercing

            // remove color from blend (manager will auto-update sprite)
            if (enableSpriteTint)
            {
                colorManager.RemoveColor(powerupID);
            }

            // remove color from camera background blend
            if (enableBloomTint && cameraBackgroundController != null)
            {
                cameraBackgroundController.RemoveColor(powerupID);
            }

            // destroy particle effect
            if (effectInstance != null)
            {
                Destroy(effectInstance);
            }
        }
    }
}

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

                // start the speed boost coroutine on the PLAYER (not this object!)
                player.StartCoroutine(ApplySpeedBoost(player));

                // destroy the vial AFTER starting the coroutine on player
                Destroy(gameObject);
            }
        }

        private IEnumerator ApplySpeedBoost(PlayerController player)
        {
            // generate unique ID for this powerup instance
            string powerupID = "speed_" + System.Guid.NewGuid().ToString();

            // store original values
            float originalSpeed = player.maxSpeed;
            float originalAnimSpeed = player.animator.speed;
            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            PowerupColorManager colorManager = player.GetComponent<PowerupColorManager>();

            if (colorManager == null)
            {
                Debug.LogError("[SPEED VIAL] No PowerupColorManager on player!");
                yield break;
            }

            Debug.Log($"[SPEED VIAL] START - ID: {powerupID}");

            // spawn particle effect if provided
            GameObject effectInstance = null;
            if (speedEffectPrefab != null)
            {
                effectInstance = Instantiate(speedEffectPrefab, player.transform);
            }

            // apply speed boost
            player.maxSpeed = originalSpeed * speedMultiplier;
            player.animator.speed = originalAnimSpeed * attackSpeedMultiplier;
            player.HasSpeedBoost = true; // enable i-frame piercing

            // add color to blend
            if (enableSpriteTint)
            {
                colorManager.AddColor(powerupID, speedTintColor);
            }

            // wait for normal duration (boost time - warning time)
            float normalDuration = Mathf.Max(0, boostDuration - warningDuration);
            yield return new WaitForSeconds(normalDuration);

            // warning phase - flash the tint to indicate boost ending soon
            if (enableSpriteTint && warningDuration > 0)
            {
                Debug.Log($"[SPEED VIAL] WARNING START");

                float elapsed = 0f;
                while (elapsed < warningDuration)
                {
                    elapsed += Time.deltaTime;

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
                Debug.Log($"[SPEED VIAL] WARNING END - Restored to current blend");
            }
            else
            {
                // no warning phase, just wait the remaining time
                yield return new WaitForSeconds(warningDuration);
            }

            // restore original values
            player.maxSpeed = originalSpeed;
            player.animator.speed = originalAnimSpeed;
            player.HasSpeedBoost = false; // disable i-frame piercing

            // remove color from blend (manager will auto-update sprite)
            if (enableSpriteTint)
            {
                colorManager.RemoveColor(powerupID);
                Debug.Log($"[SPEED VIAL] END - Removed from blend");
            }

            // destroy particle effect
            if (effectInstance != null)
            {
                Destroy(effectInstance);
            }
        }
    }
}

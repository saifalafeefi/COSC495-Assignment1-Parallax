using System.Collections;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// health vial powerup that heals the player when collected.
    /// </summary>
    public class HealthVial : MonoBehaviour
    {
        [Header("Healing Settings")]
        /// <summary>
        /// how much HP to restore when collected.
        /// </summary>
        public int healAmount = 1;

        [Header("Visual Feedback")]
        /// <summary>
        /// enable green tint on player sprite after healing.
        /// </summary>
        public bool enableTint = true;
        /// <summary>
        /// green tint color to apply to player sprite.
        /// </summary>
        public Color tintColor = new Color(0.3f, 1f, 0.3f, 1f); // bright green
        /// <summary>
        /// how long the tint stays at full brightness (in seconds).
        /// </summary>
        public float tintDuration = 1f;
        /// <summary>
        /// how long the fade-out takes after tint duration (in seconds).
        /// </summary>
        public float fadeOutDuration = 1f;

        [Header("Bloom Effect")]
        /// <summary>
        /// enable bloom tint flash on collection.
        /// </summary>
        public bool enableBloomTint = true;
        /// <summary>
        /// bloom tint color (applied to camera bloom).
        /// </summary>
        public Color bloomTintColor = new Color(0.3f, 1f, 0.3f, 1f); // bright green

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
                // heal the player
                var health = player.health;
                if (health != null && health.CurrentHP < health.maxHP)
                {
                    // restore HP
                    for (int i = 0; i < healAmount; i++)
                    {
                        if (health.CurrentHP < health.maxHP)
                        {
                            health.Increment();
                        }
                    }

                    // play collection sound
                    if (collectSound != null && player.audioSource != null)
                    {
                        player.audioSource.PlayOneShot(collectSound);
                    }

                    // apply bloom tint effect (instant fade, no duration)
                    if (enableBloomTint)
                    {
                        var bloomController = FindFirstObjectByType<BloomTintController>();
                        if (bloomController != null)
                        {
                            bloomController.ApplyTint(bloomTintColor);
                        }
                    }

                    // camera background for health vial uses timed coroutine (follows sprite tint duration)
                    var cameraBackgroundController = FindFirstObjectByType<CameraBackgroundController>();

                    // apply green tint effect if enabled
                    if (enableTint)
                    {
                        player.StartCoroutine(ApplyHealTint(player, cameraBackgroundController));
                    }

                    // destroy the vial
                    Destroy(gameObject);
                }
                else if (health != null && health.CurrentHP >= health.maxHP)
                {
                    // player already at full health - don't collect
                }
            }
        }

        /// <summary>
        /// apply green tint to player sprite that fades out over time.
        /// </summary>
        private IEnumerator ApplyHealTint(PlayerController player, CameraBackgroundController cameraBackgroundController)
        {
            // generate unique ID for this powerup instance
            string powerupID = "health_" + System.Guid.NewGuid().ToString();

            PowerupColorManager colorManager = player.GetComponent<PowerupColorManager>();
            if (colorManager == null)
            {
                yield break;
            }


            // add green to blend (player sprite)
            if (enableTint)
            {
                colorManager.AddColor(powerupID, tintColor);
            }

            // add color to camera background blend (no pulsing for health vial - just fade)
            if (enableBloomTint && cameraBackgroundController != null)
            {
                cameraBackgroundController.AddColor(powerupID, bloomTintColor, shouldPulse: false);
            }

            // wait for tint duration (full brightness)
            yield return new WaitForSeconds(tintDuration);

            // gradually fade out the color contribution over time
            if (enableTint && fadeOutDuration > 0)
            {
                float elapsed = 0f;
                Color startSpriteColor = tintColor;

                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeOutDuration;

                    // fade sprite tint to white (neutral)
                    Color fadedSpriteColor = startSpriteColor;
                    fadedSpriteColor.r = Mathf.Lerp(startSpriteColor.r, 1f, t);
                    fadedSpriteColor.g = Mathf.Lerp(startSpriteColor.g, 1f, t);
                    fadedSpriteColor.b = Mathf.Lerp(startSpriteColor.b, 1f, t);

                    // update the sprite color in the dictionary
                    colorManager.UpdateColor(powerupID, fadedSpriteColor);

                    yield return null;
                }
            }

            // for camera background, just remove it instantly - no manual fade
            // (removing triggers automatic FadeToDefault which works correctly)

            // remove from blend after fade completes
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

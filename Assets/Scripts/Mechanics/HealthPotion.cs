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

                    // apply green tint effect if enabled
                    if (enableTint)
                    {
                        player.StartCoroutine(ApplyHealTint(player));
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
        private IEnumerator ApplyHealTint(PlayerController player)
        {
            // generate unique ID for this powerup instance
            string powerupID = "health_" + System.Guid.NewGuid().ToString();

            PowerupColorManager colorManager = player.GetComponent<PowerupColorManager>();
            if (colorManager == null)
            {
                Debug.LogError("[HEALTH VIAL] No PowerupColorManager on player!");
                yield break;
            }

            Debug.Log($"[HEALTH VIAL] START - ID: {powerupID}");

            // add green to blend
            if (enableTint)
            {
                colorManager.AddColor(powerupID, tintColor);
            }

            // wait for tint duration (full brightness)
            yield return new WaitForSeconds(tintDuration);

            // gradually fade out the color contribution over time
            if (enableTint && fadeOutDuration > 0)
            {
                float elapsed = 0f;
                Color startColor = tintColor;

                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeOutDuration;

                    // lerp alpha from 1 to 0 (fade out contribution)
                    Color fadedColor = startColor;
                    fadedColor.r = Mathf.Lerp(startColor.r, 1f, t); // fade to white (neutral)
                    fadedColor.g = Mathf.Lerp(startColor.g, 1f, t);
                    fadedColor.b = Mathf.Lerp(startColor.b, 1f, t);

                    // update the color in the dictionary (other pulses will see this change)
                    colorManager.UpdateColor(powerupID, fadedColor);

                    yield return null;
                }
            }

            // remove from blend after fade completes
            if (enableTint)
            {
                colorManager.RemoveColor(powerupID);
                Debug.Log($"[HEALTH VIAL] END - Removed from blend");
            }
        }
    }
}

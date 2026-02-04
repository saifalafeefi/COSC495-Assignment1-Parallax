using System.Collections;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// damage vial powerup that temporarily increases player attack damage.
    /// </summary>
    public class DamageVial : MonoBehaviour
    {
        [Header("Damage Boost Settings")]
        /// <summary>
        /// how long the damage boost lasts (in seconds).
        /// </summary>
        public float boostDuration = 5f;
        /// <summary>
        /// multiplier for player attack damage (2.0 = double damage).
        /// </summary>
        public float damageMultiplier = 2f;

        [Header("Visual Feedback")]
        /// <summary>
        /// enable red tint on player sprite during boost.
        /// </summary>
        public bool enableTint = true;
        /// <summary>
        /// red tint color to apply to player sprite.
        /// </summary>
        public Color tintColor = new Color(1f, 0.3f, 0.3f, 1f); // bright red
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
        public Color bloomTintColor = new Color(1f, 0f, 0f, 1f); // pure red

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

                // start the damage boost coroutine on the PLAYER (not this object!)
                player.StartCoroutine(ApplyDamageBoost(player));

                // destroy the vial AFTER starting the coroutine on player
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// apply damage boost to player attacks with visual feedback.
        /// </summary>
        private IEnumerator ApplyDamageBoost(PlayerController player)
        {
            // generate unique ID for this powerup instance
            string powerupID = "damage_" + System.Guid.NewGuid().ToString();

            // store original values
            int originalAttack12Damage = player.attack12Damage;
            int originalAttack3Damage = player.attack3Damage;
            int originalAttackAirDamage = player.attackAirDamage;
            int originalRangedAttackDamage = player.rangedAttackDamage;
            PowerupColorManager colorManager = player.GetComponent<PowerupColorManager>();
            CameraBackgroundController cameraBackgroundController = FindFirstObjectByType<CameraBackgroundController>();

            if (colorManager == null)
            {
                Debug.LogError("[DAMAGE VIAL] No PowerupColorManager on player!");
                yield break;
            }


            // apply damage boost (multiply all attack damage values)
            player.attack12Damage = Mathf.RoundToInt(originalAttack12Damage * damageMultiplier);
            player.attack3Damage = Mathf.RoundToInt(originalAttack3Damage * damageMultiplier);
            player.attackAirDamage = Mathf.RoundToInt(originalAttackAirDamage * damageMultiplier);
            player.rangedAttackDamage = Mathf.RoundToInt(originalRangedAttackDamage * damageMultiplier);

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

            // wait for normal duration (boost time - warning time)
            float normalDuration = Mathf.Max(0, boostDuration - warningDuration);
            yield return new WaitForSeconds(normalDuration);

            // warning phase - flash the tint to indicate boost ending soon
            if (enableTint && warningDuration > 0)
            {
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
            }
            else
            {
                // no warning phase, just wait the remaining time
                yield return new WaitForSeconds(warningDuration);
            }

            // restore original damage values
            player.attack12Damage = originalAttack12Damage;
            player.attack3Damage = originalAttack3Damage;
            player.attackAirDamage = originalAttackAirDamage;
            player.rangedAttackDamage = originalRangedAttackDamage;

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

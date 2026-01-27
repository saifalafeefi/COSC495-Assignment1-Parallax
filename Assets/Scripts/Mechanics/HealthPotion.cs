using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// health potion powerup that heals the player when collected.
    /// </summary>
    public class HealthPotion : MonoBehaviour
    {
        [Header("Healing Settings")]
        /// <summary>
        /// how much HP to restore when collected.
        /// </summary>
        public int healAmount = 1;

        /// <summary>
        /// sound effect to play when collected.
        /// </summary>
        public AudioClip collectSound;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // check if player collected the potion
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

                    // destroy the potion
                    Destroy(gameObject);
                }
                else if (health != null && health.CurrentHP >= health.maxHP)
                {
                    // player already at full health - don't collect
                }
            }
        }
    }
}

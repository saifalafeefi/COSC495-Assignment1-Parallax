using UnityEngine;
using Platformer.Core;
using Platformer.Gameplay;

namespace Platformer.Mechanics
{
    /// <summary>
    /// enemy2 projectile that moves toward player and deals damage on hit.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Enemy2Projectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private int damage;
        private float lifetime = 5f;
        private float lifeTimer = 0f;

        private Rigidbody2D rb;
        private bool hasHit = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// initialize the projectile with direction, speed, and damage.
        /// </summary>
        public void Initialize(Vector2 direction, float speed, int damage)
        {
            this.direction = direction.normalized;
            this.speed = speed;
            this.damage = damage;

            // rotate sprite to point toward player (add 180 to flip it)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);            
        }

        void Update()
        {
            // move in direction
            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            // lifetime timer
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // prevent multiple hits
            if (hasHit) return;

            // check if hit player
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                hasHit = true;

                // check if player is invincible (rolling, i-frames, or respawning)
                if (!player.IsInvincible && !player.IsRolling && !player.IsRespawning)
                {
                    // deal damage to player
                    var playerHealth = player.GetComponent<Health>();
                    if (playerHealth != null && playerHealth.IsAlive)
                    {
                        playerHealth.Decrement(damage);

                        // check if player died
                        if (!playerHealth.IsAlive)
                        {
                            Simulation.Schedule<Platformer.Gameplay.PlayerDeath>();
                        }
                        else
                        {
                            // player survived - play hurt sound
                            if (player.ouchAudio != null && player.audioSource != null)
                            {
                                player.audioSource.PlayOneShot(player.ouchAudio);
                            }

                            // activate invincibility and hurt animation
                            player.ActivateInvincibility();

                            // apply knockback
                            Vector2 knockbackDir = new Vector2(direction.x, 0.3f);
                            player.ApplyKnockback(knockbackDir, 3f);
                        }
                    }
                }

                // destroy projectile regardless (even if player is invincible)
                Destroy(gameObject);
            }
        }
    }
}

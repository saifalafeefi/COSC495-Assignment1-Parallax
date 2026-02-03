using UnityEngine;
using Platformer.Core;
using Platformer.Gameplay;

namespace Platformer.Mechanics
{
    /// <summary>
    /// enemy3 projectile that falls straight down and damages player or destroys on ground hit.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Enemy3Projectile : MonoBehaviour
    {
        private float speed;
        private int damage;
        private float lifetime = 10f; // longer lifetime since it just falls
        private float lifeTimer = 0f;

        private bool hasHit = false;

        /// <summary>
        /// initialize the projectile with fall speed and damage.
        /// </summary>
        public void Initialize(float speed, int damage)
        {
            this.speed = speed;
            this.damage = damage;
        }

        void Update()
        {
            // fall straight down
            transform.position += Vector3.down * speed * Time.deltaTime;

            // lifetime timer (auto-destroy if it somehow doesn't hit anything)
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

            // only collide with player (like Enemy2Projectile)
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                hasHit = true;

                // check if player is invincible (rolling or i-frames)
                if (!player.IsInvincible && !player.IsRolling)
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
                            Vector2 knockbackDir = new Vector2(0f, 0.5f);
                            player.ApplyKnockback(knockbackDir, 3f);
                        }
                    }
                }

                // destroy on player hit (regardless of invincibility)
                Destroy(gameObject);
            }
            // ignore everything else (vials, walls, platforms, etc.) - lifetime will destroy it
        }
    }
}

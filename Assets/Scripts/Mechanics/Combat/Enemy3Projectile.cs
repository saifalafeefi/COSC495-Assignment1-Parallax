using UnityEngine;

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
            Debug.Log($"[ENEMY3 PROJECTILE] Initialized at {transform.position}, speed={speed}, damage={damage}");
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

            // ONLY collide with player (like Enemy2Projectile)
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                hasHit = true;
                Debug.Log($"[ENEMY3 PROJECTILE] Hit player! Invincible: {player.IsInvincible}, Rolling: {player.IsRolling}");

                // check if player is invincible (rolling or i-frames)
                if (!player.IsInvincible && !player.IsRolling)
                {
                    // deal damage to player
                    var playerHealth = player.GetComponent<Health>();
                    if (playerHealth != null && playerHealth.IsAlive)
                    {
                        playerHealth.Decrement(damage);
                        Debug.Log($"[ENEMY3 PROJECTILE] Dealt {damage} damage to player");

                        // apply slight knockback
                        Vector2 knockbackDir = new Vector2(0f, 0.5f);
                        player.ApplyKnockback(knockbackDir, 3f);
                    }
                }
                else
                {
                    Debug.Log("[ENEMY3 PROJECTILE] Player is invincible, no damage dealt");
                }

                // destroy on player hit (regardless of invincibility)
                Debug.Log("[ENEMY3 PROJECTILE] Destroying projectile");
                Destroy(gameObject);
            }
            // ignore everything else (vials, walls, platforms, etc.) - lifetime will destroy it
        }
    }
}

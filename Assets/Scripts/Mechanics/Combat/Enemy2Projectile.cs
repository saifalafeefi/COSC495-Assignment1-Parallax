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
        [SerializeField] private float parryExtraRadius = 0.1f;
        [SerializeField] private int parryOverlapBufferSize = 8;

        private Rigidbody2D rb;
        private Collider2D projectileCollider;
        private bool hasHit = false;
        private bool isParried = false;
        private Collider2D[] overlapResults;
        private ContactFilter2D overlapFilter;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            projectileCollider = GetComponent<Collider2D>();
            if (parryOverlapBufferSize < 1) parryOverlapBufferSize = 8;
            overlapResults = new Collider2D[parryOverlapBufferSize];
            overlapFilter = new ContactFilter2D();
            overlapFilter.useTriggers = true;
            overlapFilter.SetLayerMask(~0);
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

            if (isParried && !hasHit)
            {
                CheckParriedOverlap();
            }

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
                if (isParried)
                {
                    return; // parried projectiles ignore the player
                }

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
            else if (isParried)
            {
                // parried projectile can damage enemies
                TryDamageEnemy(other);
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (hasHit) return;
            if (!isParried) return;

            TryDamageEnemy(collision.collider);
        }

        private void TryDamageEnemy(Collider2D other)
        {
            if (!isParried) return;
            if (hasHit) return;

            var enemy1 = other.GetComponentInParent<enemy1>();
            var enemy2 = other.GetComponentInParent<enemy2>();
            var enemy3 = other.GetComponentInParent<enemy3>();

            if (enemy1 != null || enemy2 != null || enemy3 != null)
            {
                hasHit = true;
                Vector2 knockbackDir = new Vector2(direction.x, 0.3f);
                Debug.Log($"[Enemy2Projectile] Parried hit. enemy1={enemy1 != null}, enemy2={enemy2 != null}, enemy3={enemy3 != null}, damage={damage}");
                if (enemy1 != null) enemy1.TakeDamage(damage, knockbackDir, 2f);
                if (enemy2 != null) enemy2.TakeDamage(damage, knockbackDir, 2f);
                if (enemy3 != null) enemy3.TakeDamage(damage, knockbackDir, 2f);
                Destroy(gameObject);
            }
        }

        private void CheckParriedOverlap()
        {
            if (projectileCollider == null) return;

            float radius = GetParryRadius();
            int count;
            if (radius > 0f)
            {
                count = Physics2D.OverlapCircle(transform.position, radius, overlapFilter, overlapResults);
            }
            else
            {
                count = projectileCollider.Overlap(overlapFilter, overlapResults);
            }
            for (int i = 0; i < count; i++)
            {
                var col = overlapResults[i];
                if (col == null) continue;
                if (col.GetComponent<PlayerController>() != null) continue;
                TryDamageEnemy(col);
                if (hasHit) return;
            }
        }

        private float GetParryRadius()
        {
            if (projectileCollider == null) return 0f;
            if (parryExtraRadius < 0f) parryExtraRadius = 0f;
            Vector2 extents = projectileCollider.bounds.extents;
            float baseRadius = Mathf.Max(extents.x, extents.y);
            return baseRadius + parryExtraRadius;
        }

        public void Parry()
        {
            if (isParried) return;
            isParried = true;
            direction = -direction;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            Debug.Log($"[Enemy2Projectile] Parried. New direction={direction}");
        }
    }
}

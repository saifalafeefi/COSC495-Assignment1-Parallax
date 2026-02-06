using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// projectile that flies in a straight line and damages enemies on contact.
    /// pierces through enemies, hitting each one once.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileFist : MonoBehaviour
    {
        [Header("Projectile Settings")]
        /// <summary>
        /// speed at which the projectile travels (units per second).
        /// </summary>
        public float projectileSpeed = 18f;
        /// <summary>
        /// maximum time the projectile can exist before self-destructing (seconds).
        /// </summary>
        public float maxLifetime = 3f;
        /// <summary>
        /// maximum distance the projectile can travel before self-destructing (units).
        /// </summary>
        public float maxDistance = 30f;

        private int damage = 1;
        private float direction = 1f; // 1 = right, -1 = left
        private Vector3 startPosition;
        private Rigidbody2D rb;
        private HashSet<MonoBehaviour> hitEnemies = new HashSet<MonoBehaviour>();

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            // ensure continuous collision detection for fast-moving projectile
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        /// <summary>
        /// initialize the projectile with direction and damage values.
        /// </summary>
        /// <param name="fireDirection">1 for right, -1 for left</param>
        /// <param name="damageAmount">damage to deal to enemies</param>
        public void Initialize(float fireDirection, int damageAmount)
        {
            direction = fireDirection;
            damage = damageAmount;
            startPosition = transform.position;

            // set velocity based on direction
            rb.linearVelocity = new Vector2(direction * projectileSpeed, 0f);

            // flip sprite to match direction
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction < 0;
            }

            // start lifetime timer
            StartCoroutine(LifetimeTimer());
        }

        void Update()
        {
            // check if projectile exceeded max distance
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);
            if (distanceTraveled >= maxDistance)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// auto-destroy after max lifetime expires.
        /// </summary>
        private IEnumerator LifetimeTimer()
        {
            yield return new WaitForSeconds(maxLifetime);

            // if still alive after max time, destroy
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // check if hit an enemy
            var enemy1 = other.GetComponent<enemy1>();
            var enemy2 = other.GetComponent<enemy2>();
            var enemy3 = other.GetComponent<enemy3>();

            MonoBehaviour enemy = null;
            if (enemy1 != null) enemy = enemy1;
            else if (enemy2 != null) enemy = enemy2;
            else if (enemy3 != null) enemy = enemy3;

            // if we hit an enemy we haven't damaged yet
            if (enemy != null && !hitEnemies.Contains(enemy))
            {
                // mark enemy as hit
                hitEnemies.Add(enemy);

                // deal damage and knockback
                Vector2 knockbackDir = new Vector2(direction, 0.3f);

                if (enemy1 != null)
                {
                    enemy1.TakeDamage(damage, knockbackDir, 2f, false);
                }
                else if (enemy2 != null)
                {
                    enemy2.TakeDamage(damage, knockbackDir, 2f, false);
                }
                else if (enemy3 != null)
                {
                    enemy3.TakeDamage(damage, knockbackDir, 2f, false);
                }

                // DON'T destroy - keep flying to hit more enemies!
            }
        }

        /// <summary>
        /// visualize projectile's collider in editor (helps debug collision issues).
        /// </summary>
        void OnDrawGizmos()
        {
            // draw green circle/box at projectile position
            Gizmos.color = Color.green;

            var circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
            }

            var boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                Gizmos.DrawWireCube(transform.position, boxCollider.size);
            }
        }
    }
}

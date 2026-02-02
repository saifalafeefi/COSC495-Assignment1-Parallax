using System.Collections;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// projectile that flies in a straight line and damages enemies on contact.
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
        private bool hasHit = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            // ensure continuous collision detection for fast-moving projectile
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                Debug.Log("[PROJECTILE] Rigidbody2D collision detection set to Continuous");
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

            // diagnostic: check projectile setup
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Debug.Log($"[PROJECTILE DIAGNOSTIC] Layer: {LayerMask.LayerToName(gameObject.layer)}, IsTrigger: {collider.isTrigger}, Rigidbody: {(rb != null ? "YES" : "NO")}, CollisionMode: {(rb != null ? rb.collisionDetectionMode.ToString() : "N/A")}");
            }
            else
            {
                Debug.LogWarning("[PROJECTILE DIAGNOSTIC] NO COLLIDER FOUND! Projectile won't hit anything!");
            }

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
                Debug.Log($"[PROJECTILE] exceeded max distance ({distanceTraveled:F1} units), destroying");
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
                Debug.Log("[PROJECTILE] lifetime expired, destroying");
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[PROJECTILE] OnTriggerEnter2D called! hit: {other.gameObject.name}, layer: {LayerMask.LayerToName(other.gameObject.layer)}");

            // prevent multiple hits
            if (hasHit)
            {
                Debug.Log("[PROJECTILE] already hit something, ignoring");
                return;
            }

            // check if hit an enemy
            var enemy1 = other.GetComponent<enemy1>();
            var enemy2 = other.GetComponent<enemy2>();
            var enemy3 = other.GetComponent<enemy3>();

            if (enemy1 != null)
            {
                hasHit = true;
                Vector2 knockbackDir = new Vector2(direction, 0.3f);
                enemy1.TakeDamage(damage, knockbackDir, 2f, false);
                Debug.Log($"[PROJECTILE] hit enemy1 '{other.gameObject.name}', dealt {damage} damage");
                Destroy(gameObject);
            }
            else if (enemy2 != null)
            {
                hasHit = true;
                Vector2 knockbackDir = new Vector2(direction, 0.3f);
                enemy2.TakeDamage(damage, knockbackDir, 2f, false);
                Debug.Log($"[PROJECTILE] hit enemy2 '{other.gameObject.name}', dealt {damage} damage");
                Destroy(gameObject);
            }
            else if (enemy3 != null)
            {
                hasHit = true;
                Vector2 knockbackDir = new Vector2(direction, 0.3f);
                enemy3.TakeDamage(damage, knockbackDir, 2f, false);
                Debug.Log($"[PROJECTILE] hit enemy3 '{other.gameObject.name}', dealt {damage} damage");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[PROJECTILE] hit '{other.gameObject.name}' but it's not an enemy");
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

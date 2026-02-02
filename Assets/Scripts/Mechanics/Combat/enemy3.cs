using System.Collections;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// flying patrol enemy (spectral drifter). floats in the air, follows patrol path horizontally.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class enemy3 : MonoBehaviour
    {
        [Header("Patrol Settings")]
        public PatrolPath path;
        public float moveSpeed = 3f;

        [Header("Audio")]
        public AudioClip ouch;

        [Header("Invincibility Settings")]
        public float invincibilityDuration = 0.5f;
        public float flashInterval = 0.1f;
        public Material flashMaterial;

        [Header("Damage Feedback")]
        public GameObject damageNumberPrefab;
        public float damageNumberOffset = 0.5f;

        [Header("Projectile Settings")]
        public GameObject projectilePrefab;
        public float dropInterval = 2f; // how often to drop projectiles (seconds)
        public float dropRange = 2f; // how close player must be horizontally to drop
        public float projectileSpeed = 5f;
        public int projectileDamage = 1;

        private float dropTimer = 0f;
        private PlayerController player;

        internal PatrolPath.Mover mover;
        internal Collider2D _collider;
        internal AudioSource _audio;
        internal Health health;
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Material originalMaterial;
        private bool isDead = false;

        private Vector2 previousPosition;

        public bool IsInvincible => isInvincible;
        public Bounds Bounds => _collider.bounds;

        void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            health = GetComponent<Health>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();

            if (spriteRenderer != null)
            {
                originalMaterial = spriteRenderer.material;
            }

            // setup rigidbody for flying (no gravity)
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            previousPosition = transform.position;
        }

        void Update()
        {
            // handle invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0)
                {
                    isInvincible = false;
                    if (flashCoroutine != null)
                    {
                        StopCoroutine(flashCoroutine);
                        flashCoroutine = null;
                    }
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.material = originalMaterial;
                    }
                }
            }

            // patrol movement (only when alive)
            if (!isDead && path != null)
            {
                // create mover if needed
                if (mover == null)
                {
                    mover = path.CreateMover(moveSpeed);
                }

                // store previous position for sprite flip detection
                previousPosition = transform.position;

                // move toward mover's current position
                Vector2 targetPosition = mover.Position;
                transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                // flip sprite based on movement direction
                if (spriteRenderer != null)
                {
                    float moveDirection = targetPosition.x - previousPosition.x;
                    if (Mathf.Abs(moveDirection) > 0.01f) // threshold to avoid jitter
                    {
                        // moving right = face right (flipX true), moving left = face left (flipX false)
                        spriteRenderer.flipX = moveDirection > 0;
                    }
                }
            }

            // projectile dropping logic (only when alive)
            if (!isDead)
            {
                HandleProjectileDropping();
            }
        }

        private void HandleProjectileDropping()
        {
            // find player if we don't have reference
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
            }

            // check if player is below us (Y axis)
            bool playerIsBelow = player.transform.position.y < transform.position.y;

            // check if player is within horizontal range (X axis)
            float horizontalDistance = Mathf.Abs(player.transform.position.x - transform.position.x);
            bool playerInXRange = horizontalDistance <= dropRange;

            // player must be BOTH below AND within X range
            bool canDrop = playerIsBelow && playerInXRange;

            // only increment timer when conditions met
            if (canDrop)
            {
                dropTimer += Time.deltaTime;

                // drop projectile when timer ready
                if (dropTimer >= dropInterval)
                {
                    Debug.LogError($"[ENEMY3] DROPPING! Player Y: {player.transform.position.y}, Enemy Y: {transform.position.y}, X Distance: {horizontalDistance:F2}, Range: {dropRange}");
                    DropProjectile();
                    dropTimer = 0f; // reset timer
                }
            }
            else
            {
                // reset timer when conditions not met
                if (dropTimer > 0f)
                {
                    Debug.Log($"[ENEMY3] Conditions not met, resetting. Below: {playerIsBelow}, X Distance: {horizontalDistance:F2}/{dropRange}");
                }
                dropTimer = 0f;
            }
        }

        private void DropProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError("[ENEMY3] Projectile prefab is NULL! Assign it in Inspector!");
                return;
            }

            Debug.Log($"[ENEMY3] Spawning projectile at {transform.position}");

            // spawn projectile at enemy position
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Enemy3Projectile projectileScript = projectile.GetComponent<Enemy3Projectile>();

            if (projectileScript != null)
            {
                projectileScript.Initialize(projectileSpeed, projectileDamage);
                Debug.Log($"[ENEMY3] Projectile initialized: speed={projectileSpeed}, damage={projectileDamage}");
            }
            else
            {
                Debug.LogError("[ENEMY3] Projectile prefab has no Enemy3Projectile script!");
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (health != null && !health.IsAlive)
            {
                return;
            }

            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var ev = Schedule<PlayerEnemyCollision>();
                ev.player = player;
                ev.enemy = this;
            }
        }

        public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce = 3f, bool pierceInvincibility = false)
        {
            if (isInvincible && !pierceInvincibility)
            {
                return;
            }

            if (health != null)
            {
                health.Decrement(damage);
                SpawnDamageNumber(damage);

                if (!health.IsAlive)
                {
                    isDead = true;
                    _collider.enabled = false;

                    // death flash effect for visual feedback
                    StartCoroutine(DeathFlash());

                    // enable gravity so corpse falls
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero; // reset velocity
                        rb.angularVelocity = 0f; // reset rotation
                        rb.bodyType = RigidbodyType2D.Dynamic;
                        rb.gravityScale = 2.5f;
                        rb.constraints = RigidbodyConstraints2D.None;
                    }

                    Schedule<EnemyDeath>().enemy = this;
                }
                else
                {
                    if (ouch != null && _audio != null)
                    {
                        _audio.PlayOneShot(ouch);
                    }

                    ActivateInvincibility();
                    // no knockback for flying enemy (stays in air)
                }
            }
            else
            {
                Schedule<EnemyDeath>().enemy = this;
            }
        }

        private void SpawnDamageNumber(int damage)
        {
            if (damageNumberPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = transform.position;
            spawnPosition.y = _collider.bounds.max.y + damageNumberOffset;

            GameObject damageNumberObj = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);
            DamageNumber damageNumber = damageNumberObj.GetComponent<DamageNumber>();

            if (damageNumber != null)
            {
                damageNumber.Initialize(damage, spawnPosition);
            }
        }

        public void ActivateInvincibility()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }

            if (!isInvincible)
            {
                isInvincible = true;
                invincibilityTimer = invincibilityDuration;
                flashCoroutine = StartCoroutine(FlashSprite());
            }
            else
            {
                invincibilityTimer = invincibilityDuration;
                flashCoroutine = StartCoroutine(FlashSprite());
            }
        }

        private IEnumerator FlashSprite()
        {
            if (flashMaterial == null || spriteRenderer == null)
            {
                yield break;
            }

            while (isInvincible)
            {
                spriteRenderer.material = flashMaterial;
                yield return new WaitForSeconds(flashInterval);

                if (isInvincible)
                {
                    spriteRenderer.material = originalMaterial;
                    yield return new WaitForSeconds(flashInterval);
                }
            }

            spriteRenderer.material = originalMaterial;
        }

        private IEnumerator DeathFlash()
        {
            if (flashMaterial == null || spriteRenderer == null)
            {
                yield break;
            }

            // flash white briefly to indicate death
            spriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.material = originalMaterial;
        }

        public void ApplyKnockback(Vector2 knockbackDirection, float knockbackForce = 3f)
        {
            // flying enemy doesn't get knocked back (stays floating)
        }
    }
}

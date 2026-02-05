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
        /// <summary>
        /// array of hit sounds - one will be randomly selected when damaged.
        /// </summary>
        public AudioClip[] hitSounds;
        /// <summary>
        /// volume multiplier for hit sounds (0 = silent, 1 = normal, 5 = 5x louder).
        /// </summary>
        [Range(0f, 5f)]
        public float hitSoundVolume = 1f;

        [Header("Score Settings")]
        /// <summary>
        /// points awarded when player kills this enemy.
        /// </summary>
        public int scoreValue = 35;

        [Header("Invincibility Settings")]
        public float invincibilityDuration = 0.5f;
        public float flashInterval = 0.1f;
        public Material flashMaterial;

        [Header("Damage Feedback")]
        public GameObject damageNumberPrefab;
        public float damageNumberOffset = 0.5f;

        [Header("Powerup Drops")]
        public GameObject healthVialPrefab;
        public GameObject speedVialPrefab;
        public GameObject damageVialPrefab;
        public GameObject timeVialPrefab;

        [Range(0, 100)] public float healthDropChance = 20f;
        [Range(0, 100)] public float speedDropChance = 30f; // higher for enemy3
        [Range(0, 100)] public float damageDropChance = 10f;
        [Range(0, 100)] public float timeDropChance = 5f;

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

        // damage number merging - tracks currently active damage number
        private DamageNumber activeDamageNumber = null;

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
                    DropProjectile();
                    dropTimer = 0f; // reset timer
                }
            }
            else
            {
                // reset timer when conditions not met
                dropTimer = 0f;
            }
        }

        private void DropProjectile()
        {
            if (projectilePrefab == null)
            {
                return;
            }

            // spawn projectile at enemy position
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Enemy3Projectile projectileScript = projectile.GetComponent<Enemy3Projectile>();

            if (projectileScript != null)
            {
                projectileScript.Initialize(projectileSpeed, projectileDamage);
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

                    // try to drop powerup
                    TryDropPowerup();

                    Schedule<EnemyDeath>().enemy = this;
                }
                else
                {
                    PlayRandomHitSound();
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

            // calculate spawn position above enemy's head
            Vector3 spawnPosition = transform.position;
            spawnPosition.y = _collider.bounds.max.y + damageNumberOffset;

            // if damage number still exists, merge with it (reset to spawn position)
            if (activeDamageNumber != null)
            {
                activeDamageNumber.AddDamage(damage, spawnPosition);
                return;
            }

            // spawn new damage number (only if none exists)
            GameObject damageNumberObj = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);
            DamageNumber damageNumber = damageNumberObj.GetComponent<DamageNumber>();

            if (damageNumber != null)
            {
                damageNumber.Initialize(damage, spawnPosition);
                activeDamageNumber = damageNumber;
            }
        }

        private void TryDropPowerup()
        {
            float roll = Random.Range(0f, 100f);
            GameObject powerupToDrop = null;

            // check each powerup in order (cumulative probabilities)
            if (roll < healthDropChance)
            {
                powerupToDrop = healthVialPrefab;
            }
            else if (roll < healthDropChance + speedDropChance)
            {
                powerupToDrop = speedVialPrefab;
            }
            else if (roll < healthDropChance + speedDropChance + damageDropChance)
            {
                powerupToDrop = damageVialPrefab;
            }
            else if (roll < healthDropChance + speedDropChance + damageDropChance + timeDropChance)
            {
                powerupToDrop = timeVialPrefab;
            }

            // spawn powerup at enemy position if one was rolled
            if (powerupToDrop != null)
            {
                GameObject vial = Instantiate(powerupToDrop, transform.position, Quaternion.identity);

                // set high sorting order so vials render in front of environment
                SpriteRenderer vialRenderer = vial.GetComponent<SpriteRenderer>();
                if (vialRenderer != null)
                {
                    vialRenderer.sortingOrder = 100;
                }
            }
        }

        /// <summary>
        /// plays a random hit sound from the hitSounds array.
        /// interrupts any currently playing sound and starts from beginning.
        /// </summary>
        private void PlayRandomHitSound()
        {
            if (hitSounds != null && hitSounds.Length > 0 && _audio != null)
            {
                // pick random sound from array
                AudioClip randomSound = hitSounds[Random.Range(0, hitSounds.Length)];
                if (randomSound != null)
                {
                    // stop current sound and play new one from beginning (allows rapid hits)
                    _audio.Stop();
                    _audio.clip = randomSound;
                    _audio.volume = hitSoundVolume;
                    _audio.Play();
                }
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

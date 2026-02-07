using System.Collections;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// stationary sentry enemy. floats in place, no gravity, no knockback. complete standalone script.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class enemy2 : MonoBehaviour
    {
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
        public int scoreValue = 50;

        [Header("Invincibility Settings")]
        public float invincibilityDuration = 0.5f;
        public float flashInterval = 0.1f;
        public Material flashMaterial;

        [Header("Damage Feedback")]
        public GameObject damageNumberPrefab;
        public GameObject scoreNumberPrefab;
        public float damageNumberOffset = 0.5f;

        [Header("Powerup Drops")]
        public GameObject healthVialPrefab;
        public GameObject speedVialPrefab;
        public GameObject damageVialPrefab;
        public GameObject timeVialPrefab;

        [Range(0, 100)] public float overallDropChance = 40f;
        [Range(0, 100)] public float healthDropChance = 20f;
        [Range(0, 100)] public float timeDropChance = 30f; // higher for enemy2
        [Range(0, 100)] public float speedDropChance = 10f;
        [Range(0, 100)] public float damageDropChance = 5f;

        [Header("Projectile Settings")]
        public GameObject projectilePrefab;
        public float shootInterval = 2f;
        public float shootRange = 10f;
        public float projectileSpeed = 5f;
        public int projectileDamage = 1;

        [Header("Animation Settings")]
        [Tooltip("which frame of enemy2_shoot animation to spawn projectile at")]
        public int projectileSpawnFrame = 5;
        [Tooltip("offset from enemy position to spawn projectile (x = horizontal, y = vertical)")]
        public Vector2 projectileSpawnOffset = new Vector2(0f, 0.5f);

        private float shootTimer = 0f;
        private PlayerController player;
        private bool isShooting = false;

        internal Collider2D _collider;
        internal AudioSource _audio;
        internal Health health;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Rigidbody2D rb;

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Material originalMaterial;
        private bool isDead = false;
        private Coroutine shootCoroutine = null;

        // damage number merging - tracks currently active damage number
        private DamageNumber activeDamageNumber = null;

        public bool IsInvincible => isInvincible;
        public Bounds Bounds => _collider.bounds;

        void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            health = GetComponent<Health>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();

            if (spriteRenderer != null)
            {
                originalMaterial = spriteRenderer.material;
            }
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

            // shooting logic and sprite flip (only when alive)
            if (!isDead)
            {
                // flip sprite to face player
                if (player != null && spriteRenderer != null)
                {
                    bool playerIsRight = player.transform.position.x > transform.position.x;
                    spriteRenderer.flipX = playerIsRight;
                }

                HandleShooting();
            }
        }

        private void HandleShooting()
        {
            // don't shoot if dead
            if (isDead) return;

            // find player if we don't have reference
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return; // no player in scene
            }

            // check if player is in range
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            if (distanceToPlayer > shootRange || player.IsRespawning)
            {
                shootTimer = 0f; // reset timer when out of range or player respawning
                return;
            }

            // player is in range and not respawning, handle shoot timer
            if (!isShooting)
            {
                shootTimer += Time.deltaTime;
                if (shootTimer >= shootInterval)
                {
                    shootCoroutine = StartCoroutine(ShootSequence());
                    shootTimer = 0f;
                }
            }
        }

        private IEnumerator ShootSequence()
        {
            isShooting = true;

            // trigger shoot animation
            if (animator != null)
            {
                animator.SetTrigger("shoot");
            }

            // wait one frame for animator to process trigger
            yield return null;

            // wait until we're in the shoot animation
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // try different possible state names
            bool inShootState = stateInfo.IsName("Enemy2_Shoot") ||
                                stateInfo.IsName("enemy2_shoot") ||
                                stateInfo.IsName("shoot") ||
                                stateInfo.IsName("Shoot") ||
                                stateInfo.IsName("Base Layer.Enemy2_Shoot");

            int waitCount = 0;
            while (!inShootState && waitCount < 100)
            {
                waitCount++;
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                inShootState = stateInfo.IsName("Enemy2_Shoot") ||
                               stateInfo.IsName("enemy2_shoot") ||
                               stateInfo.IsName("shoot") ||
                               stateInfo.IsName("Shoot") ||
                               stateInfo.IsName("Base Layer.Enemy2_Shoot");
            }

            if (!inShootState)
            {
                isShooting = false;
                yield break;
            }

            // calculate at what normalized time we should spawn projectile
            float animationLength = stateInfo.length;
            float fps = 12f;
            float totalFrames = animationLength * fps;
            float targetNormalizedTime = projectileSpawnFrame / totalFrames;

            // if frame is beyond animation length, just shoot at end (0.9)
            if (targetNormalizedTime > 1f)
            {
                targetNormalizedTime = 0.9f;
            }

            // wait until animation reaches spawn frame (or exceeds it)
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            while (stateInfo.normalizedTime < targetNormalizedTime)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            }

            // don't spawn if died during animation
            if (isDead)
            {
                isShooting = false;
                yield break;
            }

            // spawn projectile
            SpawnProjectile();

            // wait for animation to complete
            while (stateInfo.normalizedTime < 0.99f)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            }

            isShooting = false;
        }

        private void SpawnProjectile()
        {
            if (projectilePrefab == null || player == null) return;

            // calculate spawn position with offset
            Vector3 spawnPosition = transform.position + (Vector3)projectileSpawnOffset;

            // calculate direction to player
            Vector2 direction = (player.transform.position - transform.position).normalized;

            // spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            Enemy2Projectile projectileScript = projectile.GetComponent<Enemy2Projectile>();

            if (projectileScript != null)
            {
                projectileScript.Initialize(direction, projectileSpeed, projectileDamage);
            }

            // set projectile to render in front of enemy
            SpriteRenderer projectileRenderer = projectile.GetComponent<SpriteRenderer>();
            if (projectileRenderer != null && spriteRenderer != null)
            {
                projectileRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
                projectileRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
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

                    // stop shooting if in progress
                    if (shootCoroutine != null)
                    {
                        StopCoroutine(shootCoroutine);
                        shootCoroutine = null;
                        isShooting = false;
                    }

                    // death flash effect for visual feedback
                    StartCoroutine(DeathFlash());

                    // enable gravity so corpse falls like enemy1
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero; // reset velocity to start from rest
                        rb.angularVelocity = 0f; // reset rotation velocity
                        rb.bodyType = RigidbodyType2D.Dynamic;
                        rb.gravityScale = 2.5f;
                        rb.constraints = RigidbodyConstraints2D.None;
                    }

                    // try to drop powerup
                    TryDropPowerup();

                    // spawn score number immediately
                    if (scoreNumberPrefab != null)
                    {
                        var scoreObj = Instantiate(scoreNumberPrefab, new Vector3(transform.position.x, _collider.bounds.center.y, transform.position.z), Quaternion.identity);
                        scoreObj.GetComponent<ScoreNumber>()?.Initialize(scoreValue, scoreObj.transform.position);
                    }

                    Schedule<EnemyDeath>().enemy = this;
                }
                else
                {
                    PlayRandomHitSound();
                    ActivateInvincibility();
                    // no knockback for sentry
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
            float overallRoll = Random.Range(0f, 100f);
            if (overallRoll > overallDropChance)
            {
                return;
            }

            float roll = Random.Range(0f, 100f);
            GameObject powerupToDrop = null;

            // check each powerup in order (cumulative probabilities)
            if (roll < healthDropChance)
            {
                powerupToDrop = healthVialPrefab;
            }
            else if (roll < healthDropChance + timeDropChance)
            {
                powerupToDrop = timeVialPrefab;
            }
            else if (roll < healthDropChance + timeDropChance + speedDropChance)
            {
                powerupToDrop = speedVialPrefab;
            }
            else if (roll < healthDropChance + timeDropChance + speedDropChance + damageDropChance)
            {
                powerupToDrop = damageVialPrefab;
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
            // sentry doesn't get knocked back
        }
    }
}

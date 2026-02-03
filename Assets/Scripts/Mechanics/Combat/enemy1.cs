using System.Collections;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// ground patrol enemy. complete standalone script.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class enemy1 : AnimationController
    {
        public PatrolPath path;

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
        [Range(0, 100)] public float damageDropChance = 30f; // higher for enemy1
        [Range(0, 100)] public float speedDropChance = 10f;
        [Range(0, 100)] public float timeDropChance = 5f;

        internal PatrolPath.Mover mover;
        internal Collider2D _collider;
        internal AudioSource _audio;
        internal Health health;
        private SpriteRenderer enemySpriteRenderer; // renamed to avoid conflict with base class

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Material originalMaterial;

        public bool IsInvincible => isInvincible;
        public Bounds Bounds => _collider.bounds;

        protected override void Awake()
        {
            base.Awake(); // AnimationController.Awake() sets up enemySpriteRenderer
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            health = GetComponent<Health>();

            // get enemySpriteRenderer from base class (AnimationController already got it)
            enemySpriteRenderer = GetComponent<SpriteRenderer>();
            originalMaterial = enemySpriteRenderer.material;
        }

        protected override void Update()
        {
            base.Update();

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
                    enemySpriteRenderer.material = originalMaterial;
                }
            }

            // patrol movement
            if (path != null)
            {
                if (mover == null) mover = path.CreateMover(maxSpeed * 0.5f);
                move.x = Mathf.Clamp(mover.Position.x - transform.position.x, -1, 1);
            }
        }

        protected void OnCollisionEnter2D(Collision2D collision)
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
                    _collider.enabled = false;

                    // death flash effect for visual feedback
                    StartCoroutine(DeathFlash());

                    // try to drop powerup
                    TryDropPowerup();

                    Schedule<EnemyDeath>().enemy = this;
                }
                else
                {
                    PlayRandomHitSound();
                    ActivateInvincibility();
                    ApplyKnockback(knockbackDirection, knockbackForce);
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

        private void TryDropPowerup()
        {
            float roll = Random.Range(0f, 100f);
            GameObject powerupToDrop = null;

            // check each powerup in order (cumulative probabilities)
            if (roll < healthDropChance)
            {
                powerupToDrop = healthVialPrefab;
            }
            else if (roll < healthDropChance + damageDropChance)
            {
                powerupToDrop = damageVialPrefab;
            }
            else if (roll < healthDropChance + damageDropChance + speedDropChance)
            {
                powerupToDrop = speedVialPrefab;
            }
            else if (roll < healthDropChance + damageDropChance + speedDropChance + timeDropChance)
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
            if (flashMaterial == null)
            {
                yield break;
            }

            while (isInvincible)
            {
                enemySpriteRenderer.material = flashMaterial;
                enemySpriteRenderer.material.SetTexture("_MainTex", enemySpriteRenderer.sprite.texture);
                yield return new WaitForSeconds(flashInterval);

                if (isInvincible)
                {
                    enemySpriteRenderer.material = originalMaterial;
                    yield return new WaitForSeconds(flashInterval);
                }
            }

            enemySpriteRenderer.material = originalMaterial;
        }

        private IEnumerator DeathFlash()
        {
            if (flashMaterial == null || enemySpriteRenderer == null)
            {
                yield break;
            }

            // flash white briefly to indicate death
            enemySpriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(0.15f);
            enemySpriteRenderer.material = originalMaterial;
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

        public void ApplyKnockback(Vector2 knockbackDirection, float knockbackForce = 3f)
        {
            transform.position += (Vector3)(knockbackDirection.normalized * knockbackForce * Time.deltaTime);
        }
    }
}

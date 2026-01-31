using System.Collections;
using System.Collections.Generic;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// A simple controller for enemies. Provides movement control over a patrol path.
    /// </summary>
    [RequireComponent(typeof(AnimationController), typeof(Collider2D))]
    public class EnemyController : MonoBehaviour
    {
        public PatrolPath path;
        public AudioClip ouch;

        [Header("Invincibility Settings")]
        /// <summary>
        /// Duration of invincibility frames after taking damage.
        /// </summary>
        public float invincibilityDuration = 0.5f;
        /// <summary>
        /// How fast the sprite flashes during invincibility.
        /// </summary>
        public float flashInterval = 0.1f;
        /// <summary>
        /// White material to swap to during flash (drag in Inspector).
        /// </summary>
        public Material flashMaterial;

        [Header("Damage Feedback")]
        /// <summary>
        /// damage number prefab to spawn when taking damage (optional).
        /// </summary>
        public GameObject damageNumberPrefab;
        /// <summary>
        /// vertical offset above sprite to spawn damage number (in units).
        /// </summary>
        public float damageNumberOffset = 0.5f;

        internal PatrolPath.Mover mover;
        internal AnimationController control;
        internal Collider2D _collider;
        internal AudioSource _audio;
        SpriteRenderer spriteRenderer;

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Color originalSpriteColor;
        private Material originalMaterial;
        private Health health;

        public Bounds Bounds => _collider.bounds;

        /// <summary>
        /// Check if enemy is currently invincible.
        /// </summary>
        public bool IsInvincible => isInvincible;

        void Awake()
        {
            control = GetComponent<AnimationController>();
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            health = GetComponent<Health>();

            // store the original sprite color so we can restore it properly
            originalSpriteColor = spriteRenderer.color;

            // store original material for flash system
            originalMaterial = spriteRenderer.material;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            // don't process collision if enemy is dead
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

        void Update()
        {
            // handle invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0)
                {
                    isInvincible = false;

                    // stop flash coroutine if it's still running
                    if (flashCoroutine != null)
                    {
                        StopCoroutine(flashCoroutine);
                        flashCoroutine = null;
                    }

                    // force material reset when invincibility ends
                    spriteRenderer.material = originalMaterial;
                }
            }

            if (path != null)
            {
                if (mover == null) mover = path.CreateMover(control.maxSpeed * 0.5f);
                control.move.x = Mathf.Clamp(mover.Position.x - transform.position.x, -1, 1);
            }
        }

        /// <summary>
        /// Called when enemy takes damage from player attack.
        /// </summary>
        /// <param name="damage">amount of damage to deal</param>
        /// <param name="knockbackDirection">direction to knock the enemy back</param>
        /// <param name="knockbackForce">force of the knockback</param>
        /// <param name="pierceInvincibility">if true, ignores i-frames and deals damage anyway</param>
        public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce = 3f, bool pierceInvincibility = false)
        {
            if (isInvincible && !pierceInvincibility)
            {
                return;
            }

            if (health != null)
            {
                health.Decrement(damage);

                // spawn damage number
                SpawnDamageNumber(damage);

                if (!health.IsAlive)
                {
                    // disable collider immediately to prevent collision with player
                    _collider.enabled = false;
                    Schedule<EnemyDeath>().enemy = this;
                }
                else
                {
                    // enemy survived - apply hurt effects
                    if (ouch != null && _audio != null)
                    {
                        _audio.PlayOneShot(ouch);
                    }

                    ActivateInvincibility();
                    ApplyKnockback(knockbackDirection, knockbackForce);
                }
            }
            else
            {
                Schedule<EnemyDeath>().enemy = this;
            }
        }

        /// <summary>
        /// spawn a damage number above the enemy.
        /// </summary>
        /// <param name="damage">damage value to display</param>
        private void SpawnDamageNumber(int damage)
        {
            if (damageNumberPrefab == null)
            {
                return; // no prefab assigned, skip
            }

            // calculate spawn position at top of sprite
            Vector3 spawnPosition = transform.position;
            spawnPosition.y = _collider.bounds.max.y + damageNumberOffset;

            // instantiate and initialize damage number
            GameObject damageNumberObj = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);
            DamageNumber damageNumber = damageNumberObj.GetComponent<DamageNumber>();

            if (damageNumber != null)
            {
                damageNumber.Initialize(damage, spawnPosition);
            }
            else
            {
                Debug.LogWarning("[ENEMY] DamageNumber prefab missing DamageNumber component!");
            }
        }

        /// <summary>
        /// Activates invincibility frames with white flash effect.
        /// </summary>
        public void ActivateInvincibility()
        {
            // stop any existing flash coroutine
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

        /// <summary>
        /// Coroutine that makes the sprite flash white during invincibility by swapping materials.
        /// </summary>
        private IEnumerator FlashSprite()
        {
            if (flashMaterial == null)
            {
                UnityEngine.Debug.LogWarning("[ENEMY FLASH] no flash material assigned!");
                yield break;
            }

            while (isInvincible)
            {
                // flash on - swap to white material and set sprite texture
                spriteRenderer.material = flashMaterial;
                spriteRenderer.material.SetTexture("_MainTex", spriteRenderer.sprite.texture);
                yield return new WaitForSeconds(flashInterval);

                // flash off - restore original material
                if (isInvincible)
                {
                    spriteRenderer.material = originalMaterial;
                    yield return new WaitForSeconds(flashInterval);
                }
            }

            // ensure original material is restored when invincibility ends
            spriteRenderer.material = originalMaterial;
        }

        /// <summary>
        /// Applies knockback force to the enemy.
        /// </summary>
        /// <param name="knockbackDirection">Direction of the knockback force.</param>
        /// <param name="knockbackForce">Strength of the knockback.</param>
        public void ApplyKnockback(Vector2 knockbackDirection, float knockbackForce = 3f)
        {
            // simple position-based knockback (enemies don't use velocity)
            transform.position += (Vector3)(knockbackDirection.normalized * knockbackForce * Time.deltaTime);
        }

    }
}
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

        internal PatrolPath.Mover mover;
        internal AnimationController control;
        internal Collider2D _collider;
        internal AudioSource _audio;
        SpriteRenderer spriteRenderer;

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Color originalSpriteColor;
        private Health health;
        private GameObject whiteOverlay;
        private Dictionary<Sprite, Sprite> whiteSpriteCache = new Dictionary<Sprite, Sprite>();
        private Sprite lastSprite;

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

            // create white overlay for damage flash effect
            CreateWhiteOverlay();

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
            // update white overlay to match current animation frame
            if (whiteOverlay != null)
            {
                UpdateOverlaySprite();
            }

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

                    // force sprite color reset when invincibility ends
                    spriteRenderer.color = originalSpriteColor;

                    // hide white overlay
                    if (whiteOverlay != null)
                    {
                        SpriteRenderer overlayRenderer = whiteOverlay.GetComponent<SpriteRenderer>();
                        if (overlayRenderer != null)
                        {
                            overlayRenderer.color = new Color(1f, 1f, 1f, 0f);
                        }
                    }
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
        /// <param name="knockbackDirection">Direction to knock the enemy back.</param>
        /// <param name="knockbackForce">Force of the knockback.</param>
        public void TakeDamage(Vector2 knockbackDirection, float knockbackForce = 3f)
        {
            if (isInvincible)
            {
                return;
            }

            if (health != null)
            {
                health.Decrement();

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
        /// Creates a solid white silhouette overlay for damage flash effect.
        /// </summary>
        private void CreateWhiteOverlay()
        {
            whiteOverlay = new GameObject("WhiteOverlay");
            whiteOverlay.transform.SetParent(transform);
            whiteOverlay.transform.localPosition = Vector3.zero;
            whiteOverlay.transform.localScale = Vector3.one;

            SpriteRenderer overlayRenderer = whiteOverlay.AddComponent<SpriteRenderer>();
            overlayRenderer.color = new Color(1f, 1f, 1f, 0f); // invisible initially
            overlayRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
            overlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1; // render on top

        }

        /// <summary>
        /// Updates the overlay sprite to match current animation frame (cached for performance).
        /// </summary>
        private void UpdateOverlaySprite()
        {
            if (spriteRenderer.sprite == null) return;

            Sprite currentSprite = spriteRenderer.sprite;

            // only update if sprite changed (don't recreate every frame!)
            if (currentSprite == lastSprite) return;

            lastSprite = currentSprite;
            SpriteRenderer overlayRenderer = whiteOverlay.GetComponent<SpriteRenderer>();

            // check cache first
            if (whiteSpriteCache.ContainsKey(currentSprite))
            {
                overlayRenderer.sprite = whiteSpriteCache[currentSprite];
                return;
            }

            // create white version and cache it
            Texture2D originalTexture = currentSprite.texture;
            Texture2D whiteTexture = new Texture2D((int)currentSprite.rect.width, (int)currentSprite.rect.height);

            Color[] pixels = originalTexture.GetPixels(
                (int)currentSprite.rect.x,
                (int)currentSprite.rect.y,
                (int)currentSprite.rect.width,
                (int)currentSprite.rect.height
            );

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0.01f)
                {
                    pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
                }
            }

            whiteTexture.SetPixels(pixels);
            whiteTexture.Apply();

            Sprite whiteSprite = Sprite.Create(
                whiteTexture,
                new Rect(0, 0, whiteTexture.width, whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                currentSprite.pixelsPerUnit
            );

            whiteSpriteCache[currentSprite] = whiteSprite;
            overlayRenderer.sprite = whiteSprite;
        }

        /// <summary>
        /// Activates invincibility frames with white overlay flash effect.
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
        /// Coroutine that makes the sprite flash white during invincibility.
        /// </summary>
        private IEnumerator FlashSprite()
        {
            if (whiteOverlay == null)
            {
                yield break;
            }

            SpriteRenderer overlayRenderer = whiteOverlay.GetComponent<SpriteRenderer>();

            while (isInvincible)
            {
                // show white overlay (flash on)
                overlayRenderer.color = new Color(1f, 1f, 1f, 1f); // fully opaque white
                yield return new WaitForSeconds(flashInterval);

                // hide white overlay (flash off)
                if (isInvincible)
                {
                    overlayRenderer.color = new Color(1f, 1f, 1f, 0f); // invisible
                    yield return new WaitForSeconds(flashInterval);
                }
            }

            // ensure overlay is hidden when invincibility ends
            overlayRenderer.color = new Color(1f, 1f, 1f, 0f);
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
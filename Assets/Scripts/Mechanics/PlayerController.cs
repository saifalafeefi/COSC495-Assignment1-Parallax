using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.InputSystem;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        [Header("Invincibility Settings")]
        /// <summary>
        /// Duration of invincibility frames after taking damage.
        /// </summary>
        public float invincibilityDuration = 1.0f;
        /// <summary>
        /// How fast the sprite flashes during invincibility.
        /// </summary>
        public float flashInterval = 0.1f;

        [Header("Attack Settings")]
        /// <summary>
        /// Distance the player lunges forward during attack.
        /// </summary>
        public float attackDashDistance = 2f;
        /// <summary>
        /// Duration of the attack dash (how long the lunge takes).
        /// </summary>
        public float attackDashDuration = 0.2f;
        /// <summary>
        /// Name of the first attack animation state in the Animator.
        /// </summary>
        public string attackStateName = "PlayerAttack1";
        /// <summary>
        /// Name of the second attack animation state in the Animator.
        /// </summary>
        public string attack2StateName = "PlayerAttack2";
        /// <summary>
        /// Time window after attack 1 to input attack 2 (combo window).
        /// </summary>
        public float comboWindow = 0.8f;
        /// <summary>
        /// Attack hitbox range (how far forward to detect enemies).
        /// </summary>
        public float attackRange = 1.5f;
        /// <summary>
        /// Attack hitbox size (width/height of the attack area).
        /// </summary>
        public Vector2 attackHitboxSize = new Vector2(1.5f, 1f);
        /// <summary>
        /// Knockback force applied to enemies when hit.
        /// </summary>
        public float enemyKnockbackForce = 3f;
        /// <summary>
        /// Layer mask for detecting enemies.
        /// </summary>
        public LayerMask enemyLayer;

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Color originalSpriteColor;
        private bool isDashing = false;
        private float dashVelocity = 0f;
        private bool isAttacking = false;
        private GameObject whiteOverlay;
        private Dictionary<Sprite, Sprite> whiteSpriteCache = new Dictionary<Sprite, Sprite>();
        private Sprite lastSprite;
        private int comboStep = 0; // 0 = no combo, 1 = attack 1 done (can do attack 2)
        private float comboWindowTimer = 0f;

        /// <summary>
        /// Check if player is currently invincible.
        /// </summary>
        public bool IsInvincible => isInvincible;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/ public Collider2D collider2d;
        /*internal new*/ public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private InputAction m_MoveAction;
        private InputAction m_JumpAction;
        private InputAction m_AttackAction;

        public Bounds Bounds => collider2d.bounds;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            // store the original sprite color so we can restore it properly
            originalSpriteColor = spriteRenderer.color;

            // create white overlay for damage flash effect
            CreateWhiteOverlay();

            m_MoveAction = InputSystem.actions.FindAction("Player/Move");
            m_JumpAction = InputSystem.actions.FindAction("Player/Jump");
            m_AttackAction = InputSystem.actions.FindAction("Player/Attack");

            m_MoveAction.Enable();
            m_JumpAction.Enable();

            // only enable attack if it exists
            if (m_AttackAction != null)
            {
                m_AttackAction.Enable();
            }
            else
            {
            }


            if (invincibilityDuration <= 0)
            {
            }
        }

        protected override void Update()
        {
            // update white overlay to match current animation frame
            if (whiteOverlay != null)
            {
                UpdateOverlaySprite();
            }

            if (controlEnabled)
            {
                // disable movement input during attack
                if (!isAttacking)
                {
                    move.x = m_MoveAction.ReadValue<Vector2>().x;
                }
                else
                {
                    move.x = 0; // lock movement during attack
                }

                if (jumpState == JumpState.Grounded && m_JumpAction.WasPressedThisFrame())
                    jumpState = JumpState.PrepareToJump;
                else if (m_JumpAction.WasReleasedThisFrame())
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }

                // handle attack input (only if not already attacking)
                if (m_AttackAction != null && m_AttackAction.WasPressedThisFrame() && !isAttacking)
                {
                    StartCoroutine(PerformAttack());
                }
            }
            else
            {
                move.x = 0;
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

            // handle combo window timer
            if (comboStep > 0)
            {
                comboWindowTimer -= Time.deltaTime;
                if (comboWindowTimer <= 0)
                {
                    comboStep = 0; // reset combo if window expires
                    UnityEngine.Debug.Log("[COMBO] timer expired, reset to step 0");
                }
            }

            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            // apply normal movement + dash velocity
            targetVelocity = move * maxSpeed;
            targetVelocity.x += dashVelocity;
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
        /// Performs the full attack sequence (animation + dash).
        /// </summary>
        private IEnumerator PerformAttack()
        {
            isAttacking = true;

            // store which attack we're doing NOW (before timer can change it)
            int currentComboStep = comboStep;

            // stop the combo timer - we've committed to this attack
            comboWindowTimer = 0f;

            // determine which attack to perform based on combo step
            string currentAttackState;
            string attackTrigger;

            if (currentComboStep == 0)
            {
                // first attack
                currentAttackState = attackStateName;
                attackTrigger = "attack";
                UnityEngine.Debug.Log("[COMBO] starting attack 1 (comboStep was 0)");
            }
            else
            {
                // second attack (combo)
                currentAttackState = attack2StateName;
                attackTrigger = "attack2";
                UnityEngine.Debug.Log($"[COMBO] starting attack 2 (comboStep was {currentComboStep})");
            }

            animator.SetTrigger(attackTrigger);

            // start the dash coroutine
            StartCoroutine(AttackDash());

            // wait one frame for the animator to transition to attack state
            yield return null;

            // wait a brief moment for the attack animation to reach the "hit" frame
            yield return new WaitForSeconds(0.1f);

            // check for enemies in attack range and deal damage
            CheckAttackHit();

            // wait until the animator exits the attack state
            while (animator.GetCurrentAnimatorStateInfo(0).IsName(currentAttackState))
            {
                yield return null;
            }

            isAttacking = false;

            // handle combo progression (use stored value, not current comboStep)
            if (currentComboStep == 0)
            {
                // first attack finished - open combo window for second attack
                comboStep = 1;
                comboWindowTimer = comboWindow;
                UnityEngine.Debug.Log($"[COMBO] attack 1 finished, opening {comboWindow}s window for attack 2");
            }
            else
            {
                // second attack finished - reset combo
                comboStep = 0;
                comboWindowTimer = 0f;
                UnityEngine.Debug.Log("[COMBO] attack 2 finished, reset to step 0");
            }

            // immediately read input so movement resumes if key is still held
            if (controlEnabled)
            {
                move.x = m_MoveAction.ReadValue<Vector2>().x;
            }

        }

        /// <summary>
        /// Checks for enemies in attack range and deals damage.
        /// </summary>
        private void CheckAttackHit()
        {
            // determine attack direction based on which way player is facing
            float direction = spriteRenderer.flipX ? -1f : 1f;

            // calculate hitbox center position (offset from player in attack direction)
            Vector2 hitboxCenter = (Vector2)transform.position + new Vector2(direction * attackRange, 0f);

            // find all colliders in the attack hitbox
            Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, attackHitboxSize, 0f, enemyLayer);


            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyController>();
                if (enemy != null && !enemy.IsInvincible)
                {

                    // calculate knockback direction (away from player)
                    Vector2 knockbackDir = new Vector2(direction, 0.5f); // slight upward angle

                    // deal damage to enemy
                    enemy.TakeDamage(knockbackDir, enemyKnockbackForce);
                }
            }
        }

        /// <summary>
        /// Coroutine that makes the player dash forward during attack.
        /// </summary>
        private IEnumerator AttackDash()
        {
            isDashing = true;
            float elapsed = 0f;

            // determine dash direction based on which way player is facing
            float dashDirection = spriteRenderer.flipX ? -1f : 1f;

            while (elapsed < attackDashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / attackDashDuration);

                // ease-out curve: starts fast, ends slow
                // inverse of ease-in: we want high speed at start, low at end
                float easeOutCurve = 1f - Mathf.Pow(t, 2f); // quadratic ease-out

                // base speed needed to cover the distance
                float baseSpeed = attackDashDistance / attackDashDuration;

                // apply curve to speed (multiply by ease-out so it starts high, ends low)
                dashVelocity = dashDirection * baseSpeed * easeOutCurve;

                yield return null;
            }

            dashVelocity = 0f;
            isDashing = false;
        }

        /// <summary>
        /// Applies knockback force to the player.
        /// </summary>
        /// <param name="knockbackDirection">Direction of the knockback force.</param>
        /// <param name="knockbackForce">Strength of the knockback.</param>
        public void ApplyKnockback(Vector2 knockbackDirection, float knockbackForce = 5f)
        {
            velocity = knockbackDirection.normalized * knockbackForce;
        }

        /// <summary>
        /// force reset player visual state (called on death or respawn)
        /// </summary>
        public void ResetVisualState()
        {
            isInvincible = false;
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
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

        /// <summary>
        /// reset sprite to face right (default direction)
        /// </summary>
        public void ResetOrientation()
        {
            spriteRenderer.flipX = false;
        }

        /// <summary>
        /// visualize attack hitbox in editor (helps with adjusting attack range)
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (spriteRenderer != null)
            {
                // determine attack direction based on which way player is facing
                float direction = spriteRenderer.flipX ? -1f : 1f;

                // calculate hitbox center position
                Vector2 hitboxCenter = (Vector2)transform.position + new Vector2(direction * attackRange, 0f);

                // draw the attack hitbox
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(hitboxCenter, attackHitboxSize);
            }
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}
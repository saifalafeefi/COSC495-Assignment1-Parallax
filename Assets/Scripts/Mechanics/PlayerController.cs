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
        public AudioClip landAudio;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 12;

        [Header("Invincibility Settings")]
        /// <summary>
        /// Duration of invincibility frames after taking damage.
        /// </summary>
        public float invincibilityDuration = 1.5f;
        /// <summary>
        /// Duration of hurt stun (player can't move). Should be less than invincibilityDuration.
        /// </summary>
        public float hurtDuration = 0.5f;
        /// <summary>
        /// How fast the sprite flashes during invincibility.
        /// </summary>
        public float flashInterval = 0.1f;
        /// <summary>
        /// White material to swap to during flash (drag in Inspector).
        /// </summary>
        public Material flashMaterial;

        [Header("Knockback Settings")]
        /// <summary>
        /// Horizontal knockback force when hit by enemy.
        /// </summary>
        public float knockbackHorizontalForce = 8f;
        /// <summary>
        /// Vertical (upward) knockback force when hit by enemy.
        /// </summary>
        public float knockbackVerticalForce = 10f;

        [Header("Attack Settings")]
        /// <summary>
        /// Name of the first attack animation state in the Animator.
        /// </summary>
        public string attackStateName = "PlayerAttack1";
        /// <summary>
        /// Name of the second attack animation state in the Animator.
        /// </summary>
        public string attack2StateName = "PlayerAttack2";
        /// <summary>
        /// Name of the third attack animation state in the Animator.
        /// </summary>
        public string attack3StateName = "PlayerAttack3";
        /// <summary>
        /// Name of the aerial attack animation state in the Animator.
        /// </summary>
        public string attackAirStateName = "PlayerAttackAir";
        /// <summary>
        /// Time window after each attack to input next attack (combo window).
        /// </summary>
        public float comboWindow = 0.8f;

        [Header("Attack 1 & 2 Hitbox")]
        /// <summary>
        /// Attack 1/2 hitbox range (how far forward to detect enemies).
        /// </summary>
        public float attack12Range = 1.5f;
        /// <summary>
        /// Attack 1/2 hitbox size (width/height of the attack area).
        /// </summary>
        public Vector2 attack12HitboxSize = new Vector2(1.5f, 1f);
        /// <summary>
        /// Damage dealt by attack 1 and 2.
        /// </summary>
        public int attack12Damage = 1;

        [Header("Attack 3 (Finisher) Hitbox")]
        /// <summary>
        /// Attack 3 hitbox range (how far forward to detect enemies).
        /// </summary>
        public float attack3Range = 2f;
        /// <summary>
        /// Attack 3 hitbox size (width/height of the attack area).
        /// </summary>
        public Vector2 attack3HitboxSize = new Vector2(2f, 1.5f);
        /// <summary>
        /// Damage dealt by attack 3 finisher.
        /// </summary>
        public int attack3Damage = 2;

        [Header("Aerial Attack Hitbox")]
        /// <summary>
        /// Aerial attack hitbox range (how far forward to detect enemies).
        /// </summary>
        public float attackAirRange = 1.2f;
        /// <summary>
        /// Aerial attack hitbox size (width/height of the attack area).
        /// </summary>
        public Vector2 attackAirHitboxSize = new Vector2(1.2f, 1.2f);
        /// <summary>
        /// Damage dealt by aerial attack.
        /// </summary>
        public int attackAirDamage = 1;

        [Header("Attack Settings")]
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
        private bool isHurtStunned = false;
        private float hurtStunTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Color originalSpriteColor;
        private Material originalMaterial;
        private bool isAttacking = false;
        private float knockbackVelocityX = 0f;
        private float knockbackDecayRate = 10f; // how fast horizontal knockback decays per second
        private int comboStep = 0; // 0 = ready for attack 1, 1 = ready for attack 2, 2 = ready for attack 3
        private float comboWindowTimer = 0f;
        private bool attackButtonHeld = false; // tracks if attack button is held from previous combo

        /// <summary>
        /// Check if player is currently invincible.
        /// </summary>
        public bool IsInvincible => isInvincible;

        /// <summary>
        /// Check if player has speed boost active (set by SpeedPotion).
        /// </summary>
        public bool HasSpeedBoost { get; set; } = false;

        public JumpState jumpState = JumpState.Grounded;
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

            // store original material for flash system
            originalMaterial = spriteRenderer.material;

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
            // handle hurt stun timer
            if (isHurtStunned)
            {
                hurtStunTimer -= Time.deltaTime;
                if (hurtStunTimer <= 0f)
                {
                    // hurt stun ended - restore control (but keep i-frames!)
                    isHurtStunned = false;
                    controlEnabled = true;
                }
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

                // handle jump input
                if (jumpState == JumpState.Grounded && m_JumpAction.WasPressedThisFrame())
                {
                    jumpState = JumpState.PrepareToJump;
                }

                // variable jump height - release jump to fall faster
                if (jumpState == JumpState.InFlight && m_JumpAction.WasReleasedThisFrame() && velocity.y > 0)
                {
                    velocity.y *= 0.5f; // cut jump short
                }

                // handle attack input (only if not already attacking)
                if (m_AttackAction != null && !isAttacking)
                {
                    bool shouldAttack = false;

                    if (comboStep == 0 && !attackButtonHeld)
                    {
                        // no combo active and button not held - require fresh press
                        shouldAttack = m_AttackAction.WasPressedThisFrame();
                    }
                    else
                    {
                        // combo window active OR button held from previous combo - holding counts
                        shouldAttack = m_AttackAction.IsPressed();
                    }

                    if (shouldAttack)
                    {
                        attackButtonHeld = true; // mark that we're in a hold sequence
                        isAttacking = true; // set immediately to prevent animator transitions
                        StartCoroutine(PerformAttack());
                    }
                }

                // reset attackButtonHeld when button is released
                if (m_AttackAction != null && !m_AttackAction.IsPressed())
                {
                    attackButtonHeld = false;
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

                    // force material reset when invincibility ends
                    spriteRenderer.material = originalMaterial;
                }
            }

            // handle combo window timer
            if (comboStep > 0)
            {
                comboWindowTimer -= Time.deltaTime;
                if (comboWindowTimer <= 0)
                {
                    comboStep = 0; // reset combo if window expires
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

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);
            animator.SetFloat("velocityY", velocity.y);
            animator.SetBool("isAttacking", isAttacking);
            animator.SetBool("hurt", isHurtStunned);

            // apply normal movement + knockback
            targetVelocity = move * maxSpeed;
            targetVelocity.x += knockbackVelocityX;

            // decay horizontal knockback over time
            if (knockbackVelocityX != 0)
            {
                float decay = knockbackDecayRate * Time.deltaTime;
                if (Mathf.Abs(knockbackVelocityX) <= decay)
                {
                    knockbackVelocityX = 0;
                }
                else
                {
                    knockbackVelocityX -= Mathf.Sign(knockbackVelocityX) * decay;
                }
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

                // disable control during hurt animation
                isHurtStunned = true;
                controlEnabled = false;
                hurtStunTimer = hurtDuration;
            }
            else
            {
                invincibilityTimer = invincibilityDuration;
                flashCoroutine = StartCoroutine(FlashSprite());

                // reset hurt stun if hit again during i-frames (shouldn't happen but just in case)
                isHurtStunned = true;
                controlEnabled = false;
                hurtStunTimer = hurtDuration;
            }
        }

        /// <summary>
        /// Coroutine that makes the sprite flash white during invincibility by swapping materials.
        /// </summary>
        private IEnumerator FlashSprite()
        {
            if (flashMaterial == null)
            {
                UnityEngine.Debug.LogWarning("[FLASH] no flash material assigned!");
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
        /// Performs the full attack sequence (animation + dash).
        /// </summary>
        private IEnumerator PerformAttack()
        {
            isAttacking = true;

            // store which attack we're doing NOW (before timer can change it)
            int currentComboStep = comboStep;

            // stop the combo timer - we've committed to this attack
            comboWindowTimer = 0f;

            // determine which attack to perform based on combo step AND grounded status
            string currentAttackState;
            string attackTrigger;

            // check if player is in the air
            if (!IsGrounded)
            {
                // aerial attack - always use air attack, no combo in air
                currentAttackState = attackAirStateName;
                attackTrigger = "attackAir";
            }
            else if (currentComboStep == 0)
            {
                // first attack (grounded)
                currentAttackState = attackStateName;
                attackTrigger = "attack1";
            }
            else if (currentComboStep == 1)
            {
                // second attack (combo)
                currentAttackState = attack2StateName;
                attackTrigger = "attack2";
            }
            else
            {
                // third attack (finisher)
                currentAttackState = attack3StateName;
                attackTrigger = "attack3";
            }

            animator.SetTrigger(attackTrigger);

            // wait one frame for the animator to transition to attack state
            yield return null;

            // wait a brief moment for the attack animation to reach the "hit" frame
            yield return new WaitForSeconds(0.1f);

            // check for enemies in attack range and deal damage (use correct hitbox per attack)
            if (currentAttackState == attackAirStateName)
            {
                CheckAttackHit(attackAirRange, attackAirHitboxSize, attackAirDamage);
            }
            else if (currentComboStep == 2) // attack 3 finisher
            {
                CheckAttackHit(attack3Range, attack3HitboxSize, attack3Damage);
            }
            else // attack 1 or 2
            {
                CheckAttackHit(attack12Range, attack12HitboxSize, attack12Damage);
            }

            // wait until the animator exits the attack state
            while (animator.GetCurrentAnimatorStateInfo(0).IsName(currentAttackState))
            {
                yield return null;
            }

            isAttacking = false;

            // handle combo progression (use stored value, not current comboStep)
            // aerial attacks don't progress combo - combo is only for grounded attacks
            if (currentAttackState == attackAirStateName)
            {
                // aerial attack finished - don't change combo state
            }
            else if (currentComboStep == 0)
            {
                // first attack finished - open combo window for second attack
                comboStep = 1;
                comboWindowTimer = comboWindow;
            }
            else if (currentComboStep == 1)
            {
                // second attack finished - open combo window for third attack
                comboStep = 2;
                comboWindowTimer = comboWindow;
            }
            else
            {
                // third attack finished - reset combo
                comboStep = 0;
                comboWindowTimer = 0f;
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
        /// <param name="range">how far forward the hitbox extends</param>
        /// <param name="hitboxSize">width/height of the hitbox</param>
        /// <param name="damage">amount of damage to deal</param>
        private void CheckAttackHit(float range, Vector2 hitboxSize, int damage)
        {
            // determine attack direction based on which way player is facing
            float direction = spriteRenderer.flipX ? -1f : 1f;

            // calculate hitbox center position (offset from player in attack direction)
            Vector2 hitboxCenter = (Vector2)transform.position + new Vector2(direction * range, 0f);

            // find all colliders in the attack hitbox
            Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, hitboxSize, 0f, enemyLayer);

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    // with speed boost, ignore enemy i-frames (pierce through invincibility)
                    bool canHit = !enemy.IsInvincible || HasSpeedBoost;

                    if (canHit)
                    {
                        // calculate knockback direction (away from player)
                        Vector2 knockbackDir = new Vector2(direction, 0.5f); // slight upward angle

                        // deal damage to enemy (pass pierce flag if we have speed boost)
                        enemy.TakeDamage(damage, knockbackDir, enemyKnockbackForce, HasSpeedBoost);
                    }
                }
            }
        }

        /// <summary>
        /// Applies knockback force to the player in a projectile arc.
        /// </summary>
        /// <param name="knockbackDirection">Direction of the knockback force (horizontal component determines direction).</param>
        /// <param name="knockbackForce">Strength of the knockback (UNUSED - uses knockbackHorizontalForce/knockbackVerticalForce instead).</param>
        public void ApplyKnockback(Vector2 knockbackDirection, float knockbackForce = 5f)
        {
            // determine horizontal direction (left or right)
            float horizontalDirection = Mathf.Sign(knockbackDirection.x);

            // apply horizontal knockback (stored separately for smooth decay)
            knockbackVelocityX = horizontalDirection * knockbackHorizontalForce;

            // apply vertical knockback directly (gravity provides natural decay)
            velocity.y = knockbackVerticalForce;
        }

        /// <summary>
        /// force reset player visual state (called on death or respawn)
        /// </summary>
        public void ResetVisualState()
        {
            isInvincible = false;
            isHurtStunned = false;
            hurtStunTimer = 0f;
            controlEnabled = true;
            knockbackVelocityX = 0f;
            HasSpeedBoost = false; // clear speed boost on death
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            spriteRenderer.material = originalMaterial;
        }

        /// <summary>
        /// reset sprite to face right (default direction)
        /// </summary>
        public void ResetOrientation()
        {
            spriteRenderer.flipX = false;
        }

        /// <summary>
        /// visualize attack hitboxes in editor (helps with adjusting attack range)
        /// </summary>
        void OnDrawGizmos()
        {
            if (spriteRenderer != null)
            {
                // determine attack direction based on which way player is facing
                float direction = spriteRenderer.flipX ? -1f : 1f;

                // draw attack 1/2 hitbox (green)
                Vector2 hitbox12Center = (Vector2)transform.position + new Vector2(direction * attack12Range, 0f);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(hitbox12Center, attack12HitboxSize);

                // draw attack 3 finisher hitbox (red)
                Vector2 hitbox3Center = (Vector2)transform.position + new Vector2(direction * attack3Range, 0f);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(hitbox3Center, attack3HitboxSize);

                // draw aerial attack hitbox (cyan)
                Vector2 hitboxAirCenter = (Vector2)transform.position + new Vector2(direction * attackAirRange, 0f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(hitboxAirCenter, attackAirHitboxSize);
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
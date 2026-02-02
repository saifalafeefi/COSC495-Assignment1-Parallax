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
        public bool isAttacking = false;
        private string currentAttackState = "";
        private float knockbackVelocityX = 0f;
        private float knockbackDecayRate = 10f; // how fast horizontal knockback decays per second
        private int comboStep = 0; // 0 = ready for attack 1, 1 = ready for attack 2, 2 = ready for attack 3
        private float comboWindowTimer = 0f;
        private bool attackButtonHeld = false; // tracks if attack button is held from previous combo

        #region Double Jump
        [Header("Double Jump Settings")]
        public bool allowDoubleJump = true;
        private bool hasDoubleJump = false;
        private bool hasUsedDoubleJump = false;
        #endregion

        #region Roll
        [Header("Roll Settings")]
        public string rollStateName = "PlayerRoll";
        public float rollDistance = 3f;
        public float rollDuration = 0.5f;
        public float rollCooldown = 1f;

        private bool isRolling = false;
        private float rollCooldownTimer = 0f;
        private float rollVelocityX = 0f; // stores roll velocity separate from normal movement
        private int enemyLayerIndex = -1; // cached enemy layer index for collision toggling
        private InputAction m_RollAction;
        #endregion

        #region Wall Jump
        [Header("Wall Jump Settings")]
        public bool allowWallJump = true;
        public float wallClingDuration = 0.3f;
        public float wallSlideGravityMultiplier = 0.5f;
        public float wallJumpHorizontalForce = 8f;
        public float wallJumpVerticalForce = 12f;

        private bool isTouchingWall = false;
        private bool isWallSliding = false;
        private float wallClingTimer = 0f;
        private Vector2 wallNormal = Vector2.zero;
        private bool canWallJump = false;
        #endregion

        #region Ranged Attack
        [Header("Ranged Attack Settings")]
        /// <summary>
        /// prefab for the ranged attack projectile (drag in Inspector).
        /// </summary>
        public GameObject rangedProjectilePrefab;
        /// <summary>
        /// cooldown time between ranged attacks (seconds).
        /// </summary>
        public float rangedAttackCooldown = 1f;
        /// <summary>
        /// normalized time (0-1) in the animation to spawn the projectile. 0 = start of animation, 0.5 = halfway, 1 = end of animation.
        /// </summary>
        [Range(0f, 1f)]
        public float projectileSpawnAnimationTime = 0.3f;
        /// <summary>
        /// offset from player position to spawn projectile (x = horizontal, y = vertical).
        /// </summary>
        public Vector2 projectileSpawnOffset = new Vector2(1f, 0f);
        /// <summary>
        /// damage dealt by ranged attack projectile.
        /// </summary>
        public int rangedAttackDamage = 1;
        /// <summary>
        /// name of the ranged attack animation state in the Animator.
        /// </summary>
        public string rangedAttackStateName = "PlayerAttackRanged";

        private InputAction m_RangedAttackAction;
        private float rangedAttackCooldownTimer = 0f;
        private bool isFiringRangedAttack = false; // prevent firing again while animation is playing
        #endregion

        /// <summary>
        /// Check if player is currently invincible.
        /// </summary>
        public bool IsInvincible => isInvincible;

        /// <summary>
        /// Check if player has speed boost active (set by SpeedPotion).
        /// </summary>
        public bool HasSpeedBoost { get; set; } = false;
        public bool HasTimeSlowActive { get; set; } = false;

        /// <summary>
        /// get correct delta time based on whether player is using unscaled time.
        /// </summary>
        private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        /// <summary>
        /// Check if player is currently wall sliding.
        /// </summary>
        public bool IsWallSliding => isWallSliding;

        /// <summary>
        /// Check if player is currently rolling.
        /// </summary>
        public bool IsRolling => isRolling;

        /// <summary>
        /// Get the original sprite color before any powerups/effects were applied.
        /// </summary>
        public Color OriginalSpriteColor => originalSpriteColor;

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
            m_RollAction = InputSystem.actions.FindAction("Player/Roll");
            m_RangedAttackAction = InputSystem.actions.FindAction("Player/RangedAttack");

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

            if (m_RollAction != null)
            {
                m_RollAction.Enable();
            }

            if (m_RangedAttackAction != null)
            {
                m_RangedAttackAction.Enable();
            }


            if (invincibilityDuration <= 0)
            {
            }

            // extract enemy layer index from enemyLayer mask (for roll collision toggling)
            for (int i = 0; i < 32; i++)
            {
                if ((enemyLayer.value & (1 << i)) != 0)
                {
                    enemyLayerIndex = i;
                    break;
                }
            }
        }

        protected override void Update()
        {
            // handle hurt stun timer
            if (isHurtStunned)
            {
                hurtStunTimer -= DeltaTime;
                if (hurtStunTimer <= 0f)
                {
                    // hurt stun ended - restore control (but keep i-frames!)
                    isHurtStunned = false;
                    controlEnabled = true;
                }
            }

            if (controlEnabled)
            {
                // lock movement during attack (but NOT during roll - roll preserves momentum!)
                if (!isAttacking)
                {
                    move.x = m_MoveAction.ReadValue<Vector2>().x;
                }
                else
                {
                    move.x = 0;
                }

                // detect jump input - handle ground jump, wall jump, and double jump
                if (m_JumpAction.WasPressedThisFrame())
                {
                    if (jumpState == JumpState.Grounded)
                    {
                        jumpState = JumpState.PrepareToJump;
                    }
                    else if (allowWallJump && canWallJump && isTouchingWall)
                    {
                        // wall jump takes priority over double jump
                        PerformWallJump();
                        Debug.Log("[WALL JUMP] wall jump executed");
                    }
                    else if (allowDoubleJump && hasDoubleJump && !hasUsedDoubleJump && jumpState == JumpState.InFlight)
                    {
                        // apply jump velocity directly (can't use ground jump system since we're airborne)
                        velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                        hasUsedDoubleJump = true;

                        // play jump sound
                        if (audioSource && jumpAudio)
                            audioSource.PlayOneShot(jumpAudio);
                    }
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

                // handle roll cooldown timer
                if (rollCooldownTimer > 0)
                {
                    rollCooldownTimer -= DeltaTime;
                }

                // handle roll input - only if not attacking, not hurt, not rolling, cooldown finished (works in air!)
                if (m_RollAction != null && !isRolling && !isAttacking && !isHurtStunned && rollCooldownTimer <= 0f)
                {
                    if (m_RollAction.WasPressedThisFrame())
                    {
                        StartCoroutine(PerformRoll());
                    }
                }

                // handle ranged attack cooldown timer
                if (rangedAttackCooldownTimer > 0)
                {
                    rangedAttackCooldownTimer -= DeltaTime;
                }

                // handle ranged attack input - not attacking, not rolling, not hurt, not firing, cooldown finished
                if (m_RangedAttackAction != null && !isAttacking && !isRolling && !isHurtStunned && !isFiringRangedAttack && rangedAttackCooldownTimer <= 0f)
                {
                    if (m_RangedAttackAction.WasPressedThisFrame())
                    {
                        Debug.Log("[RANGED] ranged attack initiated");
                        StartCoroutine(PerformRangedAttack());
                    }
                }

                // handle wall detection and wall sliding
                if (allowWallJump && !IsGrounded && IsTouchingWall && !isAttacking && !isRolling)
                {
                    // determine if player is moving toward wall
                    bool movingTowardWall = (move.x > 0 && CurrentWallNormal.x < 0) || (move.x < 0 && CurrentWallNormal.x > 0);

                    if (movingTowardWall && velocity.y <= 0)
                    {
                        isTouchingWall = true;
                        wallNormal = CurrentWallNormal;
                        wallClingTimer += DeltaTime;

                        if (wallClingTimer < wallClingDuration)
                        {
                            // cling to wall - stop vertical movement
                            velocity.y = 0f;
                            isWallSliding = false;
                            canWallJump = true;
                            Debug.Log("[WALL JUMP] clinging to wall");
                        }
                        else
                        {
                            // slide down wall - reduced gravity
                            isWallSliding = true;
                            canWallJump = true;
                            Debug.Log("[WALL JUMP] sliding down wall");
                        }
                    }
                    else
                    {
                        ResetWallState();
                    }
                }
                else if (IsGrounded || !IsTouchingWall)
                {
                    ResetWallState();
                }
            }
            else
            {
                move.x = 0;
            }

            // handle invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= DeltaTime;
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
                comboWindowTimer -= DeltaTime;
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

                        // grant double jump when leaving ground
                        if (allowDoubleJump)
                        {
                            hasDoubleJump = true;
                            hasUsedDoubleJump = false;
                        }
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
                    // reset double jump flags on landing
                    hasDoubleJump = false;
                    hasUsedDoubleJump = false;

                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override float GetGravityMultiplier()
        {
            if (isWallSliding)
            {
                return gravityModifier * wallSlideGravityMultiplier;
            }
            return base.GetGravityMultiplier();
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
            animator.SetBool("isRolling", isRolling);
            animator.SetBool("isWallSliding", isWallSliding);
            animator.SetBool("isFiringRanged", isFiringRangedAttack);

            // apply normal movement + knockback + roll boost
            targetVelocity = move * maxSpeed;
            targetVelocity.x += knockbackVelocityX;
            targetVelocity.x += rollVelocityX; // roll boost is additive, respects player input!

            // decay horizontal knockback over time
            if (knockbackVelocityX != 0)
            {
                float decay = knockbackDecayRate * DeltaTime;
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
                // flash on - swap to white material (GUI/Text shader renders as solid white)
                spriteRenderer.material = flashMaterial;
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
            yield return new WaitForSecondsRealtime(0.1f);

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
        /// Performs the full roll sequence (animation + movement + invincibility).
        /// </summary>
        private IEnumerator PerformRoll()
        {
            isRolling = true;

            // disable collision with enemies (phase through them!)
            if (enemyLayerIndex != -1)
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, true);
                RefreshContactFilter(); // update cached layer mask!
            }

            // activate invincibility immediately
            bool wasInvincible = isInvincible;
            if (!wasInvincible)
            {
                isInvincible = true;
                invincibilityTimer = rollDuration;
                flashCoroutine = StartCoroutine(FlashSprite());
            }

            // determine roll direction based on sprite orientation
            float rollDirection = spriteRenderer.flipX ? -1f : 1f;

            // calculate roll BOOST (this gets ADDED to your current movement)
            float maxRollBoost = rollDirection * (rollDistance / rollDuration);


            // trigger roll animation
            animator.SetTrigger("roll");

            yield return null;

            // ease roll BOOST from max to 0 (player input still controls base movement!)
            float elapsedTime = 0f;
            while (elapsedTime < rollDuration)
            {
                float t = elapsedTime / rollDuration;
                // ease-out quadratic: boost goes from full to 0 smoothly
                float boostMultiplier = Mathf.Pow(1f - t, 2f);

                // rollVelocityX is just the BOOST (gets added to player input in ComputeVelocity)
                rollVelocityX = maxRollBoost * boostMultiplier;

                elapsedTime += DeltaTime;
                yield return null;
            }

            rollVelocityX = 0f; // boost ends

            // wait until animator exits roll state
            float timeout = rollDuration + 0.5f;
            float waitTime = 0f;
            while (animator.GetCurrentAnimatorStateInfo(0).IsName(rollStateName) && waitTime < timeout)
            {
                waitTime += DeltaTime;
                yield return null;
            }

            if (waitTime >= timeout)
            {
            }

            isRolling = false;
            rollCooldownTimer = rollCooldown;

            // re-enable collision with enemies
            if (enemyLayerIndex != -1)
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, false);
                RefreshContactFilter(); // update cached layer mask!
            }

        }

        /// <summary>
        /// perform the full ranged attack sequence (animation + spawn projectile at specific frame).
        /// </summary>
        private IEnumerator PerformRangedAttack()
        {
            if (rangedProjectilePrefab == null)
            {
                Debug.LogWarning("[RANGED] no projectile prefab assigned!");
                yield break;
            }

            isFiringRangedAttack = true;

            // trigger ranged attack animation
            animator.SetTrigger("rangedAttack");
            Debug.Log("[RANGED] animation triggered");

            // wait one frame for animator to transition
            yield return null;

            // wait until animation reaches the spawn frame
            bool projectileSpawned = false;
            while (animator.GetCurrentAnimatorStateInfo(0).IsName(rangedAttackStateName))
            {
                // get normalized time (0-1) of current animation
                float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f; // mod 1 handles looping animations

                // spawn projectile when animation reaches the target time
                if (!projectileSpawned && normalizedTime >= projectileSpawnAnimationTime)
                {
                    SpawnProjectile();
                    projectileSpawned = true;
                    Debug.Log($"[RANGED] projectile spawned at animation time {normalizedTime:F2} (target: {projectileSpawnAnimationTime:F2})");
                }

                yield return null;
            }

            // safety: if animation ended before reaching spawn time, spawn it now
            if (!projectileSpawned)
            {
                SpawnProjectile();
                Debug.LogWarning($"[RANGED] animation ended before reaching spawn time {projectileSpawnAnimationTime:F2}, spawned at end");
            }

            isFiringRangedAttack = false;

            // start cooldown
            rangedAttackCooldownTimer = rangedAttackCooldown;

            Debug.Log("[RANGED] attack complete");
        }

        /// <summary>
        /// spawn the ranged attack projectile at current player position.
        /// </summary>
        private void SpawnProjectile()
        {
            // determine direction based on sprite orientation
            float direction = spriteRenderer.flipX ? -1f : 1f;

            // calculate spawn position with offset
            Vector2 spawnOffset = new Vector2(projectileSpawnOffset.x * direction, projectileSpawnOffset.y);
            Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;

            // spawn projectile
            GameObject projectile = Instantiate(rangedProjectilePrefab, spawnPosition, Quaternion.identity);
            ProjectileFist fist = projectile.GetComponent<ProjectileFist>();

            if (fist != null)
            {
                fist.Initialize(direction, rangedAttackDamage);
                Debug.Log($"[RANGED] projectile created at {spawnPosition}, direction: {(direction > 0 ? "right" : "left")}, damage: {rangedAttackDamage}");
            }
            else
            {
                Debug.LogWarning("[RANGED] projectile prefab missing ProjectileFist component!");
            }
        }

        /// <summary>
        /// Perform a wall jump - jump away from wall in opposite direction.
        /// </summary>
        private void PerformWallJump()
        {
            // jump direction opposite of wall normal
            float jumpDirectionX = Mathf.Sign(wallNormal.x);

            // apply velocities
            velocity.x = jumpDirectionX * wallJumpHorizontalForce;
            velocity.y = wallJumpVerticalForce * model.jumpModifier;

            // flip sprite to face jump direction
            spriteRenderer.flipX = jumpDirectionX < 0;

            // reset wall state
            ResetWallState();

            // refresh double jump
            if (allowDoubleJump)
            {
                hasDoubleJump = true;
                hasUsedDoubleJump = false;
                Debug.Log("[WALL JUMP] double jump refreshed");
            }

            // set jump state
            jumpState = JumpState.InFlight;

            // play jump sound
            if (audioSource && jumpAudio)
                audioSource.PlayOneShot(jumpAudio);

            Debug.Log($"[WALL JUMP] jumped {(jumpDirectionX > 0 ? "right" : "left")} away from wall");
        }

        /// <summary>
        /// Reset all wall-related state variables.
        /// </summary>
        private void ResetWallState()
        {
            if (isTouchingWall || isWallSliding)
            {
                Debug.Log("[WALL JUMP] leaving wall");
            }

            isTouchingWall = false;
            isWallSliding = false;
            canWallJump = false;
            wallClingTimer = 0f;
            wallNormal = Vector2.zero;
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

            // check if this is an air attack (for bounce mechanic)
            bool isAirAttack = currentAttackState == attackAirStateName;
            bool hitEnemy = false;

            foreach (var hit in hits)
            {
                // try both enemy types
                var enemy1 = hit.GetComponent<enemy1>();
                var enemy2 = hit.GetComponent<enemy2>();

                bool isInvincible = false;
                if (enemy1 != null)
                {
                    isInvincible = enemy1.IsInvincible;
                    bool canHit = !isInvincible || HasSpeedBoost || HasTimeSlowActive;
                    if (canHit)
                    {
                        Vector2 knockbackDir = new Vector2(direction, 0.5f);
                        enemy1.TakeDamage(damage, knockbackDir, enemyKnockbackForce, HasSpeedBoost || HasTimeSlowActive);
                        hitEnemy = true;
                    }
                }
                else if (enemy2 != null)
                {
                    isInvincible = enemy2.IsInvincible;
                    bool canHit = !isInvincible || HasSpeedBoost || HasTimeSlowActive;
                    if (canHit)
                    {
                        Vector2 knockbackDir = new Vector2(direction, 0.5f);
                        enemy2.TakeDamage(damage, knockbackDir, enemyKnockbackForce, HasSpeedBoost || HasTimeSlowActive);
                        hitEnemy = true;
                    }
                }
            }

            // if air attacking and hit enemy, bounce!
            if (isAirAttack && hitEnemy)
            {
                Bounce(7); // bounce on successful air attack hit
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

            // reset time slow effects if active
            if (HasTimeSlowActive)
            {
                HasTimeSlowActive = false;
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                useUnscaledTime = false;

                // reset animator to 1.0
                animator.speed = 1f;

                // disable ghost trail
                var ghostTrail = GetComponent<GhostTrail>();
                if (ghostTrail != null)
                {
                    ghostTrail.DisableTrail();
                }

                // remove time vial color tint
                var colorManager = GetComponent<PowerupColorManager>();
                if (colorManager != null)
                {
                    colorManager.ClearAllColors(); // nuke all color tints on death
                }
            }

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            spriteRenderer.material = originalMaterial;

            // reset double jump state
            hasDoubleJump = false;
            hasUsedDoubleJump = false;

            // reset roll state
            isRolling = false;
            rollCooldownTimer = 0f;
            rollVelocityX = 0f;

            // reset ranged attack state
            rangedAttackCooldownTimer = 0f;
            isFiringRangedAttack = false;

            // re-enable collision with enemies (in case player died during roll)
            if (enemyLayerIndex != -1)
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, false);
                RefreshContactFilter(); // update cached layer mask!
            }

            // reset wall state
            ResetWallState();
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
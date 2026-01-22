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

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;
        private Coroutine flashCoroutine = null;
        private Color originalSpriteColor;

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

            m_MoveAction = InputSystem.actions.FindAction("Player/Move");
            m_JumpAction = InputSystem.actions.FindAction("Player/Jump");

            m_MoveAction.Enable();
            m_JumpAction.Enable();

            Debug.Log($"[PlayerController] Awake - Health Max HP: {health?.maxHP}, Invincibility Duration: {invincibilityDuration}, Original Color: {originalSpriteColor}");

            if (invincibilityDuration <= 0)
            {
                Debug.LogWarning("[PlayerController] Invincibility Duration is 0 or less! Player won't have i-frames. Set it to 1.0 in the inspector.");
            }
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = m_MoveAction.ReadValue<Vector2>().x;
                if (jumpState == JumpState.Grounded && m_JumpAction.WasPressedThisFrame())
                    jumpState = JumpState.PrepareToJump;
                else if (m_JumpAction.WasReleasedThisFrame())
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
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
                    Debug.Log("[PlayerController] Invincibility ended, sprite color reset to original");
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

            targetVelocity = move * maxSpeed;
        }

        /// <summary>
        /// Activates invincibility frames with sprite flashing effect.
        /// </summary>
        public void ActivateInvincibility()
        {
            Debug.Log("[PlayerController] ActivateInvincibility called!");

            // stop any existing flash coroutine
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
                Debug.Log("[PlayerController] Stopped existing flash coroutine");
            }

            // always reset sprite color to original before starting
            spriteRenderer.color = originalSpriteColor;

            if (!isInvincible)
            {
                isInvincible = true;
                invincibilityTimer = invincibilityDuration;
                Debug.Log($"[PlayerController] Starting invincibility for {invincibilityDuration} seconds");
                flashCoroutine = StartCoroutine(FlashSprite());
            }
            else
            {
                Debug.Log("[PlayerController] Already invincible, restarting timer");
                invincibilityTimer = invincibilityDuration;
                flashCoroutine = StartCoroutine(FlashSprite());
            }
        }

        /// <summary>
        /// Coroutine that makes the sprite flash during invincibility.
        /// </summary>
        private IEnumerator FlashSprite()
        {
            Debug.Log("[PlayerController] FlashSprite coroutine started");
            while (isInvincible)
            {
                // flash bright white (more visible)
                spriteRenderer.color = new Color(2f, 2f, 2f, 1f);
                yield return new WaitForSeconds(flashInterval);

                // flash back to original color (if still invincible)
                if (isInvincible)
                {
                    spriteRenderer.color = originalSpriteColor;
                    yield return new WaitForSeconds(flashInterval);
                }
            }
            // ensure sprite is back to original color when invincibility ends
            spriteRenderer.color = originalSpriteColor;
            Debug.Log("[PlayerController] FlashSprite ended, sprite reset to original color");
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
            Debug.Log($"[PlayerController] visual state reset (sprite color = {originalSpriteColor})");
        }

        /// <summary>
        /// reset sprite to face right (default direction)
        /// </summary>
        public void ResetOrientation()
        {
            spriteRenderer.flipX = false;
            Debug.Log("[PlayerController] orientation reset to face right");
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
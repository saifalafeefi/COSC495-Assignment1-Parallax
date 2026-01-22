using Platformer.Core;
using Platformer.Mechanics;
using Platformer.Model;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Gameplay
{

    /// <summary>
    /// Fired when a Player collides with an Enemy.
    /// </summary>
    /// <typeparam name="EnemyCollision"></typeparam>
    public class PlayerEnemyCollision : Simulation.Event<PlayerEnemyCollision>
    {
        public EnemyController enemy;
        public PlayerController player;

        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            Debug.Log("[PlayerEnemyCollision] Collision detected!");
            var willHurtEnemy = player.Bounds.center.y >= enemy.Bounds.max.y;

            if (willHurtEnemy)
            {
                // player is attacking from above - damage enemy
                var enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.Decrement();
                    if (!enemyHealth.IsAlive)
                    {
                        Schedule<EnemyDeath>().enemy = enemy;
                        player.Bounce(2);
                    }
                    else
                    {
                        player.Bounce(7);
                    }
                }
                else
                {
                    Schedule<EnemyDeath>().enemy = enemy;
                    player.Bounce(2);
                }
            }
            else
            {
                // player hit by enemy - take damage if not invincible
                Debug.Log($"[PlayerEnemyCollision] Player hit! IsInvincible: {player.IsInvincible}");
                if (!player.IsInvincible)
                {
                    var playerHealth = player.health;
                    if (playerHealth != null)
                    {
                        Debug.Log($"[PlayerEnemyCollision] Decrementing health from {playerHealth.CurrentHP}");
                        playerHealth.Decrement();
                        Debug.Log($"[PlayerEnemyCollision] Health now: {playerHealth.CurrentHP}");

                        // check if player died - handle death immediately without invincibility
                        if (!playerHealth.IsAlive)
                        {
                            Debug.Log("[PlayerEnemyCollision] Player HP reached 0, scheduling death");
                            Schedule<PlayerDeath>();
                        }
                        else
                        {
                            // player survived - apply hurt effects
                            // play hurt sound
                            if (player.ouchAudio != null && player.audioSource != null)
                            {
                                player.audioSource.PlayOneShot(player.ouchAudio);
                            }

                            // activate invincibility frames
                            player.ActivateInvincibility();

                            // apply knockback away from enemy
                            Vector2 knockbackDirection = new Vector2(
                                player.Bounds.center.x - enemy.Bounds.center.x,
                                1f // slight upward force
                            );
                            player.ApplyKnockback(knockbackDirection, 5f);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerEnemyCollision] No health component, instant death!");
                        // no health component, instant death (old behavior)
                        Schedule<PlayerDeath>();
                    }
                }
                else
                {
                    Debug.Log("[PlayerEnemyCollision] Player is invincible, ignoring collision");
                }
            }
        }
    }
}
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
        public MonoBehaviour enemy;
        public PlayerController player;

        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {

            // ignore collision if player is rolling (phasing through enemies)
            if (player != null && player.IsRolling)
            {
                return;
            }

            // ignore collision if enemy is dead or doesn't exist
            if (enemy == null)
            {
                return;
            }

            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null)
            {
            }
            else
            {
            }

            if (enemyHealth != null && !enemyHealth.IsAlive)
            {
                return;
            }

            // get enemy bounds (works for both enemy1 and enemy2)
            Bounds enemyBounds;
            var enemy1 = enemy as enemy1;
            var enemy2 = enemy as enemy2;
            if (enemy1 != null)
                enemyBounds = enemy1.Bounds;
            else if (enemy2 != null)
                enemyBounds = enemy2.Bounds;
            else
                return; // not a valid enemy

            // player only hurts enemy if ATTACKING (from any angle!)
            bool playerIsAttacking = player.isAttacking;
            var willHurtEnemy = playerIsAttacking;

            if (willHurtEnemy)
            {
                // player is attacking from above - damage enemy
                // calculate knockback direction (downward from player)
                Vector2 knockbackDir = new Vector2(0, -1f);

                // call TakeDamage on the specific enemy type (so all death logic runs!)
                if (enemy1 != null)
                {
                    enemy1.TakeDamage(1, knockbackDir, 0f);
                    player.Bounce(enemyHealth != null && enemyHealth.IsAlive ? 7 : 2);
                }
                else if (enemy2 != null)
                {
                    enemy2.TakeDamage(1, knockbackDir, 0f);
                    player.Bounce(enemyHealth != null && enemyHealth.IsAlive ? 7 : 2);
                }
                else
                {
                    // fallback for unknown enemy type
                    Schedule<EnemyDeath>().enemy = enemy;
                    player.Bounce(2);
                }
            }
            else
            {
                // player hit by enemy - take damage if not invincible
                if (!player.IsInvincible)
                {
                    var playerHealth = player.health;
                    if (playerHealth != null)
                    {
                        playerHealth.Decrement();

                        // check if player died - handle death immediately without invincibility
                        if (!playerHealth.IsAlive)
                        {
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
                                player.Bounds.center.x - enemyBounds.center.x,
                                1f // slight upward force
                            );
                            player.ApplyKnockback(knockbackDirection, 5f);
                        }
                    }
                    else
                    {
                        // no health component, instant death (old behavior)
                        Schedule<PlayerDeath>();
                    }
                }
                else
                {
                }
            }
        }
    }
}
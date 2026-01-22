using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the player has died.
    /// </summary>
    /// <typeparam name="PlayerDeath"></typeparam>
    public class PlayerDeath : Simulation.Event<PlayerDeath>
    {
        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            var player = model.player;
            Debug.Log("[PlayerDeath] Execute called");

            // make sure health is actually 0
            if (player.health.CurrentHP > 0)
            {
                Debug.Log("[PlayerDeath] Health > 0, forcing death");
                player.health.Die();
            }

            // reset sprite color and invincibility state
            player.ResetVisualState();

            model.virtualCamera.Follow = null;
            model.virtualCamera.LookAt = null;

            // disable collider so enemies can't hit the corpse
            player.collider2d.enabled = false;
            player.controlEnabled = false;
            Debug.Log("[PlayerDeath] Disabled player collider and controls");

            if (player.audioSource && player.ouchAudio)
                player.audioSource.PlayOneShot(player.ouchAudio);

            Debug.Log("[PlayerDeath] Setting death animation");
            player.animator.SetTrigger("hurt");
            player.animator.SetBool("dead", true);

            Simulation.Schedule<PlayerSpawn>(2);
            Debug.Log("[PlayerDeath] Scheduled respawn in 2 seconds");
        }
    }
}
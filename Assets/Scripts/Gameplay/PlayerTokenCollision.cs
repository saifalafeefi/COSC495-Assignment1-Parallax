using Platformer.Core;
using Platformer.Mechanics;
using Platformer.Model;
using Platformer.UI;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when a player collides with a token.
    /// </summary>
    /// <typeparam name="PlayerCollision"></typeparam>
    public class PlayerTokenCollision : Simulation.Event<PlayerTokenCollision>
    {
        public PlayerController player;
        public TokenInstance token;

        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            AudioSource.PlayClipAtPoint(token.tokenCollectAudio, token.transform.position);

            // add score for collecting ink (uses token's scoreValue)
            var gameHUD = Object.FindFirstObjectByType<GameHUD>();
            if (gameHUD != null && token != null)
            {
                gameHUD.AddScore(token.scoreValue);
            }
        }
    }
}
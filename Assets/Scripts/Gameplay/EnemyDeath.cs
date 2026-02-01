using Platformer.Core;
using Platformer.Mechanics;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the health component on an enemy has a hitpoint value of  0.
    /// </summary>
    /// <typeparam name="EnemyDeath"></typeparam>
    public class EnemyDeath : Simulation.Event<EnemyDeath>
    {
        public MonoBehaviour enemy;

        public override void Execute()
        {
            // check if it's enemy1 or enemy2 and disable accordingly
            var enemy1 = enemy as enemy1;
            var enemy2 = enemy as enemy2;

            if (enemy1 != null)
            {
                enemy1._collider.enabled = false;
                enemy1.enabled = false;
                if (enemy1._audio && enemy1.ouch)
                    enemy1._audio.PlayOneShot(enemy1.ouch);
            }
            else if (enemy2 != null)
            {
                enemy2._collider.enabled = false;
                enemy2.enabled = false;
                if (enemy2._audio && enemy2.ouch)
                    enemy2._audio.PlayOneShot(enemy2.ouch);
            }
        }
    }
}
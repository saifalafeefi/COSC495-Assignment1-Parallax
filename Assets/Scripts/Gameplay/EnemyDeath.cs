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
            // check enemy type and disable accordingly
            var enemy1 = enemy as enemy1;
            var enemy2 = enemy as enemy2;
            var enemy3 = enemy as enemy3;

            if (enemy1 != null)
            {
                enemy1._collider.enabled = false;
                enemy1.enabled = false;
                // play random death sound
                PlayRandomSound(enemy1._audio, enemy1.hitSounds, enemy1.hitSoundVolume);
            }
            else if (enemy2 != null)
            {
                enemy2._collider.enabled = false;
                enemy2.enabled = false;
                // play random death sound
                PlayRandomSound(enemy2._audio, enemy2.hitSounds, enemy2.hitSoundVolume);
            }
            else if (enemy3 != null)
            {
                enemy3._collider.enabled = false;
                enemy3.enabled = false;
                // play random death sound
                PlayRandomSound(enemy3._audio, enemy3.hitSounds, enemy3.hitSoundVolume);
            }
        }

        /// <summary>
        /// plays a random sound from the provided array with volume control.
        /// </summary>
        private void PlayRandomSound(AudioSource audioSource, AudioClip[] sounds, float volume)
        {
            if (sounds != null && sounds.Length > 0 && audioSource != null)
            {
                AudioClip randomSound = sounds[Random.Range(0, sounds.Length)];
                if (randomSound != null)
                {
                    audioSource.Stop();
                    audioSource.clip = randomSound;
                    audioSource.volume = volume;
                    audioSource.Play();
                }
            }
        }
    }
}
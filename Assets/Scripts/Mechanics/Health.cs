using System;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Represebts the current vital statistics of some game entity.
    /// </summary>
    public class Health : MonoBehaviour
    {
        /// <summary>
        /// The maximum hit points for the entity.
        /// </summary>
        public int maxHP = 3;

        /// <summary>
        /// Indicates if the entity should be considered 'alive'.
        /// </summary>
        public bool IsAlive => currentHP > 0;

        /// <summary>
        /// Get the current HP value for UI updates.
        /// </summary>
        public int CurrentHP => currentHP;

        int currentHP;

        /// <summary>
        /// Increment the HP of the entity.
        /// </summary>
        public void Increment()
        {
            currentHP = Mathf.Clamp(currentHP + 1, 0, maxHP);
        }

        /// <summary>
        /// Reset HP to maximum value (used on respawn).
        /// </summary>
        public void ResetToFull()
        {
            int oldHP = currentHP;
            currentHP = maxHP;
            Debug.Log($"[Health] {gameObject.name} - HP reset: {oldHP} -> {currentHP} (full)");
        }

        /// <summary>
        /// Decrement the HP of the entity. Will trigger a HealthIsZero event when
        /// current HP reaches 0.
        /// </summary>
        public void Decrement()
        {
            int oldHP = currentHP;
            currentHP = Mathf.Clamp(currentHP - 1, 0, maxHP);
            Debug.Log($"[Health] {gameObject.name} - HP: {oldHP} -> {currentHP}");
            if (currentHP == 0)
            {
                Debug.Log($"[Health] {gameObject.name} - HP reached 0, triggering HealthIsZero event");
                var ev = Schedule<HealthIsZero>();
                ev.health = this;
            }
        }

        /// <summary>
        /// Decrement the HP of the entitiy until HP reaches 0.
        /// </summary>
        public void Die()
        {
            while (currentHP > 0) Decrement();
        }

        void Awake()
        {
            currentHP = maxHP;
            Debug.Log($"[Health] {gameObject.name} - Awake, maxHP: {maxHP}, currentHP: {currentHP}");
        }
    }
}

using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// manages shared duration for damage vials.
    /// allows multiple vials to reset the timer to full duration.
    /// </summary>
    public class DamageBoostState : MonoBehaviour
    {
        /// <summary>
        /// shared remaining duration across all damage vial pickups (in seconds).
        /// </summary>
        public float remainingDuration = 0f;

        /// <summary>
        /// reset the damage boost duration back to full amount.
        /// </summary>
        /// <param name="duration">duration to reset to in seconds</param>
        public void ResetDuration(float duration)
        {
            remainingDuration = duration;
        }
    }
}

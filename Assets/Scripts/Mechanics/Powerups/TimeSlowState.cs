using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// manages shared duration for time vials.
    /// allows multiple vials to reset the timer to full duration.
    /// </summary>
    public class TimeSlowState : MonoBehaviour
    {
        /// <summary>
        /// shared remaining duration across all time vial pickups (in seconds, real time).
        /// </summary>
        public float remainingDuration = 0f;

        /// <summary>
        /// reset the time slow duration back to full amount.
        /// </summary>
        /// <param name="duration">duration to reset to in seconds</param>
        public void ResetDuration(float duration)
        {
            remainingDuration = duration;
        }
    }
}

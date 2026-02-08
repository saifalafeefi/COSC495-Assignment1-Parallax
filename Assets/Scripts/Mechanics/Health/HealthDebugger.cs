using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Simple debug component to display health info in the console.
    /// Attach to Player to see health changes in real-time.
    /// </summary>
    public class HealthDebugger : MonoBehaviour
    {
        private Health health;
        private int lastHP = -1;

        void Start()
        {
            health = GetComponent<Health>();
            if (health != null)
            {
                lastHP = health.CurrentHP;
            }
            else
            {
            }
        }

        void Update()
        {
            if (health != null && health.CurrentHP != lastHP)
            {
                lastHP = health.CurrentHP;
            }
        }
    }
}

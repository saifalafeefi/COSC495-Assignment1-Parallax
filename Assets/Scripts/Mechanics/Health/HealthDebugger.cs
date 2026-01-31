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
                Debug.Log($"[HealthDebugger] Health component found! Max HP: {health.maxHP}, Current HP: {health.CurrentHP}");
                lastHP = health.CurrentHP;
            }
            else
            {
                Debug.LogError("[HealthDebugger] No Health component found on this GameObject!");
            }
        }

        void Update()
        {
            if (health != null && health.CurrentHP != lastHP)
            {
                Debug.Log($"[HealthDebugger] HP Changed: {lastHP} -> {health.CurrentHP}");
                lastHP = health.CurrentHP;
            }
        }
    }
}

using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Platformer.Mechanics;

namespace Platformer.UI
{
    /// <summary>
    /// displays active powerup timers in collection order.
    /// updates every 0.2 seconds with colored text based on powerup type.
    /// </summary>
    public class PowerupTimerUI : MonoBehaviour
    {
        [Header("Settings")]
        /// <summary>
        /// how often to update the timer displays (in seconds).
        /// </summary>
        public float updateInterval = 0.2f;

        /// <summary>
        /// vertical spacing between powerup timer entries (pixels).
        /// </summary>
        public float entrySpacing = 25f;

        /// <summary>
        /// font size for powerup timer text.
        /// </summary>
        public float fontSize = 24f;

        [Header("Colors")]
        /// <summary>
        /// color for Time Vial text.
        /// </summary>
        public Color timeVialColor = new Color(0.7f, 0.3f, 1f); // purple

        /// <summary>
        /// color for Speed Vial text.
        /// </summary>
        public Color speedVialColor = new Color(1f, 1f, 0f); // yellow

        /// <summary>
        /// color for Damage Vial text.
        /// </summary>
        public Color damageVialColor = new Color(1f, 0.3f, 0.3f); // red

        // internal state
        private PlayerController player;
        private Dictionary<string, PowerupTimerEntry> activeTimers = new Dictionary<string, PowerupTimerEntry>();
        private List<string> collectionOrder = new List<string>(); // tracks order powerups were collected

        /// <summary>
        /// represents a single powerup timer entry.
        /// </summary>
        private class PowerupTimerEntry
        {
            public TextMeshProUGUI textObject;
            public int orderIndex; // position in collection order
        }

        void Start()
        {
            // find player
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[POWERUP TIMER UI] No PlayerController found!");
                return;
            }

            // start updating timers
            InvokeRepeating(nameof(UpdatePowerupTimers), 0f, updateInterval);
        }

        /// <summary>
        /// update all powerup timer displays (called every 0.2s).
        /// </summary>
        void UpdatePowerupTimers()
        {
            if (player == null) return;

            // check each powerup type
            UpdatePowerupDisplay("time", player.HasTimeSlowActive,
                () => player.GetComponent<TimeSlowState>()?.remainingDuration ?? 0f,
                "Time Vial", timeVialColor);

            UpdatePowerupDisplay("speed", player.HasSpeedBoost,
                () => player.GetComponent<SpeedBoostState>()?.remainingDuration ?? 0f,
                "Speed Vial", speedVialColor);

            UpdatePowerupDisplay("damage", player.HasDamageBoost,
                () => player.GetComponent<DamageBoostState>()?.remainingDuration ?? 0f,
                "Damage Vial", damageVialColor);
        }

        /// <summary>
        /// update a single powerup display (create/update/destroy as needed).
        /// </summary>
        void UpdatePowerupDisplay(string key, bool isActive, System.Func<float> getDuration, string displayName, Color color)
        {
            if (isActive)
            {
                float remainingTime = getDuration();

                // create entry if it doesn't exist
                if (!activeTimers.ContainsKey(key))
                {
                    CreateTimerEntry(key, displayName, color);
                }

                // update text
                var entry = activeTimers[key];
                if (entry.textObject != null)
                {
                    entry.textObject.text = $"{displayName}: {remainingTime:F1}s";
                }
            }
            else
            {
                // remove entry if it exists
                if (activeTimers.ContainsKey(key))
                {
                    DestroyTimerEntry(key);
                }
            }
        }

        /// <summary>
        /// create a new timer entry and add it to the display.
        /// </summary>
        void CreateTimerEntry(string key, string displayName, Color color)
        {
            // create text object
            GameObject textObj = new GameObject($"PowerupTimer_{key}");
            textObj.transform.SetParent(transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap; // prevent text from wrapping to multiple lines
            tmp.overflowMode = TextOverflowModes.Overflow; // allow text to overflow instead of wrap
            tmp.text = $"{displayName}: 0.0s";

            // configure rect transform
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1); // top-left anchor
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.sizeDelta = new Vector2(400, 50); // wide enough for text at any reasonable font size

            // determine position based on collection order
            int orderIndex;
            if (collectionOrder.Contains(key))
            {
                // reuse existing position (vial was picked up again)
                orderIndex = collectionOrder.IndexOf(key);
            }
            else
            {
                // new powerup - add to end of collection order
                orderIndex = collectionOrder.Count;
                collectionOrder.Add(key);
            }

            // position based on order index
            float yOffset = -orderIndex * entrySpacing;
            rectTransform.anchoredPosition = new Vector2(0, yOffset);

            // create entry
            var entry = new PowerupTimerEntry
            {
                textObject = tmp,
                orderIndex = orderIndex
            };
            activeTimers[key] = entry;
        }

        /// <summary>
        /// destroy a timer entry and remove it from the display.
        /// </summary>
        void DestroyTimerEntry(string key)
        {
            if (activeTimers.TryGetValue(key, out var entry))
            {
                if (entry.textObject != null)
                {
                    Destroy(entry.textObject.gameObject);
                }
                activeTimers.Remove(key);

                // NOTE: Don't remove from collectionOrder! This preserves position if powerup is picked up again
            }
        }

        /// <summary>
        /// clear all timer entries (called on player death/respawn).
        /// </summary>
        public void ClearAllTimers()
        {
            foreach (var entry in activeTimers.Values)
            {
                if (entry.textObject != null)
                {
                    Destroy(entry.textObject.gameObject);
                }
            }
            activeTimers.Clear();
            collectionOrder.Clear();
        }
    }
}

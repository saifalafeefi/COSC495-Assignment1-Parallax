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

        /// <summary>
        /// optional template for timer entries (assign a TextMeshProUGUI from the scene/prefab).
        /// when set, styling/layout is controlled by the template.
        /// </summary>
        public TextMeshProUGUI entryTemplate;

        /// <summary>
        /// use template's anchored position as base (otherwise uses baseOffset).
        /// </summary>
        public bool useTemplatePosition = true;

        /// <summary>
        /// when using a template, do not override its RectTransform/layout.
        /// </summary>
        public bool respectTemplateLayout = true;

        /// <summary>
        /// stack entries using entrySpacing (prevents overlap).
        /// </summary>
        public bool stackEntries = true;

        /// <summary>
        /// base anchored position offset applied to all entries.
        /// </summary>
        public Vector2 baseOffset = Vector2.zero;

        /// <summary>
        /// apply powerup color to template text (otherwise keeps template color).
        /// </summary>
        public bool applyColorToTemplate = true;

        /// <summary>
        /// override font settings even when using a template.
        /// </summary>
        public bool overrideFont = false;
        public TMP_FontAsset overrideFontAsset;
        public bool overrideFontSize = false;
        /// <summary>
        /// override material preset (use this to apply outlines/shaders).
        /// </summary>
        public bool overrideMaterial = false;
        public Material overrideMaterialPreset;

        [Header("Outline (Code)")]
        /// <summary>
        /// enable outline via code (creates a material instance per entry).
        /// </summary>
        public bool outlineEnabled = true;
        /// <summary>
        /// outline width for timer text.
        /// </summary>
        [Range(0f, 1f)]
        public float outlineWidth = 0.2f;
        /// <summary>
        /// outline color for timer text.
        /// </summary>
        public Color outlineColor = Color.black;

        /// <summary>
        /// format for timer text.
        /// </summary>
        public string timerFormat = "{0}: {1:F1}s";

        /// <summary>
        /// display labels (editable in inspector).
        /// </summary>
        public string timeLabel = "Time Vial";
        public string speedLabel = "Speed Vial";
        public string damageLabel = "Damage Vial";

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
                timeLabel, timeVialColor);

            UpdatePowerupDisplay("speed", player.HasSpeedBoost,
                () => player.GetComponent<SpeedBoostState>()?.remainingDuration ?? 0f,
                speedLabel, speedVialColor);

            UpdatePowerupDisplay("damage", player.HasDamageBoost,
                () => player.GetComponent<DamageBoostState>()?.remainingDuration ?? 0f,
                damageLabel, damageVialColor);
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
                    entry.textObject.text = string.Format(timerFormat, displayName, remainingTime);
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
            TextMeshProUGUI tmp;
            RectTransform rectTransform;

            if (entryTemplate != null)
            {
                // use template for styling/layout
                tmp = Instantiate(entryTemplate, transform);
                tmp.gameObject.name = $"PowerupTimer_{key}";
                if (applyColorToTemplate)
                {
                    tmp.color = color;
                }
                // keep template material unless override is requested
                if (overrideMaterial && overrideMaterialPreset != null)
                {
                    tmp.fontSharedMaterial = overrideMaterialPreset;
                }
                ApplyOutlineIfNeeded(tmp);
                if (overrideFont && overrideFontAsset != null)
                {
                    tmp.font = overrideFontAsset;
                }
                if (overrideFontSize)
                {
                    tmp.fontSize = fontSize;
                }
                tmp.text = string.Format(timerFormat, displayName, 0f);
                rectTransform = tmp.GetComponent<RectTransform>();
            }
            else
            {
                // create text object
                GameObject textObj = new GameObject($"PowerupTimer_{key}");
                textObj.transform.SetParent(transform, false);

                tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.textWrappingMode = TextWrappingModes.NoWrap; // prevent text from wrapping to multiple lines
                tmp.overflowMode = TextOverflowModes.Overflow; // allow text to overflow instead of wrap
                if (overrideMaterial && overrideMaterialPreset != null)
                {
                    tmp.fontSharedMaterial = overrideMaterialPreset;
                }
                ApplyOutlineIfNeeded(tmp);
                if (overrideFont && overrideFontAsset != null)
                {
                    tmp.font = overrideFontAsset;
                }
                if (overrideFontSize)
                {
                    tmp.fontSize = fontSize;
                }
                tmp.text = string.Format(timerFormat, displayName, 0f);

                // configure rect transform
                rectTransform = textObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1); // top-left anchor
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.sizeDelta = new Vector2(400, 50); // wide enough for text at any reasonable font size
            }

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
            if (stackEntries)
            {
                float yOffset = -orderIndex * entrySpacing;
                Vector2 basePos = Vector2.zero;
                if (entryTemplate != null && useTemplatePosition)
                {
                    basePos = rectTransform.anchoredPosition;
                }
                basePos += baseOffset;
                rectTransform.anchoredPosition = new Vector2(basePos.x, basePos.y + yOffset);
            }

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

        private void ApplyOutlineIfNeeded(TextMeshProUGUI tmp)
        {
            if (!outlineEnabled || tmp == null) return;

            // create a unique material instance so we can tweak outline without affecting shared assets
            var baseMat = tmp.fontSharedMaterial;
            if (baseMat == null) return;

            var matInstance = new Material(baseMat);
            matInstance.EnableKeyword("OUTLINE_ON");
            matInstance.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            matInstance.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            tmp.fontMaterial = matInstance;
        }
    }
}

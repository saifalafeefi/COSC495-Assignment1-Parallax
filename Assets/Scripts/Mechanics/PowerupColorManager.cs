using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// manages blending of multiple powerup colors on the player sprite.
    /// </summary>
    public class PowerupColorManager : MonoBehaviour
    {
        private Dictionary<string, Color> activePowerupColors = new Dictionary<string, Color>();
        private SpriteRenderer spriteRenderer;
        private Color originalColor;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer.color;
        }

        /// <summary>
        /// add a powerup color to the blend.
        /// </summary>
        /// <param name="powerupID">unique ID for this powerup instance</param>
        /// <param name="color">color to add to the blend</param>
        public void AddColor(string powerupID, Color color)
        {
            activePowerupColors[powerupID] = color;
            UpdateSpriteColor();
            Debug.Log($"[COLOR MGR] Added {powerupID}: {color}, Total active: {activePowerupColors.Count}");
        }

        /// <summary>
        /// update an existing powerup color and trigger sprite update (for fades).
        /// </summary>
        /// <param name="powerupID">unique ID for this powerup instance</param>
        /// <param name="color">new color value</param>
        public void UpdateColor(string powerupID, Color color)
        {
            if (activePowerupColors.ContainsKey(powerupID))
            {
                activePowerupColors[powerupID] = color;
                UpdateSpriteColor();
            }
        }

        /// <summary>
        /// remove a powerup color from the blend.
        /// </summary>
        /// <param name="powerupID">unique ID of the powerup to remove</param>
        public void RemoveColor(string powerupID)
        {
            if (activePowerupColors.Remove(powerupID))
            {
                UpdateSpriteColor();
                Debug.Log($"[COLOR MGR] Removed {powerupID}, Total active: {activePowerupColors.Count}");
            }
        }

        /// <summary>
        /// get the current blended color of all active powerups.
        /// </summary>
        public Color GetCurrentBlend()
        {
            if (activePowerupColors.Count == 0)
            {
                return originalColor;
            }

            // average all active colors
            Color blend = Color.black;
            foreach (var color in activePowerupColors.Values)
            {
                blend += color;
            }
            blend /= activePowerupColors.Count;
            blend.a = 1f; // ensure full opacity

            return blend;
        }

        /// <summary>
        /// get what the blend would be WITHOUT a specific powerup (for warning pulses).
        /// </summary>
        /// <param name="excludePowerupID">ID of the powerup to exclude</param>
        public Color GetBlendWithout(string excludePowerupID)
        {
            if (activePowerupColors.Count <= 1)
            {
                // if this is the only powerup, blend without it = original
                return originalColor;
            }

            // average all colors EXCEPT the excluded one
            Color blend = Color.black;
            int count = 0;
            foreach (var kvp in activePowerupColors)
            {
                if (kvp.Key != excludePowerupID)
                {
                    blend += kvp.Value;
                    count++;
                }
            }

            if (count == 0)
            {
                return originalColor;
            }

            blend /= count;
            blend.a = 1f; // ensure full opacity

            return blend;
        }

        /// <summary>
        /// manually set the sprite color (used during warning pulses).
        /// </summary>
        public void SetSpriteColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        /// <summary>
        /// update the sprite to show the current blend of all active powerups.
        /// </summary>
        private void UpdateSpriteColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetCurrentBlend();
                Debug.Log($"[COLOR MGR] Sprite color updated to: {spriteRenderer.color}");
            }
        }

        /// <summary>
        /// get the original sprite color before any powerups.
        /// </summary>
        public Color OriginalColor => originalColor;
    }
}

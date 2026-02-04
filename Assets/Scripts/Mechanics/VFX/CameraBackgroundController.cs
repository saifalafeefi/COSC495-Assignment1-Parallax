using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// controls camera background color tint for powerup visual feedback with color blending.
    /// </summary>
    public class CameraBackgroundController : MonoBehaviour
    {
        [Header("References")]
        /// <summary>
        /// main camera to tint (auto-found if empty).
        /// </summary>
        public Camera mainCamera;
        /// <summary>
        /// grayscale material to tint along with camera background (optional).
        /// drag the material asset here, or it will auto-find from grayscaleRenderer.
        /// </summary>
        public Material grayscaleMaterial;
        /// <summary>
        /// renderer using the grayscale material (optional - for auto-finding material instance).
        /// </summary>
        public Renderer grayscaleRenderer;

        [Header("Color Settings")]
        /// <summary>
        /// default background color when no powerups active.
        /// </summary>
        public Color defaultBackgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f); // light blue sky
        /// <summary>
        /// default grayscale material color when no powerups active.
        /// </summary>
        public Color defaultGrayscaleColor = Color.white; // white for normal grayscale
        /// <summary>
        /// opacity of powerup tints (0 = invisible/default color, 1 = full tint color).
        /// </summary>
        [Range(0f, 1f)]
        public float tintOpacity = 0.7f;

        [Header("Pulse/Breathing Effect")]
        /// <summary>
        /// enable pulsing effect when powerups are active.
        /// </summary>
        public bool enablePulsing = true;
        /// <summary>
        /// how many pulses per second (breathing speed).
        /// </summary>
        public float pulseSpeed = 1.5f;
        /// <summary>
        /// how much to pulse towards default color (0 = no pulse, 1 = fully pulse to default).
        /// </summary>
        [Range(0f, 1f)]
        public float pulseIntensity = 0.3f;

        [Header("Fade Settings")]
        /// <summary>
        /// how long the tint fades back to default when all powerups expire (in seconds).
        /// </summary>
        public float fadeDuration = 1f;

        private Dictionary<string, Color> activePowerupColors = new Dictionary<string, Color>();
        private HashSet<string> pulsingPowerupIDs = new HashSet<string>(); // track which powerups should pulse
        private Coroutine pulseCoroutine;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            // find main camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("[CAMERA BACKGROUND] Main Camera not found!");
                enabled = false;
                return;
            }

            // get grayscale material from renderer if not assigned directly
            if (grayscaleMaterial == null && grayscaleRenderer != null)
            {
                grayscaleMaterial = grayscaleRenderer.material; // gets instance, not shared
                Debug.Log($"[CAMERA BACKGROUND] Found grayscale material instance from {grayscaleRenderer.name}");
            }

            // set grayscale material to default color at startup
            if (grayscaleMaterial != null)
            {
                grayscaleMaterial.SetColor("_Color", defaultGrayscaleColor);
                Debug.Log($"[CAMERA BACKGROUND] Grayscale material setup complete. Default color: {defaultGrayscaleColor}");
            }
            else
            {
                Debug.LogWarning("[CAMERA BACKGROUND] No grayscale material or renderer assigned - grayscale tinting disabled");
            }
        }

        /// <summary>
        /// add a powerup color to the blend.
        /// </summary>
        /// <param name="powerupID">unique ID for this powerup instance</param>
        /// <param name="color">color to blend</param>
        /// <param name="shouldPulse">whether this color should pulse (true for timed buffs, false for instant effects)</param>
        public void AddColor(string powerupID, Color color, bool shouldPulse = true)
        {
            activePowerupColors[powerupID] = color;

            if (shouldPulse)
            {
                pulsingPowerupIDs.Add(powerupID);
            }
            else
            {
                pulsingPowerupIDs.Remove(powerupID);
            }

            UpdateBackgroundColor();
        }

        /// <summary>
        /// update an existing powerup color in the blend (for manual fading).
        /// </summary>
        public void UpdateColor(string powerupID, Color newColor)
        {
            if (activePowerupColors.ContainsKey(powerupID))
            {
                activePowerupColors[powerupID] = newColor;
                // don't call UpdateBackgroundColor() - color updates happen every frame in pulse loop
            }
        }

        /// <summary>
        /// remove a powerup color from the blend.
        /// </summary>
        public void RemoveColor(string powerupID)
        {
            if (activePowerupColors.Remove(powerupID))
            {
                pulsingPowerupIDs.Remove(powerupID);
                UpdateBackgroundColor();
            }
        }

        /// <summary>
        /// clear all powerup colors and fade back to default.
        /// </summary>
        public void ClearAllColors()
        {
            activePowerupColors.Clear();
            pulsingPowerupIDs.Clear();
            UpdateBackgroundColor();
        }

        /// <summary>
        /// get the current blended color of all active powerups.
        /// </summary>
        public Color GetCurrentBlend()
        {
            if (activePowerupColors.Count == 0)
            {
                return defaultBackgroundColor;
            }

            // average all active colors (same as PowerupColorManager)
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
        /// update the background to show the current blend of all active powerups.
        /// </summary>
        private void UpdateBackgroundColor()
        {
            if (mainCamera == null)
                return;

            // stop any active fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (activePowerupColors.Count > 0)
            {
                // powerups active - start pulsing with blended color
                if (pulseCoroutine == null)
                {
                    pulseCoroutine = StartCoroutine(PulseBackground());
                }
            }
            else
            {
                // no powerups - stop pulsing and fade back to default
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }
                fadeCoroutine = StartCoroutine(FadeToDefault());
            }
        }

        /// <summary>
        /// pulse the background with the current blended color.
        /// </summary>
        private IEnumerator PulseBackground()
        {
            float elapsed = 0f;

            while (activePowerupColors.Count > 0)
            {
                elapsed += Time.deltaTime;

                // get current blended color
                Color blendedColor = GetCurrentBlend();

                // apply opacity for camera background (blend between camera default and powerup color)
                Color cameraTargetColor = Color.Lerp(defaultBackgroundColor, blendedColor, tintOpacity);

                // apply opacity for grayscale material (blend between grayscale default and powerup color)
                Color grayscaleTargetColor = Color.Lerp(defaultGrayscaleColor, blendedColor, tintOpacity);

                // only pulse if there are powerups marked for pulsing
                bool shouldPulseNow = pulsingPowerupIDs.Count > 0;

                // apply pulsing/breathing effect if enabled and there are pulsing powerups
                if (enablePulsing && pulseIntensity > 0 && shouldPulseNow)
                {
                    // calculate pulse using sine wave (smooth breathing)
                    float phase = Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f);
                    // convert from -1...1 to 0...1 range
                    float alpha = (phase + 1f) / 2f;

                    // pulse camera between target color and camera default
                    Color cameraPulseColor = Color.Lerp(cameraTargetColor, defaultBackgroundColor, alpha * pulseIntensity);
                    mainCamera.backgroundColor = cameraPulseColor;

                    // pulse grayscale between target color and grayscale default
                    if (grayscaleMaterial != null)
                    {
                        Color grayscalePulseColor = Color.Lerp(grayscaleTargetColor, defaultGrayscaleColor, alpha * pulseIntensity);
                        grayscaleMaterial.SetColor("_Color", grayscalePulseColor);
                    }
                }
                else
                {
                    // no pulsing, just hold at blended colors
                    mainCamera.backgroundColor = cameraTargetColor;

                    // grayscale uses its own default, not camera default
                    if (grayscaleMaterial != null)
                    {
                        grayscaleMaterial.SetColor("_Color", grayscaleTargetColor);
                    }
                }

                yield return null;
            }

            pulseCoroutine = null;
        }

        /// <summary>
        /// fade background from current color back to default.
        /// </summary>
        private IEnumerator FadeToDefault()
        {
            Color startBackgroundColor = mainCamera.backgroundColor;
            Color startGrayscaleColor = grayscaleMaterial != null ? grayscaleMaterial.GetColor("_Color") : Color.white;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                // ease-out cubic (starts fast, ends slow)
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                // lerp camera background to default background color
                Color fadeBackgroundColor = Color.Lerp(startBackgroundColor, defaultBackgroundColor, easedT);
                mainCamera.backgroundColor = fadeBackgroundColor;

                // lerp grayscale material to default grayscale color (separate from background)
                if (grayscaleMaterial != null)
                {
                    Color fadeGrayscaleColor = Color.Lerp(startGrayscaleColor, defaultGrayscaleColor, easedT);
                    grayscaleMaterial.SetColor("_Color", fadeGrayscaleColor);
                }

                yield return null;
            }

            // ensure final colors are default
            mainCamera.backgroundColor = defaultBackgroundColor;

            // reset grayscale material to default grayscale color
            if (grayscaleMaterial != null)
            {
                grayscaleMaterial.SetColor("_Color", defaultGrayscaleColor);
            }

            fadeCoroutine = null;
        }

        /// <summary>
        /// instantly reset camera background to default color.
        /// </summary>
        public void ResetTint()
        {
            activePowerupColors.Clear();

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (mainCamera != null)
            {
                mainCamera.backgroundColor = defaultBackgroundColor;
            }

            // reset grayscale material to default grayscale color
            if (grayscaleMaterial != null)
            {
                grayscaleMaterial.SetColor("_Color", defaultGrayscaleColor);
            }

            pulsingPowerupIDs.Clear();
        }
    }
}

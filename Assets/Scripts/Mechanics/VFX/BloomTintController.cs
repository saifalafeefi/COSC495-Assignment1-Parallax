using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Platformer.Mechanics
{
    /// <summary>
    /// controls bloom tint effect for powerup visual feedback.
    /// </summary>
    public class BloomTintController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("volume component containing bloom effect")]
        public Volume volume;

        [Header("Fade Settings")]
        [Tooltip("how long the tint fades back to white (in seconds)")]
        public float fadeDuration = 1f;
        [Tooltip("color to fade back to (usually white)")]
        public Color defaultTint = Color.white;

        private Bloom bloom;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            if (volume == null)
            {
                volume = GetComponent<Volume>();
            }

            // get bloom effect from volume profile
            if (volume != null && volume.profile.TryGet(out Bloom bloomEffect))
            {
                bloom = bloomEffect;
            }
            else
            {
            }
        }

        /// <summary>
        /// apply tint to bloom and fade back to default color.
        /// </summary>
        /// <param name="tintColor">color to tint bloom to</param>
        /// <param name="customFadeDuration">optional override for fade duration</param>
        public void ApplyTint(Color tintColor, float customFadeDuration = -1f)
        {
            if (bloom == null)
            {
                return;
            }

            // stop existing fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // use custom duration if provided, otherwise use default
            float duration = customFadeDuration > 0 ? customFadeDuration : fadeDuration;

            fadeCoroutine = StartCoroutine(FadeTint(tintColor, 0f, duration));
        }

        /// <summary>
        /// apply tint to bloom, hold for duration, then fade back to default color.
        /// </summary>
        /// <param name="tintColor">color to tint bloom to</param>
        /// <param name="holdDuration">how long to hold at full tint before fading</param>
        /// <param name="customFadeDuration">optional override for fade duration</param>
        public void ApplyTintWithHold(Color tintColor, float holdDuration, float customFadeDuration = -1f)
        {
            if (bloom == null)
            {
                return;
            }

            // stop existing fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // use custom duration if provided, otherwise use default
            float duration = customFadeDuration > 0 ? customFadeDuration : fadeDuration;

            fadeCoroutine = StartCoroutine(FadeTint(tintColor, holdDuration, duration));
        }

        /// <summary>
        /// fade bloom tint from target color back to default.
        /// </summary>
        /// <param name="targetColor">color to tint to</param>
        /// <param name="holdDuration">how long to hold at full tint before fading</param>
        /// <param name="fadeDuration">how long to fade back to default</param>
        private IEnumerator FadeTint(Color targetColor, float holdDuration, float fadeDuration)
        {
            // instantly set to target color
            bloom.tint.value = targetColor;

            // hold at full tint for holdDuration
            if (holdDuration > 0)
            {
                float holdElapsed = 0f;
                while (holdElapsed < holdDuration)
                {
                    holdElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // fade back to default over fadeDuration
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                // ease-out cubic (starts fast, ends slow)
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                // lerp from target color to default
                bloom.tint.value = Color.Lerp(targetColor, defaultTint, easedT);

                yield return null;
            }

            // ensure final color is default
            bloom.tint.value = defaultTint;
            fadeCoroutine = null;
        }

        /// <summary>
        /// instantly reset bloom tint to default color.
        /// </summary>
        public void ResetTint()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (bloom != null)
            {
                bloom.tint.value = defaultTint;
            }
        }
    }
}

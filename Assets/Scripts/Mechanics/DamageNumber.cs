using System.Collections;
using TMPro;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// floating damage number with juicy animations (pop-in, float, fade).
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("how long the entire animation lasts (in seconds)")]
        public float lifetime = 1f;
        [Tooltip("how far up the number floats (in units)")]
        public float floatDistance = 2f;
        [Tooltip("random angle variation from vertical (in degrees)")]
        public float angleVariation = 30f;

        [Header("Pop Animation")]
        [Tooltip("scale overshoot amount (1.2 = 120% size before settling)")]
        public float popScale = 1.2f;
        [Tooltip("how long the pop-in takes (in seconds)")]
        public float popDuration = 0.15f;

        [Header("Color Options")]
        [Tooltip("random color palette for damage numbers")]
        public Color[] damageColors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f),     // red
            new Color(1f, 0.5f, 0.2f),     // orange
            new Color(1f, 0.7f, 0.2f),     // yellow-orange
            new Color(1f, 0.9f, 0.3f),     // yellow
        };

        private TextMeshPro textMesh;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Color textColor;

        private void Awake()
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        /// <summary>
        /// initialize and start the damage number animation.
        /// </summary>
        /// <param name="damage">damage value to display</param>
        /// <param name="spawnPosition">where to spawn the number</param>
        public void Initialize(int damage, Vector3 spawnPosition)
        {
            // set damage text
            textMesh.text = damage.ToString();

            // pick random color
            textColor = damageColors[Random.Range(0, damageColors.Length)];
            textMesh.color = textColor;

            // set start position
            transform.position = spawnPosition;
            startPosition = spawnPosition;

            // calculate target position with random angle
            float randomAngle = Random.Range(-angleVariation, angleVariation);
            Vector3 direction = Quaternion.Euler(0f, 0f, randomAngle) * Vector3.up;
            targetPosition = startPosition + direction * floatDistance;

            // start animation
            StartCoroutine(AnimateNumber());
        }

        /// <summary>
        /// full animation sequence: pop-in, float up, fade out.
        /// </summary>
        private IEnumerator AnimateNumber()
        {
            float elapsed = 0f;

            // phase 1: pop-in animation (scale 0 -> popScale -> 1)
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;

                // ease-out back (overshoot effect)
                float scale;
                if (t < 0.5f)
                {
                    // first half: 0 -> popScale
                    float t1 = t * 2f;
                    scale = Mathf.Lerp(0f, popScale, t1 * t1);
                }
                else
                {
                    // second half: popScale -> 1
                    float t2 = (t - 0.5f) * 2f;
                    scale = Mathf.Lerp(popScale, 1f, t2);
                }

                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            // ensure final scale is 1
            transform.localScale = Vector3.one;

            // phase 2: float up and fade out
            elapsed = 0f;
            float floatDuration = lifetime - popDuration;

            while (elapsed < floatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatDuration;

                // ease-out cubic (starts fast, ends slow)
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                // move position
                transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);

                // fade out alpha (start fading halfway through)
                if (t > 0.5f)
                {
                    float fadeT = (t - 0.5f) * 2f; // 0 to 1 over second half
                    float alpha = Mathf.Lerp(1f, 0f, fadeT);
                    textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                }

                yield return null;
            }

            // destroy after animation completes
            Destroy(gameObject);
        }
    }
}

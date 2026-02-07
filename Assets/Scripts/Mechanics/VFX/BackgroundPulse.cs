using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Height-based brightness for background SpriteRenderers.
    /// Attach to the background parent (auto-finds children).
    /// </summary>
    public class BackgroundPulse : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("auto-find sprite renderers in this object and children")]
        public bool autoFind = true;
        [Tooltip("include inactive children when auto-finding")]
        public bool includeInactiveChildren = true;
        public SpriteRenderer[] targets;

        [Header("Height-Based Brightness")]
        [Tooltip("enable brightness scaling based on this object's Y position")]
        public bool useHeightBrightness = true;
        [Tooltip("world Y at which brightness is at minimum")]
        public float minY = -5f;
        [Tooltip("world Y at which brightness is at maximum")]
        public float maxY = 5f;
        [Tooltip("brightness multiplier at minY")]
        public float minHeightBrightness = 0.7f;
        [Tooltip("brightness multiplier at maxY")]
        public float maxHeightBrightness = 1.3f;
        [Tooltip("smoothing time for brightness transitions")]
        public float smoothTime = 0.15f;

        private Color[] baseColors;
        private float currentBrightness = 1f;
        private float brightnessVelocity = 0f;

        void Awake()
        {
            InitTargets();
        }

        void OnEnable()
        {
            InitTargets();
        }

        void OnDisable()
        {
            RestoreBaseColors();
        }

        private void InitTargets()
        {
            if (autoFind || targets == null || targets.Length == 0)
            {
                targets = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
            }

            if (targets == null || targets.Length == 0)
            {
                baseColors = null;
                return;
            }

            baseColors = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                baseColors[i] = targets[i] != null ? targets[i].color : Color.white;
            }
        }

        void Update()
        {
            if (!useHeightBrightness || targets == null || targets.Length == 0 || baseColors == null) return;
            if (maxY <= minY) return;

            float y = transform.position.y;
            float tHeight = Mathf.InverseLerp(minY, maxY, y);
            float targetBrightness = Mathf.Lerp(minHeightBrightness, maxHeightBrightness, tHeight);

            currentBrightness = Mathf.SmoothDamp(currentBrightness, targetBrightness, ref brightnessVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            Apply(currentBrightness);
        }

        private void Apply(float brightness)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var sr = targets[i];
                if (sr == null) continue;

                Color c = baseColors[i];
                c.r = Mathf.Clamp01(c.r * brightness);
                c.g = Mathf.Clamp01(c.g * brightness);
                c.b = Mathf.Clamp01(c.b * brightness);
                sr.color = c;
            }
        }

        private void RestoreBaseColors()
        {
            if (targets == null || baseColors == null) return;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                targets[i].color = baseColors[i];
            }
        }
    }
}

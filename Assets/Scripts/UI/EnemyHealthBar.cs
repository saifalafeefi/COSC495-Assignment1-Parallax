using UnityEngine;
using UnityEngine.UI;
using Platformer.Mechanics;

namespace Platformer.UI
{
    /// <summary>
    /// displays enemy health as a bar above their head with configurable outline.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("Position Settings")]
        [Tooltip("vertical offset above enemy (in world units)")]
        public float yOffset = 0.8f;

        [Header("Size Settings")]
        [Tooltip("width of the health bar in pixels")]
        public float barWidth = 40f;

        [Tooltip("height of the health bar in pixels")]
        public float barHeight = 6f;

        [Header("Outline Settings")]
        [Tooltip("thickness of the outline border in pixels")]
        public float outlineThickness = 2f;

        [Tooltip("color of the outline border")]
        public Color outlineColor = Color.white;

        [Header("Bar Colors")]
        [Tooltip("background color behind the fill bar")]
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // dark gray

        [Tooltip("fill bar color (shows current HP)")]
        public Color fillColor = new Color(1f, 0f, 0f, 1f); // red

        private Health health;
        private Canvas canvas;
        private GameObject canvasObject;
        private Image outlineImage;
        private Image backgroundImage;
        private Image fillImage;
        private int lastKnownHP = -1;
        private Sprite whiteSprite;

        void Awake()
        {
            // find health component on this enemy
            health = GetComponent<Health>();

            if (health == null)
            {
                enabled = false;
                return;
            }

            CreateWhiteSprite();
            CreateHealthBar();
        }

        void CreateWhiteSprite()
        {
            // create a simple white texture for the bars
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // create sprite from texture
            whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        }

        void CreateHealthBar()
        {
            // create world-space canvas
            canvasObject = new GameObject("HealthBarCanvas");
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // position canvas above enemy
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(barWidth + outlineThickness * 2, barHeight + outlineThickness * 2);
            canvasRect.localScale = Vector3.one * 0.01f; // scale down for world space
            canvasRect.localPosition = new Vector3(0, yOffset, 0);

            // create outline (outermost layer)
            GameObject outlineObject = new GameObject("Outline");
            outlineObject.transform.SetParent(canvasObject.transform, false);

            outlineImage = outlineObject.AddComponent<Image>();
            outlineImage.sprite = whiteSprite;
            outlineImage.color = outlineColor;

            RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.sizeDelta = Vector2.zero;
            outlineRect.anchoredPosition = Vector2.zero;

            // create background (sits on top of outline)
            GameObject bgObject = new GameObject("Background");
            bgObject.transform.SetParent(canvasObject.transform, false);

            backgroundImage = bgObject.AddComponent<Image>();
            backgroundImage.sprite = whiteSprite;
            backgroundImage.color = backgroundColor;

            RectTransform bgRect = bgObject.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = new Vector2(-outlineThickness * 2, -outlineThickness * 2);
            bgRect.anchoredPosition = Vector2.zero;

            // create fill bar (red, on top of background)
            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(canvasObject.transform, false);

            fillImage = fillObject.AddComponent<Image>();
            fillImage.sprite = whiteSprite;
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-outlineThickness * 2, -outlineThickness * 2);
            fillRect.anchoredPosition = Vector2.zero;

            // set initial fill amount
            UpdateHealthBar();
        }

        void Update()
        {
            if (health == null) return;

            // update bar when HP changes
            if (lastKnownHP != health.CurrentHP)
            {
                lastKnownHP = health.CurrentHP;
                UpdateHealthBar();
            }

            // hide bar when enemy is dead
            if (health.CurrentHP <= 0)
            {
                if (canvasObject != null)
                {
                    canvasObject.SetActive(false);
                }
            }
        }

        void UpdateHealthBar()
        {
            if (fillImage != null && health != null)
            {
                float fillAmount = (float)health.CurrentHP / (float)health.maxHP;
                fillImage.fillAmount = Mathf.Clamp01(fillAmount);
            }
        }
    }
}

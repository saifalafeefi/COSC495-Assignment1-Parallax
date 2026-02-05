using UnityEngine;
using UnityEngine.UI;
using Platformer.Mechanics;

namespace Platformer.UI
{
    /// <summary>
    /// displays cooldown bars for roll and ranged attack beside the player.
    /// uses world-space canvas to follow player movement.
    /// </summary>
    public class CooldownUI : MonoBehaviour
    {
        [Header("References")]
        /// <summary>
        /// reference to the player controller (auto-found if empty).
        /// </summary>
        public PlayerController playerController;

        [Header("Bar Settings")]
        /// <summary>
        /// width of each cooldown bar.
        /// </summary>
        public float barWidth = 8f;
        /// <summary>
        /// height of each cooldown bar.
        /// </summary>
        public float barHeight = 40f;

        [Header("Roll Bar Position")]
        /// <summary>
        /// roll bar X offset from player (world units).
        /// </summary>
        public float rollBarOffsetX = 0.6f;
        /// <summary>
        /// roll bar Y offset from player (world units).
        /// </summary>
        public float rollBarOffsetY = 0.5f;

        [Header("Ranged Bar Position")]
        /// <summary>
        /// ranged bar X offset from player (world units).
        /// </summary>
        public float rangedBarOffsetX = 0.8f;
        /// <summary>
        /// ranged bar Y offset from player (world units).
        /// </summary>
        public float rangedBarOffsetY = 0.5f;

        [Header("Colors")]
        /// <summary>
        /// color for roll cooldown bar.
        /// </summary>
        public Color rollColor = new Color(0.2f, 0.6f, 1f, 0.9f); // light blue
        /// <summary>
        /// color for ranged attack cooldown bar.
        /// </summary>
        public Color rangedColor = new Color(1f, 0.8f, 0.2f, 0.9f); // yellow/orange
        /// <summary>
        /// background color for bars.
        /// </summary>
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.6f); // dark gray


        private Canvas canvas;
        private GameObject canvasObject;

        // roll bar components
        private GameObject rollBarContainer;
        private Image rollBarBackground;
        private Image rollBarFill;

        // ranged bar components
        private GameObject rangedBarContainer;
        private Image rangedBarBackground;
        private Image rangedBarFill;

        // shared white sprite for all bars
        private Sprite whiteSprite;

        void Awake()
        {
            // find player if not assigned
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (playerController == null)
            {
                enabled = false;
                return;
            }

            CreateWhiteSprite();
            SetupWorldSpaceCanvas();
            CreateCooldownBars();
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

        void SetupWorldSpaceCanvas()
        {
            // create a world-space canvas that follows the player
            canvasObject = new GameObject("CooldownCanvas");
            canvasObject.transform.SetParent(playerController.transform, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // configure canvas rect transform
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100, 100);
            canvasRect.localScale = Vector3.one * 0.01f; // scale down for world space
            canvasRect.localPosition = Vector3.zero;
        }

        void CreateCooldownBars()
        {
            // create roll cooldown bar using direct offset
            rollBarContainer = CreateBar("RollBar", rollBarOffsetX, rollBarOffsetY, rollColor, out rollBarBackground, out rollBarFill);

            // create ranged attack cooldown bar using direct offset
            rangedBarContainer = CreateBar("RangedBar", rangedBarOffsetX, rangedBarOffsetY, rangedColor, out rangedBarBackground, out rangedBarFill);
        }

        GameObject CreateBar(string name, float posX, float posY, Color fillColor, out Image bgImage, out Image fillImage)
        {
            // create container
            GameObject container = new GameObject(name);
            container.transform.SetParent(canvas.transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(barWidth, barHeight);
            containerRect.anchoredPosition = new Vector2(posX * 100f, posY * 100f); // scale for canvas space

            // create background
            GameObject bgObj = new GameObject($"{name}_Background");
            bgObj.transform.SetParent(container.transform, false);

            bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = whiteSprite;
            bgImage.color = backgroundColor;

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // create fill (foreground)
            GameObject fillObj = new GameObject($"{name}_Fill");
            fillObj.transform.SetParent(container.transform, false);

            fillImage = fillObj.AddComponent<Image>();
            fillImage.sprite = whiteSprite;
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            return container;
        }

        void Update()
        {
            if (playerController == null)
                return;

            // update roll cooldown bar (only visible when on cooldown)
            bool rollOnCooldown = playerController.RollCooldownTimer > 0f;
            rollBarContainer.SetActive(rollOnCooldown);
            if (rollOnCooldown)
            {
                float rollProgress = GetCooldownProgress(playerController.RollCooldownTimer, playerController.rollCooldown);
                rollBarFill.fillAmount = rollProgress;
            }

            // update ranged attack cooldown bar (only visible when on cooldown)
            bool rangedOnCooldown = playerController.RangedCooldownTimer > 0f;
            rangedBarContainer.SetActive(rangedOnCooldown);
            if (rangedOnCooldown)
            {
                float rangedProgress = GetCooldownProgress(playerController.RangedCooldownTimer, playerController.rangedAttackCooldown);
                rangedBarFill.fillAmount = rangedProgress;
            }
        }

        /// <summary>
        /// calculate cooldown progress (1 = just used/full bar, 0 = ready/empty bar).
        /// </summary>
        float GetCooldownProgress(float currentTimer, float maxCooldown)
        {
            if (maxCooldown <= 0)
                return 0f;

            // timer counts down from maxCooldown to 0
            // progress should be 1 (full) when timer is at max (just used)
            // progress should be 0 (empty) when timer is at 0 (ready)
            return Mathf.Clamp01(currentTimer / maxCooldown);
        }
    }
}

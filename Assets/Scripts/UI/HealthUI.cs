using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Platformer.Mechanics;

namespace Platformer.UI
{
    /// <summary>
    /// Displays player health as a row of heart icons.
    /// AUTO-CREATES Canvas and UI if not in scene.
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("References")]
        /// <summary>
        /// Reference to the player's Health component (auto-found if empty).
        /// </summary>
        public Health playerHealth;

        [Header("Heart Sprites")]
        /// <summary>
        /// Sprite to display for a full heart (drag and drop your custom sprite here).
        /// </summary>
        public Sprite fullHeartSprite;

        /// <summary>
        /// Sprite to display for an empty heart (drag and drop your custom sprite here).
        /// </summary>
        public Sprite emptyHeartSprite;

        [Header("UI Settings")]
        /// <summary>
        /// Size of each heart icon in pixels.
        /// </summary>
        public float heartSize = 40f;

        /// <summary>
        /// Spacing between heart icons.
        /// </summary>
        public float heartSpacing = 10f;

        [Header("UI Position")]
        /// <summary>
        /// Position offset from top-left corner.
        /// </summary>
        public Vector2 screenPosition = new Vector2(20, -20);

        private List<Image> heartImages = new List<Image>();
        private int lastKnownMaxHP = 0;
        private int lastKnownCurrentHP = -1;
        private Canvas canvas;
        private GameObject healthDisplayContainer;

        void Awake()
        {
            SetupCanvas();
            SetupHealthDisplay();
        }

        void SetupCanvas()
        {
            // find or create Canvas
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        void SetupHealthDisplay()
        {
            // create a container for health hearts
            healthDisplayContainer = new GameObject("HealthDisplay");
            healthDisplayContainer.transform.SetParent(canvas.transform, false);

            RectTransform rect = healthDisplayContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = screenPosition;
            rect.sizeDelta = new Vector2(300, 50);

        }

        void Start()
        {

            // find player health if not assigned
            if (playerHealth == null)
            {
                var playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    playerHealth = playerController.health;
                }
            }

            // create placeholder sprites if none are assigned
            if (fullHeartSprite == null)
            {
                fullHeartSprite = CreatePlaceholderSprite(new Color(1f, 0f, 0f, 1f)); // bright red
            }
            if (emptyHeartSprite == null)
            {
                emptyHeartSprite = CreatePlaceholderSprite(new Color(0.2f, 0.2f, 0.2f, 1f)); // dark gray
            }

            // initial UI setup
            if (playerHealth != null)
            {
                InitializeHearts();
            }
            else
            {
            }
        }

        void Update()
        {
            if (playerHealth != null)
            {
                // rebuild hearts if max HP changed
                if (lastKnownMaxHP != playerHealth.maxHP)
                {
                    InitializeHearts();
                }

                // update heart display only when HP changes
                if (lastKnownCurrentHP != playerHealth.CurrentHP)
                {
                    lastKnownCurrentHP = playerHealth.CurrentHP;
                    UpdateHearts();
                }
            }
        }

        /// <summary>
        /// Creates heart icons based on max HP.
        /// </summary>
        void InitializeHearts()
        {

            // clear existing hearts
            foreach (var heart in heartImages)
            {
                if (heart != null)
                {
                    Destroy(heart.gameObject);
                }
            }
            heartImages.Clear();

            // create new hearts in the container
            lastKnownMaxHP = playerHealth.maxHP;

            for (int i = 0; i < playerHealth.maxHP; i++)
            {
                GameObject heartObj = new GameObject($"Heart_{i}");
                heartObj.transform.SetParent(healthDisplayContainer.transform, false);

                Image heartImage = heartObj.AddComponent<Image>();
                heartImage.sprite = fullHeartSprite;
                heartImage.preserveAspect = true;
                heartImage.raycastTarget = false;

                RectTransform rectTransform = heartObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(heartSize, heartSize);
                rectTransform.anchoredPosition = new Vector2(i * (heartSize + heartSpacing), 0);

                heartImages.Add(heartImage);
            }

            lastKnownCurrentHP = playerHealth.CurrentHP;
            UpdateHearts();
        }

        /// <summary>
        /// Updates heart sprites based on current HP.
        /// </summary>
        void UpdateHearts()
        {
            int currentHP = playerHealth.CurrentHP;

            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] != null)
                {
                    heartImages[i].sprite = i < currentHP ? fullHeartSprite : emptyHeartSprite;
                }
            }
        }

        /// <summary>
        /// Creates a simple colored square sprite as a placeholder.
        /// </summary>
        Sprite CreatePlaceholderSprite(Color color)
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];

            // create a filled square
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}

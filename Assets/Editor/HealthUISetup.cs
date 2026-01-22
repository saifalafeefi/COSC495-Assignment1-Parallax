using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Platformer.UI;
using Platformer.Mechanics;

/// <summary>
/// Editor utility to automatically set up the Health UI in the scene.
/// </summary>
public class HealthUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Health UI")]
    public static void SetupHealthUI()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("Created new Canvas");
        }

        // Find or create HealthDisplay GameObject
        Transform healthDisplayTransform = canvas.transform.Find("HealthDisplay");
        GameObject healthDisplay;

        if (healthDisplayTransform == null)
        {
            healthDisplay = new GameObject("HealthDisplay");
            healthDisplay.transform.SetParent(canvas.transform, false);
            Debug.Log("Created HealthDisplay GameObject");
        }
        else
        {
            healthDisplay = healthDisplayTransform.gameObject;
            Debug.Log("Found existing HealthDisplay GameObject");
        }

        // Set up RectTransform for top-left positioning
        RectTransform rectTransform = healthDisplay.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = healthDisplay.AddComponent<RectTransform>();
        }

        // Position at top-left
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(20, -20);
        rectTransform.sizeDelta = new Vector2(200, 50);

        // Add or get HealthUI component
        HealthUI healthUI = healthDisplay.GetComponent<HealthUI>();
        if (healthUI == null)
        {
            healthUI = healthDisplay.AddComponent<HealthUI>();
            Debug.Log("Added HealthUI component");
        }

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>()?.gameObject;
        }

        if (player != null)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                healthUI.playerHealth = playerHealth;
                Debug.Log("Connected HealthUI to Player's Health component");
            }
            else
            {
                Debug.LogWarning("Player found but has no Health component!");
            }
        }
        else
        {
            Debug.LogWarning("Could not find Player in scene. Please assign manually.");
        }

        EditorUtility.SetDirty(healthDisplay);
        Selection.activeGameObject = healthDisplay;

        Debug.Log("Health UI setup complete! Select HealthDisplay to assign heart sprites.");
        EditorUtility.DisplayDialog("Success",
            "Health UI has been set up!\n\n" +
            "The HealthDisplay GameObject is now selected.\n" +
            "You can drag your heart sprites into the inspector.\n\n" +
            "For now, placeholder hearts will appear when you play.",
            "OK");
    }
}

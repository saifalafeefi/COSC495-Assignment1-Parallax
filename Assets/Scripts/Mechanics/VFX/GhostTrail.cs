using System.Collections;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// creates ghost trail afterimages for motion blur effect on sprites.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GhostTrail : MonoBehaviour
    {
        [Header("Trail Settings")]
        [Tooltip("enable ghost trail effect")]
        public bool enableTrail = false;
        [Tooltip("time between ghost spawns (lower = more ghosts)")]
        public float ghostSpawnInterval = 0.05f;
        [Tooltip("how long each ghost lasts before fading out")]
        public float ghostLifetime = 0.3f;
        [Tooltip("starting opacity of ghost (0-1)")]
        public float ghostStartAlpha = 0.5f;
        [Tooltip("minimum speed required to spawn ghosts")]
        public float minSpeedThreshold = 2f;

        private SpriteRenderer spriteRenderer;
        private KinematicObject kinematicObject;
        private float ghostSpawnTimer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            kinematicObject = GetComponent<KinematicObject>();
        }

        private void Update()
        {
            if (!enableTrail) return;

            // use unscaled time so it works during time slow
            ghostSpawnTimer -= Time.unscaledDeltaTime;

            // check if moving fast enough (use kinematic velocity, not rigidbody)
            float speed = kinematicObject != null ? kinematicObject.velocity.magnitude : 0f;
            bool isMovingFast = speed >= minSpeedThreshold;

            // spawn ghost if moving fast and time reached
            if (ghostSpawnTimer <= 0f && isMovingFast)
            {
                SpawnGhost();
                ghostSpawnTimer = ghostSpawnInterval;
            }
        }

        /// <summary>
        /// spawn a ghost sprite at current position.
        /// </summary>
        private void SpawnGhost()
        {
            // create ghost object
            GameObject ghost = new GameObject("Ghost");
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = transform.localScale;

            // add sprite renderer with current sprite
            SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
            ghostRenderer.sprite = spriteRenderer.sprite;
            ghostRenderer.flipX = spriteRenderer.flipX; // match player's facing direction
            ghostRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
            ghostRenderer.sortingOrder = spriteRenderer.sortingOrder - 1; // behind player

            // set initial alpha
            Color ghostColor = spriteRenderer.color;
            ghostColor.a = ghostStartAlpha;
            ghostRenderer.color = ghostColor;

            // start fade coroutine
            StartCoroutine(FadeGhost(ghostRenderer, ghostLifetime));
        }

        /// <summary>
        /// fade ghost sprite and destroy after lifetime.
        /// </summary>
        private IEnumerator FadeGhost(SpriteRenderer ghostRenderer, float lifetime)
        {
            float elapsed = 0f;
            Color startColor = ghostRenderer.color;

            while (elapsed < lifetime)
            {
                // use unscaled time so ghosts fade properly during time slow
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / lifetime;

                // fade alpha from start to 0
                Color color = startColor;
                color.a = Mathf.Lerp(ghostStartAlpha, 0f, t);
                ghostRenderer.color = color;

                yield return null;
            }

            // destroy ghost
            Destroy(ghostRenderer.gameObject);
        }

        /// <summary>
        /// enable trail effect.
        /// </summary>
        public void EnableTrail()
        {
            enableTrail = true;
        }

        /// <summary>
        /// disable trail effect.
        /// </summary>
        public void DisableTrail()
        {
            enableTrail = false;
        }
    }
}

using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// makes a powerup float up and down smoothly with optional rotation.
    /// </summary>
    public class FloatingPowerup : MonoBehaviour
    {
        [Header("Float Settings")]
        /// <summary>
        /// how high/low the powerup bobs (in units).
        /// </summary>
        public float floatAmplitude = 0.3f;
        /// <summary>
        /// how fast the powerup bobs up and down.
        /// </summary>
        public float floatSpeed = 2f;
        /// <summary>
        /// random offset so multiple powerups don't sync.
        /// </summary>
        public bool randomizeStartOffset = true;

        [Header("Rotation Settings")]
        /// <summary>
        /// enable smooth side-to-side rotation animation.
        /// </summary>
        public bool enableRotation = false;
        /// <summary>
        /// maximum rotation angle in degrees (rotates from -max to +max).
        /// </summary>
        public float rotationMaxAngle = 15f;
        /// <summary>
        /// how fast the powerup rotates side to side.
        /// </summary>
        public float rotationSpeed = 2f;
        /// <summary>
        /// random offset for rotation so multiple powerups don't sync.
        /// </summary>
        public bool randomizeRotationOffset = true;

        private Vector3 startPosition;
        private float timeOffset;
        private float rotationTimeOffset;

        void Start()
        {
            // store the initial position
            startPosition = transform.position;

            // randomize start time so powerups don't all bob in sync
            if (randomizeStartOffset)
            {
                timeOffset = Random.Range(0f, Mathf.PI * 2f);
            }

            // randomize rotation start time so powerups don't all rotate in sync
            if (randomizeRotationOffset)
            {
                rotationTimeOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        void Update()
        {
            // calculate smooth bobbing motion using sine wave
            float newY = startPosition.y + Mathf.Sin((Time.time * floatSpeed) + timeOffset) * floatAmplitude;

            // apply the floating motion
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // optional side-to-side rotation
            if (enableRotation)
            {
                // calculate smooth rotation angle using sine wave (-maxAngle to +maxAngle)
                float rotationAngle = Mathf.Sin((Time.time * rotationSpeed) + rotationTimeOffset) * rotationMaxAngle;

                // apply the rotation on Z axis (side-to-side tilt)
                transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
            }
        }
    }
}

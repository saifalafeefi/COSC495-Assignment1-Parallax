using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// tweens the cinemachine camera to center on the player during victory.
    /// attach to the cinemachine camera object in the scene.
    /// </summary>
    public class VictoryCameraZoom : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("Cinemachine camera to zoom. Auto-found if empty.")]
        public CinemachineCamera virtualCamera;

        [Header("Zoom")]
        [Tooltip("Duration of the zoom tween in seconds.")]
        public float zoomDuration = 1.0f;
        [Tooltip("Easing curve for the zoom.")]
        public AnimationCurve zoomEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Target orthographic size (used if camera is orthographic).")]
        public float targetOrthoSize = 3.5f;
        [Tooltip("Target field of view (used if camera is perspective).")]
        public float targetFOV = 35f;

        [Header("Follow Tween")]
        [Tooltip("If true, tween a proxy target to the player instead of snapping.")]
        public bool useFollowProxy = true;
        [Tooltip("Duration of the follow tween in seconds.")]
        public float followTweenDuration = 0.6f;
        [Tooltip("Easing curve for the follow tween.")]
        public AnimationCurve followTweenEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Switch follow target to the player after tween completes.")]
        public bool switchToPlayerAfterTween = true;
        [Tooltip("Seed proxy from the Cinemachine Brain output camera for zero pop.")]
        public bool useBrainCameraPosition = true;

        [Header("Centering")]
        [Tooltip("Force Position Composer to center the target during the tween.")]
        public bool forceCenterComposer = true;
        [Tooltip("Disable Confiner2D during the tween so it doesn't block centering.")]
        public bool disableConfiner2D = true;
        [Tooltip("Composer damping during the tween (0 = instant, higher = smoother).")]
        public Vector3 composerDamping = new Vector3(0.5f, 0.5f, 0.5f);

        private Coroutine zoomRoutine;
        private Coroutine followRoutine;
        private Coroutine beginRoutine;
        private Transform followProxy;

        public void StartVictoryZoom(Transform player)
        {
            if (virtualCamera == null)
            {
                virtualCamera = FindFirstObjectByType<CinemachineCamera>();
                if (virtualCamera == null) return;
            }

            var brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;

            if (beginRoutine != null)
            {
                StopCoroutine(beginRoutine);
            }

            if (useFollowProxy && player != null)
            {
                beginRoutine = StartCoroutine(BeginFollowProxy(player, brain));
            }
            else if (player != null)
            {
                virtualCamera.Follow = player;
                virtualCamera.LookAt = player;
            }

            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
            }
            zoomRoutine = StartCoroutine(ZoomRoutine());
        }

        private IEnumerator BeginFollowProxy(Transform player, CinemachineBrain brain)
        {
            EnsureFollowProxy();

            Vector3 startPos;
            Quaternion startRot;
            if (useBrainCameraPosition && brain != null && brain.OutputCamera != null)
            {
                startPos = brain.OutputCamera.transform.position;
                startRot = brain.OutputCamera.transform.rotation;
            }
            else
            {
                startPos = virtualCamera.transform.position;
                startRot = virtualCamera.transform.rotation;
            }

            followProxy.position = startPos;
            virtualCamera.Follow = followProxy;
            virtualCamera.LookAt = followProxy;
            virtualCamera.ForceCameraPosition(startPos, startRot);

            // wait until end of frame to avoid a visible pop on the transition frame
            yield return new WaitForEndOfFrame();

            if (disableConfiner2D)
            {
                var confiner2D = virtualCamera.GetComponent<CinemachineConfiner2D>();
                if (confiner2D != null) confiner2D.enabled = false;
            }

            if (forceCenterComposer)
            {
                var composer = virtualCamera.GetComponent<CinemachinePositionComposer>();
                if (composer != null)
                {
                    var comp = composer.Composition;
                    comp.ScreenPosition = Vector2.zero; // 0,0 is center in this api
                    comp.DeadZone.Enabled = true;
                    comp.DeadZone.Size = Vector2.zero;
                    comp.HardLimits.Enabled = true;
                    comp.HardLimits.Size = Vector2.zero;
                    comp.HardLimits.Offset = Vector2.zero;
                    composer.Composition = comp;

                    composer.TargetOffset = Vector3.zero;
                    composer.Damping = composerDamping;
                    composer.CenterOnActivate = false;
                }
            }

            if (followRoutine != null)
            {
                StopCoroutine(followRoutine);
            }
            followRoutine = StartCoroutine(FollowProxyRoutine(player));
        }

        private IEnumerator FollowProxyRoutine(Transform player)
        {
            float duration = Mathf.Max(0.01f, followTweenDuration);
            float t = 0f;
            Vector3 start = followProxy.position;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = followTweenEase.Evaluate(p);
                Vector3 target = player != null ? player.position : start;
                followProxy.position = Vector3.Lerp(start, target, eased);
                yield return null;
            }

            if (player != null)
            {
                followProxy.position = player.position;
                if (switchToPlayerAfterTween)
                {
                    virtualCamera.Follow = player;
                    virtualCamera.LookAt = player;
                }
            }
        }

        private IEnumerator ZoomRoutine()
        {
            var lens = virtualCamera.Lens;
            bool isOrtho = lens.Orthographic;
            float start = isOrtho ? lens.OrthographicSize : lens.FieldOfView;
            float end = isOrtho ? targetOrthoSize : targetFOV;

            float t = 0f;
            float duration = Mathf.Max(0.01f, zoomDuration);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = zoomEase.Evaluate(p);
                float value = Mathf.Lerp(start, end, eased);

                lens = virtualCamera.Lens;
                if (isOrtho)
                    lens.OrthographicSize = value;
                else
                    lens.FieldOfView = value;
                virtualCamera.Lens = lens;

                yield return null;
            }

            // ensure final value
            lens = virtualCamera.Lens;
            if (isOrtho)
                lens.OrthographicSize = end;
            else
                lens.FieldOfView = end;
            virtualCamera.Lens = lens;
        }

        private void EnsureFollowProxy()
        {
            if (followProxy != null) return;
            var go = new GameObject("VictoryCameraFollowProxy");
            go.hideFlags = HideFlags.DontSave;
            followProxy = go.transform;
        }
    }
}

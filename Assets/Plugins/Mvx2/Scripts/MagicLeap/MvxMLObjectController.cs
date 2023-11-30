#if UNITY_LUMIN
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MVXUnity.ML
{
    public class MvxMLObjectController : MonoBehaviour
    {
        [SerializeField] public Transform mvxObject;

        [SerializeField] public MvxMLControllerConnectionHandlerBehavior controllerConnectionHandler;
        [SerializeField] public MvxMLRaycastBehavior raycastController = null;

        [SerializeField] public float triggerButtonThreshold = 0.2f;

        [SerializeField] public float minObjectScale = 0.1f;
        [SerializeField] public float maxObjectScale = 10f;
        [SerializeField] public float scaleMultiplier = 1.0f;
        private float m_gestureStartScale;
        private float m_gestureStartSwipeX;
        private bool m_gestureSwipeInProgress;
        private float m_gestureStartRotation;
        private float m_gestureStartRadialScrollAngle;

        public void Start()
        {
#if PLATFORM_LUMIN
            MLResult result = MLInput.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Mvx2: Failed to start MLInput");
                enabled = false;
                return;
            }

            raycastController.OnRaycastResult += OnControllerRaycastHit;
            MLInput.OnControllerTouchpadGestureStart += OnControllerTouchpadGestureStart;
            MLInput.OnControllerTouchpadGestureEnd += OnControllerTouchpadGestureEnd;
            MLInput.OnControllerTouchpadGestureContinue += OnControllerTouchpadGestureContinue;
#endif
        }

        public void OnDestroy()
        {
#if PLATFORM_LUMIN
            raycastController.OnRaycastResult -= OnControllerRaycastHit;
            MLInput.OnControllerTouchpadGestureStart -= OnControllerTouchpadGestureStart;
            MLInput.OnControllerTouchpadGestureEnd -= OnControllerTouchpadGestureEnd;
            MLInput.OnControllerTouchpadGestureContinue -= OnControllerTouchpadGestureContinue;
            MLInput.Stop();
#endif
        }

        public void Update()
        {
            TryUpdateSwipeGesture();
        }

        public void OnControllerRaycastHit(MLRaycast.ResultState state, MvxMLRaycastBehavior.Mode mode, Ray ray, RaycastHit hit, float confidence)
        {
            if (state == MLRaycast.ResultState.RequestFailed || state == MLRaycast.ResultState.NoCollision)
                return;

            if (TriggerButtonPressed())
                mvxObject.position = hit.point;
        }

        private bool TriggerButtonPressed()
        {
            if (!controllerConnectionHandler.IsControllerValid())
                return false;

            MLInput.Controller controller = controllerConnectionHandler.ConnectedController;

#if PLATFORM_LUMIN
            return controller.TriggerValue > triggerButtonThreshold;
#else
            return false;
#endif
        }

        private void UpdateRadialScrollGesture(bool justStarted, float gestureAngle)
        {
            if (justStarted)
            {
                m_gestureStartRotation = mvxObject.localRotation.eulerAngles.y * Mathf.Rad2Deg;
                m_gestureStartRadialScrollAngle = gestureAngle;
            }
            else
            {
                float angleDiff = gestureAngle * Mathf.Rad2Deg - m_gestureStartRadialScrollAngle;
                Vector3 eulerAngles = mvxObject.localRotation.eulerAngles;
                eulerAngles.y = m_gestureStartRotation + angleDiff;
                mvxObject.localRotation = Quaternion.Euler(eulerAngles);
            }
        }

        private void StartSwipeGesture(Vector3? gesturePosAndForce)
        {
            if (!gesturePosAndForce.HasValue)
                return;

            Vector3 posAndForce = gesturePosAndForce.Value;

            m_gestureStartScale = mvxObject.localScale.x;
            m_gestureStartSwipeX = posAndForce.x;
            m_gestureSwipeInProgress = true;            
        }

        private void TryUpdateSwipeGesture()
        {
            if (!m_gestureSwipeInProgress)
                return;

#if PLATFORM_LUMIN
            if (!controllerConnectionHandler.IsControllerValid())
                return;

            MLInput.Controller controller = controllerConnectionHandler.ConnectedController;

            if (controller.CurrentTouchpadGesture.Type != MLInput.Controller.TouchpadGesture.GestureType.Swipe)
                return;

            float swipeXDiff = controller.Touch1PosAndForce.x - m_gestureStartSwipeX;
            float scaleDiff = swipeXDiff * scaleMultiplier;
            float currentScale = m_gestureStartScale + scaleDiff;
            currentScale = Mathf.Clamp(currentScale, minObjectScale, maxObjectScale);
            currentScale = Mathf.Lerp(mvxObject.localScale.x, currentScale, 0.1f);
            mvxObject.localScale = new Vector3(currentScale, currentScale, currentScale);
#endif
        }

        private void StopSwipeGesture()
        {
            m_gestureSwipeInProgress = false;
        }

        private void OnControllerTouchpadGestureStart(byte controllerId, MLInput.Controller.TouchpadGesture touchpadGesture)
        {
#if PLATFORM_LUMIN
            if (touchpadGesture.Type == MLInput.Controller.TouchpadGesture.GestureType.RadialScroll)
            {
                UpdateRadialScrollGesture(true, touchpadGesture.Angle);
            }
            else if (touchpadGesture.Type == MLInput.Controller.TouchpadGesture.GestureType.Swipe)
            {
                StartSwipeGesture(touchpadGesture.PosAndForce);
            }
#endif
        }

        private void OnControllerTouchpadGestureEnd(byte controllerId, MLInput.Controller.TouchpadGesture touchpadGesture)
        {
#if PLATFORM_LUMIN
            if (touchpadGesture.Type == MLInput.Controller.TouchpadGesture.GestureType.Swipe)
            {
                StopSwipeGesture();
            }
#endif
        }

        private void OnControllerTouchpadGestureContinue(byte controllerId, MLInput.Controller.TouchpadGesture touchpadGesture)
        {
#if PLATFORM_LUMIN
            if (touchpadGesture.Type == MLInput.Controller.TouchpadGesture.GestureType.RadialScroll)
            {
                UpdateRadialScrollGesture(false, touchpadGesture.Angle);
            }
            // Swipe gesture does not invoke 'Continue' event -> must be updated from regular Update()
#endif
        }
    }
}
#endif
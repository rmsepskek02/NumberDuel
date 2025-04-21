using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 마우스/터치 입력을 감지하고,
    /// 클릭 및 드래그 여부를 판단하여 외부에 이벤트를 전달
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private Camera mainCamera;         // 입력 기준 카메라
        [SerializeField] private float dragThreshold = 10f; // 드래그로 간주될 최소 이동 거리 (픽셀)

        [Header("Click Event")]
        public UnityAction OnClicked; // 클릭 발생 시 외부로 전달하는 이벤트

        [Header("Interaction Control")]
        public bool isDraggable = true;
        public bool isClickable = true;

        private Vector2 dragStartPos;
        private float zDistance;

        private bool isDragging = false;
        private bool wasDragged = false;
        private bool dragEndedOnce = false;
        private bool clickRequested = false;
        private bool wasClickRelease = false;

        private bool isToggleOn = false; // 클릭 토글 상태

        private DragHandler dragHandler;
        private Transform rootTransform;

        #region Public Properties

        public bool IsDragging => isDragging;
        public bool DragEndedOnce => dragEndedOnce;
        public bool WasClickRelease => wasClickRelease;
        public bool IsToggleOn => isToggleOn;

        #endregion

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            rootTransform = transform.parent != null ? transform.parent : transform;

            dragHandler = rootTransform.GetComponent<DragHandler>();
            if (dragHandler == null && isDraggable)
            {
                Debug.LogWarning("[ObjectMouseEvent] DragHandler가 상위 오브젝트에 없습니다.");
            }
        }

        private void Update()
        {
            Vector2 inputPos = GetInputPosition();
            bool pressed = GetInputPressed();
            bool released = GetInputReleased();
            bool isHeld = GetInputHeld();

            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            // 입력 시작
            if (pressed && isClickable &&
                Physics.Raycast(ray, out RaycastHit hit) &&
                hit.collider != null &&
                hit.collider.transform.IsChildOf(transform))
            {
                dragStartPos = inputPos;
                zDistance = Vector3.Distance(mainCamera.transform.position, rootTransform.position);

                isDragging = true;
                wasDragged = false;
                clickRequested = true;

                if (isDraggable && dragHandler != null)
                    dragHandler.StartDrag(inputPos);
            }

            // 드래그 중
            if (isDragging && isDraggable && isHeld)
            {
                if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
                {
                    wasDragged = true;
                    clickRequested = false;
                }

                if (wasDragged)
                {
                    Vector3 rot = rootTransform.localEulerAngles;
                    rot.y = 0f;
                    rootTransform.localEulerAngles = rot;
                }
            }

            // 입력 해제
            if (released && isDragging)
            {
                isDragging = false;
                dragEndedOnce = true;
                wasClickRelease = clickRequested && !wasDragged;

                if (dragHandler != null)
                    dragHandler.EndDrag();

                // 실제 클릭 인정되면 토글 전환 + 이벤트 발행
                if (clickRequested && !wasDragged && isClickable)
                {
                    isToggleOn = !isToggleOn;
                    OnClicked?.Invoke();
                }
            }
        }

        private void LateUpdate()
        {
            // 드래그 종료 플래그는 한 프레임만 유지
            dragEndedOnce = false;

            // 클릭 플래그는 드래그 아닐 때만 유지
            if (!isDragging)
                clickRequested = false;
        }

        #region Public Reset Methods

        public void ResetClickFlag()
        {
            clickRequested = false;
        }

        public void ResetDragEndFlag()
        {
            dragEndedOnce = false;
            wasClickRelease = false;
        }

        /// <summary>
        /// 외부에서 클릭 토글 상태를 강제로 초기화
        /// (Hover 해제 시 회전 복귀 후 상태 동기화를 위해 사용)
        /// </summary>
        public void ForceResetToggle()
        {
            isToggleOn = false;
        }

        #endregion

        #region Input Helpers

        private Vector2 GetInputPosition()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Mouse.current.position.ReadValue();
#else
            return Touchscreen.current.primaryTouch.position.ReadValue();
#endif
        }

        private bool GetInputPressed()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
#endif
        }

        private bool GetInputReleased()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
#endif
        }

        private bool GetInputHeld()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Mouse.current.leftButton.isPressed;
#else
            return Touchscreen.current.primaryTouch.press.isPressed;
#endif
        }

        #endregion
    }
}

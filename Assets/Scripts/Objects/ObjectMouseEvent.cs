using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 마우스/터치 입력을 감지하고,
    /// Hover, Click, Drag 상태를 외부에 이벤트로 전달하는 컴포넌트
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float dragThreshold = 10f;

        [Header("Interaction Control")]
        public bool isDraggable = true;
        public bool isClickable = true;

        [Header("Events")]
        public UnityAction OnHoverEnter;
        public UnityAction OnHoverExit;
        public UnityAction OnClicked;
        public UnityAction OnBeginDrag;
        public UnityAction OnEndDrag;

        private Vector2 dragStartPos;
        private float zDistance;

        private bool isHovered = false;
        private bool isDragging = false;
        private bool wasDragged = false;
        private bool dragEndedOnce = false;
        private bool clickRequested = false;
        private bool wasClickRelease = false;
        private bool isToggleOn = false;

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
                Debug.LogWarning("[ObjectMouseEvent] DragHandler가 상위 오브젝트에 없습니다.");
        }

        private void Update()
        {
            Vector2 inputPos = GetInputPosition();
            bool pressed = GetInputPressed();
            bool released = GetInputReleased();
            bool isHeld = GetInputHeld();

            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            // Hover 판정
            bool isHit = Physics.Raycast(ray, out RaycastHit hit) &&
                         hit.collider != null &&
                         (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform));

            if (isHit && !isHovered)
            {
                isHovered = true;
                OnHoverEnter?.Invoke();
            }
            else if (!isHit && isHovered && !isDragging)
            {
                isHovered = false;
                OnHoverExit?.Invoke();
            }

            // 입력 시작
            if (pressed && isClickable && isHit)
            {
                dragStartPos = inputPos;
                zDistance = Vector3.Distance(mainCamera.transform.position, rootTransform.position);

                isDragging = true;
                wasDragged = false;
                clickRequested = true;
            }

            // 입력 유지 중: 드래그 판정
            if (isDragging && isDraggable && isHeld)
            {
                float dragDistance = Vector2.Distance(inputPos, dragStartPos);
                if (!wasDragged && dragDistance > dragThreshold)
                {
                    wasDragged = true;
                    clickRequested = false;

                    if (dragHandler != null)
                        dragHandler.StartDrag(dragStartPos);

                    OnBeginDrag?.Invoke();
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

                if (wasDragged)
                    OnEndDrag?.Invoke();

                if (clickRequested && !wasDragged && isClickable)
                {
                    isToggleOn = !isToggleOn;
                    OnClicked?.Invoke();
                }
            }
        }

        private void LateUpdate()
        {
            dragEndedOnce = false;

            if (!isDragging)
                clickRequested = false;
        }

        #region Public Methods

        public void ResetClickFlag()
        {
            clickRequested = false;
        }

        public void ResetDragEndFlag()
        {
            dragEndedOnce = false;
            wasClickRelease = false;
        }

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

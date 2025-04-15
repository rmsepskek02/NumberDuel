using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 마우스(또는 터치) 입력을 감지하고,
    /// 클릭 및 드래그 시작/종료 이벤트를 판단하여 DragHandler에 위치 이동을 위임하는 컨트롤러
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private Camera mainCamera;         // 입력 기준이 되는 카메라
        [SerializeField] private float dragThreshold = 10f; // 드래그로 판단하기 위한 최소 이동 거리 (픽셀 단위)

        [Header("Click Event")]
        public UnityAction OnClicked; // 클릭 감지 시 외부에 발행되는 이벤트

        [Header("Interaction Control")]
        public bool isDraggable = true;   // 드래그 허용 여부
        public bool isClickable = true;   // 클릭 허용 여부

        private Vector2 dragStartPos;     // 입력 시작 위치
        private float zDistance;          // 카메라와 오브젝트 간 거리

        private bool isDragging = false;      // 현재 드래그 중인지 여부
        private bool wasDragged = false;      // 실제 드래그로 간주되었는지 여부
        private bool dragEndedOnce = false;   // 드래그 종료 직후 한 프레임만 true
        private bool clickRequested = false;  // 클릭이 의도된 상태인지 여부
        private bool wasClickRelease = false; // 클릭 후 손을 뗀 상태인지 여부

        private DragHandler dragHandler;      // 위치 이동을 담당할 외부 핸들러
        private Transform rootTransform;      // 실제 드래그 대상 (보통 상위 카드 루트 오브젝트)

        #region Public Properties

        public bool IsDragging => isDragging;
        public bool DragEndedOnce => dragEndedOnce;
        public bool WasClickRelease => wasClickRelease;

        #endregion

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            // 드래그 대상은 부모 오브젝트 기준
            rootTransform = transform.parent != null ? transform.parent : transform;

            // DragHandler는 루트에 존재해야 함
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

            // 입력 시작: 클릭 가능한 상태에서 Raycast로 자신 또는 자식 감지
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

                // 드래그 위치 이동 시작
                if (isDraggable && dragHandler != null)
                    dragHandler.StartDrag(inputPos);
            }

            // 드래그 처리: 입력 유지 중이며 드래그가 허용되었을 때
            if (isDragging && isDraggable && isHeld)
            {
                // 일정 거리 이상 이동 시 드래그로 간주
                if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
                {
                    wasDragged = true;
                    clickRequested = false;
                }

                // 드래그 중이면 Y축 회전 잠금
                if (wasDragged)
                {
                    Vector3 rot = rootTransform.localEulerAngles;
                    rot.y = 0f;
                    rootTransform.localEulerAngles = rot;
                }
            }

            // 입력 종료 처리
            if (released && isDragging)
            {
                isDragging = false;
                dragEndedOnce = true;
                wasClickRelease = clickRequested && !wasDragged;

                // 드래그 위치 이동 종료
                if (dragHandler != null)
                    dragHandler.EndDrag();

                // 클릭 인정 조건: 이동이 거의 없었고 클릭 가능 상태
                if (clickRequested && !wasDragged && isClickable)
                    OnClicked?.Invoke();
            }
        }

        private void LateUpdate()
        {
            // 드래그 종료 플래그는 한 프레임만 유지
            dragEndedOnce = false;

            // 드래그 상태가 아니라면 클릭 상태 초기화
            if (!isDragging)
                clickRequested = false;
        }

        #region Public Reset Methods

        /// <summary>
        /// 외부에서 클릭 판정을 초기화할 때 사용
        /// </summary>
        public void ResetClickFlag() => clickRequested = false;

        /// <summary>
        /// 외부에서 드래그 종료 상태를 초기화할 때 사용
        /// </summary>
        public void ResetDragEndFlag()
        {
            dragEndedOnce = false;
            wasClickRelease = false;
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

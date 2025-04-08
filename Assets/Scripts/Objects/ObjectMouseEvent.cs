using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 오브젝트의 마우스 입력을 감지하여 드래그/클릭 상태를 처리하는 컴포넌트
    /// - 클릭과 드래그를 거리 기반으로 구분
    /// - Raycast를 통해 자신의 오브젝트가 클릭된 경우만 처리
    /// - 클릭 발생 시 외부로 이벤트 발행
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private Camera mainCamera;         // 입력 감지를 위한 카메라
        [SerializeField] private float dragThreshold = 10f; // 드래그 판단 거리 (px)

        [Header("Click Event")]
        public UnityAction OnClicked; // 클릭 시 외부에 알리는 이벤트

        private Transform rootTransform;

        private Vector3 offset;
        private float zDistance;
        private Vector2 dragStartPos;

        private bool wasDragged = false;
        private bool isDragging = false;
        private bool dragEnded = false;
        private bool dragEndedOnce = false;
        private bool clickRequested = false;
        private bool wasClickRelease = false;

        #region Public Properties

        public bool IsDragging => isDragging;
        public bool WasDragged => wasDragged || dragEnded;
        public bool DragEndedOnce => dragEndedOnce;
        public bool ClickRequested => clickRequested;
        public bool WasClickRelease => wasClickRelease;

        #endregion

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            rootTransform = transform.parent != null ? transform.parent : transform;
        }

        private void Update()
        {
            Vector2 inputPos = GetInputPosition();
            bool pressed = GetInputPressed();
            bool released = GetInputReleased();
            bool isHeld = GetInputHeld();

            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            // 클릭 시작
            if (pressed && Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null && hit.collider.transform.IsChildOf(transform))
            {
                dragStartPos = inputPos;
                zDistance = Vector3.Distance(mainCamera.transform.position, rootTransform.position);
                offset = rootTransform.position - mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));

                isDragging = true;
                wasDragged = false;
                clickRequested = true;
            }

            // 드래그 중
            if (isDragging && isHeld)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                rootTransform.position = worldPos + offset;

                if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
                {
                    wasDragged = true;
                    clickRequested = false; // 클릭이 아님
                }

                if (wasDragged)
                {
                    Vector3 localEuler = rootTransform.localEulerAngles;
                    localEuler.y = 0;
                    rootTransform.localEulerAngles = localEuler;
                }
            }

            // 입력 해제
            if (released && isDragging)
            {
                dragEnded = true;
                dragEndedOnce = true;
                isDragging = false;

                wasClickRelease = clickRequested && !wasDragged;

                // 클릭으로 인정된 경우에만 이벤트 호출
                if (clickRequested && !wasDragged)
                {
                    OnClicked?.Invoke();
                }
            }
        }

        private void LateUpdate()
        {
            dragEnded = false;

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

        #endregion

        #region Raycast Utility

        public bool IsPointerOverMe(Vector2 inputPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(inputPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var owner = hit.collider.GetComponentInParent<ObjectMouseEvent>();
                return owner == this;
            }
            return false;
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

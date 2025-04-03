using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 오브젝트 드래그 처리를 담당
    /// 클릭과 드래그 구분, 카메라 기준 위치 조정, 드래그 종료 처리 등을 수행
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        // 카메라 기준으로 오브젝트 위치 조정
        private Camera mainCamera;
        private Vector3 offset;
        private float zDistance;
        private bool isDragging;

        // 드래그 감지 변수
        private Vector2 dragStartPos;
        private bool wasDragged = false;
        private bool dragEnded = false;
        private float dragThreshold = 10f;

        private bool dragEndedOnce = false;

        public bool IsDragging => isDragging;                         // 현재 드래그 중인지 여부
        public bool WasDragged => wasDragged || dragEnded;            // 드래그된 적 있는지 여부
        public bool DragEndedOnce => dragEndedOnce;                   // 드래그가 한번 종료된 적 있는지 여부
        public bool ClickRequested { get; private set; } = false;     // 클릭으로 시작된 입력인지 여부
        public bool WasClickRelease { get; private set; } = false;    // 클릭으로 해제되었는지 여부

        private Transform rootTransform;

        void Start()
        {
            mainCamera = Camera.main;
            rootTransform = transform.parent;
        }

        void Update()
        {
            Vector2 inputPos = GetInputPosition();
            bool pressed = GetInputPressed();
            bool released = GetInputReleased();
            bool isPressed = GetInputHeld();

            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            // 클릭 시작
            if (pressed && Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                wasDragged = false;
                dragStartPos = inputPos;

                zDistance = Vector3.Distance(mainCamera.transform.position, rootTransform.position);
                offset = rootTransform.position - mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                isDragging = true;
                ClickRequested = true;
            }

            // 드래그 중
            if (isDragging && isPressed)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                rootTransform.position = worldPos + offset;

                // 일정 거리 이상 이동하면 드래그로 간주
                if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
                {
                    wasDragged = true;
                    ClickRequested = false; // 클릭이 아니라 드래그
                }

                // 실제 드래그일 때만 Y 회전 고정
                if (wasDragged)
                {
                    Vector3 localEuler = rootTransform.localEulerAngles;
                    localEuler.y = 0;
                    rootTransform.localEulerAngles = localEuler;
                }
            }


            // 입력 해제 시
            if (released)
            {
                if (isDragging)
                {
                    dragEnded = true;
                    dragEndedOnce = true;
                    isDragging = false;

                    WasClickRelease = ClickRequested && !wasDragged;
                }
            }
        }

        public void ResetClickFlag()
        {
            ClickRequested = false;
        }

        public void ResetDragEndFlag()
        {
            dragEndedOnce = false;
            WasClickRelease = false; // HoverCardMotion에서 복귀 판단 후 초기화
        }

        void LateUpdate()
        {
            dragEnded = false;
            // WasClickRelease는 여기서 초기화하면 안 됨 (타이밍 문제)
        }

        // 입력 관련 유틸 메서드
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
    }
}

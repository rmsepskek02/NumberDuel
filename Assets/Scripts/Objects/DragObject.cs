using UnityEngine;
using UnityEngine.InputSystem;
namespace Objects
{
    /// <summary>
    /// 오브젝트를 마우스 또는 터치 입력으로 드래그할 수 있도록 하는 컴포넌트.
    /// PC/모바일 모두 지원하며, 일정 거리 이상 움직이면 드래그로 간주함.
    /// </summary>
    public class DragObject : MonoBehaviour
    {
        private Camera mainCamera; // 입력 위치를 월드 좌표로 변환하는 데 사용할 카메라
        private Vector3 offset;    // 클릭한 위치와 오브젝트 중심 사이의 거리 보정값
        private float zDistance;   // 카메라와 오브젝트 간의 Z 거리
        private bool isDragging;   // 현재 드래그 중인지 여부

        private Vector2 dragStartPos;   // 드래그 시작 지점의 입력 위치
        private bool wasDragged = false;    // 일정 거리 이상 움직였는지 여부
        private bool dragEnded = false;
        private float dragThreshold = 1f; // 드래그로 간주할 최소 거리
        public bool IsDragging => isDragging;


        public bool WasDragged => wasDragged || dragEnded; // 외부에서 드래그 여부를 확인할 수 있도록 제공


        void Start()
        {
            mainCamera = Camera.main;
        }

        void Update()
        {
            // 현재 입력 상태 읽기 (위치, 눌림, 해제 등)
            Vector2 inputPos = GetInputPosition();
            bool pressed = GetInputPressed();
            bool released = GetInputReleased();
            bool isPressed = GetInputHeld();

            // 입력 위치로부터 레이 생성
            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            // 드래그 시작 조건: 클릭한 대상이 자기 자신일 경우
            if (pressed && Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                wasDragged = false;
                dragStartPos = inputPos;

                zDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
                offset = transform.position - mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                isDragging = true;
            }

            // 드래그 중일 때 위치 업데이트
            if (isDragging && isPressed)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                transform.position = worldPos + offset;

                // 회전 고정 (부모의 X 회전 유지, Y만 정면으로)
                Vector3 localEuler = transform.localEulerAngles;
                localEuler.y = 0;
                transform.localEulerAngles = localEuler;

                // 일정 거리 이상 움직였으면 드래그로 간주
                if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
                    wasDragged = true;
            }

            // 드래그 해제
            if (released)
            {
                isDragging = false;
                dragEnded = wasDragged;
            }
        }
        void LateUpdate()
        {
            dragEnded = false;
        }

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

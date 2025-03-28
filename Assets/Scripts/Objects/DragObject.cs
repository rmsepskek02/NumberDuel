using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 오브젝트를 마우스 또는 터치로 드래그할 수 있게 해주는 컴포넌트
    /// PC와 모바일 환경 모두 지원
    /// </summary>
    public class DragObject : MonoBehaviour
    {
        private Camera mainCamera;
        private Vector3 offset;
        private float zDistance;
        private bool isDragging;

        private Vector2 dragStartPos;
        private bool wasDragged = false;
        private bool dragEnded = false;
        private float dragThreshold = 1f;

        public bool IsDragging => isDragging;
        public bool WasDragged => wasDragged || dragEnded;

        void Start()
        {
            mainCamera = Camera.main;
        }

        void Update()
        {
            Vector2 inputPos = GetInputPosition();
            bool pressed = GetInputPressed();
            bool released = GetInputReleased();
            bool isPressed = GetInputHeld();

            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            // 드래그 시작 조건: 클릭한 오브젝트가 자기 자신인지 체크
            if (pressed && Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                wasDragged = false;
                dragStartPos = inputPos;

                zDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
                offset = transform.position - mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                isDragging = true;
            }

            if (isDragging && isPressed)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                transform.position = worldPos + offset;

                // 카드가 항상 정면을 향하게 Y축 회전만 제어
                Vector3 localEuler = transform.localEulerAngles;
                localEuler.y = 0;
                transform.localEulerAngles = localEuler;

                if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
                    wasDragged = true;
            }

            if (released)
            {
                if (isDragging)
                    dragEnded = true;

                isDragging = false;
            }
        }

        void LateUpdate()
        {
            dragEnded = false;
        }

        // 입력 처리: 플랫폼별 분기
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

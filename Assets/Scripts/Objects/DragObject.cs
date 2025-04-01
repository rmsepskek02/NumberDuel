using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    public class DragObject : MonoBehaviour
    {
        private Camera mainCamera;
        private Vector3 offset;
        private float zDistance;
        private bool isDragging;

        private Vector2 dragStartPos;
        private bool wasDragged = false;
        private bool dragEnded = false;
        private float dragThreshold = 10f;

        private bool dragEndedOnce = false;

        public bool IsDragging => isDragging;
        public bool WasDragged => wasDragged || dragEnded;
        public bool DragEndedOnce => dragEndedOnce;

        private Transform rootTransform;
        public bool ClickRequested { get; private set; } = false;

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

            if (pressed && Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                wasDragged = false;
                dragStartPos = inputPos;

                zDistance = Vector3.Distance(mainCamera.transform.position, rootTransform.position);
                offset = rootTransform.position - mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                isDragging = true;
                ClickRequested = true; // 후보 클릭 요청
            }

            if (isDragging && isPressed)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                rootTransform.position = worldPos + offset;

                Vector3 localEuler = rootTransform.localEulerAngles;
                localEuler.y = 0;
                rootTransform.localEulerAngles = localEuler;

                if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
                {
                    wasDragged = true;
                    ClickRequested = false; // 드래그로 판별되면 클릭 취소
                }
            }

            if (released)
            {
                if (isDragging)
                {
                    dragEnded = true;
                    dragEndedOnce = true;
                    isDragging = false;
                }
            }
        }
        public void ResetClickFlag()
        {
            ClickRequested = false;
        }
        void LateUpdate()
        {
            dragEnded = false;
        }

        public void ResetDragEndFlag()
        {
            dragEndedOnce = false;
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

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    public class ObjectMouseEvent : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float dragThreshold = 5f;

        [Header("Interaction Control")]
        public bool isDraggable = true;
        public bool isClickable = true;

        [Header("Events")]
        public UnityAction OnHoverEnter;
        public UnityAction OnHoverExit;
        public UnityAction OnClickPressed;
        public UnityAction OnClickReleased;
        public UnityAction OnBeginDrag;
        public UnityAction OnEndDrag;
        public UnityAction<bool> OnToggleChanged;

        private Vector2 dragStartPos;
        private bool isHovered;
        private bool isDragging;
        private bool wasDragged;
        private bool clickRequested;
        private bool isToggleOn;
        private bool interactionBlocked = false;

        private DragHandler dragHandler;
        private Transform rootTransform;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            rootTransform = transform.parent ?? transform;
            dragHandler = rootTransform.GetComponent<DragHandler>();
        }

        private void Update()
        {
            if (interactionBlocked) return;

            Vector2 inputPos = GetInputPosition();
            bool pressed = GetInputPressed();
            bool released = GetInputReleased();
            bool isHeld = GetInputHeld();

            HandleHoverRaycast(inputPos);
            HandleInputPress(inputPos, pressed);
            HandleDragUpdate(inputPos, isHeld);
            HandleInputRelease(released);
        }

        private void LateUpdate()
        {
            if (!isDragging)
                clickRequested = false;
        }

        private void HandleHoverRaycast(Vector2 inputPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(inputPos);
            bool isHit = Physics.Raycast(ray, out RaycastHit hit) &&
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
        }

        private void HandleInputPress(Vector2 inputPos, bool pressed)
        {
            if (!pressed || !isClickable || !isHovered) return;

            dragStartPos = inputPos;
            isDragging = true;
            wasDragged = false;
            clickRequested = true;

            OnClickPressed?.Invoke();
        }

        private void HandleDragUpdate(Vector2 inputPos, bool isHeld)
        {
            if (!isDragging || !isDraggable || !isHeld) return;

            float dragDistance = Vector2.Distance(inputPos, dragStartPos);
            if (!wasDragged && dragDistance > dragThreshold)
            {
                wasDragged = true;
                clickRequested = false;
                dragHandler?.StartDrag(dragStartPos);
                OnBeginDrag?.Invoke();
            }

            if (wasDragged)
            {
                Vector3 rot = rootTransform.localEulerAngles;
                rot.y = 0f;
                rootTransform.localEulerAngles = rot;
            }
        }

        private void HandleInputRelease(bool released)
        {
            if (!released || !isDragging) return;

            isDragging = false;

            if (wasDragged)
            {
                dragHandler?.EndDrag();
                OnEndDrag?.Invoke();
                isToggleOn = false;
            }
            else if (clickRequested && isClickable)
            {
                isToggleOn = !isToggleOn;
                OnToggleChanged?.Invoke(isToggleOn);
                OnClickReleased?.Invoke();
            }
        }

        public void SetInteractionBlocked(bool blocked)
        {
            interactionBlocked = blocked;
        }

        public void ForceResetToggle()
        {
            isToggleOn = false;
        }

        public void ForceHoverEnter()
        {
            isHovered = true;
            OnHoverEnter?.Invoke();
        }

        public void RegisterListeners(
            UnityAction onHoverEnter,
            UnityAction onHoverExit,
            UnityAction onClickPressed,
            UnityAction onClickReleased,
            UnityAction onBeginDrag,
            UnityAction onEndDrag,
            UnityAction<bool> onToggleChanged)
        {
            OnHoverEnter += onHoverEnter;
            OnHoverExit += onHoverExit;
            OnClickPressed += onClickPressed;
            OnClickReleased += onClickReleased;
            OnBeginDrag += onBeginDrag;
            OnEndDrag += onEndDrag;
            OnToggleChanged += onToggleChanged;
        }

        public void UnregisterListeners(
            UnityAction onHoverEnter,
            UnityAction onHoverExit,
            UnityAction onClickPressed,
            UnityAction onClickReleased,
            UnityAction onBeginDrag,
            UnityAction onEndDrag,
            UnityAction<bool> onToggleChanged)
        {
            OnHoverEnter -= onHoverEnter;
            OnHoverExit -= onHoverExit;
            OnClickPressed -= onClickPressed;
            OnClickReleased -= onClickReleased;
            OnBeginDrag -= onBeginDrag;
            OnEndDrag -= onEndDrag;
            OnToggleChanged -= onToggleChanged;
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

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 마우스/터치 입력을 감지하여
    /// Hover, Click, Drag 상태를 외부 이벤트로 전달하는 컴포넌트
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        #region ───── Inspector Fields ─────

        [Header("Input Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float dragThreshold = 10f;

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

        #endregion

        #region ───── Internal Fields ─────

        private Vector2 dragStartPos;
        private float zDistance;
        private bool isHovered;
        private bool isDragging;
        private bool wasDragged;
        private bool clickRequested;
        private bool isToggleOn;

        private DragHandler dragHandler;
        private Transform rootTransform;

        #endregion

        #region ───── Unity Lifecycle ─────

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            rootTransform = transform.parent ?? transform;
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

        #endregion

        #region ───── Input Logic ─────

        /// <summary>
        /// Hover 상태 판정 및 이벤트 발행
        /// </summary>
        private void HandleHoverRaycast(Vector2 inputPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(inputPos);
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
        }

        /// <summary>
        /// 입력 시작 처리 (클릭/드래그 후보)
        /// </summary>
        private void HandleInputPress(Vector2 inputPos, bool pressed)
        {
            if (!pressed || !isClickable || !isHovered)
                return;

            dragStartPos = inputPos;
            zDistance = Vector3.Distance(mainCamera.transform.position, rootTransform.position);

            isDragging = true;
            wasDragged = false;
            clickRequested = true;

            OnClickPressed?.Invoke();
        }

        /// <summary>
        /// 드래그 진행 처리
        /// </summary>
        private void HandleDragUpdate(Vector2 inputPos, bool isHeld)
        {
            if (!isDragging || !isDraggable || !isHeld)
                return;

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

        /// <summary>
        /// 입력 해제 처리 (클릭/드래그 종료 판단)
        /// </summary>
        private void HandleInputRelease(bool released)
        {
            if (!released || !isDragging)
                return;

            isDragging = false;

            if (wasDragged)
            {
                dragHandler?.EndDrag();
                OnEndDrag?.Invoke();
                isToggleOn = false; // 드래그 종료 시 토글 리셋
            }
            else if (clickRequested && isClickable)
            {
                isToggleOn = !isToggleOn;
                OnToggleChanged?.Invoke(isToggleOn);
                OnClickReleased?.Invoke();
            }
        }

        #endregion

        #region ───── Listener Utilities ─────

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

        #endregion

        #region ───── Input Helpers ─────

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

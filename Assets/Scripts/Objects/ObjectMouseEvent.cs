using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 마우스(또는 터치) 입력을 감지하여 카드의 인터랙션 상태를 관리하는 컴포넌트
    /// - Hover 진입/이탈 감지
    /// - 클릭 및 토글 동작
    /// - 드래그 시작/종료 감지 및 연동
    /// - 드래그 중 회전 제한
    /// CardMotion 등에서 이벤트 구독하여 시각적 연출 처리
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("입력 처리를 위한 메인 카메라")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("드래그로 판정할 최소 거리")]
        [SerializeField] private float dragThreshold = 5f;

        [Header("Interaction Control")]
        [Tooltip("드래그 가능 여부")]
        public bool isDraggable = true;

        [Tooltip("클릭 가능 여부")]
        public bool isClickable = true;

        [Header("Events")]
        public UnityAction OnHoverEnter;              // Hover 진입 시 호출
        public UnityAction OnHoverExit;               // Hover 종료 시 호출
        public UnityAction OnClickPressed;            // 클릭 시작 시 호출
        public UnityAction OnClickReleased;           // 클릭 끝났을 때 호출
        public UnityAction OnBeginDrag;               // 드래그 시작 시 호출
        public UnityAction OnEndDrag;                 // 드래그 종료 시 호출
        public UnityAction<bool> OnToggleChanged;     // 클릭으로 상태 토글 시 호출 (토글 상태 전달)

        /// <summary>
        /// 현재 마우스(또는 손가락)가 카드 위에 있는지 여부
        /// </summary>
        public bool IsHovered => isHovered;

        private bool isHovered;            // Hover 상태 추적
        private bool isDragging;           // 현재 드래그 중인지 여부
        private bool wasDragged;           // 실제 드래그로 판정되었는지 여부
        private bool clickRequested;       // 클릭 시작 후 Release 대기 중인지 여부
        private bool isToggleOn;           // 클릭 토글 상태
        private bool interactionBlocked;   // 외부 제어로 상호작용 차단

        private Vector2 dragStartPos;      // 드래그 시작 지점

        private DragHandler dragHandler;   // 실제 Transform 이동을 담당하는 핸들러
        private Transform rootTransform;   // 카드 루트 (회전 처리용)

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            rootTransform = transform.parent ?? transform;
            dragHandler = rootTransform.GetComponent<DragHandler>();
        }

        private void Update()
        {
            Vector2 inputPos = GetInputPosition();

            // Hover 감지는 interactionBlocked 여부와 관계없이 항상 처리
            HandleHoverRaycast(inputPos);

            if (interactionBlocked) return;

            HandleInputPress(inputPos, GetInputPressed());
            HandleDragUpdate(inputPos, GetInputHeld());
            HandleInputRelease(GetInputReleased());
        }

        private void LateUpdate()
        {
            // 드래그 중이 아니면 클릭 요청 상태 초기화
            if (!isDragging)
                clickRequested = false;
        }

        /// <summary>
        /// Raycast를 사용해 마우스(터치)가 카드 위에 있는지 감지
        /// Hover 상태 전환 이벤트 발생
        /// </summary>
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

        /// <summary>
        /// 클릭 시작 처리
        /// 클릭 조건: 카드 위 + 클릭 가능 설정
        /// </summary>
        private void HandleInputPress(Vector2 inputPos, bool pressed)
        {
            if (!pressed || !isClickable || !isHovered) return;

            dragStartPos = inputPos;
            isDragging = true;
            wasDragged = false;
            clickRequested = true;

            OnClickPressed?.Invoke();
        }

        /// <summary>
        /// 드래그 거리 측정 및 드래그 시작 처리
        /// 드래그 중일 때는 카드 회전 Y를 0으로 고정
        /// </summary>
        private void HandleDragUpdate(Vector2 inputPos, bool isHeld)
        {
            if (!isDragging || !isDraggable || !isHeld) return;

            float dragDistance = Vector2.Distance(inputPos, dragStartPos);

            // 드래그 조건 충족 시 시작 처리
            if (!wasDragged && dragDistance > dragThreshold)
            {
                wasDragged = true;
                clickRequested = false;

                dragHandler?.StartDrag(dragStartPos);
                OnBeginDrag?.Invoke();
            }

            // 드래그 중이면 Y축 회전 고정 (카드가 돌아가지 않도록)
            if (wasDragged)
            {
                Vector3 rot = rootTransform.localEulerAngles;
                rot.y = 0f;
                rootTransform.localEulerAngles = rot;
            }
        }

        /// <summary>
        /// 클릭 또는 드래그 종료 처리
        /// 클릭 요청 상태였고 드래그가 아니면 클릭 Release로 간주
        /// </summary>
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

        /// <summary>
        /// 외부에서 입력 차단 제어
        /// (ex. 드래그 복귀 중엔 입력 막기 위함)
        /// </summary>
        public void SetInteractionBlocked(bool blocked)
        {
            interactionBlocked = blocked;
        }

        /// <summary>
        /// Toggle 상태 강제 초기화 (HoverExit 등에서 사용)
        /// </summary>
        public void ForceResetToggle()
        {
            isToggleOn = false;
        }

        /// <summary>
        /// 이벤트 리스너 등록
        /// </summary>
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

        /// <summary>
        /// 이벤트 리스너 해제
        /// </summary>
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

        /// <summary>
        /// 현재 입력 위치를 반환 (에디터/모바일 대응)
        /// </summary>
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

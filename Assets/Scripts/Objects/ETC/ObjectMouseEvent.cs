using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 마우스(또는 터치) 입력을 감지하여 카드의 인터랙션 상태를 관리하는 컴포넌트
    /// - Hover 진입/탈출 감지
    /// - 클릭 및 토글 감지
    /// - 드래그 시작/종료 감지 및 추적
    /// - 드래그 중 회전 제어
    /// - 실제로 인터랙션은 항상 가능, 프로세스 중인지는 Card.cs에서 처리
    /// CardMotion 등과 이벤트 연동하여 시각적 효과 처리
    /// </summary>
    public class ObjectMouseEvent : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Input Settings")]
        [Tooltip("입력 처리에 사용 되는 카메라")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("드래그로 간주될 최소 거리")]
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
        public UnityAction OnClickReleased;           // 클릭 릴리즈 시 호출
        public UnityAction OnBeginDrag;               // 드래그 시작 시 호출
        public UnityAction OnEndDrag;                 // 드래그 종료 시 호출
        public UnityAction<bool> OnToggleChanged;     // 클릭으로 토글 모드 시 호출 (회전 제어 등에)

        private bool isHovered;            // Hover 상태 저장
        private bool isDragging;           // 현재 드래그 중인지 상태
        private bool wasDragged;           // 이번 드래그로 이동되었는지 여부
        private bool clickRequested;       // 클릭 시작 후 Release 시에 발생될 여부
        private bool isToggleOn;           // 클릭 토글 상태
        private bool interactionBlocked;   // 외부 잠금시 상호작용 차단

        private Vector2 dragStartPos;      // 드래그 시작 좌표

        private DragHandler dragHandler;   // 실제 Transform 이동을 담당하는 핸들러
        private Transform rootTransform;   // 카드 루트 (회전 처리용)

        /// <summary>
        /// 현재 마우스(또는 터치포인트)가 카드 위에 있는지 여부
        /// </summary>
        public bool IsHovered => isHovered;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            rootTransform = transform.parent ?? transform;
            dragHandler = rootTransform.GetComponent<DragHandler>();

            // DragHandler가 없으면 드래그 불가능 처리
            if (dragHandler == null)
                isDraggable = false;
        }

        private void Update()
        {
            Vector2 inputPos = GetInputPosition();

            // Hover 감지는 항상 처리
            HandleHoverRaycast(inputPos);

            // 외부 잠금 상황에선 입력 처리 중단 (프로세스 중인지는 Card.cs에서 처리)
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
        #endregion

        #region Hover Detection
        /// <summary>
        /// Raycast를 이용해 마우스(터치)가 카드 위에 있는지 감지
        /// Hover 상태 전환 이벤트 발생
        /// </summary>
        private void HandleHoverRaycast(Vector2 inputPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            // Detector 레이어 감지는 제외 레이어 마스크 사용함
            int detectorLayer = LayerMask.NameToLayer("Detector");
            int detectorLayerMask = 1 << detectorLayer;
            int everythingExceptDetector = ~detectorLayerMask; // Detector는 제외!

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, everythingExceptDetector))
            {
                bool isHit = (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform));

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
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// 클릭 시작 처리
        /// 클릭 조건: 카드 위 + 클릭 가능 상태
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
        /// 드래그 거리 확인 후 드래그 모드 처리
        /// 드래그 중인 경우 카드 회전 Y축 0도로 고정
        /// </summary>
        private void HandleDragUpdate(Vector2 inputPos, bool isHeld)
        {
            if (!isDragging || !isDraggable || !isHeld) return;

            float dragDistance = Vector2.Distance(inputPos, dragStartPos);

            // 드래그 거리 임계 도달
            if (!wasDragged && dragDistance > dragThreshold)
            {
                wasDragged = true;
                clickRequested = false;

                if (dragHandler != null)
                {
                    dragHandler.StartDrag(dragStartPos);
                    OnBeginDrag?.Invoke();
                }
                else
                {
                    // 드래그 불가 카드인 경우에는 클릭으로도 인식되지 않도록 처리
                    isDragging = false;
                }
            }

            if (wasDragged)
            {
                Vector3 rot = rootTransform.localEulerAngles;
                rot.y = 0f;
                rootTransform.localEulerAngles = rot;
            }
        }

        /// <summary>
        /// 클릭 또는 드래그 종료 처리
        /// 클릭 요청 상태였고 드래그가 아니면 클릭 Release로 발생
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
        #endregion

        #region Public Control
        /// <summary>
        /// 외부에서 입력 잠금 설정
        /// (ex. 드래그 복귀 중에 입력 처리 차단)
        /// </summary>
        public void SetInteractionBlocked(bool blocked)
        {
            interactionBlocked = blocked;
        }

        /// <summary>
        /// Toggle 상태 강제 초기화 (HoverExit 시에 사용)
        /// </summary>
        public void ForceResetToggle()
        {
            isToggleOn = false;
        }
        #endregion

        #region Event Registration
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
        #endregion

        #region Platform Input Abstraction
        /// <summary>
        /// 현재 입력 위치를 반환 (데스크톱/모바일 대응)
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
        #endregion
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 카드 Hover 및 클릭 후 회전 토글을 처리하는 컴포넌트
    /// - Hover 시: 확대 + Y 위치 상승
    /// - 클릭 시: 정면(0도) ↔ 원래 회전값으로 회전
    /// - Hover 해제 시: 회전 및 위치 복귀, 클릭 토글 상태 초기화
    /// </summary>
    public class CardMotion : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.3f;           // Hover 시 확대 비율
        [SerializeField] private float hoverYOffset = 0.3f;         // Hover 시 Y축 상승 거리
        [SerializeField] private float returnSpeed = 10f;           // 위치/크기 보간 속도
        [SerializeField] private float rotateSpeed = 180f;          // 회전 속도
        [SerializeField] private float rotationThreshold = 0.5f;    // 회전 완료 판단 각도

        private Transform rootTransform;

        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;
        private float originalY;

        private Vector3 originalRootPosition;
        private Quaternion originalRootRotation;

        private bool initialized = false;
        private bool isHovered = false;
        private bool isReturning = false;

        private bool isRotating = false;
        private Quaternion targetRotation;

        private ObjectMouseEvent objectMouseEvent;
        private Camera mainCamera;

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
            rootTransform = transform.parent;
        }

        private void OnEnable()
        {
            if (objectMouseEvent != null)
            {
                objectMouseEvent.OnClicked += HandleClick;
                objectMouseEvent.OnHoverEnter += HandleHoverEnter;
                objectMouseEvent.OnHoverExit += HandleHoverExit;
            }
        }

        private void OnDisable()
        {
            if (objectMouseEvent != null)
            {
                objectMouseEvent.OnClicked -= HandleClick;
                objectMouseEvent.OnHoverEnter -= HandleHoverEnter;
                objectMouseEvent.OnHoverExit -= HandleHoverExit;
            }
        }

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                SetInitialState();
                initialized = true;
            }
        }

        /// <summary>
        /// 카드 배치 이후 초기 상태값 저장
        /// </summary>
        public void SetInitialState()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalScale = transform.localScale;
            originalY = originalLocalPosition.y;

            if (rootTransform != null)
            {
                originalRootPosition = rootTransform.localPosition;
                originalRootRotation = rootTransform.localRotation;
            }
        }

        private void Update()
        {
            if (!initialized || objectMouseEvent == null)
                return;

            UpdateTransform();
        }

        /// <summary>
        /// 클릭 이벤트에 의해 회전 토글 실행
        /// </summary>
        private void HandleClick()
        {
            if (!isRotating)
            {
                SetTargetRotation(objectMouseEvent.IsToggleOn);
                isRotating = true;
            }
        }

        /// <summary>
        /// Hover 진입 시 처리
        /// </summary>
        private void HandleHoverEnter()
        {
            isHovered = true;
            transform.SetSiblingIndex(transform.parent.childCount - 1);
        }

        /// <summary>
        /// Hover 해제 시 처리
        /// </summary>
        private void HandleHoverExit()
        {
            isHovered = false;
            isReturning = true;
        }

        /// <summary>
        /// 클릭 상태에 따라 목표 회전값 설정
        /// </summary>
        private void SetTargetRotation(bool alignToFront)
        {
            if (alignToFront)
                targetRotation = Quaternion.Euler(0f, 0f, 0f); // 정면
            else
                targetRotation = originalRootRotation;         // 부채꼴 상태
        }

        /// <summary>
        /// 회전, Hover, 복귀 등 위치/스케일/회전 보간 처리
        /// </summary>
        private void UpdateTransform()
        {
            Vector3 targetScale = isHovered ? originalLocalScale * hoverScale : originalLocalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);

            if (objectMouseEvent.IsDragging)
                return;

            if (objectMouseEvent.DragEndedOnce && !objectMouseEvent.WasClickRelease)
                isReturning = true;

            // 회전 처리
            if (isRotating)
            {
                if (rootTransform != null)
                {
                    rootTransform.localRotation = Quaternion.RotateTowards(
                        rootTransform.localRotation,
                        targetRotation,
                        rotateSpeed * Time.deltaTime
                    );

                    float angle = Quaternion.Angle(rootTransform.localRotation, targetRotation);
                    if (angle < rotationThreshold)
                    {
                        rootTransform.localRotation = targetRotation;
                        isRotating = false;
                    }
                }
            }

            // Hover 상태일 때: 위치 보간
            if (isHovered)
            {
                Vector3 targetPos = originalLocalPosition;
                targetPos.y = originalY + hoverYOffset;
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    Vector3 rootTarget = originalRootPosition;
                    rootTarget.y = originalRootPosition.y + hoverYOffset;
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, rootTarget, Time.deltaTime * returnSpeed);
                }

                return;
            }

            // Hover 해제 후 복귀 처리
            if (isReturning)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);

                    // 회전 복귀 및 토글 상태 초기화
                    targetRotation = originalRootRotation;
                    isRotating = true;

                    if (objectMouseEvent.IsToggleOn)
                        objectMouseEvent.ForceResetToggle();
                }

                if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.001f &&
                    Vector3.Distance(rootTransform.localPosition, originalRootPosition) < 0.001f)
                {
                    isReturning = false;
                    objectMouseEvent.ResetDragEndFlag();
                }

                return;
            }
        }
    }
}

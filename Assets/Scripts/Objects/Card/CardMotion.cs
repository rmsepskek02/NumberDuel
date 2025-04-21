using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드의 Hover / Click / Drag 시 연출을 담당하는 컴포넌트
    /// - 입력 처리는 ObjectMouseEvent에서 받아 이벤트 기반으로 동작
    /// - Hover 시: 확대 및 Y축 상승
    /// - Click 시: Y축 회전 토글
    /// - Drag 시: Hover 및 회전 중단, 위치 복귀
    /// </summary>
    public class CardMotion : MonoBehaviour
    {
        #region ───── Inspector Settings ─────

        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.3f;
        [SerializeField] private float hoverYOffset = 0.3f;
        [SerializeField] private float returnSpeed = 10f;
        [SerializeField] private float rotateSpeed = 180f;

        #endregion

        #region ───── Internal References ─────

        private Transform rootTransform;
        private ObjectMouseEvent objectMouseEvent;

        #endregion

        #region ───── State Fields ─────

        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;
        private float originalY;

        private Vector3 originalRootPosition;
        private Quaternion originalRootRotation;

        private Quaternion targetRotation;

        private bool initialized = false;
        private bool isHovered = false;
        private bool isReturning = false;
        private bool isRotating = false;
        private bool isDragging = false;

        #endregion

        #region ───── Unity Lifecycle ─────

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
            rootTransform = transform.parent;
        }

        private void OnEnable()
        {
            if (objectMouseEvent != null)
            {
                objectMouseEvent.RegisterListeners(
                    HandleHoverEnter,
                    HandleHoverExit,
                    HandleClickPressed,
                    HandleClickReleased,
                    HandleDragBegin,
                    HandleDragEnd,
                    HandleToggleChanged
                );
            }
        }

        private void OnDisable()
        {
            if (objectMouseEvent != null)
            {
                objectMouseEvent.UnregisterListeners(
                    HandleHoverEnter,
                    HandleHoverExit,
                    HandleClickPressed,
                    HandleClickReleased,
                    HandleDragBegin,
                    HandleDragEnd,
                    HandleToggleChanged
                );
            }
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                SetInitialState();
                initialized = true;
            }
        }

        private void Update()
        {
            if (!initialized || objectMouseEvent == null)
                return;

            UpdateTransform();
        }

        #endregion

        #region ───── Initialization ─────

        /// <summary>
        /// 카드의 초기 상태(위치/회전/크기)를 저장
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

        #endregion

        #region ───── Input Event Handlers ─────

        private void HandleHoverEnter()
        {
            isHovered = true;

            // 복귀 중일 경우 즉시 중단
            if (isReturning)
            {
                isReturning = false;
            }

            transform.SetSiblingIndex(transform.parent.childCount - 1);
        }

        private void HandleHoverExit()
        {
            isHovered = false;
            isReturning = true;
        }

        private void HandleClickPressed()
        {
            // 클릭 누른 시점에 필요한 처리가 있다면 여기에
        }

        private void HandleClickReleased()
        {
            // 클릭 떼는 순간의 효과는 여기에서 처리 가능
        }

        private void HandleToggleChanged(bool toFront)
        {
            SetTargetRotation(toFront);
            isRotating = true;
        }

        private void HandleDragBegin()
        {
            isDragging = true;
            isReturning = false;
        }

        private void HandleDragEnd()
        {
            isDragging = false;
            isReturning = true;
        }

        #endregion

        #region ───── Motion Update Logic ─────

        private void UpdateTransform()
        {
            // Hover 위치 변화 먼저 (즉각적인 반응을 위해)
            if (!isReturning && isHovered)
                UpdateHoverMotion();

            // 크기 변화 (그 다음 시각적으로 따라옴)
            UpdateScale();

            // 회전은 항상 처리
            UpdateRotation();

            // 복귀
            if (isReturning)
            {
                UpdateReturnMotion();
                return;
            }
        }

        private void UpdateScale()
        {
            Vector3 targetScale = isHovered ? originalLocalScale * hoverScale : originalLocalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);
        }

        private void UpdateRotation()
        {
            if (!isRotating || rootTransform == null) return;

            rootTransform.localRotation = Quaternion.RotateTowards(
                rootTransform.localRotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(rootTransform.localRotation, targetRotation) < 0.5f)
            {
                rootTransform.localRotation = targetRotation;
                isRotating = false;
            }
        }

        private void UpdateReturnMotion()
        {
            // Sprite 위치 복귀
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);

            // Root 위치 복귀 + 회전도 원래대로
            if (rootTransform != null)
            {
                rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
                SetTargetRotation(false);
                isRotating = true;
            }

            if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.001f &&
                Vector3.Distance(rootTransform.localPosition, originalRootPosition) < 0.001f)
            {
                isReturning = false;
            }
        }

        private void UpdateHoverMotion()
        {
            Vector3 current = transform.localPosition;
            Vector3 target = new Vector3(current.x, originalY + hoverYOffset, current.z);
            transform.localPosition = Vector3.Lerp(current, target, Time.deltaTime * returnSpeed);

            if (rootTransform != null)
            {
                Vector3 rootCurrent = rootTransform.localPosition;
                Vector3 rootTarget = new Vector3(rootCurrent.x, originalRootPosition.y + hoverYOffset, rootCurrent.z);
                rootTransform.localPosition = Vector3.Lerp(rootCurrent, rootTarget, Time.deltaTime * returnSpeed);
            }
        }

        #endregion

        #region ───── Utility ─────

        private void SetTargetRotation(bool toFront)
        {
            targetRotation = toFront
                ? Quaternion.Euler(0f, 0f, 0f)
                : originalRootRotation;
        }

        #endregion
    }
}

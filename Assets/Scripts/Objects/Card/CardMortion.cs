using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 카드의 Hover 애니메이션 및 클릭 후 회전 복귀, 위치 복귀 등을 담당하는 컴포넌트
    /// - 드래그 중에는 복귀 방지
    /// - Hover 시 확대 및 Y축 이동
    /// - 클릭 후 회전 복원
    /// </summary>
    public class CardMortion : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.3f;     // Hover 시 확대 비율
        [SerializeField] private float hoverYOffset = 0.3f;   // Hover 시 위로 뜨는 높이
        [SerializeField] private float returnSpeed = 10f;     // 복귀 속도

        private Transform rootTransform;

        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Vector3 originalLocalScale;
        private float originalY;

        private Vector3 originalRootPosition;
        private Quaternion originalRootRotation;
        private float originalRootY;

        private bool initialized = false;
        private bool isHovered = false;
        private bool isReturning = false;
        private bool isRotatingToZero = false;

        private ObjectMouseEvent objectMouseEvent;
        private ResponsiveObject responsiveObject;
        private Camera mainCamera;

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
            responsiveObject = GetComponent<ResponsiveObject>();
            rootTransform = transform.parent;
            mainCamera = Camera.main;
        }

        /// <summary>
        /// 현재 Transform의 초기 상태를 기록 (복귀 기준점으로 사용)
        /// </summary>
        public void SetInitialState()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            originalLocalScale = transform.localScale;
            originalY = originalLocalPosition.y;

            if (rootTransform != null)
            {
                originalRootPosition = rootTransform.localPosition;
                originalRootRotation = rootTransform.localRotation;
                originalRootY = originalRootPosition.y;
            }

            initialized = true;
        }

        private void Update()
        {
            if (!initialized || objectMouseEvent == null)
                return;

#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseHover();
#else
            isHovered = false;
#endif

            if (objectMouseEvent.WasClickRelease)
            {
                isRotatingToZero = true;
                objectMouseEvent.ResetClickFlag();
            }

            UpdateTransform();
        }

        /// <summary>
        /// 마우스가 카드 위에 있는지 확인하고 Hover 상태 갱신
        /// </summary>
        private void HandleMouseHover()
        {
            Vector2 inputPos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(inputPos);

            bool isHit = Physics.Raycast(ray, out RaycastHit hit) &&
                         hit.collider != null &&
                         hit.collider.gameObject == gameObject;

            if (isHit && !isHovered)
            {
                isHovered = true;
                OnHoverEnter();
            }
            else if (!isHit && isHovered && !objectMouseEvent.IsDragging)
            {
                isHovered = false;
                OnHoverExit();
                isReturning = true;
            }
        }

        /// <summary>
        /// 카드의 위치, 회전, 스케일을 상태에 따라 부드럽게 보간합니다.
        /// </summary>
        private void UpdateTransform()
        {
            Vector3 targetScale = isHovered ? originalLocalScale * hoverScale : originalLocalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);

            if (objectMouseEvent.IsDragging)
                return;

            if (!objectMouseEvent.IsDragging && objectMouseEvent.DragEndedOnce && !objectMouseEvent.WasClickRelease)
                isReturning = true;

            // 복귀 처리
            if (isReturning)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
                    rootTransform.localRotation = Quaternion.Lerp(rootTransform.localRotation, originalRootRotation, Time.deltaTime * returnSpeed);
                }

                if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.001f &&
                    Vector3.Distance(rootTransform.localPosition, originalRootPosition) < 0.001f)
                {
                    isReturning = false;
                    objectMouseEvent.ResetDragEndFlag();
                }

                return;
            }

            // 클릭 후 회전 복귀 처리
            if (isRotatingToZero)
            {
                Quaternion current = transform.localRotation;
                Quaternion target = Quaternion.Euler(current.eulerAngles.x, 0f, current.eulerAngles.z);
                float rotateSpeed = 180f;

                transform.localRotation = Quaternion.RotateTowards(current, target, rotateSpeed * Time.deltaTime);

                if (rootTransform != null)
                {
                    Quaternion rootCurrent = rootTransform.localRotation;
                    Quaternion rootTarget = Quaternion.Euler(rootCurrent.eulerAngles.x, 0f, rootCurrent.eulerAngles.z);
                    rootTransform.localRotation = Quaternion.RotateTowards(rootCurrent, rootTarget, rotateSpeed * Time.deltaTime);
                }

                float angle = Quaternion.Angle(transform.localRotation, target);
                if (angle < 0.5f)
                    isRotatingToZero = false;

                return;
            }

            // Hover 상태일 때 위치만 위로 보간
            if (isHovered)
            {
                Vector3 targetPos = originalLocalPosition;
                targetPos.y = originalY + hoverYOffset;

                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    Vector3 rootPos = rootTransform.localPosition;
                    rootPos.y = Mathf.Lerp(rootPos.y, originalRootY + hoverYOffset, Time.deltaTime * returnSpeed);
                    rootTransform.localPosition = rootPos;
                }

                return;
            }
        }

        /// <summary>
        /// Hover 시작 시 처리
        /// </summary>
        private void OnHoverEnter()
        {
            if (responsiveObject != null)
                responsiveObject.IsLockedByHover = true;

            transform.SetSiblingIndex(transform.parent.childCount - 1);
        }

        /// <summary>
        /// Hover 종료 시 처리
        /// </summary>
        private void OnHoverExit()
        {
            if (responsiveObject != null)
                responsiveObject.IsLockedByHover = false;
        }
    }
}

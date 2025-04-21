using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드의 Hover/Click/Drag에 따른 연출 처리를 담당
    /// 입력 감지는 ObjectMouseEvent에 위임
    /// </summary>
    public class CardMotion : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.3f;
        [SerializeField] private float hoverYOffset = 0.3f;
        [SerializeField] private float returnSpeed = 10f;
        [SerializeField] private float rotateSpeed = 180f;

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
        private bool isDragging = false;

        private Quaternion targetRotation;

        private ObjectMouseEvent objectMouseEvent;

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
            rootTransform = transform.parent;
        }

        private void OnEnable()
        {
            if (objectMouseEvent != null)
            {
                objectMouseEvent.OnHoverEnter += HandleHoverEnter;
                objectMouseEvent.OnHoverExit += HandleHoverExit;
                objectMouseEvent.OnClicked += HandleClick;
                objectMouseEvent.OnBeginDrag += HandleDragBegin;
                objectMouseEvent.OnEndDrag += HandleDragEnd;
            }
        }

        private void OnDisable()
        {
            if (objectMouseEvent != null)
            {
                objectMouseEvent.OnHoverEnter -= HandleHoverEnter;
                objectMouseEvent.OnHoverExit -= HandleHoverExit;
                objectMouseEvent.OnClicked -= HandleClick;
                objectMouseEvent.OnBeginDrag -= HandleDragBegin;
                objectMouseEvent.OnEndDrag -= HandleDragEnd;
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

        private void HandleHoverEnter()
        {
            isHovered = true;
            transform.SetSiblingIndex(transform.parent.childCount - 1);
        }

        private void HandleHoverExit()
        {
            isHovered = false;

            // 드래그 중일 때는 위치 복귀하지 않음
            if (!objectMouseEvent.IsDragging)
                isReturning = true;
        }

        private void HandleClick()
        {
            if (!isRotating)
            {
                SetTargetRotation(objectMouseEvent.IsToggleOn);
                isRotating = true;
            }
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

        private void SetTargetRotation(bool toFront)
        {
            targetRotation = toFront ? Quaternion.Euler(0f, 0f, 0f) : originalRootRotation;
        }

        private void UpdateTransform()
        {
            // 크기 보간은 Hover 여부에 따라 처리
            Vector3 targetScale = isHovered ? originalLocalScale * hoverScale : originalLocalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);

            // 1. 회전 먼저 처리
            if (isRotating && rootTransform != null)
            {
                rootTransform.localRotation = Quaternion.RotateTowards(
                    rootTransform.localRotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );

                float angle = Quaternion.Angle(rootTransform.localRotation, targetRotation);
                if (angle < 0.5f)
                {
                    rootTransform.localRotation = targetRotation;
                    isRotating = false;
                }
            }

            // 2. 복귀 우선 처리
            if (isReturning)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
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

                return; // ← 복귀 중이면 Hover 처리 안 함
            }

            // 3. Hover 위치 처리 (복귀보다 낮은 우선순위)
            if (isHovered)
            {
                Vector3 current = transform.localPosition;
                Vector3 targetPos = new Vector3(current.x, originalY + hoverYOffset, current.z);
                transform.localPosition = Vector3.Lerp(current, targetPos, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    Vector3 rootCurrent = rootTransform.localPosition;
                    Vector3 rootTarget = new Vector3(rootCurrent.x, originalRootPosition.y + hoverYOffset, rootCurrent.z);
                    rootTransform.localPosition = Vector3.Lerp(rootCurrent, rootTarget, Time.deltaTime * returnSpeed);
                }

                return;
            }
        }

    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    public class HoverCardMotion : MonoBehaviour
    {
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

        [Header("Hover Settings")]
        public float hoverScale = 1.3f;
        public float hoverYOffset = 0.3f;
        public float returnSpeed = 10f;

        private DragObject dragObject;
        private ResponsiveObject responsiveObject;

        private void Awake()
        {
            dragObject = GetComponent<DragObject>();
            responsiveObject = GetComponent<ResponsiveObject>();
            rootTransform = transform.parent;
        }

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
            if (!initialized || dragObject == null)
                return;

            Vector2 inputPos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(inputPos);

            bool isHit = Physics.Raycast(ray, out RaycastHit hit) &&
                         hit.collider != null &&
                         hit.collider.gameObject == gameObject;

            if (isHit && !isHovered)
            {
                isHovered = true;
                OnHoverEnter();
            }
            else if (!isHit && isHovered && !dragObject.IsDragging)
            {
                isHovered = false;
                OnHoverExit();
            }

            UpdateTransform();
        }

        private void UpdateTransform()
        {
            bool isDragging = dragObject.IsDragging;

            // 드래그가 끝났으면 복귀 시작
            if (!isDragging && dragObject.DragEndedOnce)
                isReturning = true;

            // 드래그 중이면 Hover 효과 포함 아무것도 안 함
            if (isDragging)
                return;

            if (isHovered && !isReturning)
            {
                Vector3 targetPos = originalLocalPosition;
                targetPos.y = originalY + hoverYOffset;

                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale * hoverScale, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    Vector3 rootPos = rootTransform.localPosition;
                    rootPos.y = Mathf.Lerp(rootPos.y, originalRootY + hoverYOffset, Time.deltaTime * returnSpeed);
                    rootTransform.localPosition = rootPos;
                }
            }
            else if (isReturning)
            {
                // Sprite 복귀
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * returnSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale, Time.deltaTime * returnSpeed);

                // 부모 복귀
                if (rootTransform != null)
                {
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
                    rootTransform.localRotation = Quaternion.Lerp(rootTransform.localRotation, originalRootRotation, Time.deltaTime * returnSpeed);
                }

                // 복귀 완료 시 플래그 리셋
                if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.001f &&
                    Vector3.Distance(rootTransform.localPosition, originalRootPosition) < 0.001f)
                {
                    isReturning = false;
                    dragObject.ResetDragEndFlag();
                }
            }
            else if (!isHovered && !isReturning)
            {
                // 호버가 끝났지만 드래그 상태도 아닌 경우 (조용히 원래 상태로 복귀)
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
                }
            }

        }

        private void OnHoverEnter()
        {
            if (responsiveObject != null)
                responsiveObject.IsLockedByHover = true;

            transform.SetSiblingIndex(transform.parent.childCount - 1);
        }

        private void OnHoverExit()
        {
            if (responsiveObject != null)
                responsiveObject.IsLockedByHover = false;
        }
    }
}

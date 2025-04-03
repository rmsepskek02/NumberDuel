using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 카드의 Hover 애니메이션과 클릭 회전, 복귀를 담당하는 스크립트
    /// </summary>
    public class CardMortion : MonoBehaviour
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
        private bool isRotatingToZero = false;

        [Header("Hover Settings")]
        public float hoverScale = 1.3f;
        public float hoverYOffset = 0.3f;
        public float returnSpeed = 10f;

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

            // 클릭 해제 시 회전 시작
            if (objectMouseEvent.WasClickRelease)
            {
                isRotatingToZero = true;
                objectMouseEvent.ResetClickFlag();
            }

            UpdateTransform();
        }

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

        private void UpdateTransform()
        {
            // 항상 스케일 업데이트: 호버 중이면 확대, 아니면 원래 스케일로 복귀
            Vector3 targetScale = isHovered ? originalLocalScale * hoverScale : originalLocalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);

            // 드래그 중일 때는 위치, 회전 업데이트만 건너뛰도록 함
            if (objectMouseEvent.IsDragging)
                return;

            // (이하 기존 로직 그대로 진행)
            if (!objectMouseEvent.IsDragging && objectMouseEvent.DragEndedOnce && !objectMouseEvent.WasClickRelease)
                isReturning = true;

            // 1. 복귀 애니메이션
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

            // 2. 클릭 후 회전 보간
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
                {
                    isRotatingToZero = false;
                }

                return;
            }

            // 3. 호버 중일 때 (위치 업데이트만 진행; 스케일은 이미 위에서 처리됨)
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

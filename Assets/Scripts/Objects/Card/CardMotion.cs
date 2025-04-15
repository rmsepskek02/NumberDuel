using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 카드 Hover 및 클릭 후 회전 복귀 애니메이션을 처리하는 컴포넌트
    /// - 드래그 중에는 Hover 및 복귀 애니메이션 비활성화
    /// - Hover 시: 카드 Sprite 확대 + Y축 상승
    /// - 드래그 종료 후: 위치 및 회전 복귀
    /// - 클릭 릴리즈 후: Y축 회전 복귀
    /// </summary>
    public class CardMotion : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.3f;       // Hover 시 확대 비율
        [SerializeField] private float hoverYOffset = 0.3f;     // Hover 시 Y축으로 떠오르는 거리
        [SerializeField] private float returnSpeed = 10f;       // 복귀 보간 속도

        private Transform rootTransform;                        // 카드 루트 (보통 카드 오브젝트)

        // Sprite Transform 기준 원래 상태 저장용
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Vector3 originalLocalScale;
        private float originalY;

        // 카드 루트 오브젝트 기준 원래 상태 저장용
        private Vector3 originalRootPosition;
        private Quaternion originalRootRotation;
        private float originalRootY;

        // 내부 상태
        private bool initialized = false;           // 초기화 여부
        private bool isHovered = false;             // Hover 중인지 여부
        private bool isReturning = false;           // 복귀 애니메이션 활성화 여부
        private bool isRotatingToZero = false;      // 클릭 후 회전 복귀 활성화 여부

        private ObjectMouseEvent objectMouseEvent;  // 입력 처리 참조
        private Camera mainCamera;

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
            rootTransform = transform.parent;
        }

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        /// <summary>
        /// 최초 Hover/복귀 기준값을 저장
        /// - CardZone에서 카드 배치 후 호출됨
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

            // 클릭 후 손 뗀 경우 → 회전 복귀 시작
            if (objectMouseEvent.WasClickRelease)
            {
                isRotatingToZero = true;
                objectMouseEvent.ResetClickFlag();
            }

            UpdateTransform();
        }

        /// <summary>
        /// 마우스 Ray를 사용해 카드 위에 있는지 체크 (Hover 감지)
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
        /// Hover, 회전 복귀, 위치 복귀 등 상태에 따른 보간 처리
        /// </summary>
        private void UpdateTransform()
        {
            // 크기 확대/축소 보간 처리
            Vector3 targetScale = isHovered ? originalLocalScale * hoverScale : originalLocalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);

            // 드래그 중이면 복귀/Hover 효과 중지
            if (objectMouseEvent.IsDragging)
                return;

            // 드래그 후 복귀 애니메이션 트리거
            if (!objectMouseEvent.IsDragging && objectMouseEvent.DragEndedOnce && !objectMouseEvent.WasClickRelease)
                isReturning = true;

            // 위치 및 회전 복귀 처리
            if (isReturning)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
                    rootTransform.localRotation = Quaternion.Lerp(rootTransform.localRotation, originalRootRotation, Time.deltaTime * returnSpeed);
                }

                // 복귀 완료 판정
                if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.001f &&
                    Vector3.Distance(rootTransform.localPosition, originalRootPosition) < 0.001f)
                {
                    isReturning = false;
                    objectMouseEvent.ResetDragEndFlag();
                }

                return;
            }

            // 클릭 후 회전 복귀 처리 (Y축만 0도로 정렬)
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

                // 회전 복귀 완료 시 종료
                float angle = Quaternion.Angle(transform.localRotation, target);
                if (angle < 0.5f)
                    isRotatingToZero = false;

                return;
            }

            // Hover 시 위치만 위로 보간 처리
            if (isHovered)
            {
                Vector3 targetPos = originalLocalPosition;
                targetPos.y = originalY + hoverYOffset;
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);

                if (rootTransform != null)
                {
                    Vector3 rootTarget = originalRootPosition;
                    rootTarget.y = originalRootY + hoverYOffset;
                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, rootTarget, Time.deltaTime * returnSpeed);
                }

                return;
            }
        }

        /// <summary>
        /// Hover 진입 시 처리 (정렬 우선순위 조정)
        /// </summary>
        private void OnHoverEnter()
        {
            transform.SetSiblingIndex(transform.parent.childCount - 1);
        }

        /// <summary>
        /// Hover 해제 시 처리
        /// </summary>
        private void OnHoverExit()
        {
            // 현재는 별도 처리 없음 (위치 복귀는 Update에서 처리됨)
        }
    }
}

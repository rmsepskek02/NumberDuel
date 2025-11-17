//using UnityEngine;
//using UnityEngine.InputSystem;

//namespace Objects
//{
//    /// <summary>
//    /// 카드의 Hover 애니메이션과 드래그 후 복귀 애니메이션을 담당하는 스크립트
//    /// - Hover 시 카드 확대 및 위치 이동
//    /// - 드래그 종료 시 원래 상태로 되돌림
//    /// - 클릭 후에는 되돌리지 않고 상태 유지
//    /// </summary>
//    public class CardMortionTest : MonoBehaviour
//    {
//        private Transform rootTransform;

//        // 카드 자체의 초기 위치/회전/스케일 정보
//        private Vector3 originalLocalPosition;
//        private Quaternion originalLocalRotation;
//        private Vector3 originalLocalScale;
//        private float originalY;

//        // 카드 부모(root)의 초기 위치/회전 정보
//        private Vector3 originalRootPosition;
//        private Quaternion originalRootRotation;
//        private float originalRootY;

//        private bool initialized = false;
//        private bool isHovered = false;
//        private bool isReturning = false;

//        [Header("Hover Settings")]
//        public float hoverScale = 1.3f;       // Hover 시 스케일 배수
//        public float hoverYOffset = 0.3f;     // Hover 시 Y축 오프셋
//        public float returnSpeed = 10f;       // 복귀 속도 (Lerp 속도)

//        private ObjectMouseEvent objectMouseEvent;
//        private ResponsiveObject responsiveObject;
//        private Camera mainCamera;

//        private void Awake()
//        {
//            objectMouseEvent = GetComponent<ObjectMouseEvent>();
//            responsiveObject = GetComponent<ResponsiveObject>();
//            rootTransform = transform.parent;
//            mainCamera = Camera.main;
//        }

//        public void SetInitialState()
//        {
//            // 카드 및 부모의 초기 상태 저장
//            originalLocalPosition = transform.localPosition;
//            originalLocalRotation = transform.localRotation;
//            originalLocalScale = transform.localScale;
//            originalY = originalLocalPosition.y;

//            if (rootTransform != null)
//            {
//                originalRootPosition = rootTransform.localPosition;
//                originalRootRotation = rootTransform.localRotation;
//                originalRootY = originalRootPosition.y;
//            }

//            initialized = true;
//        }

//        private void Update()
//        {
//            if (!initialized || objectMouseEvent == null)
//                return;

//#if UNITY_EDITOR || UNITY_STANDALONE
//            HandleMouseHover(); // PC에서 Hover 처리
//#else
//            isHovered = false; // 모바일에서는 Hover 효과 비활성화
//#endif

//            UpdateTransform();
//        }

//        private void HandleMouseHover()
//        {
//            Vector2 inputPos = Mouse.current.position.ReadValue();
//            Ray ray = mainCamera.ScreenPointToRay(inputPos);

//            bool isHit = Physics.Raycast(ray, out RaycastHit hit) &&
//                         hit.collider != null &&
//                         hit.collider.gameObject == gameObject;

//            if (isHit && !isHovered)
//            {
//                isHovered = true;
//                OnHoverEnter();
//            }
//            else if (!isHit && isHovered && !objectMouseEvent.IsDragging)
//            {
//                isHovered = false;
//                OnHoverExit();
//            }
//        }

//        private void UpdateTransform()
//        {
//            bool isDragging = objectMouseEvent.IsDragging;

//            // 드래그가 종료됐지만 클릭이 아닌 경우에만 복귀
//            if (!isDragging && objectMouseEvent.DragEndedOnce && !objectMouseEvent.WasClickRelease)
//                isReturning = true;

//            if (isDragging)
//                return;

//            // Hover 중 확대 및 이동 처리
//            if (isHovered && !isReturning)
//            {
//                Vector3 targetPos = originalLocalPosition;
//                targetPos.y = originalY + hoverYOffset;

//                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);
//                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale * hoverScale, Time.deltaTime * returnSpeed);

//                // Y축 회전만 부드럽게 0도로 보간
//                Vector3 currentEuler = transform.localRotation.eulerAngles;
//                currentEuler.y = Mathf.LerpAngle(currentEuler.y, 0f, Time.deltaTime * returnSpeed);
//                transform.localRotation = Quaternion.Euler(currentEuler);

//                if (rootTransform != null)
//                {
//                    // 부모도 Y축 회전만 보간 처리
//                    Vector3 rootEuler = rootTransform.localRotation.eulerAngles;
//                    rootEuler.y = Mathf.LerpAngle(rootEuler.y, 0f, Time.deltaTime * returnSpeed);
//                    rootTransform.localRotation = Quaternion.Euler(rootEuler);
//                }
//            }
//            // 드래그 종료 후 원래 상태로 복귀
//            else if (isReturning)
//            {
//                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
//                transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * returnSpeed);
//                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale, Time.deltaTime * returnSpeed);

//                if (rootTransform != null)
//                {
//                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
//                    rootTransform.localRotation = Quaternion.Lerp(rootTransform.localRotation, originalRootRotation, Time.deltaTime * returnSpeed);
//                }

//                // 복귀 완료 시 플래그 리셋
//                if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.001f &&
//                    Vector3.Distance(rootTransform.localPosition, originalRootPosition) < 0.001f)
//                {
//                    isReturning = false;
//                    objectMouseEvent.ResetDragEndFlag();
//                }
//            }
//            // Hover 상태 종료 시 조용히 복귀 (드래그/클릭과 무관)
//            else if (!isHovered && !isReturning)
//            {
//                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
//                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale, Time.deltaTime * returnSpeed);
//                transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * returnSpeed);

//                if (rootTransform != null)
//                {
//                    rootTransform.localPosition = Vector3.Lerp(rootTransform.localPosition, originalRootPosition, Time.deltaTime * returnSpeed);
//                    rootTransform.localRotation = Quaternion.Lerp(rootTransform.localRotation, originalRootRotation, Time.deltaTime * returnSpeed);
//                }
//            }
//        }

//        private void OnHoverEnter()
//        {
//            if (responsiveObject != null)
//                responsiveObject.IsLockedByHover = true;

//            transform.SetSiblingIndex(transform.parent.childCount - 1);
//        }

//        private void OnHoverExit()
//        {
//            if (responsiveObject != null)
//                responsiveObject.IsLockedByHover = false;
//        }
//    }
//}

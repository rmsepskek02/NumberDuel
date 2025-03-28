using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 카드에 마우스 오버 시 시각적 효과를 주는 컴포넌트
    /// </summary>
    public class HoverCardMotion : MonoBehaviour
    {
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Vector3 originalLocalScale;
        private float originalY;

        private bool initialized = false;
        private bool isHovered = false;
        private bool wasHovered = false;

        [Header("Hover Settings")]
        public float hoverScale = 1.3f;          // 확대 배율
        public float hoverYOffset = 0.3f;        // 위로 띄우는 높이
        public float returnSpeed = 10f;          // 원래 상태로 돌아가는 속도

        private DragObject dragObject;
        private ResponsiveObject responsiveObject;

        private void Awake()
        {
            dragObject = GetComponent<DragObject>();
            responsiveObject = GetComponent<ResponsiveObject>();
        }

        // 초기 상태 저장 (레이아웃 완료 후 호출됨)
        public void SetInitialState()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            originalLocalScale = transform.localScale;
            originalY = originalLocalPosition.y;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized || dragObject == null)
                return;

            // 현재 마우스가 이 카드 위에 있는지 Ray로 확인
            Vector2 inputPos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(inputPos);

            bool isHit = Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null && hit.collider.gameObject == gameObject;

            // hover 시작
            if (isHit && !wasHovered)
            {
                wasHovered = true;
                OnHoverEnter();
            }
            // hover 종료
            else if (!isHit && wasHovered && !dragObject.IsDragging)
            {
                wasHovered = false;
                OnHoverExit();
            }

            UpdateTransform();
        }

        // 시각적 위치/크기/회전을 갱신
        private void UpdateTransform()
        {
            // 1. 드래그 중인 경우: 위치 고정, 복귀 X
            if (dragObject.IsDragging)
                return;

            // 2. Hover 중인 경우: 확대된 상태 유지
            if (isHovered)
            {
                Vector3 targetPos = originalLocalPosition;
                targetPos.y = originalY + hoverYOffset;

                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale * hoverScale, Time.deltaTime * returnSpeed);
                return;
            }

            // 3. 드래그가 끝났거나 Hover 종료 시: 원래 상태로 복귀
            {
                // Y좌표 먼저 복귀
                Vector3 currentPos = transform.localPosition;
                Vector3 targetYPos = new Vector3(currentPos.x, originalY, currentPos.z);
                transform.localPosition = Vector3.Lerp(currentPos, targetYPos, Time.deltaTime * returnSpeed * 1.5f);

                // 이후 전체 위치 및 회전, 스케일 복귀
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * returnSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, originalLocalScale, Time.deltaTime * returnSpeed);
            }
        }

        private void OnHoverEnter()
        {
            isHovered = true;
            if (responsiveObject != null)
                responsiveObject.IsLockedByHover = true;

            // 자식오브젝트 중 가장 앞으로 보내기
            transform.SetSiblingIndex(transform.parent.childCount - 1);

            Debug.Log($"[Hover ON] {gameObject.name}");
        }

        private void OnHoverExit()
        {
            isHovered = false;
            if (responsiveObject != null)
                responsiveObject.IsLockedByHover = false;

            Debug.Log($"[Hover OFF] {gameObject.name}");
        }
    }
}

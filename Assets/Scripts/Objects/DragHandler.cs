using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 드래그 중 카드의 실제 위치 이동을 담당하는 컴포넌트
    /// - Drag 시작/종료는 외부에서 입력됨
    /// - 위치 갱신은 Update에서 매 프레임 수행됨
    /// </summary>
    public class DragHandler : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera; // 월드 좌표 계산용 카메라

        private bool isDragging = false;    // 드래그 활성화 여부
        private Vector3 offset;             // 마우스 위치와 오브젝트 간 거리
        private float zDistance;            // 카메라와 오브젝트 간 Z 거리

        /// <summary>
        /// 드래그 시작 시 기준 위치를 기록하고 드래그 상태 활성화
        /// </summary>
        public void StartDrag(Vector2 screenPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            zDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
            offset = transform.position - worldPos;

            isDragging = true;
        }

        /// <summary>
        /// 드래그 종료 시 호출되어 드래그 상태를 비활성화
        /// </summary>
        public void EndDrag()
        {
            isDragging = false;
        }

        private void Update()
        {
            if (!isDragging) return;

            // 현재 마우스 위치 → 월드 위치로 변환 후 offset만큼 보정하여 이동
            Vector2 screenPosition = GetInputPosition();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
            transform.position = worldPos + offset;
        }

        /// <summary>
        /// 현재 입력 위치를 반환 (에디터/모바일 대응)
        /// </summary>
        private Vector2 GetInputPosition()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
            return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
#endif
        }
    }
}

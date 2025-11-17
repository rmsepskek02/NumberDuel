using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 드래그 시 카드의 실제 위치 이동을 담당하는 컴포넌트
    /// - Drag 시작/종료는 외부에서 입력됨
    /// - 위치 업데이트 Update에서 매 프레임 자동 수행
    /// </summary>
    public class DragHandler : MonoBehaviour
    {
        #region Fields and Properties
        [SerializeField] private Camera mainCamera; // 화면 좌표 변환 카메라

        private bool isDragging = false;    // 드래그 활성화 상태
        private Vector3 offset;             // 마우스 위치와 오브젝트 간 거리
        private float zDistance;            // 카메라와 오브젝트 간 Z 거리
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!isDragging) return;

            // 현재 마우스 위치 및 월드 위치로 변환 후 offset만큼 보정하여 이동
            Vector2 screenPosition = GetInputPosition();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
            transform.position = worldPos + offset;
        }
        #endregion

        #region Drag Control
        /// <summary>
        /// 드래그 시작 시 초기 위치를 저장하고 드래그 모드 활성화
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
        #endregion

        #region Input Handling
        /// <summary>
        /// 현재 입력 위치를 반환 (데스크톱/모바일 대응)
        /// </summary>
        private Vector2 GetInputPosition()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Mouse.current.position.ReadValue();
#else
            return Touchscreen.current.primaryTouch.position.ReadValue();
#endif
        }
        #endregion
    }
}

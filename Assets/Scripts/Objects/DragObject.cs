using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 오브젝트를 마우스로 드래그할 수 있도록 하는 컴포넌트
    /// Unity의 New Input System 기반으로 동작
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DragObject : MonoBehaviour
    {
        private Camera mainCamera;             // 드래그 시 사용할 카메라
        private bool isDragging = false;       // 현재 드래그 중인지 여부
        private Vector3 offset;                // 클릭 위치와 오브젝트 중심 간 거리
        private float zDistance;               // 카메라와의 거리 (Z축)
        private Vector2 startMousePos;         // 마우스를 누른 초기 위치
        private float dragThreshold = 1f;      // 드래그로 인식할 최소 거리 (픽셀)

        /// <summary>
        /// 드래그가 일어난 프레임에서 true가 됨 (클릭과 구분용)
        /// </summary>
        public bool WasDragged { get; private set; }

        private void Awake()
        {
            // 메인 카메라 참조
            mainCamera = Camera.main;
        }

        private void Update()
        {
            // 마우스 누름 시작
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryStartDrag();
            }

            // 마우스 놓을 때
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                TryEndDrag();
            }

            // 드래그 중이면 마우스 위치로 이동
            if (isDragging)
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                transform.position = mouseWorldPos + offset;
            }
        }

        /// <summary>
        /// 드래그 시작 조건 검사 및 초기값 설정
        /// </summary>
        private void TryStartDrag()
        {
            startMousePos = Mouse.current.position.ReadValue();

            // 마우스 아래에 있는 오브젝트인지 확인
            Ray ray = mainCamera.ScreenPointToRay(startMousePos);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                zDistance = Vector3.Distance(transform.position, mainCamera.transform.position);
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                offset = transform.position - mouseWorldPos;

                isDragging = true;
                WasDragged = false;
            }
        }

        /// <summary>
        /// 드래그 종료 처리 및 드래그 여부 판정
        /// </summary>
        private void TryEndDrag()
        {
            if (isDragging)
            {
                float moved = Vector2.Distance(Mouse.current.position.ReadValue(), startMousePos);
                WasDragged = moved >= dragThreshold; // 일정 거리 이상 움직이면 드래그로 간주
            }

            isDragging = false;
        }

        /// <summary>
        /// 마우스 위치를 월드 좌표로 변환
        /// </summary>
        /// <returns>월드 공간상의 마우스 위치</returns>
        private Vector3 GetMouseWorldPosition()
        {
            Vector3 screenMousePos = Mouse.current.position.ReadValue();
            screenMousePos.z = zDistance; // Z축 거리 보정
            return mainCamera.ScreenToWorldPoint(screenMousePos);
        }
    }
}

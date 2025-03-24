using UnityEngine;
using UnityEngine.InputSystem;
using Objects;

namespace Objects
{
    /// <summary>
    /// 카드 오브젝트의 클릭 이벤트를 처리하는 컴포넌트
    /// 드래그 기능(DragObject)과 병행되며, 드래그와 클릭을 정확히 구분하여 처리
    /// </summary>
    public class Card : MonoBehaviour
    {
        private DragObject dragObject; // 드래그 상태 확인용 컴포넌트 참조

        private void Awake()
        {
            // 동일한 오브젝트에 존재하는 DragObject 컴포넌트 참조
            dragObject = GetComponent<DragObject>();
        }

        private void LateUpdate()
        {
            // 마우스 버튼을 뗐을 때 실행 (드래그가 끝난 뒤 처리되도록 LateUpdate 사용)
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                // 드래그가 아니고, 마우스가 이 오브젝트 위에 있다면 클릭으로 판단
                if (!dragObject.WasDragged && IsMouseOver())
                {
                    OnClick();
                }
            }
        }

        /// <summary>
        /// 현재 마우스 포인터가 이 오브젝트 위에 있는지 확인
        /// </summary>
        /// <returns>포인터가 이 오브젝트 위에 있으면 true</returns>
        private bool IsMouseOver()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            return Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject;
        }

        /// <summary>
        /// 카드가 클릭되었을 때 호출되는 로직
        /// </summary>
        private void OnClick()
        {
            Debug.Log($"Card '{gameObject.name}' was clicked!");
            // 여기에 클릭 시 수행할 동작 추가 가능
        }
    }
}

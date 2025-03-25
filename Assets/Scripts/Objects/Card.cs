using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 카드 오브젝트의 클릭 이벤트를 처리하는 컴포넌트.
    /// DragObject와 함께 사용되어, 드래그와 클릭을 정확히 구분하여 처리함.
    /// </summary>
    public class Card : MonoBehaviour
    {
        private DragObject dragObject; // 드래그 여부를 판단하기 위한 참조
        public TextMeshPro testText;   // 클릭 시 이름을 표시할 텍스트

        void Awake()
        {
            dragObject = GetComponent<DragObject>();
        }

        void LateUpdate()
        {
            Vector2 inputPos;
            bool released = false;

            // 입력 구분 (에디터/PC vs 모바일)
#if UNITY_EDITOR || UNITY_STANDALONE
            released = Mouse.current.leftButton.wasReleasedThisFrame;
            inputPos = Mouse.current.position.ReadValue();
#else
            released = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
#endif

            // 드래그가 아니고, 포인터가 오브젝트 위에 있을 경우 클릭으로 처리
            if (released && !dragObject.WasDragged && IsPointerOver(inputPos))
            {
                OnClick();
            }
        }

        /// <summary>
        /// 주어진 입력 위치가 이 오브젝트 위에 있는지 Raycast로 확인
        /// </summary>
        private bool IsPointerOver(Vector2 inputPos)
        {
            Ray ray = Camera.main.ScreenPointToRay(inputPos);
            return Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject;
        }

        /// <summary>
        /// 클릭 시 호출되는 로직
        /// </summary>
        private void OnClick()
        {
            Debug.Log($"Card '{gameObject.name}' was clicked!");
            testText.text = gameObject.name; // UI 텍스트에 카드 이름 표시
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Objects
{
    /// <summary>
    /// 클릭 또는 터치 입력을 감지하여 처리할 수 있는 베이스 클래스
    /// - Deck, Card와 같은 오브젝트에서 상속받아 사용
    /// - PC 및 모바일 입력을 모두 지원함
    /// </summary>
    public abstract class ClickableObjectBase : MonoBehaviour
    {
        protected virtual void LateUpdate()
        {
            if (TryGetInputReleased(out Vector2 inputPos) && IsPointerOver(inputPos) && CanTriggerClick())
            {
                OnClick();
            }
        }

        protected abstract bool CanTriggerClick();
        protected abstract void OnClick();

        /// <summary>
        /// 입력 해제 시 위치를 반환
        /// </summary>
        protected bool TryGetInputReleased(out Vector2 inputPos)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            inputPos = Mouse.current.position.ReadValue();
            return Mouse.current.leftButton.wasReleasedThisFrame;
#else
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
#endif
        }

        /// <summary>
        /// 입력 위치가 이 오브젝트 또는 자식에 닿았는지 확인
        /// </summary>
        protected virtual bool IsPointerOver(Vector2 inputPos)
        {
            Ray ray = Camera.main.ScreenPointToRay(inputPos);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                    return true;
            }
            return false;
        }
    }
}

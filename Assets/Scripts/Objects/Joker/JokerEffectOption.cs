using UnityEngine;
using Objects;

namespace Objects
{
    /// <summary>
    /// 조커 효과 선택 옵션 하나를 나타내는 컴포넌트
    /// CardModeOption과 유사한 구조
    /// </summary>
    [RequireComponent(typeof(ObjectMouseEvent))]
    public class JokerEffectOption : MonoBehaviour
    {
        [Tooltip("이 항목이 나타내는 조커 효과 (Draw, Delete, Swap)")]
        [SerializeField] public JokerEffectType effectType;

        private ObjectMouseEvent mouseEvent;
        private JokerModeSelector selector;

        private void Awake()
        {
            mouseEvent = GetComponent<ObjectMouseEvent>();
        }

        private void OnEnable()
        {
            if (mouseEvent != null)
                mouseEvent.OnClickReleased += HandleClick;
        }

        private void OnDisable()
        {
            if (mouseEvent != null)
                mouseEvent.OnClickReleased -= HandleClick;
        }

        /// <summary>
        /// 외부에서 상위 셀렉터를 연결한다
        /// </summary>
        public void SetSelector(JokerModeSelector selector)
        {
            this.selector = selector;
        }

        /// <summary>
        /// 이 항목이 클릭되었을 때 호출됨
        /// 선택된 효과를 상위 셀렉터에 전달한다
        /// </summary>
        private void HandleClick()
        {
            if (selector != null)
            {
                Debug.Log($"[JokerEffectOption] {effectType} 효과 선택됨");
                selector.OnJokerEffectSelected(effectType);
            }
        }

        /// <summary>
        /// 이 옵션의 효과 타입을 가져온다
        /// </summary>
        public JokerEffectType GetEffectType()
        {
            return effectType;
        }
    }
}
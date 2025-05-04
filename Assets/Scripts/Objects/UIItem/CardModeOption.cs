using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드 제출 방식(Open 또는 Secret)을 선택하는 하나의 옵션 항목.
    /// 클릭 시 상위 CardModeSelector에 선택 결과를 전달한다.
    /// </summary>
    [RequireComponent(typeof(ObjectMouseEvent))]
    public class CardModeOption : MonoBehaviour
    {
        [Tooltip("이 항목이 나타내는 카드 모드 (Open 또는 Secret)")]
        [SerializeField] private CardModeType modeType;

        private ObjectMouseEvent mouseEvent;
        private CardModeSelector selector;

        private void Awake()
        {
            mouseEvent = GetComponent<ObjectMouseEvent>();
        }

        private void OnEnable()
        {
            mouseEvent.OnClickReleased += HandleClick;
        }

        private void OnDisable()
        {
            mouseEvent.OnClickReleased -= HandleClick;
        }

        /// <summary>
        /// 외부에서 상위 셀렉터를 연결한다.
        /// </summary>
        public void SetSelector(CardModeSelector selector)
        {
            this.selector = selector;
        }

        /// <summary>
        /// 이 항목이 클릭되었을 때 호출됨.
        /// 선택된 모드를 상위 셀렉터에 전달한다.
        /// </summary>
        private void HandleClick()
        {
            selector?.OnCardModeSelected(modeType);
        }
    }

    /// <summary>
    /// 카드 제출 방식: 앞면(Open) 또는 뒷면(Secret)
    /// </summary>
    public enum CardModeType
    {
        Open,
        Secret
    }
}

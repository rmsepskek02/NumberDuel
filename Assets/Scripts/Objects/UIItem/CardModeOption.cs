using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드 배치 모드(Open 또는 Secret)를 선택하는 하나의 옵션 그림.
    /// 클릭 시 상위 CardModeSelector에 선택 결과를 전달한다.
    /// </summary>
    [RequireComponent(typeof(ObjectMouseEvent))]
    public class CardModeOption : MonoBehaviour
    {
        #region Fields and Properties
        [Tooltip("이 그림이 나타내는 카드 모드 (Open 또는 Secret)")]
        [SerializeField] private CardModeType modeType;

        private ObjectMouseEvent mouseEvent;
        private CardModeSelector selector;
        #endregion

        #region Unity Lifecycle
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
        #endregion

        #region Public Methods
        /// <summary>
        /// 외부에서 상위 셀렉터를 설정한다.
        /// </summary>
        public void SetSelector(CardModeSelector selector)
        {
            this.selector = selector;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 이 그림이 클릭되었을 때 호출됨.
        /// 선택된 모드를 상위 셀렉터에 전달한다.
        /// </summary>
        private void HandleClick()
        {
            selector?.OnCardModeSelected(modeType);
        }
        #endregion
    }
}

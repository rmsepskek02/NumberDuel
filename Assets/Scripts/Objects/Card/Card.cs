using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Objects
{
    /// <summary>
    /// 개별 카드 오브젝트의 상태 및 클릭 반응을 관리하는 컴포넌트
    /// - ICard 구현을 통해 Zone에서 인터랙션 설정을 받을 수 있음
    /// - ObjectMouseEvent로부터 클릭 이벤트를 수신함
    /// </summary>
    public class Card : MonoBehaviour, ICard
    {
        [Header("Display")]
        [SerializeField] private TextMeshPro testText; // 클릭 시 이름 출력용 텍스트

        [Header("Events")]
        public UnityEvent<Card> onClicked; // 외부에서 구독 가능한 카드 클릭 이벤트

        private ObjectMouseEvent mouseEvent;

        private void Awake()
        {
            mouseEvent = GetComponentInChildren<ObjectMouseEvent>();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void RegisterEvents()
        {
            if (mouseEvent == null)
                return;

            mouseEvent.OnClickReleased += HandleClick;
        }

        private void UnregisterEvents()
        {
            if (mouseEvent == null)
                return;

            mouseEvent.OnClickReleased -= HandleClick;
        }

        /// <summary>
        /// 클릭 시 실행되는 내부 로직
        /// </summary>
        private void HandleClick()
        {
            if (testText != null)
                testText.text = gameObject.name;

            Debug.Log($"[Card] Clicked: {gameObject.name}");
            onClicked?.Invoke(this);
        }

        /// <summary>
        /// Zone 정보에 따라 카드 상호작용 권한 설정
        /// </summary>
        public void SetInteraction(CardZone.ZoneType zoneType, CardZone.OwnerType ownerType)
        {
            if (zoneType == CardZone.ZoneType.Hand && ownerType == CardZone.OwnerType.Player)
                ApplyInteraction(CardInteractionType.DragAndClick);
            else if (zoneType == CardZone.ZoneType.Field)
                ApplyInteraction(CardInteractionType.ClickOnly);
            else
                ApplyInteraction(CardInteractionType.None);
        }

        /// <summary>
        /// Interaction 유형에 따라 드래그/클릭 허용 여부 설정
        /// </summary>
        private void ApplyInteraction(CardInteractionType type)
        {
            if (mouseEvent == null)
                mouseEvent = GetComponentInChildren<ObjectMouseEvent>();

            mouseEvent.isClickable = (type == CardInteractionType.ClickOnly || type == CardInteractionType.DragAndClick);
            mouseEvent.isDraggable = (type == CardInteractionType.DragAndClick);
        }

        /// <summary>
        /// 카드 상호작용 종류를 정의하는 내부 열거형
        /// </summary>
        private enum CardInteractionType
        {
            None,
            ClickOnly,
            DragAndClick
        }
    }
}

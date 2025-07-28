using Manager;
using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드가 배치되는 Zone (손패, 필드 등)을 관리하는 컴포넌트
    /// - 카드 리스트 관리
    /// - 배치는 CardLayoutHelper에 위임
    /// - 카드의 Sprite 및 Material 설정 포함
    /// </summary>
    public class CardZone : MonoBehaviour
    {
        public enum ZoneType { Hand, Field }
        public enum OwnerType { Player, Opponent }

        [Header("Zone Settings")]
        public ZoneType Zone => zoneType;
        [SerializeField] private ZoneType zoneType;
        public OwnerType Owner => ownerType;
        [SerializeField] private OwnerType ownerType;

        [Header("Layout Helper")]
        [SerializeField] private CardLayoutHelper layoutHelper;

        private readonly List<Transform> cards = new List<Transform>();

        public static Transform AllZonesRoot { get; private set; }

        private void Awake()
        {
            if (AllZonesRoot == null)
            {
                AllZonesRoot = transform.parent;
            }
        }

        /// <summary>
        /// 현재 Zone에 있는 카드 개수 반환
        /// </summary>
        public int GetCardCount()
        {
            return cards.Count;
        }

        /// <summary>
        /// Zone이 가득 찬 상태인지 확인 (손패용)
        /// </summary>
        public bool IsHandFull()
        {
            return zoneType == ZoneType.Hand && cards.Count >= 10;
        }

        /// <summary>
        /// 카드 추가 가능 여부 확인
        /// </summary>
        public bool CanAddCard()
        {
            if (zoneType == ZoneType.Hand)
                return cards.Count < 10;
            else if (zoneType == ZoneType.Field)
                return cards.Count < 5;  // 필드 5장 제한 추가

            return true;
        }

        /// <summary>
        /// 필드가 가득 찬 상태인지 확인
        /// </summary>
        public bool IsFieldFull()
        {
            return zoneType == ZoneType.Field && cards.Count >= 5;
        }

        /// <summary>
        /// 카드를 Zone에 추가
        /// NetworkCard 기반의 네트워크 동기화 및 위치 추적 포함
        /// 자동 네트워크 동기화는 특정 조건에서만 수행
        /// </summary>
        /// <param name="card">추가할 카드의 Transform</param>
        public void AddCard(Transform card)
        {
            // 기본 제한 체크들
            if (zoneType == ZoneType.Hand && cards.Count >= 10)
            {
                Debug.LogWarning($"[CardZone] 손패가 가득참 (10장): {ownerType}");
                return;
            }

            if (zoneType == ZoneType.Field && cards.Count >= 5)
            {
                Debug.LogWarning($"[CardZone] 필드가 가득함 (5장): {ownerType}");
                return;
            }

            if (cards.Contains(card))
            {
                Debug.LogWarning($"[CardZone] 카드가 이미 Zone에 존재합니다: {card.name}");
                return;
            }

            // 카드 컴포넌트 검증
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent == null)
            {
                Debug.LogError($"[CardZone] Card 컴포넌트를 찾을 수 없습니다: {card.name}");
                return;
            }

            // 연산자 카드는 필드에 배치 불가
            if (zoneType == ZoneType.Field && cardComponent.CardType == CardType.Operator)
            {
                Debug.LogWarning($"[CardZone] 연산자 카드는 필드에 배치할 수 없습니다: {card.name}");
                return;
            }

            // NetworkCard 컴포넌트 확인 및 추가
            NetworkCard networkCard = card.GetComponent<NetworkCard>();
            if (networkCard == null)
            {
                Debug.Log($"[CardZone] NetworkCard 컴포넌트가 없어 자동 추가합니다: {card.name}");
                networkCard = card.gameObject.AddComponent<NetworkCard>();
            }

            // 네트워크 동기화 필요성 판단
            // 조건: 필드 배치 + 플레이어 소유 + 로컬 플레이어 턴 + 네트워크 매니저 존재
            bool shouldSyncToNetwork = (zoneType == ZoneType.Field &&
                                       ownerType == OwnerType.Player &&
                                       NetworkGameManager.Instance != null &&
                                       TurnManager.Instance != null &&
                                       TurnManager.Instance.IsLocalPlayerTurn &&
                                       networkCard.CanPerformAction(NetworkActionType.PlaceToField));

            // 네트워크 동기화 전송 (카드 실제 추가 전에 실행)
            if (shouldSyncToNetwork)
            {
                Manager.CardData cardData = ExtractCardData(cardComponent);
                bool isSecret = cardComponent.IsSecret;

                NetworkGameManager.Instance.SyncCardPlacement(cardData, ownerType, zoneType, isSecret);
                Debug.Log($"[CardZone] 네트워크 동기화 전송: {cardComponent.CardType} to {ownerType} {zoneType} (ID: {networkCard.UniqueId})");
            }

            // 실제 카드 추가 로직 시작
            cards.Add(card);
            card.SetParent(transform);

            // Opponent 손패 텍스트 비활성화 (보안)
            if (ownerType == OwnerType.Opponent && zoneType == ZoneType.Hand)
            {
                var tmp = card.GetComponentInChildren<TMPro.TextMeshPro>();
                if (tmp != null)
                {
                    tmp.gameObject.SetActive(false);
                }
            }

            // 손패에서는 GLOW 효과 비활성화
            if (zoneType == ZoneType.Hand)
            {
                var glow = card.GetComponentInChildren<CardEffect>();
                if (glow != null)
                {
                    glow.enabled = false;
                }
            }

            // 필드 카드 등록 및 상태 설정
            if (zoneType == ZoneType.Field && cardComponent != null)
            {
                // InGameManager에 필드 카드로 등록
                if (InGameManager.Instance != null)
                {
                    InGameManager.Instance.RegisterFieldCard(cardComponent);
                }

                // 플레이어 카드인 경우 이번 턴에 배치됨 표시 (공격 제한용)
                if (ownerType == OwnerType.Player)
                {
                    cardComponent.SetWasPlayedThisTurn(true);
                    Debug.Log($"[CardZone] 플레이어 카드 이번 턴 배치 표시: {card.name}");
                }
            }

            // NetworkCard 위치 정보 업데이트
            networkCard.UpdateLocationInfo();

            // 카드 인터랙션 권한 설정
            ICard cardInterface = card.GetComponentInChildren<ICard>();
            if (cardInterface != null)
            {
                cardInterface.SetInteraction(zoneType, ownerType);
            }
            else
            {
                Debug.LogWarning($"[CardZone] ICard 인터페이스를 찾을 수 없습니다: {card.name}");
            }

            // 레이아웃 업데이트
            UpdateLayout();

            // 손패에만 적용되는 추가 설정들
            if (zoneType == ZoneType.Hand)
            {
                AddDragHandler(card);

                var spriteTransform = card.GetComponentInChildren<SpriteRenderer>()?.transform;
                if (spriteTransform != null)
                {
                    AddHover(spriteTransform);
                }
            }

            Debug.Log($"[CardZone] 카드 추가 완료: {card.name} to {ownerType} {zoneType} (총 {cards.Count}장)");
        }

        /// <summary>
        /// Card 컴포넌트에서 Manager.CardData 추출
        /// CardPlayDetector의 동일한 메서드와 일치하는 로직
        /// </summary>
        /// <param name="card">데이터를 추출할 카드</param>
        /// <returns>추출된 CardData</returns>
        private Manager.CardData ExtractCardData(Card card)
        {
            switch (card.CardType)
            {
                case CardType.Number:
                    // 숫자 카드: CardText에서 현재 값 추출
                    var cardText = card.GetComponentInChildren<CardText>();
                    long value = cardText != null ? (long)cardText.RawValue : 1;
                    return new Manager.CardData(value);

                case CardType.Operator:
                    // 연산자 카드: OperatorType 사용
                    return new Manager.CardData(card.OperatorType);

                case CardType.Joker:
                    // 조커 카드: 정적 생성 메서드 사용
                    return Manager.CardData.CreateJoker();

                default:
                    // 알 수 없는 타입: 기본값 1로 설정
                    Debug.LogWarning($"[CardZone] 알 수 없는 카드 타입: {card.CardType}");
                    return new Manager.CardData(1);
            }
        }

        /// <summary>
        /// 카드 포함 여부 확인 (외부에서 조회용)
        /// </summary>
        public bool Contains(Transform card) => cards.Contains(card);

        /// <summary>
        /// 카드 제거 후 배치 갱신
        /// </summary>
        public void RemoveCard(Transform card)
        {
            if (!cards.Contains(card)) return;

            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
                InGameManager.Instance.UnregisterFieldCard(cardComponent);
            cards.Remove(card);
            UpdateLayout();
        }

        /// <summary>
        /// 현재 카드 상태에 맞춰 레이아웃 재배치
        /// </summary>
        public void UpdateLayout()
        {
            if (layoutHelper == null)
            {
                Debug.LogWarning("[CardZone] LayoutHelper is not assigned.");
                return;
            }

            if (zoneType == ZoneType.Hand)
                layoutHelper.ArrangeFanLayout(cards);
            else
                layoutHelper.ArrangeFieldLayout(cards);
        }

        /// <summary>
        /// 카드에 Hover 애니메이션 추가
        /// </summary>
        private void AddHover(Transform card)
        {
            if (!card.TryGetComponent<CardMotion>(out _))
                card.gameObject.AddComponent<CardMotion>();
        }

        /// <summary>
        /// 카드에 DragHandler 추가
        /// </summary>
        private void AddDragHandler(Transform card)
        {
            if (!card.TryGetComponent<DragHandler>(out _))
                card.gameObject.AddComponent<DragHandler>();
        }
    }
}

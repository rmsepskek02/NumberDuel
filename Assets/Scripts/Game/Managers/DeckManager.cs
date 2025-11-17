using System;
using System.Collections.Generic;
using UnityEngine;
using Objects;
using Utills;

namespace Manager
{
    /// <summary>
    /// 카드 데이터를 저장하는 데이터 구조체
    /// </summary>
    [Serializable]
    public struct CardData
    {
        public CardType cardType;
        public long numberValue;      // 숫자 카드일 때 사용
        public OperatorType operatorType; // 연산자 카드일 때 사용

        // 숫자 카드 생성자
        public CardData(long value)
        {
            cardType = CardType.Number;
            numberValue = value;
            operatorType = OperatorType.Plus; // 기본값
        }

        // 연산자 카드 생성자
        public CardData(OperatorType op)
        {
            cardType = CardType.Operator;
            numberValue = 0;
            operatorType = op;
        }

        // 조커 카드 생성자
        public static CardData CreateJoker()
        {
            return new CardData
            {
                cardType = CardType.Joker,
                numberValue = 0,
                operatorType = OperatorType.Plus // 기본값
            };
        }
    }

    /// <summary>
    /// 덱 관리를 담당 매니저
    /// DeckStacker와 연동하여 실제 카드 데이터를 관리하고 드로우 로직 처리
    /// </summary>
    public class DeckManager : Singleton<DeckManager>
    {
        #region Fields and Properties
        // 플레이어 덱 데이터
        private List<CardData> playerDeck = new List<CardData>();
        private List<CardData> opponentDeck = new List<CardData>();

        // DeckStacker 참조 (시각적 덱 표시)
        private DeckStacker playerDeckStacker;
        private DeckStacker opponentDeckStacker;

        // 덱 구성 상수
        private const int DECK_SIZE = 30;
        private const int NUMBER_CARDS_PER_VALUE = 4; // 각 숫자당 4장
        private const int OPERATOR_CARDS_PER_TYPE = 2; // 각 연산자당 2장
        private const int JOKER_CARDS = 2; // 조커 2장

        /// <summary>
        /// 플레이어 덱에 남은 카드 수
        /// </summary>
        public int PlayerDeckCount => playerDeck.Count;

        /// <summary>
        /// 상대 덱에 남은 카드 수
        /// </summary>
        public int OpponentDeckCount => opponentDeck.Count;

        /// <summary>
        /// 플레이어 덱이 비었는지 확인
        /// </summary>
        public bool IsPlayerDeckEmpty => playerDeck.Count == 0;

        /// <summary>
        /// 상대 덱이 비었는지 확인
        /// </summary>
        public bool IsOpponentDeckEmpty => opponentDeck.Count == 0;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // DeckStacker 찾기 (씬에 배치된 DeckStacker들을 자동 연결)
            FindDeckStackers();
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 게임 시작 시 양쪽 플레이어의 덱을 생성하고 섞음
        /// </summary>
        public void InitializeDecks()
        {
            CreatePlayerDeck();
            CreateOpponentDeck();

            ShuffleDeck(playerDeck);
            ShuffleDeck(opponentDeck);

            Debug.Log($"[DeckManager] 덱 초기화 완료 - 플레이어: {playerDeck.Count}장, 상대: {opponentDeck.Count}장");
        }

        /// <summary>
        /// 플레이어 덱에서 카드 1장 드로우 (시각적 덱도 갱신)
        /// </summary>
        public CardData? DrawPlayerCard()
        {
            CardData? cardData = DrawCard(playerDeck, "플레이어");

            // 시각적 덱에서 카드 제거
            if (cardData.HasValue && playerDeckStacker != null)
            {
                playerDeckStacker.RemoveTopCard();
            }

            return cardData;
        }

        /// <summary>
        /// 상대 덱에서 카드 1장 드로우 (시각적 덱도 갱신)
        /// </summary>
        public CardData? DrawOpponentCard()
        {
            CardData? cardData = DrawCard(opponentDeck, "상대");

            // 시각적 덱에서 카드 제거
            if (cardData.HasValue && opponentDeckStacker != null)
            {
                opponentDeckStacker.RemoveTopCard();
            }

            return cardData;
        }

        /// <summary>
        /// 플레이어 덱에서 여러 장 드로우
        /// </summary>
        public List<CardData> DrawPlayerCards(int count)
        {
            List<CardData> drawnCards = new List<CardData>();

            for (int i = 0; i < count && !IsPlayerDeckEmpty; i++)
            {
                CardData? card = DrawPlayerCard();
                if (card.HasValue)
                    drawnCards.Add(card.Value);
            }

            return drawnCards;
        }

        /// <summary>
        /// 상대 덱에서 여러 장 드로우
        /// </summary>
        public List<CardData> DrawOpponentCards(int count)
        {
            List<CardData> drawnCards = new List<CardData>();

            for (int i = 0; i < count && !IsOpponentDeckEmpty; i++)
            {
                CardData? card = DrawOpponentCard();
                if (card.HasValue)
                    drawnCards.Add(card.Value);
            }

            return drawnCards;
        }

        /// <summary>
        /// CardData를 기반 GameObject를 생성하여 지정된 Zone에 배치
        /// NetworkCard 컴포넌트 자동 추가 및 초기화 포함
        /// </summary>
        /// <param name="cardData">생성할 카드 데이터</param>
        /// <param name="owner">카드 소유자</param>
        /// <param name="targetZone">배치할 Zone (null이면 배치하지 않음)</param>
        /// <returns>생성된 카드 GameObject 또는 null (실패시)</returns>
        public GameObject CreateCardObject(CardData cardData, CardZone.OwnerType owner, CardZone targetZone = null)
        {
            // 카드 템플릿 가져오기
            GameObject template = owner == CardZone.OwnerType.Player
                ? ResourcesManager.Instance.GetPlayerCardTemplate()
                : ResourcesManager.Instance.GetOpponentCardTemplate();

            if (template == null)
            {
                Debug.LogError($"[DeckManager] {owner} 카드 템플릿을 찾을 수 없습니다.");
                return null;
            }

            // 카드 오브젝트 생성
            GameObject cardObject = Instantiate(template);
            if (cardObject == null)
            {
                Debug.LogError("[DeckManager] 카드 오브젝트 생성에 실패했습니다.");
                return null;
            }

            // 기본 설정
            cardObject.SetActive(true);
            cardObject.transform.localPosition = Vector3.zero;
            cardObject.transform.localRotation = Quaternion.identity;

            // Card 컴포넌트 초기화
            var cardComponent = cardObject.GetComponent<Card>();
            if (cardComponent != null)
            {
                InitializeCardComponent(cardComponent, cardData);
                SetCardName(cardObject, cardData);
            }
            else
            {
                Debug.LogError("[DeckManager] Card 컴포넌트를 찾을 수 없습니다.");
                Destroy(cardObject);
                return null;
            }

            // NetworkCard 컴포넌트 추가 및 초기화
            var networkCard = cardObject.GetComponent<NetworkCard>();
            if (networkCard == null)
            {
                networkCard = cardObject.AddComponent<NetworkCard>();
            }

            // Zone에 추가 (옵션)
            if (targetZone != null)
            {
                targetZone.AddCard(cardObject.transform);

                // Zone 추가 후 NetworkCard 위치 정보 업데이트
                networkCard.UpdateLocationInfo();
            }

            return cardObject;
        }

        /// <summary>
        /// 덱 데이터 초기화 (게임 재시작 시 사용)
        /// </summary>
        public void ResetDecks()
        {
            playerDeck.Clear();
            opponentDeck.Clear();

            // 시각적 덱도 초기화
            if (playerDeckStacker != null)
                playerDeckStacker.ResetDeck();
            if (opponentDeckStacker != null)
                opponentDeckStacker.ResetDeck();

            Debug.Log("[DeckManager] 덱 리셋 완료");
        }
        #endregion

        #region DeckStacker Integration
        /// <summary>
        /// 씬에서 DeckStacker들을 찾아 연결
        /// </summary>
        private void FindDeckStackers()
        {
            DeckStacker[] stackers = FindObjectsByType<DeckStacker>(FindObjectsSortMode.None);

            foreach (var stacker in stackers)
            {
                if (stacker.IsMyDeck)
                {
                    playerDeckStacker = stacker;
                }
                else
                {
                    opponentDeckStacker = stacker;
                }
            }
        }

        /// <summary>
        /// DeckStacker 수동 등록 (필요시 사용)
        /// </summary>
        public void RegisterDeckStacker(DeckStacker stacker, bool isPlayer)
        {
            if (isPlayer)
                playerDeckStacker = stacker;
            else
                opponentDeckStacker = stacker;
        }
        #endregion

        #region Deck Creation
        /// <summary>
        /// 플레이어 덱 생성 (30장)
        /// </summary>
        private void CreatePlayerDeck()
        {
            playerDeck.Clear();
            AddStandardCards(playerDeck);
        }

        /// <summary>
        /// 상대 덱 생성 (30장)
        /// </summary>
        private void CreateOpponentDeck()
        {
            opponentDeck.Clear();
            AddStandardCards(opponentDeck);
        }

        /// <summary>
        /// 표준 덱 구성 추가 (숫자 + 연산자 + 조커)
        /// </summary>
        private void AddStandardCards(List<CardData> deck)
        {
            // 1. 숫자 카드 추가 (1~5, 각 4장) = 20장
            for (int value = 1; value <= 5; value++)
            {
                for (int count = 0; count < NUMBER_CARDS_PER_VALUE; count++)
                {
                    deck.Add(new CardData(value));
                }
            }

            // 2. 연산자 카드 추가 (각 2장) = 8장
            OperatorType[] operators = { OperatorType.Plus, OperatorType.Minus, OperatorType.Multiply, OperatorType.Divide };
            foreach (var op in operators)
            {
                for (int count = 0; count < OPERATOR_CARDS_PER_TYPE; count++)
                {
                    deck.Add(new CardData(op));
                }
            }

            // 3. 조커 카드 추가 = 2장
            for (int count = 0; count < JOKER_CARDS; count++)
            {
                deck.Add(CardData.CreateJoker());
            }

            // 총 30장 확인
            if (deck.Count != DECK_SIZE)
            {
                Debug.LogError($"[DeckManager] 덱 크기 오류: {deck.Count}장 (예상: {DECK_SIZE}장)");
            }
        }
        #endregion

        #region Deck Operations
        /// <summary>
        /// 덱 섞기 (Fisher-Yates 알고리즘)
        /// </summary>
        private void ShuffleDeck(List<CardData> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
            }
        }

        /// <summary>
        /// 지정된 카드 1장 드로우 (내부 로직)
        /// </summary>
        private CardData? DrawCard(List<CardData> deck, string playerName)
        {
            if (deck.Count == 0)
            {
                Debug.LogWarning($"[DeckManager] {playerName} 덱이 비어있어 드로우할 수 없습니다.");
                return null;
            }

            CardData drawnCard = deck[0];
            deck.RemoveAt(0);

            return drawnCard;
        }
        #endregion

        #region Card Management
        /// <summary>
        /// CardData로 Card 컴포넌트 초기화
        /// </summary>
        private void InitializeCardComponent(Card cardComponent, CardData cardData)
        {
            switch (cardData.cardType)
            {
                case CardType.Number:
                    cardComponent.InitializeAsNumber(cardData.numberValue);
                    break;
                case CardType.Operator:
                    cardComponent.InitializeAsOperator(cardData.operatorType);
                    break;
                case CardType.Joker:
                    cardComponent.InitializeAsJoker();
                    break;
            }
        }

        /// <summary>
        /// 카드 이름 설정
        /// </summary>
        private void SetCardName(GameObject cardObject, CardData cardData)
        {
            string name = cardData.cardType switch
            {
                CardType.Number => $"NumCard_{cardData.numberValue}",
                CardType.Operator => $"OpCard_{cardData.operatorType}",
                CardType.Joker => "JokerCard",
                _ => "UnknownCard"
            };
            cardObject.name = name;
        }
        #endregion

        #region Utility & Debug
        /// <summary>
        /// 카드 타입 문자열 변환
        /// </summary>
        private string GetCardDescription(CardData cardData)
        {
            return cardData.cardType switch
            {
                CardType.Number => $"숫자({cardData.numberValue})",
                CardType.Operator => $"연산자({cardData.operatorType})",
                CardType.Joker => "조커",
                _ => "알 수 없음"
            };
        }

        /// <summary>
        /// 덱 구성 로그 출력
        /// </summary>
        private void LogDeckComposition(List<CardData> deck, string playerName)
        {
            int numberCards = 0, operatorCards = 0, jokerCards = 0;

            foreach (var card in deck)
            {
                switch (card.cardType)
                {
                    case CardType.Number: numberCards++; break;
                    case CardType.Operator: operatorCards++; break;
                    case CardType.Joker: jokerCards++; break;
                }
            }

            Debug.Log($"[DeckManager] {playerName} 덱 구성: 숫자 {numberCards}장, 연산자 {operatorCards}장, 조커 {jokerCards}장");
        }
        #endregion
    }
}

using Objects;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 통합 RPC 시스템을 제공하는 네트워크 매니저
    /// 모든 네트워크 동기화를 중앙 집중식으로 관리
    /// NetworkCard 기반의 강화된 검증 시스템 사용
    /// Secret 카드 해제 동기화 포함
    /// </summary>
    public class NetworkGameManager : MonoBehaviourPun
    {
        #region Singleton Pattern
        private static NetworkGameManager instance;

        /// <summary>
        /// NetworkGameManager 싱글톤 인스턴스
        /// </summary>
        public static NetworkGameManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindAnyObjectByType<NetworkGameManager>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        #endregion

        #region Card Color Synchronization System
        /// <summary>
        /// 카드 색상 동기화 데이터
        /// 모든 클라이언트가 동일한 카드 색상을 사용하도록 보장
        /// </summary>
        [Serializable]
        public class CardColorData
        {
            /// <summary>플레이어 카드 스프라이트 이름</summary>
            public string playerSpriteName;

            /// <summary>상대방 카드 스프라이트 이름</summary>
            public string opponentSpriteName;

            /// <summary>
            /// CardColorData 생성자
            /// </summary>
            /// <param name="playerSprite">플레이어 스프라이트 이름</param>
            /// <param name="opponentSprite">상대방 스프라이트 이름</param>
            public CardColorData(string playerSprite, string opponentSprite)
            {
                playerSpriteName = playerSprite;
                opponentSpriteName = opponentSprite;
            }
        }

        /// <summary>
        /// 카드 색상을 모든 클라이언트에 동기화
        /// 방장이 색상을 선택하고 다른 플레이어들에게 전송
        /// </summary>
        public void SyncCardColors()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[NetworkGameManager] 방장만 카드 색상을 설정할 수 있습니다.");
                return;
            }

            if (ResourcesManager.Instance == null)
            {
                Debug.LogError("[NetworkGameManager] ResourcesManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            Debug.Log("[NetworkGameManager] 카드 색상 동기화 시작");

            // 방장이 랜덤 색상 선택
            var (playerSpriteName, opponentSpriteName) = ResourcesManager.Instance.SelectRandomColors();

            if (string.IsNullOrEmpty(playerSpriteName) || string.IsNullOrEmpty(opponentSpriteName))
            {
                Debug.LogError("[NetworkGameManager] 카드 색상 선택에 실패했습니다.");
                return;
            }

            // 방장 자신도 색상 적용
            ResourcesManager.Instance.SetPlayerColors(playerSpriteName, opponentSpriteName);

            // 다른 클라이언트들에게 색상 정보 전송
            var colorData = new CardColorData(playerSpriteName, opponentSpriteName);
            string jsonData = JsonUtility.ToJson(colorData);

            photonView.RPC("RPC_SyncCardColors", RpcTarget.Others, jsonData);

            Debug.Log("[NetworkGameManager] 카드 색상 동기화 RPC 전송 완료");
        }

        /// <summary>
        /// 저장된 색상으로 동기화
        /// </summary>
        /// <param name="senderColor">보내는 사람(방에 남아있던 사람)의 색상</param>
        /// <param name="receiverColor">받는 사람(새로 들어온 사람)의 색상</param>
        public void SyncStoredColors(string senderColor, string receiverColor)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[NetworkGameManager] 방장만 색상 동기화를 할 수 있습니다.");
                return;
            }

            if (string.IsNullOrEmpty(senderColor) || string.IsNullOrEmpty(receiverColor))
            {
                Debug.LogError("[NetworkGameManager] 색상 정보가 유효하지 않습니다.");
                return;
            }

            Debug.Log($"[NetworkGameManager] 색상 동기화 전송: 보내는사람={senderColor}, 받는사람={receiverColor}");

            // RPC로 새로 들어온 플레이어에게 색상 정보 전송
            var colorData = new CardColorData(receiverColor, senderColor); // 순서 주의!
            string jsonData = JsonUtility.ToJson(colorData);

            photonView.RPC("RPC_SyncCardColors", RpcTarget.Others, jsonData);

            Debug.Log("[NetworkGameManager] 색상 동기화 RPC 전송 완료");
        }

        /// <summary>
        /// 카드 색상 동기화 RPC 수신 처리
        /// </summary>
        [PunRPC]
        private void RPC_SyncCardColors(string jsonData)
        {
            var colorData = JsonUtility.FromJson<CardColorData>(jsonData);

            // 받은 색상 정보를 그대로 적용
            ResourcesManager.Instance.SetPlayerColors(
                colorData.playerSpriteName,    // 내 색상
                colorData.opponentSpriteName   // 상대방 색상
            );

            Debug.Log($"[NetworkGameManager] 색상 적용 완료: 내색상={colorData.playerSpriteName}, 상대색상={colorData.opponentSpriteName}");
        }
        #endregion

        #region Network Data Structures
        /// <summary>
        /// 카드 드로우 네트워크 데이터
        /// 상대방에게 드로우 행위를 알리기 위한 구조체
        /// </summary>
        [Serializable]
        public class CardDrawData
        {
            /// <summary>카드 소유자 (0=Player, 1=Opponent)</summary>
            public int ownerType;

            /// <summary>드로우할 카드 수</summary>
            public int count;

            /// <summary>덱에서 제거 애니메이션 표시 여부</summary>
            public bool showAnimation;

            /// <summary>
            /// CardDrawData 생성자
            /// </summary>
            /// <param name="owner">카드 소유자</param>
            /// <param name="drawCount">드로우할 카드 수</param>
            /// <param name="animate">애니메이션 표시 여부</param>
            public CardDrawData(CardZone.OwnerType owner, int drawCount, bool animate = true)
            {
                ownerType = (int)owner;
                count = drawCount;
                showAnimation = animate;
            }
        }

        /// <summary>
        /// 카드 배치 네트워크 데이터
        /// 상대방 화면에 카드 배치를 동기화하기 위한 구조체
        /// </summary>
        [Serializable]
        public class CardPlacementData
        {
            /// <summary>카드 타입 (Number, Operator, Joker)</summary>
            public CardType cardType;

            /// <summary>숫자 카드의 값</summary>
            public long numberValue;

            /// <summary>연산자 카드의 타입</summary>
            public OperatorType operatorType;

            /// <summary>카드 소유자 (0=Player, 1=Opponent)</summary>
            public int ownerType;

            /// <summary>배치될 Zone 타입 (0=Hand, 1=Field)</summary>
            public int zoneType;

            /// <summary>Secret 모드 여부</summary>
            public bool isSecret;

            /// <summary>카드 고유 ID (NetworkCard 기반)</summary>
            public string uniqueId;

            /// <summary>Zone 내에서의 배치 인덱스</summary>
            public int targetIndex;

            /// <summary>
            /// CardPlacementData 생성자
            /// </summary>
            /// <param name="cardData">배치할 카드 데이터</param>
            /// <param name="owner">카드 소유자</param>
            /// <param name="zone">배치될 Zone</param>
            /// <param name="secret">Secret 모드 여부</param>
            /// <param name="id">고유 ID</param>
            /// <param name="index">배치 인덱스</param>
            public CardPlacementData(Manager.CardData cardData, CardZone.OwnerType owner,
                                   CardZone.ZoneType zone, bool secret, string id, int index = -1)
            {
                cardType = cardData.cardType;
                numberValue = cardData.numberValue;
                operatorType = cardData.operatorType;
                ownerType = (int)owner;
                zoneType = (int)zone;
                isSecret = secret;
                uniqueId = id;
                targetIndex = index;
            }

            /// <summary>
            /// CardPlacementData를 Manager.CardData로 변환
            /// </summary>
            /// <returns>변환된 CardData</returns>
            public Manager.CardData ToCardData()
            {
                switch (cardType)
                {
                    case CardType.Number:
                        return new Manager.CardData(numberValue);
                    case CardType.Operator:
                        return new Manager.CardData(operatorType);
                    case CardType.Joker:
                        return Manager.CardData.CreateJoker();
                    default:
                        return new Manager.CardData(1);
                }
            }
        }

        /// <summary>
        /// 덱 상태 동기화 데이터
        /// 양쪽 덱의 남은 카드 수를 동기화하기 위한 구조체
        /// </summary>
        [Serializable]
        public class DeckSyncData
        {
            /// <summary>플레이어 덱 남은 카드 수</summary>
            public int playerDeckCount;

            /// <summary>상대방 덱 남은 카드 수</summary>
            public int opponentDeckCount;

            /// <summary>
            /// DeckSyncData 생성자
            /// </summary>
            /// <param name="playerCount">플레이어 덱 카드 수</param>
            /// <param name="opponentCount">상대방 덱 카드 수</param>
            public DeckSyncData(int playerCount, int opponentCount)
            {
                playerDeckCount = playerCount;
                opponentDeckCount = opponentCount;
            }
        }
        #endregion

        #region NetworkCard Registry System
        private Dictionary<string, NetworkCard> registeredCards = new Dictionary<string, NetworkCard>();

        /// <summary>
        /// NetworkCard를 등록하여 중앙에서 관리
        /// </summary>
        public void RegisterNetworkCard(NetworkCard card)
        {
            if (card == null)
            {
                Debug.LogWarning("[NetworkGameManager] 등록할 카드가 null입니다.");
                return;
            }

            string uniqueId = card.UniqueId;

            if (string.IsNullOrEmpty(uniqueId))
            {
                Debug.LogWarning($"[NetworkGameManager] 카드 {card.name}의 ID가 비어있어 등록할 수 없습니다.");
                return;
            }

            if (registeredCards.ContainsKey(uniqueId))
            {
                if (registeredCards[uniqueId] == card)
                {
                    Debug.Log($"[NetworkGameManager] 카드 {card.name} (ID: {uniqueId})는 이미 등록되어 있습니다.");
                    return;
                }

                Debug.LogWarning($"[NetworkGameManager] ID 충돌 감지! 기존 카드를 새 카드로 교체합니다. ID: {uniqueId}");
                registeredCards[uniqueId] = card;
            }
            else
            {
                registeredCards.Add(uniqueId, card);
                Debug.Log($"[NetworkGameManager] 카드 등록 완료: {card.name} (ID: {uniqueId}) - 총 {registeredCards.Count}장");
            }
        }

        /// <summary>
        /// NetworkCard 등록 해제
        /// </summary>
        public void UnregisterNetworkCard(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                Debug.LogWarning("[NetworkGameManager] 등록 해제할 ID가 비어있습니다.");
                return;
            }

            if (registeredCards.ContainsKey(uniqueId))
            {
                var card = registeredCards[uniqueId];
                registeredCards.Remove(uniqueId);
                Debug.Log($"[NetworkGameManager] 카드 등록 해제: {(card != null ? card.name : "null")} (ID: {uniqueId}) - 남은 카드: {registeredCards.Count}장");
            }
            else
            {
                Debug.LogWarning($"[NetworkGameManager] 등록되지 않은 카드 ID입니다: {uniqueId}");
            }
        }

        /// <summary>
        /// ID로 등록된 NetworkCard 찾기
        /// </summary>
        public NetworkCard FindNetworkCard(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                return null;
            }

            if (registeredCards.TryGetValue(uniqueId, out NetworkCard card))
            {
                if (card != null)
                {
                    return card;
                }
                else
                {
                    Debug.LogWarning($"[NetworkGameManager] 카드가 파괴되었으나 등록은 남아있습니다. ID: {uniqueId}");
                    registeredCards.Remove(uniqueId);
                    return null;
                }
            }

            Debug.LogWarning($"[NetworkGameManager] ID를 찾을 수 없습니다: {uniqueId}");
            return null;
        }

        /// <summary>
        /// 등록된 모든 카드 정보 출력 (디버깅용)
        /// </summary>
        public void PrintRegisteredCards()
        {
            Debug.Log($"[NetworkGameManager] === 등록된 카드 목록 ({registeredCards.Count}장) ===");

            foreach (var kvp in registeredCards)
            {
                string cardName = kvp.Value != null ? kvp.Value.name : "null";
                string cardInfo = kvp.Value != null ? $"IsValid: {kvp.Value != null}" : "Destroyed";
                Debug.Log($"  - ID: {kvp.Key}, Name: {cardName}, {cardInfo}");
            }
        }

        /// <summary>
        /// 모든 카드 등록 해제 (씬 전환 시 등)
        /// </summary>
        public void ClearAllRegisteredCards()
        {
            int count = registeredCards.Count;
            registeredCards.Clear();
            Debug.Log($"[NetworkGameManager] 모든 카드 등록 해제 완료 ({count}장)");
        }
        #endregion

        #region Card Draw Synchronization System
        /// <summary>
        /// 네트워크 액션 수행 가능 여부 확인 (초기 드로우 허용)
        /// 게임 상태 및 네트워크 연결 상태를 종합적으로 검증
        /// </summary>
        /// <returns>네트워크 액션 수행 가능 여부</returns>
        private bool CanPerformNetworkAction()
        {
            // 기본 네트워크 연결 확인
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[NetworkGameManager] 방에 접속되어 있지 않습니다.");
                return false;
            }

            // TurnManager 상태 확인
            if (TurnManager.Instance == null)
            {
                Debug.LogWarning("[NetworkGameManager] TurnManager 인스턴스를 찾을 수 없습니다.");
                return false;
            }

            // 수정: 초기 드로우는 게임 시작 전에도 허용 (IsGameStarted 체크 제거)
            // 플레이어가 2명이면 드로우 동기화 허용
            return PhotonNetwork.CurrentRoom.PlayerCount >= 2;
        }

        /// <summary>
        /// 카드 드로우를 다른 플레이어에게 동기화
        /// 상대방 화면에서 뒷면 카드 드로우 표시 및 덱 업데이트
        /// </summary>
        /// <param name="owner">카드를 드로우한 플레이어</param>
        /// <param name="count">드로우한 카드 수</param>
        public void SyncCardDraw(CardZone.OwnerType owner, int count)
        {
            if (!CanPerformNetworkAction())
            {
                Debug.LogWarning("[NetworkGameManager] 네트워크 액션 수행이 불가능합니다.");
                return;
            }

            Debug.Log($"[NetworkGameManager] 카드 드로우 동기화 시작: {owner} {count}장");

            var drawData = new CardDrawData(owner, count);
            string jsonData = JsonUtility.ToJson(drawData);

            photonView.RPC("RPC_SyncCardDraw", RpcTarget.Others, jsonData);

            Debug.Log($"[NetworkGameManager] 카드 드로우 동기화 RPC 전송 완료: {owner} {count}장");
        }

        /// <summary>
        /// 카드 드로우 동기화 RPC 수신 처리
        /// 원격 플레이어의 드로우 행위를 로컬 화면에 반영
        /// </summary>
        /// <param name="jsonData">직렬화된 CardDrawData</param>
        [PunRPC]
        private void RPC_SyncCardDraw(string jsonData)
        {
            try
            {
                var drawData = JsonUtility.FromJson<CardDrawData>(jsonData);
                ApplyRemoteCardDraw(drawData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 카드 드로우 동기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 원격 카드 드로우 적용
        /// 상대방 화면에서 실제 드로우 효과를 구현
        /// 주의: 상대방 관점에서 카드 소유자가 바뀜 (Player ↔ Opponent)
        /// </summary>
        /// <param name="drawData">드로우 데이터</param>
        private void ApplyRemoteCardDraw(CardDrawData drawData)
        {
            CardZone.OwnerType originalOwner = (CardZone.OwnerType)drawData.ownerType;

            // 상대방 관점에서 소유자 변환
            CardZone.OwnerType displayOwner = originalOwner == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            // 1단계: 시각적 덱에서 카드 제거 및 애니메이션
            if (drawData.showAnimation)
            {
                var deckStacker = FindDeckStacker(displayOwner);
                if (deckStacker != null)
                {
                    for (int i = 0; i < drawData.count; i++)
                    {
                        deckStacker.RemoveTopCard();
                    }
                }
            }

            // 2단계: 해당 소유자의 손패에 뒷면 카드 추가
            var handZone = FindHandZone(displayOwner);
            if (handZone != null)
            {
                for (int i = 0; i < drawData.count; i++)
                {
                    CreateBackCardInHand(handZone, displayOwner);
                }
            }
        }

        /// <summary>
        /// 손패에 뒷면 카드 생성
        /// ResourcesManager가 이미 설정한 empty 스프라이트를 그대로 사용
        /// </summary>
        /// <param name="handZone">카드를 추가할 손패 Zone</param>
        /// <param name="owner">카드 소유자</param>
        private void CreateBackCardInHand(CardZone handZone, CardZone.OwnerType owner)
        {
            // 카드 템플릿 가져오기
            GameObject template = owner == CardZone.OwnerType.Player
                ? ResourcesManager.Instance.GetPlayerCardTemplate()
                : ResourcesManager.Instance.GetOpponentCardTemplate();

            if (template == null)
            {
                Debug.LogError($"[NetworkGameManager] {owner}의 카드 템플릿을 찾을 수 없습니다.");
                return;
            }

            // 뒷면 카드 오브젝트 생성
            GameObject backCard = Instantiate(template);
            backCard.SetActive(true);
            backCard.name = $"BackCard_Remote_{owner}";

            // 카드 컴포넌트 설정
            var card = backCard.GetComponent<Card>();
            if (card != null)
            {
                card.InitializeAsNumber(0); // 임시 값으로 초기화
                Debug.Log($"[NetworkGameManager] 뒷면 카드 생성 완료: {backCard.name}");
            }

            // CardText 오브젝트만 비활성화 (뒷면처럼 보이도록)
            var cardText = backCard.GetComponentInChildren<CardText>();
            if (cardText != null)
            {
                cardText.gameObject.SetActive(false);
            }

            // TMPro 텍스트도 비활성화
            var tmpText = backCard.GetComponentInChildren<TMPro.TextMeshPro>();
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(false);
            }

            // NetworkCard 컴포넌트 추가
            var networkCard = backCard.GetComponent<NetworkCard>();
            if (networkCard == null)
            {
                networkCard = backCard.AddComponent<NetworkCard>();
            }

            // 상호작용 완전 비활성화
            var mouseEvent = backCard.GetComponentInChildren<ObjectMouseEvent>();
            if (mouseEvent != null)
            {
                mouseEvent.isClickable = false;
                mouseEvent.isDraggable = false;
            }

            // DragHandler 비활성화
            var dragHandler = backCard.GetComponent<DragHandler>();
            if (dragHandler != null)
            {
                dragHandler.enabled = false;
            }

            // 손패에 추가
            handZone.AddCard(backCard.transform);
        }
        #endregion

        #region Card Placement Synchronization System
        /// <summary>
        /// 카드 배치를 다른 플레이어에게 동기화
        /// 상대방 화면에 실제 카드 배치 및 손패에서 뒷면 카드 제거
        /// </summary>
        /// <param name="cardData">배치할 카드 데이터</param>
        /// <param name="owner">카드 소유자</param>
        /// <param name="zoneType">배치될 Zone 타입</param>
        /// <param name="isSecret">Secret 모드 여부</param>
        /// <param name="uniqueId">카드의 고유 ID (NetworkCard에서 전달받음)</param>
        public void SyncCardPlacement(Manager.CardData cardData, CardZone.OwnerType owner,
                                     CardZone.ZoneType zoneType, bool isSecret, string uniqueId)
        {
            Debug.Log($"[NetworkGameManager] SyncCardPlacement 호출됨: {cardData.cardType} to {owner} {zoneType} (ID: {uniqueId})");

            if (!CanPerformNetworkAction())
            {
                Debug.LogWarning($"[NetworkGameManager] 네트워크 액션 수행 불가 - 동기화 중단");
                return;
            }

            // uniqueId 검증
            if (string.IsNullOrEmpty(uniqueId))
            {
                Debug.LogError("[NetworkGameManager] uniqueId가 비어있어 동기화를 중단합니다.");
                return;
            }

            var placementData = new CardPlacementData(cardData, owner, zoneType, isSecret, uniqueId);
            string jsonData = JsonUtility.ToJson(placementData);

            Debug.Log($"[NetworkGameManager] RPC 전송 시작: {jsonData}");
            photonView.RPC("RPC_SyncCardPlacement", RpcTarget.Others, jsonData);
            Debug.Log($"[NetworkGameManager] RPC 전송 완료");
        }

        /// <summary>
        /// 카드 배치 동기화 RPC 수신 처리
        /// 원격 플레이어의 카드 배치를 로컬 화면에 반영
        /// </summary>
        /// <param name="jsonData">직렬화된 CardPlacementData</param>
        [PunRPC]
        private void RPC_SyncCardPlacement(string jsonData)
        {
            Debug.Log($"[NetworkGameManager] RPC_SyncCardPlacement 수신됨: {jsonData}");

            try
            {
                var placementData = JsonUtility.FromJson<CardPlacementData>(jsonData);
                Debug.Log($"[NetworkGameManager] 카드 배치 데이터 파싱 성공: {placementData.cardType} to {(CardZone.OwnerType)placementData.ownerType} {(CardZone.ZoneType)placementData.zoneType}");

                ApplyRemoteCardPlacement(placementData);
                Debug.Log($"[NetworkGameManager] 원격 카드 배치 적용 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 카드 배치 동기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 원격 카드 배치 적용 (소유자 변환 로직 포함)
        /// 상대방 화면에서 실제 카드 배치 효과를 구현
        /// 핵심: 상대방 관점에서 소유자를 올바르게 변환
        /// </summary>
        /// <param name="placementData">배치 데이터</param>
        private void ApplyRemoteCardPlacement(CardPlacementData placementData)
        {
            CardZone.OwnerType originalOwner = (CardZone.OwnerType)placementData.ownerType;
            CardZone.ZoneType zoneType = (CardZone.ZoneType)placementData.zoneType;

            Debug.Log($"[NetworkGameManager] ApplyRemoteCardPlacement 시작: 원본 소유자={originalOwner} {zoneType}");

            // 핵심: 상대방 관점에서 소유자 변환
            CardZone.OwnerType displayOwner = originalOwner == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            Debug.Log($"[NetworkGameManager] 소유자 변환 완료: {originalOwner} → {displayOwner}");

            // 1단계: 대상 Zone 찾기 및 검증 (변환된 소유자 사용)
            var targetZone = FindZone(displayOwner, zoneType);
            if (targetZone == null)
            {
                Debug.LogError($"[NetworkGameManager] Zone을 찾을 수 없음: {displayOwner} {zoneType}");
                return;
            }
            Debug.Log($"[NetworkGameManager] 1단계 완료: 대상 Zone 찾기 성공 ({displayOwner} {zoneType})");

            // 2단계: Zone 용량 체크
            if (!targetZone.CanAddCard())
            {
                Debug.LogWarning($"[NetworkGameManager] Zone이 가득참: {displayOwner} {zoneType}");
                return;
            }
            Debug.Log($"[NetworkGameManager] 2단계 완료: Zone 용량 체크 통과");

            // 3단계: 카드 데이터 복원
            var cardData = placementData.ToCardData();
            Debug.Log($"[NetworkGameManager] 3단계 완료: 카드 데이터 복원 ({cardData.cardType})");

            // 4단계: 카드 오브젝트 생성 (변환된 소유자 사용)
            GameObject cardObject = DeckManager.Instance.CreateCardObject(cardData, displayOwner, targetZone);
            if (cardObject == null)
            {
                Debug.LogError("[NetworkGameManager] 카드 오브젝트 생성 실패");
                return;
            }
            Debug.Log($"[NetworkGameManager] 4단계 완료: 카드 오브젝트 생성 성공 ({cardObject.name})");

            // 5단계: NetworkCard 설정
            var networkCard = cardObject.GetComponent<NetworkCard>();
            if (networkCard == null)
            {
                networkCard = cardObject.AddComponent<NetworkCard>();
            }
            networkCard.SetUniqueId(placementData.uniqueId);
            Debug.Log($"[NetworkGameManager] 5단계 완료: NetworkCard 설정 (ID: {placementData.uniqueId})");

            // 6단계: Secret 모드 설정
            var card = cardObject.GetComponent<Card>();
            if (card != null && placementData.isSecret)
            {
                card.SetSecret(true);
            }
            Debug.Log($"[NetworkGameManager] 6단계 완료: Secret 모드 설정 (isSecret: {placementData.isSecret})");

            // 7단계: 손패에서 뒷면 카드 제거 (필드 배치인 경우만, 변환된 소유자 사용)
            if (zoneType == CardZone.ZoneType.Field)
            {
                Debug.Log($"[NetworkGameManager] 7단계 시작: 손패에서 뒷면 카드 제거 ({displayOwner})");
                RemoveBackCardFromHand(displayOwner);
                Debug.Log($"[NetworkGameManager] 7단계 완료: 뒷면 카드 제거 완료");
            }

            Debug.Log($"[NetworkGameManager] ApplyRemoteCardPlacement 완료! {originalOwner} → {displayOwner} {zoneType}");
        }

        /// <summary>
        /// 손패에서 아무 카드나 1장 제거 (개수 맞추기용)
        /// </summary>
        /// <param name="owner">카드 소유자</param>
        private void RemoveBackCardFromHand(CardZone.OwnerType owner)
        {
            var handZone = FindHandZone(owner);
            if (handZone == null) return;

            // 손패에 카드가 있으면 첫 번째 카드 제거
            if (handZone.transform.childCount > 0)
            {
                var child = handZone.transform.GetChild(0);
                handZone.RemoveCard(child);
                Destroy(child.gameObject);
                Debug.Log($"[NetworkGameManager] {owner} 손패에서 카드 1장 제거 완료");
            }
        }
        #endregion

        #region Combat Synchronization System

        /// <summary>
        /// 전투 액션 데이터 구조체
        /// </summary>
        [Serializable]
        public class CombatActionData
        {
            public string attackerCardId;
            public string defenderCardId;
            public float attackerValue;
            public float defenderValue;
            public bool attackerWasSecret;
            public bool defenderWasSecret;
            public int damageToOpponent;
            public bool isEmptyFieldAttack;

            // 전투 결과로 제거될 카드들
            public bool destroyAttacker;
            public bool destroyDefender;

            // 추가: 전투 후 변경될 카드 값
            public float newAttackerValue;  // 공격자 승리 시 새로운 값
            public float newDefenderValue;  // 방어자 승리 시 새로운 값

            public CombatActionData(string attId, string defId, float attVal, float defVal,
                                   bool attSecret, bool defSecret, int damage,
                                   bool destroyAtt = false, bool destroyDef = false,
                                   float newAttVal = 0f, float newDefVal = 0f)
            {
                attackerCardId = attId;
                defenderCardId = defId;
                attackerValue = attVal;
                defenderValue = defVal;
                attackerWasSecret = attSecret;
                defenderWasSecret = defSecret;
                damageToOpponent = damage;
                isEmptyFieldAttack = string.IsNullOrEmpty(defId);
                destroyAttacker = destroyAtt;
                destroyDefender = destroyDef;
                newAttackerValue = newAttVal;
                newDefenderValue = newDefVal;
            }
        }

        /// <summary>
        /// 전투 액션 동기화 (공격, ExpressionZone, HP 모두 포함)
        /// </summary>
        public void SyncCombatAction(Card attacker, Card defender, float attackerValue,
                                     float defenderValue, int damageAmount)
        {
            if (attacker == null)
            {
                Debug.LogWarning("[NetworkGameManager] SyncCombatAction: attacker가 null입니다.");
                return;
            }

            if (!CanPerformNetworkAction())
            {
                Debug.LogWarning("[NetworkGameManager] 네트워크 액션 수행 불가");
                return;
            }

            // 공격자 정보
            var attackerNetworkCard = attacker.GetComponent<NetworkCard>();
            string attackerCardId = attackerNetworkCard != null ? attackerNetworkCard.UniqueId : "";
            bool attackerWasSecret = attacker.IsSecret;

            // 방어자 정보
            string defenderCardId = "";
            bool defenderWasSecret = false;
            if (defender != null)
            {
                var defenderNetworkCard = defender.GetComponent<NetworkCard>();
                defenderCardId = defenderNetworkCard != null ? defenderNetworkCard.UniqueId : "";
                defenderWasSecret = defender.IsSecret;
            }

            // 전투 결과 계산 (어느 카드가 파괴되는지 + 새로운 값)
            bool destroyAttacker = false;
            bool destroyDefender = false;
            float newAttackerValue = 0f;
            float newDefenderValue = 0f;

            if (defender != null) // 일반 공격
            {
                float result = attackerValue - defenderValue;
                if (result > 0) // 공격자 승리
                {
                    destroyDefender = true;
                    newAttackerValue = result; // 공격자의 새로운 값
                }
                else if (result < 0) // 방어자 승리
                {
                    destroyAttacker = true;
                    newDefenderValue = Mathf.Abs(result); // 방어자의 새로운 값
                }
                else // 무승부
                {
                    destroyAttacker = true;
                    destroyDefender = true;
                }
            }
            // 빈 필드 공격은 카드 파괴 없음

            Debug.Log($"[NetworkGameManager] 전투 액션 동기화: Attacker={attackerCardId}, Defender={defenderCardId}, Damage={damageAmount}, DestroyAtt={destroyAttacker}, DestroyDef={destroyDefender}, NewAttVal={newAttackerValue}, NewDefVal={newDefenderValue}");

            var combatData = new CombatActionData(
                attackerCardId, defenderCardId,
                attackerValue, defenderValue,
                attackerWasSecret, defenderWasSecret,
                damageAmount,
                destroyAttacker, destroyDefender,
                newAttackerValue, newDefenderValue
            );

            string jsonData = JsonUtility.ToJson(combatData);
            photonView.RPC("RPC_SyncCombatAction", RpcTarget.Others, jsonData);
        }

        [PunRPC]
        private void RPC_SyncCombatAction(string jsonData)
        {
            Debug.Log($"[NetworkGameManager] RPC_SyncCombatAction 수신");

            try
            {
                var combatData = JsonUtility.FromJson<CombatActionData>(jsonData);
                ApplyRemoteCombatAction(combatData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 전투 동기화 오류: {ex.Message}");
            }
        }

        private void ApplyRemoteCombatAction(CombatActionData combatData)
        {
            Debug.Log("[NetworkGameManager] 원격 전투 액션 적용 시작");

            // 1. 공격자/방어자 카드 찾기
            Card attackerCard = FindCardByNetworkId(combatData.attackerCardId);
            Card defenderCard = !string.IsNullOrEmpty(combatData.defenderCardId)
                ? FindCardByNetworkId(combatData.defenderCardId)
                : null;

            if (attackerCard == null)
            {
                Debug.LogError($"[NetworkGameManager] 공격자 찾을 수 없음: {combatData.attackerCardId}");
                return;
            }

            // 2. Secret 해제
            if (combatData.attackerWasSecret)
            {
                attackerCard.RevealSecret();
                Debug.Log($"[NetworkGameManager] 공격자 Secret 해제: {attackerCard.name}");
            }

            if (defenderCard != null && combatData.defenderWasSecret)
            {
                defenderCard.RevealSecret();
                Debug.Log($"[NetworkGameManager] 방어자 Secret 해제: {defenderCard.name}");
            }

            // 3. ExpressionZone 업데이트
            StartCoroutine(SyncExpressionZone(attackerCard, defenderCard, combatData));
        }

        private IEnumerator SyncExpressionZone(Card attacker, Card defender, CombatActionData data)
        {
            var ezManager = ExpressionZoneManager.Instance;

            // 공격 초기화
            ezManager.InitForAttack();
            ezManager.SetAttackerCard(attacker);

            yield return new WaitForSeconds(0.5f);

            if (data.isEmptyFieldAttack)
            {
                // 빈 필드 공격
                ezManager.SetEmptyFieldDefender(CardZone.OwnerType.Player);
                yield return new WaitForSeconds(1.5f);
                ezManager.ShowEmptyFieldResult(data.attackerValue);
            }
            else
            {
                // 일반 공격
                if (defender != null)
                {
                    ezManager.SetDefenderCard(defender);
                }

                yield return new WaitForSeconds(1.5f);
                ezManager.ShowResult(data.attackerValue, data.defenderValue);
            }

            yield return new WaitForSeconds(1f);

            // HP 감소 (상대방 관점에서는 내 HP가 감소)
            if (data.damageToOpponent > 0 && HealthManager.Instance != null)
            {
                HealthManager.Instance.ApplyDamage(data.damageToOpponent, CardZone.OwnerType.Player);
                Debug.Log($"[NetworkGameManager] HP 감소 적용: {data.damageToOpponent}");
            }

            yield return new WaitForSeconds(0.5f);

            // 추가: 카드 값 업데이트 (제거 전에 실행)
            if (data.newAttackerValue > 0 && attacker != null && !data.destroyAttacker)
            {
                var attackerText = attacker.GetComponentInChildren<CardText>();
                if (attackerText != null)
                {
                    attackerText.SetRawValue(data.newAttackerValue);
                    Debug.Log($"[NetworkGameManager] 공격자 값 업데이트: {data.newAttackerValue}");
                }
            }

            if (data.newDefenderValue > 0 && defender != null && !data.destroyDefender)
            {
                var defenderText = defender.GetComponentInChildren<CardText>();
                if (defenderText != null)
                {
                    defenderText.SetRawValue(data.newDefenderValue);
                    Debug.Log($"[NetworkGameManager] 방어자 값 업데이트: {data.newDefenderValue}");
                }
            }

            // 카드 제거 처리
            if (data.destroyAttacker && attacker != null)
            {
                Debug.Log($"[NetworkGameManager] 공격자 카드 제거: {attacker.name}");
                var zone = attacker.GetComponentInParent<CardZone>();
                zone?.RemoveCard(attacker.transform);
                Destroy(attacker.gameObject);
            }

            if (data.destroyDefender && defender != null)
            {
                Debug.Log($"[NetworkGameManager] 방어자 카드 제거: {defender.name}");
                var zone = defender.GetComponentInParent<CardZone>();
                zone?.RemoveCard(defender.transform);
                Destroy(defender.gameObject);
            }

            yield return new WaitForSeconds(0.3f);

            // ExpressionZone은 다음 공격 시작 시 InitForAttack()에서 초기화됨
            Debug.Log("[NetworkGameManager] 원격 전투 액션 완료");
        }
        /// <summary>
        /// NetworkCard ID로 Card 컴포넌트 찾기
        /// </summary>
        private Card FindCardByNetworkId(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;

            NetworkCard[] networkCards = FindObjectsByType<NetworkCard>(FindObjectsSortMode.None);
            foreach (var networkCard in networkCards)
            {
                if (networkCard.UniqueId == cardId)
                {
                    return networkCard.GetComponent<Card>();
                }
            }

            Debug.LogWarning($"[NetworkGameManager] NetworkCard ID {cardId}를 찾을 수 없습니다.");
            return null;
        }

        #endregion
        
        #region Network Utility Methods
        /// <summary>
        /// NetworkCard용 고유 ID 생성
        /// NetworkCard 클래스의 ID 생성 방식과 일치하는 8자리 알파뉴메릭 ID
        /// </summary>
        /// <returns>8자리 알파뉴메릭 고유 ID</returns>
        private string GenerateNetworkCardId()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }

        /// <summary>
        /// 지정된 소유자의 DeckStacker 찾기
        /// 플레이어 관점에서의 덱 매칭 (내 덱, 상대 덱)
        /// </summary>
        /// <param name="owner">덱 소유자</param>
        /// <returns>해당하는 DeckStacker 또는 null</returns>
        private DeckStacker FindDeckStacker(CardZone.OwnerType owner)
        {
            var stackers = FindObjectsByType<DeckStacker>(FindObjectsSortMode.None);
            foreach (var stacker in stackers)
            {
                // 플레이어 관점에서 "내 덱"은 Player, "상대 덱"은 Opponent
                bool isMyDeck = stacker.IsMyDeck;
                bool isTargetPlayer = (owner == CardZone.OwnerType.Player);

                if (isMyDeck == isTargetPlayer)
                {
                    return stacker;
                }
            }

            return null;
        }

        /// <summary>
        /// 지정된 소유자의 손패 Zone 찾기
        /// </summary>
        /// <param name="owner">Zone 소유자</param>
        /// <returns>해당하는 손패 Zone 또는 null</returns>
        private CardZone FindHandZone(CardZone.OwnerType owner)
        {
            return FindZone(owner, CardZone.ZoneType.Hand);
        }

        /// <summary>
        /// 특정 소유자와 타입의 Zone 찾기
        /// 게임 내 모든 Zone을 검색하여 조건에 맞는 Zone 반환
        /// </summary>
        /// <param name="owner">Zone 소유자</param>
        /// <param name="zoneType">Zone 타입</param>
        /// <returns>해당하는 Zone 또는 null</returns>
        private CardZone FindZone(CardZone.OwnerType owner, CardZone.ZoneType zoneType)
        {
            if (CardZone.AllZonesRoot == null)
            {
                Debug.LogError("[NetworkGameManager] CardZone.AllZonesRoot가 설정되지 않았습니다.");
                return null;
            }

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            foreach (var zone in zones)
            {
                if (zone.Owner == owner && zone.Zone == zoneType)
                {
                    return zone;
                }
            }

            return null;
        }
        #endregion

        #region Operator & Joker Synchronization
        /// <summary>
        /// 연산자 사용 결과 동기화 데이터
        /// </summary>
        [Serializable]
        public class OperationData
        {
            public int operatorType;        // OperatorType
            public string firstCardId;      // 결과가 적용될 카드
            public string secondCardId;     // 두 번째 카드 (삭제될 수 있음)
            public float firstCardValue;    // 첫 번째 카드의 원래 값
            public float secondCardValue;   // 두 번째 카드의 원래 값
            public float result;            // 연산 결과
            public float remainder;         // 나머지 (Divide만)
            public bool destroySecondCard;  // 두 번째 카드 삭제 여부

            public OperationData(OperatorType op, string first, string second,
                                float firstVal, float secondVal, float res, float rem = 0)
            {
                operatorType = (int)op;
                firstCardId = first;
                secondCardId = second;
                firstCardValue = firstVal;
                secondCardValue = secondVal;
                result = res;
                remainder = rem;
                destroySecondCard = false;
            }
        }

        /// <summary>
        /// 조커 효과 동기화 데이터
        /// </summary>
        [Serializable]
        public class JokerData
        {
            public int effectType;          // JokerEffectType
            public string[] targetCardIds;  // 대상 카드 ID 배열
            public float[] cardValues;      // 카드 값 배열 (Swap용)

            public JokerData(JokerEffectType type, string[] targets = null, float[] values = null)
            {
                effectType = (int)type;
                targetCardIds = targets ?? new string[0];
                cardValues = values ?? new float[0];
            }
        }

        /// <summary>
        /// 연산자 사용 결과 동기화
        /// </summary>
        public void SyncOperationResult(Card operatorCard, Card firstCard, Card secondCard, float result, OperatorType operatorType)
        {
            if (!CanPerformNetworkAction()) return;

            // 카드 ID 추출
            var firstNetworkCard = firstCard?.GetComponent<NetworkCard>();
            var secondNetworkCard = secondCard?.GetComponent<NetworkCard>();

            string firstId = firstNetworkCard?.UniqueId ?? "";
            string secondId = secondNetworkCard?.UniqueId ?? "";

            if (string.IsNullOrEmpty(firstId) || string.IsNullOrEmpty(secondId))
            {
                Debug.LogWarning("[NetworkGameManager] 연산 카드 ID를 찾을 수 없습니다.");
                return;
            }

            // 카드 값 추출
            float firstValue = firstCard.GetComponentInChildren<CardText>()?.RawValue ?? 0;
            float secondValue = secondCard.GetComponentInChildren<CardText>()?.RawValue ?? 0;

            // 나머지 계산 (Divide만)
            float remainder = 0;
            if (operatorType == OperatorType.Divide && secondValue != 0)
            {
                remainder = firstValue % secondValue;
            }

            var opData = new OperationData(operatorType, firstId, secondId, firstValue, secondValue, result, remainder);

            // secondCard가 삭제되는 경우 체크
            if (operatorType == OperatorType.Minus && result <= 0)
                opData.destroySecondCard = false; // Minus는 firstCard가 삭제됨
            else if (operatorType == OperatorType.Divide && result <= 0)
                opData.destroySecondCard = false; // Divide도 firstCard가 삭제됨

            string jsonData = JsonUtility.ToJson(opData);
            photonView.RPC("RPC_SyncOperation", RpcTarget.Others, jsonData);

            Debug.Log($"[NetworkGameManager] 연산 결과 동기화: {operatorType} = {result}");
        }

        /// <summary>
        /// 연산 결과 RPC 수신
        /// </summary>
        [PunRPC]
        private void RPC_SyncOperation(string jsonData)
        {
            try
            {
                var opData = JsonUtility.FromJson<OperationData>(jsonData);
                StartCoroutine(ApplyRemoteOperation(opData));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 연산 동기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 원격 연산 결과 적용
        /// </summary>
        private IEnumerator ApplyRemoteOperation(OperationData data)
        {
            OperatorType opType = (OperatorType)data.operatorType;
            Debug.Log($"[NetworkGameManager] 원격 연산 적용 시작: {opType}");

            // 카드 찾기 (소유자 변환 적용)
            var firstCard = FindCardByNetworkId(data.firstCardId);
            var secondCard = FindCardByNetworkId(data.secondCardId);

            if (firstCard == null || secondCard == null)
            {
                Debug.LogError($"[NetworkGameManager] 연산 대상 카드를 찾을 수 없습니다.");
                yield break;
            }

            // ExpressionZone에 수식 표시
            var ezManager = ExpressionZoneManager.Instance;
            if (ezManager != null)
            {
                ezManager.InitForOperation();
                ezManager.SetFirstOperand(firstCard);

                // 연산자 표시 (임시 연산자 카드 생성 필요 없이 타입만 전달)
                ezManager.SetOperatorByType(opType);

                ezManager.SetSecondOperand(secondCard);
                ezManager.ShowResult(data.firstCardValue, data.secondCardValue, opType);
            }

            yield return new WaitForSeconds(1.5f);

            // 결과 적용
            switch (opType)
            {
                case OperatorType.Plus:
                case OperatorType.Multiply:
                    UpdateRemoteCardValue(firstCard, data.result);
                    break;

                case OperatorType.Minus:
                    if (data.result > 0)
                        UpdateRemoteCardValue(firstCard, data.result);
                    else
                        DestroyRemoteCard(firstCard);
                    break;

                case OperatorType.Divide:
                    if (data.result > 0)
                        UpdateRemoteCardValue(firstCard, data.result);
                    else
                        DestroyRemoteCard(firstCard);

                    // 나머지 카드 생성
                    if (data.remainder > 0)
                    {
                        yield return new WaitForSeconds(0.3f);
                        CreateRemoteRemainderCard(data.remainder, firstCard.CurrentOwnerType);
                    }
                    break;
            }

            RemoveBackCardFromHand(CardZone.OwnerType.Opponent);

            Debug.Log($"[NetworkGameManager] 원격 연산 완료: {opType}");
        }

        /// <summary>
        /// 조커 효과 결과 동기화
        /// </summary>
        public void SyncJokerResult(Card jokerCard, JokerEffectType effectType, List<Card> targetCards = null)
        {
            if (!CanPerformNetworkAction()) return;

            string[] targetIds = null;
            float[] cardValues = null;

            // 대상 카드 정보 추출
            if (targetCards != null && targetCards.Count > 0)
            {
                targetIds = new string[targetCards.Count];
                cardValues = new float[targetCards.Count];

                for (int i = 0; i < targetCards.Count; i++)
                {
                    var networkCard = targetCards[i]?.GetComponent<NetworkCard>();
                    targetIds[i] = networkCard?.UniqueId ?? "";

                    var cardText = targetCards[i]?.GetComponentInChildren<CardText>();
                    cardValues[i] = cardText?.RawValue ?? 0;
                }
            }

            var jokerData = new JokerData(effectType, targetIds, cardValues);
            string jsonData = JsonUtility.ToJson(jokerData);
            photonView.RPC("RPC_SyncJoker", RpcTarget.Others, jsonData);

            Debug.Log($"[NetworkGameManager] 조커 효과 동기화: {effectType}");
        }

        /// <summary>
        /// 조커 효과 RPC 수신
        /// </summary>
        [PunRPC]
        private void RPC_SyncJoker(string jsonData)
        {
            try
            {
                var jokerData = JsonUtility.FromJson<JokerData>(jsonData);
                StartCoroutine(ApplyRemoteJoker(jokerData));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 조커 동기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 원격 조커 효과 적용
        /// </summary>
        private IEnumerator ApplyRemoteJoker(JokerData data)
        {
            JokerEffectType effectType = (JokerEffectType)data.effectType;
            Debug.Log($"[NetworkGameManager] 원격 조커 효과 적용: {effectType}");

            switch (effectType)
            {
                case JokerEffectType.Draw:
                    yield return ApplyRemoteDraw();
                    break;

                case JokerEffectType.Delete:
                    yield return ApplyRemoteDelete(data);
                    break;

                case JokerEffectType.Swap:
                    yield return ApplyRemoteSwap(data);
                    break;
            }

            Debug.Log($"[NetworkGameManager] 원격 조커 효과 완료: {effectType}");
        }

        /// <summary>
        /// 원격 Draw 효과 (상대방이 2장 드로우)
        /// </summary>
        private IEnumerator ApplyRemoteDraw()
        {
            Debug.Log("[NetworkGameManager] 원격 Draw 효과: 조커 카드 제거");

            // DrawCardsToHand 내부에서 이미 SyncCardDraw를 호출하여
            // 상대방 화면에 뒷면 카드 2장이 생성되었음

            // 하지만 조커 카드 자체도 상대방 화면에서는 뒷면 카드로 보이므로
            // 조커 사용 후 해당 뒷면 카드를 제거해야 함
            yield return new WaitForSeconds(0.1f);
            RemoveBackCardFromHand(CardZone.OwnerType.Opponent);

            Debug.Log("[NetworkGameManager] 원격 Draw 효과 완료: 조커 뒷면 카드 제거됨");

            yield break;
        }

        /// <summary>
        /// 원격 Delete 효과
        /// </summary>
        private IEnumerator ApplyRemoteDelete(JokerData data)
        {
            if (data.targetCardIds == null || data.targetCardIds.Length == 0)
                yield break;

            string targetId = data.targetCardIds[0];
            var targetCard = FindCardByNetworkId(targetId);

            if (targetCard == null)
            {
                Debug.LogWarning($"[NetworkGameManager] 삭제 대상 카드를 찾을 수 없습니다: {targetId}");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);

            RemoveBackCardFromHand(CardZone.OwnerType.Opponent);
            DestroyRemoteCard(targetCard);
        }

        /// <summary>
        /// 원격 Swap 효과
        /// </summary>
        private IEnumerator ApplyRemoteSwap(JokerData data)
        {
            if (data.targetCardIds == null || data.targetCardIds.Length < 2)
                yield break;

            var firstCard = FindCardByNetworkId(data.targetCardIds[0]);
            var secondCard = FindCardByNetworkId(data.targetCardIds[1]);

            if (firstCard == null || secondCard == null)
            {
                Debug.LogWarning("[NetworkGameManager] 교환 대상 카드를 찾을 수 없습니다.");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);

            // 값 교환
            var firstCardText = firstCard.GetComponentInChildren<CardText>();
            var secondCardText = secondCard.GetComponentInChildren<CardText>();

            if (firstCardText != null && secondCardText != null)
            {
                float firstValue = firstCardText.RawValue;
                float secondValue = secondCardText.RawValue;

                firstCardText.SetRawValue(secondValue);
                secondCardText.SetRawValue(firstValue);
            }

            RemoveBackCardFromHand(CardZone.OwnerType.Opponent);
        }

        /// <summary>
        /// 원격 카드 값 업데이트
        /// </summary>
        private void UpdateRemoteCardValue(Card card, float newValue)
        {
            var cardText = card.GetComponentInChildren<CardText>();
            if (cardText != null)
            {
                cardText.SetRawValue(newValue);
                Debug.Log($"[NetworkGameManager] 카드 값 업데이트: {card.name} = {newValue}");
            }
        }

        /// <summary>
        /// 원격 카드 삭제
        /// </summary>
        private void DestroyRemoteCard(Card card)
        {
            var zone = card.GetComponentInParent<CardZone>();
            zone?.RemoveCard(card.transform);
            Destroy(card.gameObject);
            Debug.Log($"[NetworkGameManager] 카드 삭제: {card.name}");
        }

        /// <summary>
        /// 원격 나머지 카드 생성
        /// </summary>
        private void CreateRemoteRemainderCard(float value, CardZone.OwnerType ownerType)
        {
            // 소유자 변환 (상대방 관점)
            CardZone.OwnerType displayOwner = ownerType == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            var fieldZone = FindZone(displayOwner, CardZone.ZoneType.Field);
            if (fieldZone == null || !fieldZone.CanAddCard())
            {
                Debug.LogWarning($"[NetworkGameManager] 나머지 카드를 생성할 수 없습니다.");
                return;
            }

            var template = displayOwner == CardZone.OwnerType.Player
                ? ResourcesManager.Instance.GetPlayerCardTemplate()
                : ResourcesManager.Instance.GetOpponentCardTemplate();

            if (template == null) return;

            var newCard = Instantiate(template);
            int intValue = Mathf.FloorToInt(value);
            newCard.name = $"RemainderCard_{intValue}";
            newCard.SetActive(true);

            var cardComponent = newCard.GetComponent<Card>();
            cardComponent?.InitializeAsNumber(intValue);

            fieldZone.AddCard(newCard.transform);
            Debug.Log($"[NetworkGameManager] 나머지 카드 생성: {intValue}");
        }
        #endregion
    }
}
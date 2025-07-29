using Objects;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 통합 RPC 시스템을 제공하는 네트워크 매니저
    /// 모든 네트워크 동기화를 중앙 집중식으로 관리
    /// NetworkCard 기반의 강화된 검증 시스템 사용
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
        [System.Serializable]
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

        /// 저장된 색상으로 동기화 (수정된 버전)
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
        /// 카드 색상 동기화 RPC 수신 처리 (기존 코드 확인)
        /// </summary>
        [PunRPC]
        private void RPC_SyncCardColors(string jsonData)
        {
            var colorData = JsonUtility.FromJson<CardColorData>(jsonData);

            // 받은 색상 정보를 그대로 적용 (순서 변경 없음)
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
        [System.Serializable]
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
        [System.Serializable]
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
        [System.Serializable]
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
            catch (System.Exception ex)
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
            // 원격에서 Player가 드로우 했다면, 내 화면에서는 Opponent 손패에 표시
            // 원격에서 Opponent가 드로우 했다면, 내 화면에서는 Player 손패에 표시
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
            }

            // 스프라이트는 이미 템플릿에서 올바르게 설정됨 (ResourcesManager가 empty 스프라이트로 설정해놨음)
            // 별도 스프라이트 처리 불필요!

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
        public void SyncCardPlacement(Manager.CardData cardData, CardZone.OwnerType owner,
                                    CardZone.ZoneType zoneType, bool isSecret)
        {
            if (!CanPerformNetworkAction())
            {
                return;
            }

            // 고유 ID 생성 (NetworkCard 기반)
            string uniqueId = GenerateNetworkCardId();

            var placementData = new CardPlacementData(cardData, owner, zoneType, isSecret, uniqueId);
            string jsonData = JsonUtility.ToJson(placementData);

            photonView.RPC("RPC_SyncCardPlacement", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 카드 배치 동기화 RPC 수신 처리
        /// 원격 플레이어의 카드 배치를 로컬 화면에 반영
        /// </summary>
        /// <param name="jsonData">직렬화된 CardPlacementData</param>
        [PunRPC]
        private void RPC_SyncCardPlacement(string jsonData)
        {
            try
            {
                var placementData = JsonUtility.FromJson<CardPlacementData>(jsonData);
                ApplyRemoteCardPlacement(placementData);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 카드 배치 동기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 원격 카드 배치 적용
        /// 상대방 화면에서 실제 카드 배치 효과를 구현
        /// </summary>
        /// <param name="placementData">배치 데이터</param>
        private void ApplyRemoteCardPlacement(CardPlacementData placementData)
        {
            CardZone.OwnerType owner = (CardZone.OwnerType)placementData.ownerType;
            CardZone.ZoneType zoneType = (CardZone.ZoneType)placementData.zoneType;

            // 1단계: 대상 Zone 찾기 및 검증
            var targetZone = FindZone(owner, zoneType);
            if (targetZone == null)
            {
                Debug.LogError($"[NetworkGameManager] Zone을 찾을 수 없음: {owner} {zoneType}");
                return;
            }

            // 2단계: Zone 용량 체크
            if (!targetZone.CanAddCard())
            {
                return;
            }

            // 3단계: 카드 데이터 복원
            var cardData = placementData.ToCardData();

            // 4단계: 카드 오브젝트 생성
            GameObject cardObject = DeckManager.Instance.CreateCardObject(cardData, owner, targetZone);
            if (cardObject == null)
            {
                Debug.LogError("[NetworkGameManager] 카드 오브젝트 생성 실패");
                return;
            }

            // 5단계: NetworkCard 설정
            var networkCard = cardObject.GetComponent<NetworkCard>();
            if (networkCard == null)
            {
                networkCard = cardObject.AddComponent<NetworkCard>();
            }
            networkCard.SetUniqueId(placementData.uniqueId);

            // 6단계: Secret 모드 설정
            var card = cardObject.GetComponent<Card>();
            if (card != null && placementData.isSecret)
            {
                card.SetSecret(true);
            }

            // 7단계: 손패에서 뒷면 카드 제거 (필드 배치인 경우만)
            if (zoneType == CardZone.ZoneType.Field)
            {
                RemoveBackCardFromHand(owner);
            }
        }

        /// <summary>
        /// 손패에서 뒷면 카드 1장 제거
        /// 상대방이 카드를 냈을 때 해당하는 뒷면 카드를 제거
        /// </summary>
        /// <param name="owner">카드 소유자</param>
        private void RemoveBackCardFromHand(CardZone.OwnerType owner)
        {
            var handZone = FindHandZone(owner);
            if (handZone == null)
            {
                return;
            }

            // 첫 번째 뒷면 카드 찾아서 제거
            for (int i = 0; i < handZone.transform.childCount; i++)
            {
                var child = handZone.transform.GetChild(i);
                var card = child.GetComponent<Card>();

                if (card != null && card.IsSecret && child.name.Contains("BackCard_Remote"))
                {
                    handZone.RemoveCard(child);
                    Destroy(child.gameObject);
                    break;
                }
            }
        }
        #endregion

        #region Deck State Synchronization System
        /// <summary>
        /// 덱 상태를 다른 플레이어에게 동기화
        /// 양쪽 덱의 남은 카드 수를 실시간 동기화
        /// </summary>
        public void SyncDeckState()
        {
            if (!CanPerformNetworkAction())
            {
                return;
            }

            if (DeckManager.Instance == null)
            {
                return;
            }

            int playerCount = DeckManager.Instance.PlayerDeckCount;
            int opponentCount = DeckManager.Instance.OpponentDeckCount;

            var deckData = new DeckSyncData(playerCount, opponentCount);
            string jsonData = JsonUtility.ToJson(deckData);

            photonView.RPC("RPC_SyncDeckState", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 덱 상태 동기화 RPC 수신 처리
        /// 상대방의 덱 상태를 받아서 UI 업데이트
        /// </summary>
        /// <param name="jsonData">직렬화된 DeckSyncData</param>
        [PunRPC]
        private void RPC_SyncDeckState(string jsonData)
        {
            try
            {
                var deckData = JsonUtility.FromJson<DeckSyncData>(jsonData);
                // TODO: DeckCountUI.UpdateCounts(deckData.playerDeckCount, deckData.opponentDeckCount);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 덱 동기화 오류: {ex.Message}");
            }
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
            return System.Guid.NewGuid().ToString("N")[..8].ToUpper();
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

        #region Legacy Game Action Result System
        /// <summary>
        /// 게임 액션 결과 데이터 (기존 시스템 유지)
        /// 연산, 공격, 조커 효과 등의 복합적인 게임 결과를 동기화
        /// </summary>
        [System.Serializable]
        public class GameActionResult
        {
            /// <summary>액션 타입 (OPERATION, ATTACK, JOKER 등)</summary>
            public string actionType;

            /// <summary>카드 상태 변경 목록</summary>
            public List<CardStateChange> cardChanges;

            /// <summary>데미지 정보 목록</summary>
            public List<DamageInfo> damages;

            /// <summary>제거될 카드 ID 목록</summary>
            public List<string> removedCards;

            /// <summary>
            /// GameActionResult 생성자
            /// </summary>
            /// <param name="type">액션 타입</param>
            public GameActionResult(string type)
            {
                actionType = type;
                cardChanges = new List<CardStateChange>();
                damages = new List<DamageInfo>();
                removedCards = new List<string>();
            }
        }

        /// <summary>
        /// 카드 상태 변경 데이터
        /// 개별 카드의 값 변경, Zone 이동 등을 표현
        /// </summary>
        [System.Serializable]
        public class CardStateChange
        {
            /// <summary>변경될 카드의 고유 ID</summary>
            public string cardId;

            /// <summary>새로운 값 (숫자 카드의 경우)</summary>
            public string newValue;

            /// <summary>이동할 Zone (Zone 변경시)</summary>
            public string newZone;

            /// <summary>연산으로 수정되었는지 여부</summary>
            public bool wasModified;

            /// <summary>
            /// CardStateChange 생성자
            /// </summary>
            /// <param name="id">카드 고유 ID</param>
            /// <param name="value">새로운 값</param>
            /// <param name="modified">수정 여부</param>
            /// <param name="zone">새로운 Zone</param>
            public CardStateChange(string id, string value, bool modified = false, string zone = "")
            {
                cardId = id;
                newValue = value;
                wasModified = modified;
                newZone = zone;
            }
        }

        /// <summary>
        /// 데미지 정보 데이터
        /// 플레이어에게 가해지는 데미지를 표현
        /// </summary>
        [System.Serializable]
        public class DamageInfo
        {
            /// <summary>데미지 양</summary>
            public int damage;

            /// <summary>대상 플레이어 (0=Player, 1=Opponent)</summary>
            public int targetPlayer;

            /// <summary>
            /// DamageInfo 생성자
            /// </summary>
            /// <param name="dmg">데미지 양</param>
            /// <param name="target">대상 플레이어</param>
            public DamageInfo(int dmg, CardZone.OwnerType target)
            {
                damage = dmg;
                targetPlayer = (int)target;
            }
        }

        /// <summary>
        /// 연산자 사용 결과 동기화 (기존 시스템)
        /// 연산 결과를 모든 클라이언트에 동기화
        /// </summary>
        /// <param name="operatorCard">사용된 연산자 카드</param>
        /// <param name="firstCard">첫 번째 피연산자 카드</param>
        /// <param name="secondCard">두 번째 피연산자 카드</param>
        /// <param name="result">연산 결과</param>
        /// <param name="operatorType">연산자 타입</param>
        public void SyncOperationResult(Card operatorCard, Card firstCard, Card secondCard, float result, OperatorType operatorType)
        {
            var actionResult = new GameActionResult("OPERATION");

            // 연산자 카드는 항상 제거
            actionResult.removedCards.Add(GetCardNetworkId(operatorCard));

            // 연산자 타입에 따른 결과 처리
            switch (operatorType)
            {
                case OperatorType.Plus:
                case OperatorType.Multiply:
                    // 첫 번째 카드의 값을 결과로 변경
                    actionResult.cardChanges.Add(new CardStateChange(
                        GetCardNetworkId(firstCard),
                        result.ToString(),
                        true
                    ));
                    break;

                case OperatorType.Minus:
                    if (result > 0)
                        // 결과가 양수면 첫 번째 카드 값 변경
                        actionResult.cardChanges.Add(new CardStateChange(GetCardNetworkId(firstCard), result.ToString(), true));
                    else
                        // 결과가 0 이하면 첫 번째 카드 제거
                        actionResult.removedCards.Add(GetCardNetworkId(firstCard));
                    break;

                case OperatorType.Divide:
                    if (result > 0)
                        // 몫이 있으면 첫 번째 카드 값 변경
                        actionResult.cardChanges.Add(new CardStateChange(GetCardNetworkId(firstCard), result.ToString(), true));
                    else
                        // 몫이 0이면 첫 번째 카드 제거
                        actionResult.removedCards.Add(GetCardNetworkId(firstCard));

                    // TODO: 나머지 카드 생성 로직 필요
                    break;
            }

            SyncGameActionResult(actionResult);
        }

        /// <summary>
        /// 공격 결과 동기화 (기존 시스템)
        /// 공격 결과를 모든 클라이언트에 동기화
        /// </summary>
        /// <param name="attacker">공격자 카드</param>
        /// <param name="defender">수비자 카드 (null이면 빈 필드 공격)</param>
        /// <param name="attackValue">공격 값</param>
        /// <param name="defenseValue">방어 값</param>
        public void SyncAttackResult(Card attacker, Card defender, float attackValue, float defenseValue)
        {
            var actionResult = new GameActionResult("ATTACK");
            float result = attackValue - defenseValue;

            if (defender == null) // 빈 필드 공격
            {
                int damage = DamageCalculator.CalculateEmptyFieldDamage(attackValue);
                actionResult.damages.Add(new DamageInfo(damage, CardZone.OwnerType.Opponent));
                actionResult.cardChanges.Add(new CardStateChange(GetCardNetworkId(attacker), "", true)); // 수정됨 표시
            }
            else // 일반 공격
            {
                if (result > 0) // 공격자 승리
                {
                    int damage = DamageCalculator.CalculateAttackDamage(attackValue, defenseValue);
                    actionResult.damages.Add(new DamageInfo(damage, CardZone.OwnerType.Opponent));
                    actionResult.cardChanges.Add(new CardStateChange(GetCardNetworkId(attacker), result.ToString(), true));
                    actionResult.removedCards.Add(GetCardNetworkId(defender));
                }
                else if (result < 0) // 수비자 승리
                {
                    actionResult.cardChanges.Add(new CardStateChange(GetCardNetworkId(defender), Mathf.Abs(result).ToString()));
                    actionResult.removedCards.Add(GetCardNetworkId(attacker));
                }
                else // 무승부
                {
                    actionResult.removedCards.Add(GetCardNetworkId(attacker));
                    actionResult.removedCards.Add(GetCardNetworkId(defender));
                }
            }

            SyncGameActionResult(actionResult);
        }

        /// <summary>
        /// 조커 효과 결과 동기화 (기존 시스템)
        /// 조커 카드 사용 결과를 모든 클라이언트에 동기화
        /// </summary>
        /// <param name="jokerCard">사용된 조커 카드</param>
        /// <param name="effectType">조커 효과 타입</param>
        /// <param name="targetCards">대상 카드들 (효과에 따라 사용)</param>
        public void SyncJokerResult(Card jokerCard, JokerEffectType effectType, List<Card> targetCards = null)
        {
            var actionResult = new GameActionResult("JOKER");

            // 조커 카드는 항상 제거
            actionResult.removedCards.Add(GetCardNetworkId(jokerCard));

            switch (effectType)
            {
                case JokerEffectType.Draw:
                    // Draw는 각자 로컬에서 처리 (덱이 다르므로)
                    break;

                case JokerEffectType.Delete:
                    if (targetCards != null && targetCards.Count > 0)
                        actionResult.removedCards.Add(GetCardNetworkId(targetCards[0]));
                    break;

                case JokerEffectType.Swap:
                    if (targetCards != null && targetCards.Count >= 2)
                    {
                        var card1Text = targetCards[0].GetComponentInChildren<CardText>();
                        var card2Text = targetCards[1].GetComponentInChildren<CardText>();

                        actionResult.cardChanges.Add(new CardStateChange(GetCardNetworkId(targetCards[0]), card2Text.RawValue.ToString()));
                        actionResult.cardChanges.Add(new CardStateChange(GetCardNetworkId(targetCards[1]), card1Text.RawValue.ToString()));
                    }
                    break;
            }

            SyncGameActionResult(actionResult);
        }

        /// <summary>
        /// 게임 액션 결과를 RPC로 전송 (기존 시스템)
        /// </summary>
        /// <param name="result">게임 액션 결과</param>
        private void SyncGameActionResult(GameActionResult result)
        {
            if (!CanPerformNetworkAction()) return;

            string jsonData = JsonUtility.ToJson(result);
            photonView.RPC("RPC_ApplyGameActionResult", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 게임 액션 결과 수신 및 적용 (기존 시스템)
        /// </summary>
        /// <param name="jsonData">직렬화된 게임 액션 결과</param>
        [PunRPC]
        private void RPC_ApplyGameActionResult(string jsonData)
        {
            try
            {
                var result = JsonUtility.FromJson<GameActionResult>(jsonData);
                ApplyGameActionResult(result);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 게임 액션 결과 적용 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 게임 액션 결과를 실제 게임 상태에 적용 (기존 시스템)
        /// </summary>
        /// <param name="result">적용할 게임 액션 결과</param>
        private void ApplyGameActionResult(GameActionResult result)
        {
            // 1단계: 카드 상태 변경 적용
            foreach (var change in result.cardChanges)
            {
                ApplyCardStateChange(change);
            }

            // 2단계: 카드 제거 적용
            foreach (var cardId in result.removedCards)
            {
                RemoveCardById(cardId);
            }

            // 3단계: 데미지 적용
            foreach (var damage in result.damages)
            {
                ApplyDamageToPlayer(damage);
            }

            // 4단계: 특별 처리 (액션 타입별)
            switch (result.actionType)
            {
                case "JOKER":
                    HandleJokerSpecialEffects(result);
                    break;
            }
        }

        /// <summary>
        /// 카드 상태 변경 적용 (기존 시스템)
        /// </summary>
        /// <param name="change">적용할 카드 상태 변경</param>
        private void ApplyCardStateChange(CardStateChange change)
        {
            Card card = FindCardByNetworkId(change.cardId);
            if (card == null)
            {
                return;
            }

            // 값 변경
            if (!string.IsNullOrEmpty(change.newValue))
            {
                var cardText = card.GetComponentInChildren<CardText>();
                if (cardText != null)
                {
                    cardText.SetRawValue(float.Parse(change.newValue));
                }
            }

            // 수정됨 표시
            if (change.wasModified)
            {
                card.SetWasModifiedThisTurn(true);
            }

            // Zone 이동
            if (!string.IsNullOrEmpty(change.newZone))
            {
                CardZone targetZone = FindZoneByReference(change.newZone);
                if (targetZone != null)
                {
                    targetZone.AddCard(card.transform);
                }
            }
        }

        /// <summary>
        /// ID로 카드 제거 (기존 시스템)
        /// </summary>
        /// <param name="cardId">제거할 카드의 네트워크 ID</param>
        private void RemoveCardById(string cardId)
        {
            Card card = FindCardByNetworkId(cardId);
            if (card == null)
            {
                return;
            }

            CardZone zone = card.GetComponentInParent<CardZone>();

            StartCoroutine(card.AnimateRemoval(() => {
                zone?.RemoveCard(card.transform);
                Destroy(card.gameObject);
            }));
        }

        /// <summary>
        /// 플레이어에게 데미지 적용 (기존 시스템)
        /// </summary>
        /// <param name="damageInfo">적용할 데미지 정보</param>
        private void ApplyDamageToPlayer(DamageInfo damageInfo)
        {
            CardZone.OwnerType target = (CardZone.OwnerType)damageInfo.targetPlayer;

            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.ApplyDamage(damageInfo.damage, target);
            }
        }

        /// <summary>
        /// 조커 특별 효과 처리 (기존 시스템)
        /// </summary>
        /// <param name="result">조커 액션 결과</param>
        private void HandleJokerSpecialEffects(GameActionResult result)
        {
            // Draw 효과는 각자 로컬에서 처리해야 함 (덱이 다르므로)
            // 여기서는 Draw 신호만 받아서 로컬 드로우 실행
            if (result.actionType == "JOKER" && result.cardChanges.Count == 0 && result.removedCards.Count == 1)
            {
                // Draw 조커로 추정
                if (InGameManager.Instance != null && TurnManager.Instance != null)
                {
                    InGameManager.Instance.DrawCardsToHand(2, TurnManager.Instance.LocalPlayerRole);
                }
            }
        }

        /// <summary>
        /// 카드의 네트워크 ID 가져오기 (기존 시스템 호환)
        /// </summary>
        /// <param name="card">ID를 가져올 카드</param>
        /// <returns>카드의 네트워크 ID</returns>
        private string GetCardNetworkId(Card card)
        {
            var networkCard = card.GetComponent<NetworkCard>();
            return networkCard?.UniqueId ?? "";
        }

        /// <summary>
        /// Zone 참조 문자열 생성 (기존 시스템 호환)
        /// </summary>
        /// <param name="zone">참조할 Zone</param>
        /// <returns>Zone 참조 문자열</returns>
        private string GetZoneReference(CardZone zone)
        {
            return $"{zone.Owner}_{zone.Zone}";
        }

        /// <summary>
        /// 네트워크 ID로 카드 찾기 (기존 시스템 호환)
        /// </summary>
        /// <param name="cardId">찾을 카드의 네트워크 ID</param>
        /// <returns>해당하는 카드 또는 null</returns>
        private Card FindCardByNetworkId(string cardId)
        {
            var networkCards = FindObjectsByType<NetworkCard>(FindObjectsSortMode.None);
            foreach (var networkCard in networkCards)
            {
                if (networkCard.UniqueId == cardId)
                {
                    return networkCard.GetComponent<Card>();
                }
            }
            return null;
        }

        /// <summary>
        /// Zone 참조 문자열로 Zone 찾기 (기존 시스템 호환)
        /// </summary>
        /// <param name="zoneRef">Zone 참조 문자열</param>
        /// <returns>해당하는 Zone 또는 null</returns>
        private CardZone FindZoneByReference(string zoneRef)
        {
            string[] parts = zoneRef.Split('_');
            if (parts.Length != 2) return null;

            try
            {
                CardZone.OwnerType owner = System.Enum.Parse<CardZone.OwnerType>(parts[0]);
                CardZone.ZoneType zone = System.Enum.Parse<CardZone.ZoneType>(parts[1]);

                return FindZone(owner, zone);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] Zone 참조 파싱 오류: {zoneRef}, {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
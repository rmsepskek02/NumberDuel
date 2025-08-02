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
        public void SyncCardPlacement(Manager.CardData cardData, CardZone.OwnerType owner,
                                     CardZone.ZoneType zoneType, bool isSecret)
        {
            Debug.Log($"[NetworkGameManager] SyncCardPlacement 호출됨: {cardData.cardType} to {owner} {zoneType}");

            if (!CanPerformNetworkAction())
            {
                Debug.LogWarning($"[NetworkGameManager] 네트워크 액션 수행 불가 - 동기화 중단");
                return;
            }

            // 고유 ID 생성 (NetworkCard 기반)
            string uniqueId = GenerateNetworkCardId();

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
            catch (System.Exception ex)
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

        #region Attack Result Synchronization (Secret 해제 포함)
        /// <summary>
        /// 공격 결과 동기화 (Secret 해제 정보 포함)
        /// </summary>
        /// <param name="attacker">공격자 카드</param>
        /// <param name="defender">방어자 카드 (빈 필드 공격 시 null)</param>
        /// <param name="attackerValue">공격자 값</param>
        /// <param name="defenderValue">방어자 값</param>
        public void SyncAttackResult(Card attacker, Card defender, float attackerValue, float defenderValue)
        {
            if (attacker == null)
            {
                Debug.LogWarning("[NetworkGameManager] SyncAttackResult: attacker가 null입니다.");
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

            Debug.Log($"[NetworkGameManager] 공격 결과 동기화: Attacker={attackerCardId}(Secret:{attackerWasSecret}), Defender={defenderCardId}(Secret:{defenderWasSecret})");

            // RPC 전송 (Secret 정보 포함)
            photonView.RPC("RPC_AttackResult", RpcTarget.All,
                attackerCardId, defenderCardId, attackerValue, defenderValue,
                attackerWasSecret, defenderWasSecret);
        }

        /// <summary>
        /// 공격 결과 RPC 수신 처리 (Secret 해제 포함)
        /// </summary>
        [PunRPC]
        private void RPC_AttackResult(string attackerCardId, string defenderCardId,
            float attackerValue, float defenderValue, bool attackerWasSecret, bool defenderWasSecret)
        {
            Debug.Log($"[NetworkGameManager] RPC_AttackResult 수신: Attacker={attackerCardId}, Defender={defenderCardId}");

            // 공격자 Secret 해제
            if (attackerWasSecret && !string.IsNullOrEmpty(attackerCardId))
            {
                var attackerCard = FindCardByNetworkId(attackerCardId);
                if (attackerCard != null)
                {
                    Debug.Log($"[NetworkGameManager] 공격자 Secret 해제: {attackerCard.name}");
                    attackerCard.RevealSecret();
                }
            }

            // 방어자 Secret 해제
            if (defenderWasSecret && !string.IsNullOrEmpty(defenderCardId))
            {
                var defenderCard = FindCardByNetworkId(defenderCardId);
                if (defenderCard != null)
                {
                    Debug.Log($"[NetworkGameManager] 방어자 Secret 해제: {defenderCard.name}");
                    defenderCard.RevealSecret();
                }
            }

            Debug.Log("[NetworkGameManager] 공격 결과 Secret 해제 처리 완료");
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

        #region Legacy System Support
        /// <summary>
        /// 연산자 사용 결과 동기화 (기존 시스템 유지)
        /// </summary>
        public void SyncOperationResult(Card operatorCard, Card firstCard, Card secondCard, float result, OperatorType operatorType)
        {
            // 기존 연산자 동기화 로직은 유지
            Debug.Log($"[NetworkGameManager] 연산 결과 동기화: {operatorType} = {result}");
        }

        /// <summary>
        /// 조커 효과 결과 동기화 (기존 시스템 유지)
        /// </summary>
        public void SyncJokerResult(Card jokerCard, JokerEffectType effectType, List<Card> targetCards = null)
        {
            // 기존 조커 동기화 로직은 유지
            Debug.Log($"[NetworkGameManager] 조커 효과 동기화: {effectType}");
        }
        #endregion
    }
}
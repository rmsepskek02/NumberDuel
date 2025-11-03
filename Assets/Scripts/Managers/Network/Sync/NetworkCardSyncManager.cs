using Objects;
using Photon.Pun;
using UnityEngine;
using Manager.Network.Data;

namespace Manager.Network.Sync
{
    /// <summary>
    /// 카드 드로우 및 배치 동기화를 담당하는 매니저
    /// 상대방 화면에 카드 드로우/배치를 동기화
    /// </summary>
    public class NetworkCardSyncManager
    {
        private readonly NetworkGameManager hub;

        /// <summary>
        /// NetworkCardSyncManager 생성자
        /// </summary>
        /// <param name="hub">NetworkGameManager 참조</param>
        public NetworkCardSyncManager(NetworkGameManager hub)
        {
            this.hub = hub;
        }

        #region Card Draw Synchronization
        /// <summary>
        /// 카드 드로우를 다른 플레이어에게 동기화
        /// 상대방 화면에서 뒷면 카드 드로우 표시 및 덱 업데이트
        /// </summary>
        /// <param name="owner">카드를 드로우한 플레이어</param>
        /// <param name="count">드로우한 카드 수</param>
        public void SyncCardDraw(CardZone.OwnerType owner, int count)
        {
            if (!hub.CanPerformNetworkAction())
                return;

            var drawData = new CardDrawData(owner, count);
            string jsonData = JsonUtility.ToJson(drawData);

            hub.photonView.RPC("RPC_SyncCardDraw", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 카드 드로우 동기화 RPC 수신 처리
        /// 원격 플레이어의 드로우 행위를 로컬 화면에 반영
        /// </summary>
        /// <param name="jsonData">직렬화된 CardDrawData</param>
        public void ApplyRemoteCardDraw(string jsonData)
        {
            try
            {
                var drawData = JsonUtility.FromJson<CardDrawData>(jsonData);
                ApplyRemoteCardDrawInternal(drawData);
            }
            catch (System.Exception)
            {
                // 오류 처리
            }
        }

        /// <summary>
        /// 원격 카드 드로우 적용
        /// 상대방 화면에서 실제 드로우 효과를 구현
        /// 주의: 상대방 관점에서 카드 소유자가 바뀜 (Player ↔ Opponent)
        /// </summary>
        /// <param name="drawData">드로우 데이터</param>
        private void ApplyRemoteCardDrawInternal(CardDrawData drawData)
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
                return;

            // 뒷면 카드 오브젝트 생성
            GameObject backCard = Object.Instantiate(template);
            backCard.SetActive(true);
            backCard.name = $"BackCard_Remote_{owner}";

            // 카드 컴포넌트 설정
            var card = backCard.GetComponent<Card>();
            if (card != null)
            {
                card.InitializeAsNumber(0); // 임시 값으로 초기화
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

        #region Card Placement Synchronization
        /// <summary>
        /// 카드 배치를 다른 플레이어에게 동기화
        /// 상대방 화면에 실제 카드 배치 및 손패에서 뒷면 카드 제거
        /// </summary>
        /// <param name="cardData">배치할 카드 데이터</param>
        /// <param name="owner">카드 소유자</param>
        /// <param name="zoneType">배치될 Zone 타입</param>
        /// <param name="isSecret">Secret 모드 여부</param>
        /// <param name="uniqueId">카드의 고유 ID (NetworkCard에서 전달받음)</param>
        public void SyncCardPlacement(CardData cardData, CardZone.OwnerType owner,
                                     CardZone.ZoneType zoneType, bool isSecret, string uniqueId)
        {
            if (!hub.CanPerformNetworkAction())
                return;

            // uniqueId 검증
            if (string.IsNullOrEmpty(uniqueId))
                return;

            var placementData = new CardPlacementData(cardData, owner, zoneType, isSecret, uniqueId);
            string jsonData = JsonUtility.ToJson(placementData);

            hub.photonView.RPC("RPC_SyncCardPlacement", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 카드 배치 동기화 RPC 수신 처리
        /// 원격 플레이어의 카드 배치를 로컬 화면에 반영
        /// </summary>
        /// <param name="jsonData">직렬화된 CardPlacementData</param>
        public void ApplyRemoteCardPlacement(string jsonData)
        {
            try
            {
                var placementData = JsonUtility.FromJson<CardPlacementData>(jsonData);
                ApplyRemoteCardPlacementInternal(placementData);
            }
            catch (System.Exception)
            {
                // 오류 처리
            }
        }

        /// <summary>
        /// 원격 카드 배치 적용 (소유자 변환 로직 포함)
        /// 상대방 화면에서 실제 카드 배치 효과를 구현
        /// 핵심: 상대방 관점에서 소유자를 올바르게 변환
        /// </summary>
        /// <param name="placementData">배치 데이터</param>
        private void ApplyRemoteCardPlacementInternal(CardPlacementData placementData)
        {
            CardZone.OwnerType originalOwner = (CardZone.OwnerType)placementData.ownerType;
            CardZone.ZoneType zoneType = (CardZone.ZoneType)placementData.zoneType;

            // 핵심: 상대방 관점에서 소유자 변환
            CardZone.OwnerType displayOwner = originalOwner == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            // 1단계: 대상 Zone 찾기 및 검증 (변환된 소유자 사용)
            var targetZone = FindZone(displayOwner, zoneType);
            if (targetZone == null)
                return;

            // 2단계: Zone 용량 체크
            if (!targetZone.CanAddCard())
                return;

            // 3단계: 카드 데이터 복원
            var cardData = placementData.ToCardData();

            // 4단계: 카드 오브젝트 생성 (변환된 소유자 사용)
            GameObject cardObject = DeckManager.Instance.CreateCardObject(cardData, displayOwner, targetZone);
            if (cardObject == null)
                return;

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

            // 7단계: 손패에서 뒷면 카드 제거 (필드 배치인 경우만, 변환된 소유자 사용)
            if (zoneType == CardZone.ZoneType.Field)
            {
                RemoveBackCardFromHand(displayOwner);
            }
        }

        /// <summary>
        /// 손패에서 아무 카드나 1장 제거 (개수 맞추기용)
        /// </summary>
        /// <param name="owner">카드 소유자</param>
        public void RemoveBackCardFromHand(CardZone.OwnerType owner)
        {
            var handZone = FindHandZone(owner);
            if (handZone == null) return;

            // 손패에 카드가 있으면 첫 번째 카드 제거
            if (handZone.transform.childCount > 0)
            {
                var child = handZone.transform.GetChild(0);
                handZone.RemoveCard(child);
                Object.Destroy(child.gameObject);
            }
        }
        #endregion

        #region Utility Methods (Public for other Sync managers)
        /// <summary>
        /// 특정 소유자와 타입의 Zone 찾기
        /// 게임 내 모든 Zone을 검색하여 조건에 맞는 Zone 반환
        /// </summary>
        /// <param name="owner">Zone 소유자</param>
        /// <param name="zoneType">Zone 타입</param>
        /// <returns>해당하는 Zone 또는 null</returns>
        public CardZone FindZone(CardZone.OwnerType owner, CardZone.ZoneType zoneType)
        {
            if (CardZone.AllZonesRoot == null)
                return null;

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

        #region Private Utility Methods
        /// <summary>
        /// 지정된 소유자의 DeckStacker 찾기
        /// 플레이어 관점에서의 덱 매칭 (내 덱, 상대 덱)
        /// </summary>
        /// <param name="owner">덱 소유자</param>
        /// <returns>해당하는 DeckStacker 또는 null</returns>
        private DeckStacker FindDeckStacker(CardZone.OwnerType owner)
        {
            var stackers = Object.FindObjectsByType<DeckStacker>(FindObjectsSortMode.None);
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
        #endregion
    }
}

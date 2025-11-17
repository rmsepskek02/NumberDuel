using Objects;
using Photon.Pun;
using System;
using UnityEngine;

namespace Manager.Network.Sync
{
    /// <summary>
    /// 카드 아이콘 동기화를 담당하는 매니저
    /// 상대방 화면에 카드 선택 아이콘 표시/숨김을 동기화
    /// </summary>
    public class NetworkIconSyncManager
    {
        private readonly NetworkGameManager hub;

        /// <summary>
        /// NetworkIconSyncManager 생성자
        /// </summary>
        /// <param name="hub">NetworkGameManager 참조</param>
        public NetworkIconSyncManager(NetworkGameManager hub)
        {
            this.hub = hub;
        }

        #region Icon Synchronization
        /// <summary>
        /// 카드 선택 아이콘 표시를 네트워크로 동기화
        /// </summary>
        /// <param name="card">아이콘을 표시할 카드</param>
        /// <param name="iconType">아이콘 타입 (CardIconType enum)</param>
        public void SyncCardIcon(Card card, CardIconType iconType)
        {
            if (card == null || iconType == CardIconType.None)
                return;

            if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList.Length < 2)
                return;

            var networkCard = card.GetComponent<NetworkCard>();
            if (networkCard == null)
            {
                Debug.LogWarning("[NetworkIconSyncManager] NetworkCard 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            string cardId = networkCard.UniqueId;
            string ownerType = card.CurrentOwnerType.ToString();
            string iconTypeString = iconType.ToString();

            Debug.Log($"[NetworkIconSyncManager] SyncCardIcon 전송: cardId={cardId}, iconType={iconTypeString}, ownerType={ownerType}, cardName={card.gameObject.name}");

            // RPC 호출 (iconType을 문자열로 변환하여 전송)
            hub.photonView.RPC("RPC_SyncCardIcon", RpcTarget.Others, cardId, iconTypeString, ownerType);
        }

        /// <summary>
        /// 카드 선택 아이콘 숨김을 네트워크로 동기화
        /// </summary>
        /// <param name="card">아이콘을 숨길 카드</param>
        public void HideCardIcon(Card card)
        {
            if (card == null)
                return;

            if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList.Length < 2)
                return;

            var networkCard = card.GetComponent<NetworkCard>();
            if (networkCard == null)
            {
                Debug.LogWarning("[NetworkIconSyncManager] NetworkCard 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            string cardId = networkCard.UniqueId;
            string ownerType = card.CurrentOwnerType.ToString();

            // RPC 호출
            hub.photonView.RPC("RPC_HideCardIcon", RpcTarget.Others, cardId, ownerType);
        }

        /// <summary>
        /// 카드 아이콘 표시 동기화 RPC 수신 처리
        /// </summary>
        public void ApplyRemoteCardIcon(string cardId, string iconType, string ownerType)
        {
            Debug.Log($"[NetworkIconSyncManager] RPC_SyncCardIcon 수신: cardId={cardId}, iconType={iconType}, ownerType={ownerType}");

            // Owner type 변환 (Player ↔ Opponent)
            CardZone.OwnerType senderOwner = Enum.Parse<CardZone.OwnerType>(ownerType);
            CardZone.OwnerType displayOwner = senderOwner == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            Debug.Log($"[NetworkIconSyncManager] Owner 변환: {senderOwner} → {displayOwner}");

            // 문자열을 CardIconType으로 변환
            if (!Enum.TryParse<CardIconType>(iconType, true, out CardIconType parsedIconType))
            {
                Debug.LogWarning($"[NetworkIconSyncManager] 잘못된 아이콘 타입: {iconType}");
                return;
            }

            // 카드 ID로 카드 찾기 (NetworkGameManager의 registeredCards 사용)
            NetworkCard networkCard = hub.FindNetworkCard(cardId);
            if (networkCard != null)
            {
                var card = networkCard.GetComponent<Card>();
                if (card != null)
                {
                    Debug.Log($"[NetworkIconSyncManager] 카드 찾음: {card.gameObject.name}, CurrentOwner={card.CurrentOwnerType}");
                    // 아이콘 표시 (CardIconType으로 변환하여 전달)
                    card.ShowSelectionIcon(parsedIconType);
                }
                else
                {
                    Debug.LogError($"[NetworkIconSyncManager] Card 컴포넌트가 없음: {networkCard.gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkIconSyncManager] 카드를 찾을 수 없습니다: {cardId}");
            }
        }

        /// <summary>
        /// 카드 아이콘 숨김 동기화 RPC 수신 처리
        /// </summary>
        public void ApplyRemoteHideIcon(string cardId, string ownerType)
        {
            // Owner type 변환 (Player ↔ Opponent)
            CardZone.OwnerType senderOwner = Enum.Parse<CardZone.OwnerType>(ownerType);
            CardZone.OwnerType displayOwner = senderOwner == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            // 카드 ID로 카드 찾기 (NetworkGameManager의 registeredCards 사용)
            NetworkCard networkCard = hub.FindNetworkCard(cardId);
            if (networkCard != null)
            {
                var card = networkCard.GetComponent<Card>();
                if (card != null)
                {
                    // 아이콘 숨김
                    card.HideSelectionIcon();
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkIconSyncManager] 카드를 찾을 수 없습니다: {cardId}");
            }
        }
        #endregion
    }
}

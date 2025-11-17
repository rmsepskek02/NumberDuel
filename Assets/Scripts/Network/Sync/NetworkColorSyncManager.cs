using Photon.Pun;
using UnityEngine;
using Manager.Network.Data;

namespace Manager.Network.Sync
{
    /// <summary>
    /// 카드 색상 동기화를 담당하는 매니저
    /// 모든 클라이언트가 동일한 카드 색상을 사용하도록 보장
    /// </summary>
    public class NetworkColorSyncManager
    {
        private readonly NetworkGameManager hub;

        /// <summary>
        /// NetworkColorSyncManager 생성자
        /// </summary>
        /// <param name="hub">NetworkGameManager 참조</param>
        public NetworkColorSyncManager(NetworkGameManager hub)
        {
            this.hub = hub;
        }

        /// <summary>
        /// 카드 색상을 모든 클라이언트에 동기화
        /// 방장이 색상을 선택하고 다른 플레이어들에게 전송
        /// </summary>
        public void SyncCardColors()
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (ResourcesManager.Instance == null)
                return;

            // 방장이 랜덤 색상 선택
            var (playerSpriteName, opponentSpriteName) = ResourcesManager.Instance.SelectRandomColors();

            if (string.IsNullOrEmpty(playerSpriteName) || string.IsNullOrEmpty(opponentSpriteName))
                return;

            // 방장 자신도 색상 적용
            ResourcesManager.Instance.SetPlayerColors(playerSpriteName, opponentSpriteName);

            // 다른 클라이언트들에게 색상 정보 전송
            var colorData = new CardColorData(playerSpriteName, opponentSpriteName);
            string jsonData = JsonUtility.ToJson(colorData);

            hub.photonView.RPC("RPC_SyncCardColors", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 저장된 색상으로 동기화
        /// </summary>
        /// <param name="senderColor">보내는 사람(방에 남아있던 사람)의 색상</param>
        /// <param name="receiverColor">받는 사람(새로 들어온 사람)의 색상</param>
        public void SyncStoredColors(string senderColor, string receiverColor)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (string.IsNullOrEmpty(senderColor) || string.IsNullOrEmpty(receiverColor))
                return;

            // RPC로 새로 들어온 플레이어에게 색상 정보 전송
            var colorData = new CardColorData(receiverColor, senderColor); // 순서 주의!
            string jsonData = JsonUtility.ToJson(colorData);

            hub.photonView.RPC("RPC_SyncCardColors", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 카드 색상 동기화 RPC 수신 처리
        /// </summary>
        /// <param name="jsonData">직렬화된 CardColorData</param>
        public void ApplyRemoteColorSync(string jsonData)
        {
            var colorData = JsonUtility.FromJson<CardColorData>(jsonData);

            // 받은 색상 정보를 그대로 적용
            ResourcesManager.Instance.SetPlayerColors(
                colorData.playerSpriteName,    // 내 색상
                colorData.opponentSpriteName   // 상대방 색상
            );
        }
    }
}

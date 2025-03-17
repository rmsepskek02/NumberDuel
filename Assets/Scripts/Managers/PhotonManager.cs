using Photon.Pun.UtilityScripts;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using System;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

namespace Manager
{
    /// <summary>
    /// Photon 관련 기능을 관리하는 매니저
    /// </summary>
    public class PhotonManager : MonoBehaviourPunCallbacks, IPunTurnManagerCallbacks
    {
        #region Variables
        PunTurnManager turnManager;

        // UI 관련 UnityAction (이벤트)
        public UnityAction MyTurn;                          // 내 턴
        public UnityAction YourTurn;                        // 상대 턴
        public UnityAction EnterPlayer;                     // 다른플레이어 입장
        public UnityAction LeavePlayer;                     // 다른플레이어 떠남
        public UnityAction<int> UpdatePlayerCount; // 현재 인원 수를 전달하는 이벤트
        #endregion

        void Start()
        {
            turnManager = GetComponent<PunTurnManager>();
            turnManager.TurnManagerListener = this;

            // InGameUIManager에서 이벤트 등록
            InGameUIManager.Instance.RegisterPhotonManager(this);
        }

        public void OnTurnBegins(int turn)
        {
            // 턴 시작 시 이벤트 호출
            if (turn % 2 == 1)
            {
                if (PhotonNetwork.IsMasterClient)
                    MyTurn?.Invoke(); // 마스터 턴
                else
                    YourTurn?.Invoke(); // 상대 턴
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                    YourTurn?.Invoke(); // 상대 턴
                else
                    MyTurn?.Invoke(); // 내 턴
            }
        }

        public void OnTurnCompleted(int turn)
        {
            Debug.Log("Turn " + turn + " completed!");
        }

        public void OnPlayerMove(Player player, int turn, object move)
        {
            Debug.Log(player.NickName + " Move ");
        }

        public void OnPlayerFinished(Player player, int turn, object move)
        {
            Debug.Log(player.NickName + " finished turn " + turn);
        }

        public void OnTurnTimeEnds(int turn) { }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log("New Player Entered Room: " + newPlayer.NickName);
            UpdateRoomPlayerCount();
            EnterPlayer?.Invoke(); // 플레이어 입장 이벤트 실행
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);
            Debug.Log("플레이어가 방을 나갔습니다: " + otherPlayer.NickName);
            UpdateRoomPlayerCount();
            LeavePlayer?.Invoke(); // 플레이어 퇴장 이벤트 실행

            // 혼자 남았으면 방 초기화
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                ResetGame();
            }
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey("currentPlayers"))
            {
                int currentPlayers = (int)propertiesThatChanged["currentPlayers"];
                Debug.Log($"[Room] 현재 인원 업데이트 감지: {currentPlayers}");

                UpdatePlayerCount?.Invoke(currentPlayers); // 현재 인원 업데이트 이벤트 호출
            }
        }

        private void UpdateRoomPlayerCount()
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

                ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "currentPlayers", currentPlayers }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                Debug.Log($"[Room] 현재 인원 업데이트: {currentPlayers}");
            }
        }

        public void OnLeaveRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom(); // 방 나가기
            }
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.LoadLevel("LobbyScene");
        }

        // 방을 초기 상태로 되돌리는 메서드
        private void ResetGame()
        {
            Debug.Log("방에 혼자 남음, 초기 상태로 리셋");

            // 초기화해야 할 내용 추가
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
    {
                { "currentPlayers", 1 }
    });
            // Turn 0으로 초기화
            PhotonNetwork.CurrentRoom.SetTurn(0, true);
            InGameUIManager.Instance.ResetUI();
        }
    }
}

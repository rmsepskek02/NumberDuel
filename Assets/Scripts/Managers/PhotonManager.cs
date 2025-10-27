using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Photon 네트워크 콜백을 처리하는 매니저
    /// 플레이어 입장/퇴장, 방 속성 변경, 색상 동기화 담당
    /// TurnManager와 연동하여 턴 관리 (PunTurnManager 의존성 제거)
    /// </summary>
    public class PhotonManager : MonoBehaviourPunCallbacks
    {
        #region Fields and Properties
        public event Action MyTurn;
        public event Action YourTurn;
        public event Action EnterPlayer;
        public event Action LeavePlayer;
        public event Action<int> UpdatePlayerCount;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // InGameUIManager에서 이벤트 등록
            InGameUIManager.Instance.RegisterPhotonManager(this);

            // 방장인 경우 색상 결정 및 저장
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                SetupRoomColors();
            }
        }
        #endregion

        #region Color Management
        /// <summary>
        /// 방장이 방 생성 시 색상 결정 및 방 속성에 저장
        /// </summary>
        private void SetupRoomColors()
        {
            if (ResourcesManager.Instance != null)
            {
                var (playerSpriteName, opponentSpriteName) = ResourcesManager.Instance.SelectRandomColors();

                if (!string.IsNullOrEmpty(playerSpriteName) && !string.IsNullOrEmpty(opponentSpriteName))
                {
                    // 방 속성에 색상 저장
                    var roomProperties = new Hashtable
                    {
                        { "masterPlayerColor", playerSpriteName },
                        { "guestPlayerColor", opponentSpriteName }
                    };
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                    // 방장 자신의 색상 적용
                    ResourcesManager.Instance.SetPlayerColors(playerSpriteName, opponentSpriteName);
                }
            }
        }

        /// <summary>
        /// 현재 내가 사용 중인 색상 이름 반환
        /// </summary>
        private string GetMyCurrentColor()
        {
            if (ResourcesManager.Instance != null)
            {
                var playerSprite = ResourcesManager.Instance.GetPlayerSprite();
                if (playerSprite != null)
                {
                    return playerSprite.name;
                }
            }

            return "";
        }

        /// <summary>
        /// 방 속성에 저장된 색상으로 동기화 전송
        /// </summary>
        private void SyncStoredColors()
        {
            var roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;

            if (roomProperties.TryGetValue("masterPlayerColor", out object masterColor) &&
                roomProperties.TryGetValue("guestPlayerColor", out object guestColor))
            {
                string color1 = masterColor.ToString();
                string color2 = guestColor.ToString();

                // 현재 내가 사용 중인 색상 확인
                string myCurrentColor = GetMyCurrentColor();

                // 새로 들어온 플레이어에게 줄 색상 결정
                string newPlayerColor;
                string myColor;

                if (myCurrentColor == color1)
                {
                    myColor = color1;
                    newPlayerColor = color2;
                }
                else if (myCurrentColor == color2)
                {
                    myColor = color2;
                    newPlayerColor = color1;
                }
                else
                {
                    // 현재 색상을 찾을 수 없는 경우 기본값 사용
                    myColor = color1;
                    newPlayerColor = color2;
                }

                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.SyncStoredColors(myColor, newPlayerColor);
                }
            }
        }
        #endregion

        #region Photon Callbacks
        /// <summary>
        /// 플레이어가 방에 입장했을 때 호출
        /// </summary>
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            UpdateRoomPlayerCount();

            // 방장이고 2명이 되었을 때 저장된 색상으로 동기화
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                SyncStoredColors();
            }

            EnterPlayer?.Invoke();
        }

        /// <summary>
        /// 플레이어가 방에서 퇴장했을 때 호출
        /// </summary>
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);
            UpdateRoomPlayerCount();
            LeavePlayer?.Invoke();

            // 혼자 남았으면 방 초기화
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                ResetGame();
            }
        }

        /// <summary>
        /// 방 속성이 변경되었을 때 호출
        /// </summary>
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey("currentPlayers"))
            {
                int currentPlayers = (int)propertiesThatChanged["currentPlayers"];
                UpdatePlayerCount?.Invoke(currentPlayers);
            }
        }
        #endregion

        #region Room Management
        /// <summary>
        /// 방의 현재 플레이어 수를 방 속성에 업데이트
        /// </summary>
        private void UpdateRoomPlayerCount()
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

                Hashtable roomProperties = new Hashtable
                {
                    { "currentPlayers", currentPlayers }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            }
        }

        /// <summary>
        /// 방 나가기
        /// </summary>
        public void OnLeaveRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }

        /// <summary>
        /// 방을 떠났을 때 호출 (로비로 이동)
        /// </summary>
        public override void OnLeftRoom()
        {
            PhotonNetwork.LoadLevel("LobbyScene");
        }

        /// <summary>
        /// 방을 초기 상태로 되돌리는 메서드
        /// </summary>
        private void ResetGame()
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { "currentPlayers", 1 }
            });

            InGameUIManager.Instance.ResetUI();
        }
        #endregion

        #region Turn Events
        /// <summary>
        /// 내 턴 이벤트 발생 (TurnManager에서 호출)
        /// </summary>
        public void TriggerMyTurn()
        {
            MyTurn?.Invoke();
        }

        /// <summary>
        /// 상대 턴 이벤트 발생 (TurnManager에서 호출)
        /// </summary>
        public void TriggerYourTurn()
        {
            YourTurn?.Invoke();
        }
        #endregion
    }
}

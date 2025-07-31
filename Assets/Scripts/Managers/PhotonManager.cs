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

            // 방장인 경우 색상 결정 및 저장
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                SetupRoomColors();
            }
        }

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
                    var roomProperties = new ExitGames.Client.Photon.Hashtable
                    {
                        { "masterPlayerColor", playerSpriteName },
                        { "guestPlayerColor", opponentSpriteName }
                    };
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                    // 방장 자신의 색상 적용
                    ResourcesManager.Instance.SetPlayerColors(playerSpriteName, opponentSpriteName);

                    Debug.Log($"[PhotonManager] 방 색상 설정 완료: Master={playerSpriteName}, Guest={opponentSpriteName}");
                }
            }
        }

        /// <summary>
        /// 턴 시작 시 이벤트 호출 (수정됨 - TurnManager 기준으로 통합)
        /// PhotonNetwork.IsMasterClient 대신 TurnManager.IsLocalPlayerTurn 사용
        /// </summary>
        /// <param name="turn">현재 턴 번호</param>
        public void OnTurnBegins(int turn)
        {
            Debug.Log($"[PhotonManager] OnTurnBegins 호출됨: Turn {turn}");

            // 🔧 핵심 수정: TurnManager 기준으로 턴 판단
            if (TurnManager.Instance != null)
            {
                // TurnManager의 턴 판단 로직을 사용
                bool isMyTurn = TurnManager.Instance.IsLocalPlayerTurn;

                Debug.Log($"[PhotonManager] TurnManager 기준 턴 판단: IsMyTurn={isMyTurn}");

                if (isMyTurn)
                {
                    Debug.Log("[PhotonManager] 내 턴 - MyTurn 이벤트 호출");
                    MyTurn?.Invoke(); // 내 턴
                }
                else
                {
                    Debug.Log("[PhotonManager] 상대 턴 - YourTurn 이벤트 호출");
                    YourTurn?.Invoke(); // 상대 턴
                }
            }
            else
            {
                // TurnManager가 없으면 기존 로직 사용 (폴백)
                Debug.LogWarning("[PhotonManager] TurnManager가 없어서 기존 로직 사용");

                if (turn % 2 == 1)
                {
                    if (PhotonNetwork.IsMasterClient)
                        MyTurn?.Invoke(); // 마스터 턴
                    else
                        YourTurn?.Invoke(); // 게스트 턴
                }
                else
                {
                    if (PhotonNetwork.IsMasterClient)
                        YourTurn?.Invoke(); // 게스트 턴
                    else
                        MyTurn?.Invoke(); // 마스터 턴
                }
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

            // 방장이고 2명이 되었을 때 저장된 색상으로 동기화
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                SyncStoredColors();
            }

            EnterPlayer?.Invoke(); // 플레이어 입장 이벤트 실행
        }

        /// <summary>
        /// 방 속성에 저장된 색상으로 동기화 전송 (완전히 새로운 버전)
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
                    Debug.LogWarning($"[PhotonManager] 현재 색상 {myCurrentColor}이 방 색상과 일치하지 않음");
                    myColor = color1;
                    newPlayerColor = color2;
                }

                if (NetworkGameManager.Instance != null)
                {
                    Debug.Log($"[PhotonManager] 올바른 색상 동기화: 내색상={myColor}, 새플레이어색상={newPlayerColor}");
                    NetworkGameManager.Instance.SyncStoredColors(myColor, newPlayerColor); // ← 간소화!
                }
            }
            else
            {
                Debug.LogError("[PhotonManager] 방 속성에서 색상 정보를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 현재 내가 사용 중인 색상 이름 반환 (새로 추가)
        /// </summary>
        private string GetMyCurrentColor()
        {
            if (ResourcesManager.Instance != null)
            {
                var playerSprite = ResourcesManager.Instance.GetPlayerSprite();
                if (playerSprite != null)
                {
                    Debug.Log($"[PhotonManager] 현재 내 색상: {playerSprite.name}");
                    return playerSprite.name;
                }
            }

            Debug.LogError("[PhotonManager] 현재 사용 중인 색상을 확인할 수 없습니다.");
            return "";
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
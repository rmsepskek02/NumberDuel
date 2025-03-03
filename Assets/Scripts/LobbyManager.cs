using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lobby
{
    /// <summary>
    /// Lobby를 관리하는 매니저
    /// </summary>
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        #region Variables
        public TextMeshProUGUI width;
        public TextMeshProUGUI height;
        public TMP_InputField roomNameInputField;
        public TMP_InputField roomPasswordInputField;
        public Transform roomListContent;
        public GameObject roomItemFactory;

        Dictionary<string, RoomInfo> roomCache = new Dictionary<string, RoomInfo>();
        string roomNameText;
        string roomPasswordText;
        #endregion
        void Start()
        {
            OnConnectedToMaster();
        }

        void Update()
        {
            roomNameText = roomNameInputField.text;
            roomPasswordText = roomPasswordInputField.text;

            // 화면 크기 표시
            width.text = $"{Screen.width}";
            height.text = $"{Screen.height}";
        }

        public void CreateRoom(string roomName, string roomPassword)
        {
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 2,
                IsVisible = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
            {
                { "roomName", roomName },
                { "roomPassword", roomPassword }
            },
                CustomRoomPropertiesForLobby = new[] { "roomName", "roomPassword" }
            };

            PhotonNetwork.CreateRoom(roomName + roomPassword, roomOptions);
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
        }

        public void JoinRoom(string roomName, string password)
        {
            PhotonNetwork.JoinRoom(roomName + password);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            PhotonNetwork.LoadLevel("GameScene");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);

            // 방 목록 갱신
            UpdateRoomCache(roomList);
            UpdateRoomListUI();
        }

        void UpdateRoomCache(List<RoomInfo> roomList)
        {
            foreach (RoomInfo room in roomList)
            {
                if (room.RemovedFromList)
                    roomCache.Remove(room.Name);
                else
                    roomCache[room.Name] = room;
            }
        }

        void UpdateRoomListUI()
        {
            // 기존 방 목록 UI 초기화
            foreach (Transform child in roomListContent)
            {
                Destroy(child.gameObject);
            }

            // 방 목록 UI 업데이트
            foreach (RoomInfo roomInfo in roomCache.Values)
            {
                GameObject roomItem = Instantiate(roomItemFactory, roomListContent);
                RoomItem itemComponent = roomItem.GetComponent<RoomItem>();
                itemComponent.SetInfo(roomInfo);

                // 방을 선택할 때 처리할 로직
                itemComponent.OnClickAction = (string roomName) =>
                {
                    roomNameInputField.text = roomName;
                };
            }
        }
        public override void OnConnectedToMaster()
        {
            // 서버에 연결된 후 로비로 진입할 수 있습니다.
            PhotonNetwork.JoinLobby();
        }
        public void OnClickCreate()
        {
            CreateRoom(roomNameText, roomPasswordText);
        }

        public void OnClickJoin()
        {
            JoinRoom(roomNameText, roomPasswordText);
        }

        public void OnClickRefresh()
        {
            // 로비에서 방 목록 새로 가져오기
            PhotonNetwork.JoinLobby();  // 로비에 다시 입장하여 방 목록을 업데이트합니다.
        }
    }
}

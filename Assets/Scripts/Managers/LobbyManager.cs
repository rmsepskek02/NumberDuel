using Objects;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Manager
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

        // 방 생성
        public void CreateRoom(string roomName, string roomPassword)
        {
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 2,
                IsVisible = true,
                IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "roomName", roomName },
                    { "roomPassword", roomPassword }
                },
                CustomRoomPropertiesForLobby = new[] { "roomName", "roomPassword" }
            };

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            Debug.Log($"방 생성 완료: {PhotonNetwork.CurrentRoom.Name}, IsVisible: {PhotonNetwork.CurrentRoom.IsVisible}");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            Debug.LogError($"방 생성 실패: {message}");
        }

        // 방 참가
        public void JoinRoom(string roomName, string password)
        {
            PhotonNetwork.JoinRoom(roomName);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            Debug.Log($"방 참가 성공: {PhotonNetwork.CurrentRoom.Name}");
            PhotonNetwork.LoadLevel("GameScene");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            Debug.LogError($"방 참가 실패: {message}");
        }

        // 방 목록 업데이트
        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            Debug.Log($"방 목록 업데이트 ({roomList.Count}개)");

            roomCache.Clear(); // 기존 목록 초기화

            foreach (RoomInfo room in roomList)
            {
                // 방이 가득 찬 경우 목록에서 제외
                if (room.PlayerCount >= room.MaxPlayers)
                {
                    Debug.Log($"방 {room.Name}이 가득 차서 목록에서 제외됨.");
                    continue;
                }

                roomCache[room.Name] = room;
            }

            UpdateRoomListUI();
        }


        void UpdateRoomCache(List<RoomInfo> roomList)
        {
            foreach (RoomInfo room in roomList)
            {
                if (room.RemovedFromList)
                {
                    roomCache.Remove(room.Name);
                }
                else
                {
                    roomCache[room.Name] = room;
                }
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
                Debug.Log($"방 정보 로드: {roomInfo.Name}");

                GameObject roomItem = Instantiate(roomItemFactory, roomListContent);
                RoomItem itemComponent = roomItem.GetComponent<RoomItem>();

                if (itemComponent != null)
                {
                    itemComponent.SetInfo(roomInfo);
                }
                else
                {
                    Debug.LogError("RoomItem 컴포넌트를 찾을 수 없습니다!");
                }
                // 방을 선택할 때 처리할 로직
                itemComponent.OnClickAction = (string roomName) =>
                {
                    roomNameInputField.text = roomName;
                };
            }
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("마스터 서버 연결됨, 로비 진입 시도");
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
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                Debug.Log("로비에 다시 입장하여 방 목록 새로고침");
            }
            else
            {
                Debug.Log("이미 로비에 있음. 강제 방 목록 갱신");
                UpdateRoomListUI();
            }
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            Debug.Log("CLICK QUIT");
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }
}

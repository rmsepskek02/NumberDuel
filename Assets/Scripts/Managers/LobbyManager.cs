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
        public Sprite enableRoomListSprite;
        public Sprite fullRoomListSprite;
        public Sprite lockRoomListSprite;

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
            { "roomPassword", roomPassword },
            { "currentPlayers", 1 } // 방장 포함 1명으로 초기화
        },
                CustomRoomPropertiesForLobby = new[] { "roomName", "roomPassword", "currentPlayers" } // 로비에서도 확인 가능하도록 설정
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

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            Debug.Log($"방 목록 업데이트 ({roomList.Count}개)");

            foreach (RoomInfo room in roomList)
            {
                if (room.RemovedFromList)
                {
                    roomCache.Remove(room.Name); // 삭제된 방 제거
                }
                else
                {
                    // Custom Properties에서 현재 인원 가져오기
                    int currentPlayers = room.CustomProperties.ContainsKey("currentPlayers") ? (int)room.CustomProperties["currentPlayers"] : 0;
                    int maxPlayers = room.MaxPlayers;

                    // 방이 가득 찼거나 비밀번호가 있어도 제거하지 않음
                    roomCache[room.Name] = room;
                    Debug.Log($"방 추가됨: {room.Name}, 인원 {currentPlayers}/{maxPlayers}");

                    //// Custom Properties에서 현재 인원 가져오기
                    //int currentPlayers = room.CustomProperties.ContainsKey("currentPlayers") ? (int)room.CustomProperties["currentPlayers"] : 0;
                    //int maxPlayers = room.MaxPlayers;

                    //// 최대 인원이 찼으면 목록에서 제거
                    //if (currentPlayers >= maxPlayers)
                    //{
                    //    if (roomCache.ContainsKey(room.Name))
                    //    {
                    //        roomCache.Remove(room.Name);
                    //        Debug.Log($"방 {room.Name}이 가득 차서 목록에서 제외됨.");
                    //    }
                    //}
                    //else
                    //{
                    //    // 인원이 줄어들면 다시 목록에 추가
                    //    roomCache[room.Name] = room;
                    //    Debug.Log($"방 {room.Name}에 자리가 생겨 다시 목록에 추가됨.");
                    //}
                }
            }

            UpdateRoomListUI(); // UI 업데이트
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
                    // 방 상태에 따라 스프라이트 변경
                    int currentPlayers = roomInfo.CustomProperties.ContainsKey("currentPlayers") ? (int)roomInfo.CustomProperties["currentPlayers"] : 0;
                    int maxPlayers = roomInfo.MaxPlayers;
                    bool hasPassword = roomInfo.CustomProperties.ContainsKey("roomPassword") && !string.IsNullOrEmpty((string)roomInfo.CustomProperties["roomPassword"]);

                    Sprite selectedSprite;

                    if (currentPlayers >= maxPlayers)
                    {
                        selectedSprite = fullRoomListSprite; // 인원 가득 찬 방
                    }
                    else if (hasPassword)
                    {
                        selectedSprite = lockRoomListSprite; // 비밀번호 있는 방
                    }
                    else
                    {
                        selectedSprite = enableRoomListSprite; // 접속 가능한 방
                    }

                    // RoomItem의 Image 컴포넌트 변경
                    if (itemComponent.TryGetComponent(out UnityEngine.UI.Image roomImage))
                    {
                        roomImage.sprite = selectedSprite;
                    }
                    else
                    {
                        Debug.LogWarning("RoomItem 프리팹에 Image 컴포넌트가 없습니다!");
                    }

                    // 방을 선택할 때 처리할 로직
                    itemComponent.OnClickAction = (string roomName) =>
                    {
                        roomNameInputField.text = roomName;
                    };
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

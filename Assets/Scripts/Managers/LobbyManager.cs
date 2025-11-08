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
    /// 방 생성, 방 참가, 방 목록 관리 담당
    /// </summary>
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        #region Fields and Properties
        public TextMeshProUGUI width;
        public TextMeshProUGUI height;
        public TMP_InputField roomNameInputField;
        public TMP_InputField roomPasswordInputField;
        public Transform roomListContent;
        public GameObject roomItemFactory;
        public Sprite enableRoomListSprite;
        public Sprite fullRoomListSprite;
        public Sprite lockRoomListSprite;

        private Dictionary<string, RoomInfo> roomCache = new Dictionary<string, RoomInfo>();
        private string roomNameText;
        private string roomPasswordText;
        #endregion

        #region Unity Lifecycle
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
        #endregion

        #region Room Management
        /// <summary>
        /// 방 생성
        /// - 변경: 바로 PhotonNetwork.CreateRoom 호출 대신 페이드인 후 생성 호출
        /// </summary>
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
                    { "currentPlayers", 1 }
                },
                CustomRoomPropertiesForLobby = new[] { "roomName", "roomPassword", "currentPlayers" }
            };

            if (LoadingScreenManager.Instance != null)
            {
                // 페이드인 (1s) 후 방 생성 호출
                LoadingScreenManager.Instance.FadeInThenAction(() =>
                {
                    PhotonNetwork.CreateRoom(roomName, roomOptions);
                });
            }
            else
            {
                PhotonNetwork.CreateRoom(roomName, roomOptions);
            }
        }

        /// <summary>
        /// 방 참가
        /// - 변경: 바로 PhotonNetwork.JoinRoom 호출 대신 페이드인 후 참가 호출
        /// </summary>
        public void JoinRoom(string roomName, string password)
        {
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.FadeInThenAction(() =>
                {
                    PhotonNetwork.JoinRoom(roomName);
                });
            }
            else
            {
                PhotonNetwork.JoinRoom(roomName);
            }
        }
        #endregion

        #region Photon Callbacks
        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();

            // 생성자는 페이드가 이미 시작되었으므로(요청시) 바로 씬 로드만 수행
            PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            // 실패 시 필요하면 로딩 UI 강제 해제
            if (LoadingScreenManager.Instance != null)
            {
                // 실패하면 페이드아웃이 되어있지 않을 수 있으므로 즉시 숨김
                // (ShowThenLoadLocal / FadeInThenAction 사용 흐름에서 실패 처리는 호출자에서 적절히 해야 합니다)
            }
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            // 방 참가 성공 시 바로 씬 로드
            PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            // 실패 시 로딩 UI 해제 필요 시 여기에 처리
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);

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

            UpdateRoomListUI();
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
        }
        #endregion

        #region UI Management
        /// <summary>
        /// 방 목록 UI 업데이트
        /// </summary>
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

                if (itemComponent != null)
                {
                    itemComponent.SetInfo(roomInfo);

                    // 방 상태에 따라 스프라이트 설정
                    int currentPlayers = roomInfo.CustomProperties.ContainsKey("currentPlayers") ? (int)roomInfo.CustomProperties["currentPlayers"] : 0;
                    int maxPlayers = roomInfo.MaxPlayers;
                    bool hasPassword = roomInfo.CustomProperties.ContainsKey("roomPassword") && !string.IsNullOrEmpty((string)roomInfo.CustomProperties["roomPassword"]);

                    Sprite selectedSprite;

                    if (currentPlayers >= maxPlayers)
                    {
                        selectedSprite = fullRoomListSprite;
                    }
                    else if (hasPassword)
                    {
                        selectedSprite = lockRoomListSprite;
                    }
                    else
                    {
                        selectedSprite = enableRoomListSprite;
                    }

                    // RoomItem의 Image 컴포넌트 설정
                    if (itemComponent.TryGetComponent(out UnityEngine.UI.Image roomImage))
                    {
                        roomImage.sprite = selectedSprite;
                    }

                    // 방 클릭 시 처리 설정
                    itemComponent.OnClickAction = (string roomName) =>
                    {
                        roomNameInputField.text = roomName;
                    };
                }
            }
        }
        #endregion

        #region Button Events
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
            }
            else
            {
                UpdateRoomListUI();
            }
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
        #endregion
    }
}

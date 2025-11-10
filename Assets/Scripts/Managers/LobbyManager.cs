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
        private bool isProcessingRoomRequest = false; // 중복 요청 방지 플래그
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
        /// 성공 시 OnCreatedRoom()에서 로딩 화면과 함께 씬 전환
        /// </summary>
        public void CreateRoom(string roomName, string roomPassword)
        {
            // 중복 요청 방지
            if (isProcessingRoomRequest)
            {
                return;
            }

            // 방 이름 검증
            if (string.IsNullOrEmpty(roomName))
            {
                SystemMessageManager.Instance?.ShowMessage("RoomNameEmpty");
                return;
            }

            isProcessingRoomRequest = true;

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

            // 로딩 화면 없이 바로 호출 (성공 시 OnCreatedRoom에서 로딩 화면 표시)
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        /// <summary>
        /// 방 참가
        /// 성공 시 OnJoinedRoom()에서 로딩 화면과 함께 씬 전환
        /// </summary>
        public void JoinRoom(string roomName, string password)
        {
            // 중복 요청 방지
            if (isProcessingRoomRequest)
            {
                return;
            }

            // 방 이름 검증
            if (string.IsNullOrEmpty(roomName))
            {
                SystemMessageManager.Instance?.ShowMessage("RoomNameEmpty");
                return;
            }

            // 방이 존재하는지 확인
            if (!roomCache.ContainsKey(roomName))
            {
                SystemMessageManager.Instance?.ShowMessage("RoomNotFound");
                return;
            }

            RoomInfo targetRoom = roomCache[roomName];

            // 방이 가득 찼는지 확인
            if (targetRoom.PlayerCount >= targetRoom.MaxPlayers)
            {
                SystemMessageManager.Instance?.ShowMessage("RoomFull");
                return;
            }

            // 비밀번호 확인
            if (targetRoom.CustomProperties.ContainsKey("roomPassword"))
            {
                string roomPassword = targetRoom.CustomProperties["roomPassword"]?.ToString();
                if (!string.IsNullOrEmpty(roomPassword))
                {
                    // 비밀번호가 설정되어 있으면 검증
                    if (password != roomPassword)
                    {
                        SystemMessageManager.Instance?.ShowMessage("PasswordIncorrect");
                        return;
                    }
                }
            }

            isProcessingRoomRequest = true;

            // 로딩 화면 없이 바로 호출 (성공 시 OnJoinedRoom에서 로딩 화면 표시)
            PhotonNetwork.JoinRoom(roomName);
        }
        #endregion

        #region Photon Callbacks
        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();

            // 방 생성 성공 시 로딩 화면과 함께 씬 전환
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.FadeInThenAction(() =>
                {
                    PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
                });
            }
            else
            {
                PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
            }

            // 씬 전환되므로 플래그 리셋은 불필요 (새 씬에서 자동 초기화)
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);

            // 플래그 리셋
            isProcessingRoomRequest = false;

            // 방 생성 실패 처리 (에러 메시지만 표시)
            if (returnCode == Photon.Realtime.ErrorCode.GameIdAlreadyExists)
            {
                SystemMessageManager.Instance?.ShowMessage("RoomNameDuplicate");
            }
            else
            {
                SystemMessageManager.Instance?.ShowMessage("NetworkError");
            }
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            // 방 참가 성공 시 로딩 화면과 함께 씬 전환
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.FadeInThenAction(() =>
                {
                    PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
                });
            }
            else
            {
                PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
            }

            // 씬 전환되므로 플래그 리셋은 불필요 (새 씬에서 자동 초기화)
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);

            // 플래그 리셋
            isProcessingRoomRequest = false;

            // 방 참가 실패 처리 (에러 메시지만 표시)
            if (returnCode == Photon.Realtime.ErrorCode.GameDoesNotExist)
            {
                SystemMessageManager.Instance?.ShowMessage("RoomNotFound");
            }
            else if (returnCode == Photon.Realtime.ErrorCode.GameFull)
            {
                SystemMessageManager.Instance?.ShowMessage("RoomFull");
            }
            else
            {
                SystemMessageManager.Instance?.ShowMessage("NetworkError");
            }
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

            // 자동 갱신 시에는 메시지 표시하지 않음 (수동 새로고침 시에만 표시)
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
                // 수동 새로고침: UI 업데이트 및 메시지 표시
                UpdateRoomListUI();
                SystemMessageManager.Instance?.ShowMessage("RoomListUpdated");
            }
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        public async void OnClickLogOut()
        {
            // 로그아웃 시작 메시지
            SystemMessageManager.Instance?.ShowMessage("LoggingOut");

            // 세션 정리 (Firebase UID 기반)
            if (SessionManager.Instance != null && AuthManager.Instance != null)
            {
                string uid = AuthManager.Instance.CurrentUserUID;
                if (!string.IsNullOrEmpty(uid))
                {
                    await SessionManager.Instance.ClearSession(uid);
                    Debug.Log("[LobbyManager] 세션 정리 완료");
                }
            }

            // Firebase 로그아웃
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.Logout();
                Debug.Log("[LobbyManager] Firebase 로그아웃 완료");
            }

            // Photon 연결 해제
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                Debug.Log("[LobbyManager] Photon 연결 해제");
            }

            // 로그아웃 완료 메시지
            SystemMessageManager.Instance?.ShowMessage("LogoutComplete");

            // JoinScene으로 이동
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.FadeInThenAction(() =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                });
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
            }
        }
        #endregion
    }
}

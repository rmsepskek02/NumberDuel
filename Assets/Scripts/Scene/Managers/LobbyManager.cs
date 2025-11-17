using Objects;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using Utills;

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

        [Header("빠른 매칭")]
        public UnityEngine.UI.Button quickMatchButton;
        public TextMeshProUGUI quickMatchButtonText;
        public GameObject matchingIcon; // 회전 아이콘

        private Dictionary<string, RoomInfo> roomCache = new Dictionary<string, RoomInfo>();
        private string roomNameText;
        private string roomPasswordText;
        private bool isProcessingRoomRequest = false; // 중복 요청 방지 플래그
        private Tweener rotationTweener; // 회전 애니메이션 Tweener
        private float lastQuickMatchClickTime = -1f; // 마지막 빠른 매칭 버튼 클릭 시간
        private const float QUICK_MATCH_COOLDOWN = 1f; // 빠른 매칭 버튼 쿨다운 (1초)
        private Coroutine buttonCooldownCoroutine = null; // 버튼 쿨다운 코루틴
        private Coroutine typingEffectCoroutine = null; // 타이핑 효과 코루틴
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // 모든 버튼에 클릭 사운드 자동 등록
            UIHelper.RegisterAllButtonSounds();

            OnConnectedToMaster();
            InitializeMatchmaking();
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
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);

            // 매칭 방 생성 실패인지 확인
            if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.IsSearching)
            {
                MatchmakingManager.Instance.OnCreateMatchmakingRoomFailed(returnCode, message);
                return;
            }

            // 일반 방 생성 실패 처리
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

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            base.OnJoinRandomFailed(returnCode, message);

            // MatchmakingManager에게 실패 알림
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnJoinRandomMatchmakingFailed(returnCode, message);
            }
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();

            // 일반 방 생성인지 매칭 방 생성인지 확인
            if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.IsSearching)
            {
                // 매칭 방 생성 성공
                MatchmakingManager.Instance.OnCreatedMatchmakingRoom();
            }
            else
            {
                // 일반 방 생성 성공 시 로딩 화면과 함께 씬 전환
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
            }
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            // 매칭 성공 사운드 재생
            SoundManager.Instance?.PlaySFX(SoundType.UI_MatchFound);

            // 매칭 방 입장인지 일반 방 입장인지 확인
            if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.IsSearching)
            {
                // 매칭 방 입장 성공
                MatchmakingManager.Instance.OnJoinedMatchmakingRoom();
            }
            else
            {
                // 일반 방 참가 성공 시 로딩 화면과 함께 씬 전환
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
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);

            // MatchmakingManager에게 알림
            if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.IsSearching)
            {
                MatchmakingManager.Instance.OnPlayerEnteredMatchmakingRoom(newPlayer);
            }
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();

            // 매칭 취소로 인한 방 나가기인지 확인
            bool isMatchmakingCancel = MatchmakingManager.Instance != null &&
                                       (MatchmakingManager.Instance.CurrentState == Objects.MatchmakingState.Searching ||
                                        MatchmakingManager.Instance.CurrentState == Objects.MatchmakingState.Idle);

            // MatchmakingManager에게 알림
            if (isMatchmakingCancel)
            {
                MatchmakingManager.Instance.OnLeftMatchmakingRoom();
                // 매칭 취소는 씬 전환하지 않고 현재 로비에 유지
                Debug.Log("[LobbyManager] 매칭 취소로 인한 방 나가기 - 씬 전환 없음");
                return;
            }

            // 일반 방 나가기인 경우에만 로비로 돌아감
            // (게임 중에 방을 나가는 경우)
            Debug.Log("[LobbyManager] 일반 방 나가기 - 로비로 씬 전환");
            if (LoadingScreenManager.Instance != null)
            {
                // Lobby는 로컬 전환이면 usePhoton = false로 호출
                LoadingScreenManager.Instance.ShowThenLoadLocal(SceneNameExtensions.GetSceneName(SceneName.LobbyScene));
            }
            else
            {
                // 폴백: 기존 동작
                PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.LobbyScene));
            }
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
        }
        #endregion

        #region Matchmaking
        /// <summary>
        /// 매칭 시스템 초기화
        /// </summary>
        private void InitializeMatchmaking()
        {
            // MatchmakingManager 이벤트 구독
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnMatchmakingStateChanged += OnMatchmakingStateChanged;
            }

            // 초기 UI 상태 설정
            UpdateQuickMatchButton(Objects.MatchmakingState.Idle);
        }

        /// <summary>
        /// 매칭 상태 변경 시 호출
        /// </summary>
        private void OnMatchmakingStateChanged(Objects.MatchmakingState newState)
        {
            UpdateQuickMatchButton(newState);
        }

        /// <summary>
        /// 빠른 매칭 버튼 UI 업데이트
        /// </summary>
        private void UpdateQuickMatchButton(Objects.MatchmakingState state)
        {
            if (quickMatchButtonText == null)
            {
                Debug.LogWarning("[LobbyManager] quickMatchButtonText가 할당되지 않았습니다.");
                return;
            }

            // 기존 타이핑 효과 중지
            if (typingEffectCoroutine != null)
            {
                StopCoroutine(typingEffectCoroutine);
                typingEffectCoroutine = null;
            }

            switch (state)
            {
                case Objects.MatchmakingState.Idle:
                    typingEffectCoroutine = StartCoroutine(TypeText("빠른 매칭"));
                    StopMatchingIconRotation();
                    // 버튼 활성화는 쿨다운 후에만 (쿨다운 코루틴에서 처리)
                    break;

                case Objects.MatchmakingState.Searching:
                    typingEffectCoroutine = StartCoroutine(TypeText("매칭 취소"));
                    StartMatchingIconRotation();
                    // 버튼 활성화는 쿨다운 후에만 (쿨다운 코루틴에서 처리)
                    break;

                case Objects.MatchmakingState.Matched:
                    // 매칭 완료 시 버튼 비활성화 (씬 전환 중)
                    if (quickMatchButton != null) quickMatchButton.interactable = false;
                    StopMatchingIconRotation();
                    break;
            }
        }

        /// <summary>
        /// 텍스트 타이핑 효과
        /// </summary>
        private System.Collections.IEnumerator TypeText(string targetText)
        {
            if (quickMatchButtonText == null)
            {
                yield break;
            }

            quickMatchButtonText.text = "";
            float charDelay = QUICK_MATCH_COOLDOWN / targetText.Length; // 1초 동안 모든 글자 출력

            foreach (char c in targetText)
            {
                quickMatchButtonText.text += c;
                yield return new WaitForSeconds(charDelay);
            }

            typingEffectCoroutine = null;
        }

        /// <summary>
        /// 매칭 아이콘 회전 애니메이션 시작
        /// </summary>
        private void StartMatchingIconRotation()
        {
            if (matchingIcon == null)
            {
                Debug.LogWarning("[LobbyManager] matchingIcon이 할당되지 않았습니다.");
                return;
            }

            // 기존 회전 애니메이션이 있으면 중지만 (아이콘은 비활성화하지 않음)
            if (rotationTweener != null)
            {
                rotationTweener.Kill();
                rotationTweener = null;
            }

            // 회전 초기화
            matchingIcon.transform.rotation = Quaternion.identity;

            // 아이콘 활성화
            matchingIcon.SetActive(true);

            // 회전 애니메이션 시작 (무한 반복)
            // Z축 기준 -360도 회전 (시계방향), 1초에 1회전
            rotationTweener = matchingIcon.transform
                .DORotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);

            Debug.Log("[LobbyManager] 매칭 아이콘 회전 시작");
        }

        /// <summary>
        /// 매칭 아이콘 회전 애니메이션 중지
        /// </summary>
        private void StopMatchingIconRotation()
        {
            if (matchingIcon == null)
            {
                return;
            }

            // 회전 애니메이션 중지
            if (rotationTweener != null)
            {
                rotationTweener.Kill();
                rotationTweener = null;
            }

            // 회전 초기화 (0도로 리셋)
            matchingIcon.transform.rotation = Quaternion.identity;

            // 아이콘 비활성화
            matchingIcon.SetActive(false);

            Debug.Log("[LobbyManager] 매칭 아이콘 회전 중지");
        }

        private void OnDestroy()
        {
            // 회전 애니메이션 정리
            StopMatchingIconRotation();

            // 코루틴 정리
            if (buttonCooldownCoroutine != null)
            {
                StopCoroutine(buttonCooldownCoroutine);
                buttonCooldownCoroutine = null;
            }

            if (typingEffectCoroutine != null)
            {
                StopCoroutine(typingEffectCoroutine);
                typingEffectCoroutine = null;
            }

            // 이벤트 구독 해제
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnMatchmakingStateChanged -= OnMatchmakingStateChanged;
            }
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
                // 매칭 전용 방은 목록에 표시하지 않음
                if (roomInfo.CustomProperties.ContainsKey("isMatchmaking"))
                {
                    bool isMatchmaking = (bool)roomInfo.CustomProperties["isMatchmaking"];
                    if (isMatchmaking)
                    {
                        continue; // 매칭 방은 스킵
                    }
                }

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

        /// <summary>
        /// 빠른 매칭 버튼 클릭
        /// </summary>
        public void OnClickQuickMatch()
        {
            if (MatchmakingManager.Instance == null)
            {
                Debug.LogError("[LobbyManager] MatchmakingManager를 찾을 수 없습니다.");
                SystemMessageManager.Instance?.ShowMessage("NetworkError");
                return;
            }

            // 쿨다운 체크
            float timeSinceLastClick = Time.time - lastQuickMatchClickTime;
            if (lastQuickMatchClickTime >= 0 && timeSinceLastClick < QUICK_MATCH_COOLDOWN)
            {
                Debug.Log($"[LobbyManager] 버튼 쿨다운 중: {QUICK_MATCH_COOLDOWN - timeSinceLastClick:F1}초 남음");
                return;
            }

            // 마지막 클릭 시간 업데이트
            lastQuickMatchClickTime = Time.time;

            // 버튼 비활성화 및 쿨다운 시작
            if (quickMatchButton != null)
            {
                quickMatchButton.interactable = false;
            }

            if (buttonCooldownCoroutine != null)
            {
                StopCoroutine(buttonCooldownCoroutine);
            }
            buttonCooldownCoroutine = StartCoroutine(EnableQuickMatchButtonAfterDelay());

            // 현재 상태에 따라 분기
            if (MatchmakingManager.Instance.IsSearching)
            {
                // 매칭 취소
                MatchmakingManager.Instance.CancelMatchmaking();
            }
            else
            {
                // 매칭 시작
                MatchmakingManager.Instance.StartMatchmaking();
            }
        }

        /// <summary>
        /// 1초 후 빠른 매칭 버튼 재활성화
        /// </summary>
        private System.Collections.IEnumerator EnableQuickMatchButtonAfterDelay()
        {
            yield return new WaitForSeconds(QUICK_MATCH_COOLDOWN);

            if (quickMatchButton != null)
            {
                quickMatchButton.interactable = true;
            }

            buttonCooldownCoroutine = null;
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

        public void OnClickLogOut()
        {
            StartCoroutine(LogoutCoroutine());
        }

        private System.Collections.IEnumerator LogoutCoroutine()
        {
            // 로그아웃 시작 메시지
            SystemMessageManager.Instance?.ShowMessage("LoggingOut");

            // Firebase 로그아웃 (AuthManager에서 세션 정리도 함께 수행)
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.Logout();
                // async void 메서드이므로 완료 대기
                yield return new WaitForSeconds(0.5f);
                Debug.Log("[LobbyManager] Firebase 로그아웃 및 세션 정리 완료");
            }

            // Photon 연결 해제
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                Debug.Log("[LobbyManager] Photon 연결 해제");
            }

            // 로그아웃 완료 메시지
            SystemMessageManager.Instance?.ShowMessage("LogoutComplete");

            yield return new WaitForSeconds(0.3f);

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

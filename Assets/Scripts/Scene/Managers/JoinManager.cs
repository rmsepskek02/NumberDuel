using Objects;
using Objects.Data;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utills;

namespace Manager
{
    /// <summary>
    /// Login 화면을 관리하는 매니저
    /// Firebase 인증 및 Photon 네트워크 연결 담당
    /// </summary>
    public class JoinManager : MonoBehaviourPunCallbacks
    {
        #region Fields and Properties
        [Header("UI Elements")]
        public TMP_InputField inputEmail;
        public TMP_InputField inputPassword;
        public TMP_InputField inputNickname;
        public Button actionButton;
        public Button toggleModeButton;
        public TextMeshProUGUI actionButtonText;
        public TextMeshProUGUI toggleModeButtonText;
        public GameObject nicknameLabel; // 회원가입 시에만 표시
        public GameObject nicknameInputField; // 회원가입 시에만 표시

        [Header("Mode Settings")]
        private bool isLoginMode = true; // true: 로그인, false: 회원가입

        private bool isProcessing = false;
        private Coroutine photonTimeoutCoroutine = null;

        // Photon 연결 상태 추적
        private bool isTrackingPhotonConnection = false;
        private float photonConnectionStartTime = 0f;
        private float loadingScreenStartTime = 0f; // 로딩스크린 시작 시간
        private Photon.Realtime.ClientState lastClientState;

        private const float PHOTON_CONNECT_TIMEOUT = 10f; // Photon 연결 타임아웃 (10초)
        private const float TASK_TIMEOUT = 10f; // Firebase Task 타임아웃 (10초)
        private const float MIN_LOADING_DURATION = 1.5f; // 최소 로딩 시간 (페이드인 + 여유)
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // 로딩스크린 안전장치: JoinScene 진입 시 혹시 남아있는 로딩스크린 페이드아웃
            if (LoadingScreenManager.Instance != null)
            {
                // 약간의 딜레이 후 페이드아웃 (OnSceneLoaded가 실행되지 않았을 경우 대비)
                StartCoroutine(EnsureLoadingScreenHidden());
            }

            // 모든 버튼에 클릭 사운드 자동 등록
            UIHelper.RegisterAllButtonSounds();

            // 한국 리전 설정
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";

            // 서버 연결 (AppId, 버전, 서버에 요청)
            PhotonNetwork.ConnectUsingSettings();

            // Photon 연결 타임아웃 체크 시작
            photonTimeoutCoroutine = StartCoroutine(CheckPhotonConnectionTimeout());

            // UI 초기화
            SetLoginMode(true);

            // 버튼 이벤트는 Unity Editor에서 등록
        }

        /// <summary>
        /// 로딩스크린이 제대로 숨겨지지 않았을 경우 강제로 숨김
        /// </summary>
        private System.Collections.IEnumerator EnsureLoadingScreenHidden()
        {
            // 0.5초 대기 (OnSceneLoaded가 실행될 시간)
            yield return new WaitForSeconds(0.5f);

            // 로딩스크린 강제 숨김 (안전장치)
            var loadingManager = LoadingScreenManager.Instance;
            if (loadingManager != null)
            {
                loadingManager.ForceHide();
                Debug.Log("[JoinManager] 로딩스크린 안전장치 실행: ForceHide() 호출");
            }
        }

        void Update()
        {
            // Photon 연결 상태 추적 중일 때만
            if (!isTrackingPhotonConnection)
                return;

            var currentState = PhotonNetwork.NetworkClientState;

            // 상태가 변경되었을 때만 업데이트
            if (currentState != lastClientState)
            {
                lastClientState = currentState;
                UpdatePhotonConnectionProgress(currentState);
            }

            // 타임아웃 체크
            float elapsedTime = Time.time - photonConnectionStartTime;
            if (elapsedTime > PHOTON_CONNECT_TIMEOUT)
            {
                Debug.LogError($"[JoinManager] Photon 연결 타임아웃 감지! 경과 시간: {elapsedTime}초");
                OnPhotonConnectionTimeout();
            }
        }

        /// <summary>
        /// 앱이 포그라운드로 돌아올 때 인증 상태 체크 (자동 새로고침)
        /// 사용자가 이메일 앱에서 인증 링크를 클릭한 후 게임으로 돌아올 때 감지
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
            {
                _ = CheckEmailVerificationStatus();
            }
        }

        void OnDestroy()
        {
            // 코루틴 정리
            if (photonTimeoutCoroutine != null)
            {
                StopCoroutine(photonTimeoutCoroutine);
                photonTimeoutCoroutine = null;
            }
        }
        #endregion

        #region Photon Callbacks
        /// <summary>
        /// 서버 연결 완료 시점에 호출 (Lobby에 진입한 후 가능 상황)
        /// </summary>
        public override void OnConnected()
        {
            base.OnConnected();
            // 서버 연결 메시지는 표시하지 않음 (너무 많은 메시지 방지)
        }

        /// <summary>
        /// 서버와 마스터 연결 성공 시점에 호출 (Lobby에 진입할 수 있는 상황 후 첫 호출가능)
        /// </summary>
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();

            // Photon 연결 성공 시 타임아웃 코루틴 중지
            if (photonTimeoutCoroutine != null)
            {
                StopCoroutine(photonTimeoutCoroutine);
                photonTimeoutCoroutine = null;
                Debug.Log("[JoinManager] Photon 연결 성공");
            }
        }

        /// <summary>
        /// 로비 진입 성공 시점에 호출
        /// </summary>
        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();

            Debug.Log("[JoinManager] OnJoinedLobby 호출됨");

            // Photon 연결 추적 중지
            isTrackingPhotonConnection = false;

            // 최소 로딩 시간 보장을 위한 코루틴 시작
            StartCoroutine(TransitionToLobbyAfterMinDelay());
        }

        /// <summary>
        /// 최소 로딩 시간을 보장한 후 LobbyScene으로 전환
        /// </summary>
        private System.Collections.IEnumerator TransitionToLobbyAfterMinDelay()
        {
            // 로딩스크린 진행률 100% 업데이트
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.UpdateProgress(1f, "완료!");
            }

            // 최소 로딩 시간이 지났는지 확인
            float elapsedTime = Time.time - loadingScreenStartTime;
            float remainingTime = MIN_LOADING_DURATION - elapsedTime;

            if (remainingTime > 0)
            {
                Debug.Log($"[JoinManager] 최소 로딩 시간 대기 중... ({remainingTime:F2}초)");
                yield return new WaitForSeconds(remainingTime);
            }

            Debug.Log("[JoinManager] LobbyScene으로 전환");

            // 씬 전환
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.LobbyScene));
        }
        #endregion

        #region Photon Connection Tracking
        /// <summary>
        /// Photon 연결 상태에 따라 로딩 진행률 업데이트
        /// </summary>
        private void UpdatePhotonConnectionProgress(Photon.Realtime.ClientState state)
        {
            if (LoadingScreenManager.Instance == null)
                return;

            float progress = 0f;
            string statusMessage = "";

            switch (state)
            {
                case Photon.Realtime.ClientState.PeerCreated:
                    progress = 0.1f;
                    statusMessage = "서버 연결 준비 중...";
                    break;

                case Photon.Realtime.ClientState.Authenticating:
                    progress = 0.33f;
                    statusMessage = "인증 중...";
                    break;

                case Photon.Realtime.ClientState.Authenticated:
                    progress = 0.66f;
                    statusMessage = "인증 완료...";
                    break;

                case Photon.Realtime.ClientState.JoiningLobby:
                    progress = 0.9f;
                    statusMessage = "로비 입장 중...";
                    break;

                case Photon.Realtime.ClientState.JoinedLobby:
                    progress = 1f;
                    statusMessage = "완료!";
                    break;

                case Photon.Realtime.ClientState.ConnectedToMasterServer:
                case Photon.Realtime.ClientState.ConnectedToGameServer:
                case Photon.Realtime.ClientState.ConnectedToNameServer:
                    progress = 0.5f;
                    statusMessage = "서버 연결 중...";
                    break;

                case Photon.Realtime.ClientState.Disconnecting:
                case Photon.Realtime.ClientState.Disconnected:
                    progress = 0f;
                    statusMessage = "연결 실패...";
                    break;

                default:
                    progress = 0.2f;
                    statusMessage = "연결 중...";
                    break;
            }

            LoadingScreenManager.Instance.UpdateProgress(progress, statusMessage);
            Debug.Log($"[JoinManager] Photon 상태: {state} ({progress * 100}%)");
        }

        /// <summary>
        /// Photon 연결 타임아웃 처리
        /// </summary>
        private void OnPhotonConnectionTimeout()
        {
            isTrackingPhotonConnection = false;

            Debug.LogError($"[JoinManager] Photon 연결 타임아웃 ({PHOTON_CONNECT_TIMEOUT}초)");

            // 로딩스크린 페이드아웃
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.FadeOutManually();
            }

            // 시스템 메시지 표시
            SystemMessageManager.Instance?.ShowMessage("ConnectionFailed");

            // 버튼 활성화
            SetButtonsInteractable(true);
        }
        #endregion

        #region Button Events
        /// <summary>
        /// 로그인/회원가입 버튼 클릭
        /// </summary>
        public async void OnClickActionButton()
        {
            if (isProcessing) return;

            string email = inputEmail?.text ?? "";
            string password = inputPassword?.text ?? "";
            string nickname = inputNickname?.text ?? "";

            // 입력 검증
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SystemMessageManager.Instance?.ShowMessage("InputEmailPassword");
                return;
            }

            if (!isLoginMode && string.IsNullOrEmpty(nickname))
            {
                SystemMessageManager.Instance?.ShowMessage("InputNickname");
                return;
            }

            isProcessing = true;
            SetButtonsInteractable(false);

            try
            {
                // ✅ 1단계: Firebase 초기화 대기
                if (!AuthManager.Instance.IsInitialized)
                {
                    SystemMessageManager.Instance?.ShowMessage("InitializingFirebase");
                    Debug.Log("[JoinManager] Firebase 초기화 대기 중...");

                    bool authReady = await AuthManager.Instance.WaitForInitialization(10f);
                    if (!authReady)
                    {
                        // 초기화 실패 시 재시도
                        Debug.Log("[JoinManager] Firebase 재초기화 시도...");
                        AuthManager.Instance.RetryInitialization();

                        // 재시도 후 다시 대기
                        authReady = await AuthManager.Instance.WaitForInitialization(10f);
                        if (!authReady)
                        {
                            SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                            Debug.LogError("[JoinManager] AuthManager 초기화 재시도 실패");
                            return;
                        }
                    }

                    Debug.Log("[JoinManager] AuthManager 초기화 완료");
                }

                if (!SessionManager.Instance.IsInitialized)
                {
                    bool sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                    if (!sessionReady)
                    {
                        // 초기화 실패 시 재시도
                        Debug.Log("[JoinManager] SessionManager 재초기화 시도...");
                        SessionManager.Instance.RetryInitialization();

                        // 재시도 후 다시 대기
                        sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                        if (!sessionReady)
                        {
                            SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                            Debug.LogError("[JoinManager] SessionManager 초기화 재시도 실패");
                            return;
                        }
                    }

                    Debug.Log("[JoinManager] SessionManager 초기화 완료");
                }

                // ✅ 2단계: 기존 로그인 로직 실행
                if (isLoginMode)
                {
                    // 로그인 처리
                    SystemMessageManager.Instance?.ShowMessage("LoggingIn");
                    var result = await AuthManager.Instance.LoginWithEmail(email, password);

                    if (result.success)
                    {
                        // ✅ 이메일 인증 체크 (테스트 계정 제외)
                        if (!AuthManager.Instance.IsEmailVerified && !IsTestAccount(email))
                        {
                            // ⚠️ 미인증 경고 팝업
                            UI.Shared.ConfirmationPopup.Show(
                                "이메일 인증이 필요합니다.\n\n" +
                                $"{email}으로 발송된\n" +
                                "이메일의 인증 링크를 클릭해주세요.\n\n" +
                                "이메일을 받지 못하셨나요?",
                                onConfirm: async () =>
                                {
                                    // [재발송] 버튼: 인증 이메일 재발송
                                    await ResendVerificationEmail();
                                },
                                onCancel: () =>
                                {
                                    // [닫기] 버튼: 아무 동작 안 함
                                },
                                confirmText: "재발송",
                                cancelText: "닫기"
                            );

                            // Firebase에서 로그아웃 (인증 완료 전까지 로그인 불가)
                            AuthManager.Instance.SignOutWithoutSessionClear();
                            return;
                        }

                        // 세션 체크 (중복 로그인 확인)
                        string uid = AuthManager.Instance.CurrentUserUID;
                        var sessionCheck = await SessionManager.Instance.CheckSession(uid);

                        if (sessionCheck.isDuplicate)
                        {
                            // 중복 로그인 감지 - 팝업으로 사용자에게 선택권 제공
                            UI.Shared.ConfirmationPopup.Show(
                                "이미 로그인 중인 계정입니다.\n기존 접속을 해제하고 로그인하시겠습니까?",
                                onConfirm: async () =>
                                {
                                    // 확인 클릭: 기존 세션 강제 종료 후 로그인
                                    SystemMessageManager.Instance?.ShowMessage("ForceLoginInProgress");

                                    bool forceLoginSuccess = await SessionManager.Instance.ForceLogin(uid);

                                    if (forceLoginSuccess)
                                    {
                                        // 세션 모니터링 시작 (다른 곳에서 로그인 시 감지)
                                        SessionManager.Instance.StartSessionMonitoring(uid);

                                        // 로그인 성공 - 이후 프로세스 계속 진행
                                        SystemMessageManager.Instance?.ShowMessage("LoginSuccess");
                                        await System.Threading.Tasks.Task.Delay(500);
                                        StartCoroutine(OnLoginSuccessCoroutine());
                                    }
                                    else
                                    {
                                        SystemMessageManager.Instance?.ShowMessage("SessionCreateFailed");
                                        AuthManager.Instance.Logout();
                                    }

                                    // 처리 완료 후 플래그 초기화
                                    isProcessing = false;
                                    SetButtonsInteractable(true);
                                },
                                onCancel: () =>
                                {
                                    // 취소 클릭: Firebase에서만 로그아웃 (Firestore 세션은 유지)
                                    // Client A의 세션을 삭제하지 않음
                                    AuthManager.Instance.SignOutWithoutSessionClear();

                                    // 로그인 취소 메시지 표시
                                    SystemMessageManager.Instance?.ShowMessage("LoginCanceled");

                                    isProcessing = false;
                                    SetButtonsInteractable(true);
                                }
                            );
                            return;
                        }

                        // 세션 생성
                        bool sessionCreated = await SessionManager.Instance.CreateSession(uid);

                        if (!sessionCreated)
                        {
                            SystemMessageManager.Instance?.ShowMessage("SessionCreateFailed");
                            AuthManager.Instance.Logout();
                            return;
                        }

                        // 세션 모니터링 시작 (다른 곳에서 로그인 시 감지)
                        SessionManager.Instance.StartSessionMonitoring(uid);

                        SystemMessageManager.Instance?.ShowMessage("LoginSuccess");
                        await System.Threading.Tasks.Task.Delay(500); // 0.5초 대기

                        // Coroutine으로 프로필 로드 및 로비 진입
                        StartCoroutine(OnLoginSuccessCoroutine());
                    }
                    else
                    {
                        // Firebase 인증 에러 - 커스텀 메시지로 표시
                        SystemMessageManager.Instance?.ShowMessage(result.message, MessageType.Error);
                    }
                }
                else
                {
                    // 회원가입 처리
                    SystemMessageManager.Instance?.ShowMessage("Registering");
                    var result = await AuthManager.Instance.RegisterWithEmail(email, password);

                    if (result.success)
                    {
                        // ✅ 인증 이메일 발송
                        var verificationResult = await AuthManager.Instance.SendVerificationEmail();

                        if (verificationResult.success)
                        {
                            // ✅ 회원가입 성공 및 인증 이메일 발송 완료 팝업
                            UI.Shared.ConfirmationPopup.Show(
                                "회원가입이 완료되었습니다!\n\n" +
                                $"{email}으로\n" +
                                "인증 이메일을 발송했습니다.\n\n" +
                                "이메일의 인증 링크를 클릭하면\n" +
                                "로그인할 수 있습니다.",
                                onConfirm: () =>
                                {
                                    // [확인] 버튼: 로그인 모드로 전환
                                    SetLoginMode(true);
                                },
                                onCancel: async () =>
                                {
                                    // [재발송] 버튼: 인증 이메일 재발송
                                    await ResendVerificationEmail();
                                },
                                confirmText: "확인",
                                cancelText: "재발송"
                            );
                        }
                        else
                        {
                            // ⚠️ 인증 이메일 발송 실패 - 재시도 권장
                            SystemMessageManager.Instance?.ShowMessage(
                                verificationResult.message + "\n잠시 후 다시 시도해주세요",
                                MessageType.Error,
                                4f
                            );
                        }
                    }
                    else
                    {
                        // Firebase 인증 에러 - 커스텀 메시지로 표시
                        SystemMessageManager.Instance?.ShowMessage(result.message, MessageType.Error);
                    }
                }
            }
            finally
            {
                isProcessing = false;
                SetButtonsInteractable(true);
            }
        }

        /// <summary>
        /// 모드 전환 버튼 클릭 (로그인 ↔ 회원가입)
        /// </summary>
        public void OnClickToggleModeButton()
        {
            SetLoginMode(!isLoginMode);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Photon 연결 타임아웃 체크
        /// </summary>
        private IEnumerator CheckPhotonConnectionTimeout()
        {
            yield return new WaitForSeconds(PHOTON_CONNECT_TIMEOUT);

            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogError($"⏱️ Photon 연결 타임아웃 ({PHOTON_CONNECT_TIMEOUT}초)");
                SystemMessageManager.Instance?.ShowMessage("PhotonConnectionTimeout");
            }

            photonTimeoutCoroutine = null;
        }

        /// <summary>
        /// Task에 타임아웃 기능 추가
        /// </summary>
        private async Task<T> RunWithTimeout<T>(Task<T> task, float timeoutSeconds, string operationName)
        {
            var timeoutTask = Task.Delay(System.TimeSpan.FromSeconds(timeoutSeconds));
            var completedTask = await Task.WhenAny(task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Debug.LogError($"⏱️ {operationName} 타임아웃 ({timeoutSeconds}초)");
                throw new System.TimeoutException($"{operationName} 타임아웃");
            }

            return await task;
        }

        /// <summary>
        /// 로그인 성공 후 처리 (Coroutine 버전 - 타임아웃 적용)
        /// </summary>
        private IEnumerator OnLoginSuccessCoroutine()
        {
            string uid = AuthManager.Instance.CurrentUserUID;
            string email = AuthManager.Instance.CurrentUserEmail;

            // 로딩스크린 활성화 및 시작 시간 기록
            loadingScreenStartTime = Time.time;

            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowManual("프로필 로드 중...");
                yield return null; // 페이드인 시작을 위한 한 프레임 대기
            }

            // 프로필 존재 여부 확인 (타임아웃 적용)
            var checkTask = DatabaseManager.Instance.UserProfileExists(uid);
            float elapsedTime = 0f;

            while (!checkTask.IsCompleted && elapsedTime < TASK_TIMEOUT)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }

            if (!checkTask.IsCompleted)
            {
                Debug.LogError($"⏱️ 프로필 존재 확인 타임아웃 ({TASK_TIMEOUT}초)");
                SystemMessageManager.Instance?.ShowMessage("NetworkTimeout");
                SetButtonsInteractable(true);
                yield break;
            }

            bool profileExists = checkTask.Result;
            UserProfile profile = null;

            if (!profileExists)
            {
                // 프로필이 없으면 생성 (기존 계정의 경우)
                SystemMessageManager.Instance?.ShowMessage("CreatingProfile");
                string defaultNickname = email.Split('@')[0]; // 이메일 앞부분을 닉네임으로

                var createTask = DatabaseManager.Instance.CreateUserProfile(uid, email, defaultNickname);
                elapsedTime = 0f;

                while (!createTask.IsCompleted && elapsedTime < TASK_TIMEOUT)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedTime += 0.1f;
                }

                if (!createTask.IsCompleted)
                {
                    Debug.LogError($"⏱️ 프로필 생성 타임아웃 ({TASK_TIMEOUT}초)");
                    SystemMessageManager.Instance?.ShowMessage("NetworkTimeout");
                    SetButtonsInteractable(true);
                    yield break;
                }

                var loadTask = DatabaseManager.Instance.GetUserProfile(uid);
                elapsedTime = 0f;

                while (!loadTask.IsCompleted && elapsedTime < TASK_TIMEOUT)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedTime += 0.1f;
                }

                if (!loadTask.IsCompleted)
                {
                    Debug.LogError($"⏱️ 프로필 로드 타임아웃 ({TASK_TIMEOUT}초)");
                    SystemMessageManager.Instance?.ShowMessage("NetworkTimeout");
                    SetButtonsInteractable(true);
                    yield break;
                }

                profile = loadTask.Result;
            }
            else
            {
                // 프로필 로드 (타임아웃 적용)
                var loadTask = DatabaseManager.Instance.GetUserProfile(uid);
                elapsedTime = 0f;

                while (!loadTask.IsCompleted && elapsedTime < TASK_TIMEOUT)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedTime += 0.1f;
                }

                if (!loadTask.IsCompleted)
                {
                    Debug.LogError($"⏱️ 프로필 로드 타임아웃 ({TASK_TIMEOUT}초)");
                    SystemMessageManager.Instance?.ShowMessage("NetworkTimeout");
                    SetButtonsInteractable(true);
                    yield break;
                }

                profile = loadTask.Result;
            }

            if (profile != null)
            {
                // 마지막 로그인 시간 업데이트 (타임아웃 적용)
                var updateTask = DatabaseManager.Instance.UpdateLastLogin(uid);
                elapsedTime = 0f;

                while (!updateTask.IsCompleted && elapsedTime < TASK_TIMEOUT)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedTime += 0.1f;
                }

                // 업데이트 실패해도 로그인은 진행 (치명적 아님)
                if (!updateTask.IsCompleted)
                {
                    Debug.LogWarning($"⏱️ 마지막 로그인 업데이트 타임아웃 ({TASK_TIMEOUT}초) - 무시하고 진행");
                }

                // Photon 닉네임 설정
                PhotonNetwork.NickName = profile.Nickname;

                // Photon Custom Property에 Firebase UID 저장
                ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "FirebaseUID", uid }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

                Debug.Log($"[JoinManager] Photon 상태: {PhotonNetwork.NetworkClientState}, InLobby: {PhotonNetwork.InLobby}");

                // 이미 로비에 있는지 확인
                if (PhotonNetwork.InLobby)
                {
                    Debug.Log("[JoinManager] 이미 로비에 있음 - 바로 LobbyScene 전환");

                    if (LoadingScreenManager.Instance != null)
                    {
                        LoadingScreenManager.Instance.UpdateProgress(1f, "완료!");
                    }

                    UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.LobbyScene));
                    yield break;
                }

                // Photon 연결 추적 시작
                isTrackingPhotonConnection = true;
                photonConnectionStartTime = Time.time;
                lastClientState = PhotonNetwork.NetworkClientState;

                Debug.Log($"[JoinManager] Photon 연결 추적 시작 - 현재 상태: {lastClientState}");

                // 로딩스크린 상태 업데이트
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.UpdateProgress(0.1f, "로비 연결 준비 중...");
                }

                // Photon 로비 진입
                PhotonNetwork.JoinLobby();

                SetButtonsInteractable(true);
            }
            else
            {
                // 로딩스크린 숨김
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.FadeOutManually();
                }

                SystemMessageManager.Instance?.ShowMessage("ProfileLoadFailed");
                SetButtonsInteractable(true);
            }
        }

        /// <summary>
        /// UI 모드 설정 (로그인/회원가입)
        /// </summary>
        private void SetLoginMode(bool isLogin)
        {
            isLoginMode = isLogin;

            // 닉네임 필드 표시/숨김
            if (nicknameLabel != null)
                nicknameLabel.SetActive(!isLoginMode);

            if (nicknameInputField != null)
                nicknameInputField.SetActive(!isLoginMode);

            // 버튼 텍스트 변경
            if (actionButtonText != null)
                actionButtonText.text = isLoginMode ? "로그인" : "회원가입";

            if (toggleModeButtonText != null)
                toggleModeButtonText.text = isLoginMode ? "회원가입" : "로그인";
        }

        /// <summary>
        /// 버튼 활성/비활성 설정
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (actionButton != null)
                actionButton.interactable = interactable;

            if (toggleModeButton != null)
                toggleModeButton.interactable = interactable;
        }

        /// <summary>
        /// 이메일 인증 상태 확인 (자동 새로고침)
        /// </summary>
        private async Task CheckEmailVerificationStatus()
        {
            await AuthManager.Instance.ReloadUserInfo();

            if (AuthManager.Instance.IsEmailVerified)
            {
                SystemMessageManager.Instance?.ShowMessage(
                    "이메일 인증이 완료되었습니다!",
                    MessageType.Success
                );
            }
        }

        /// <summary>
        /// 인증 이메일 재발송
        /// </summary>
        private async Task ResendVerificationEmail()
        {
            // 입력 필드에서 이메일/비밀번호 가져오기
            string email = inputEmail.text.Trim();
            string password = inputPassword.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SystemMessageManager.Instance?.ShowMessage(
                    "이메일과 비밀번호를 입력해주세요",
                    MessageType.Error
                );
                return;
            }

            // 로그인 상태가 아니라면 재로그인
            if (!AuthManager.Instance.IsLoggedIn)
            {
                var loginResult = await AuthManager.Instance.LoginWithEmail(email, password);

                if (!loginResult.success)
                {
                    SystemMessageManager.Instance?.ShowMessage(
                        "로그인 실패: " + loginResult.message,
                        MessageType.Error
                    );
                    return;
                }
            }

            // 인증 이메일 재발송
            var result = await AuthManager.Instance.SendVerificationEmail();

            if (result.success)
            {
                SystemMessageManager.Instance?.ShowMessage(
                    "인증 이메일을 다시 발송했습니다",
                    MessageType.Success
                );
            }
            else
            {
                SystemMessageManager.Instance?.ShowMessage(
                    result.message + "\n잠시 후 다시 시도해주세요",
                    MessageType.Error,
                    4f
                );
            }

            // 재발송 후 다시 로그아웃
            AuthManager.Instance.SignOutWithoutSessionClear();
        }

        /// <summary>
        /// 테스트 계정 여부 확인 (이메일 인증 제외 대상)
        /// </summary>
        /// <param name="email">확인할 이메일 주소</param>
        /// <returns>테스트 계정이면 true</returns>
        private bool IsTestAccount(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            // 테스트 계정 목록
            string[] testAccounts = new string[]
            {
                "a@a.com",
                "b@b.com",
                "c@c.com",
                "d@d.com",
                "e@e.com"
            };

            // 이메일이 테스트 계정 목록에 있는지 확인
            foreach (var testEmail in testAccounts)
            {
                if (email.Equals(testEmail, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[JoinManager] 테스트 계정 감지: {email} - 이메일 인증 건너뜀");
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}

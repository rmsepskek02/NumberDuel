using Objects;
using Objects.Data;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

        // ============================================
        // Panels
        // ============================================
        [Header("Panels")]
        public GameObject emailLoginPanel;          // 이메일 로그인 전체 패널
        public GameObject socialLoginButtonsPanel;  // 소셜 로그인 버튼 패널

        // ============================================
        // Sections
        // ============================================
        [Header("Sections")]
        public GameObject loginSection;             // 로그인 섹션
        public GameObject emailVerificationSection; // 이메일 인증 섹션 (Step 1)
        public GameObject passwordSection;          // 비밀번호 설정 섹션 (Step 2)
        public GameObject nicknameSection;          // 닉네임 설정 섹션 (Step 3)

        // ============================================
        // Login Section - Input Fields
        // ============================================
        [Header("Login Section - Inputs")]
        public TMP_InputField loginEmailInput;      // 로그인 이메일 입력
        public TMP_InputField loginPasswordInput;   // 로그인 비밀번호 입력

        // ============================================
        // Email Verification Section - Input Fields
        // ============================================
        [Header("Email Verification Section - Inputs")]
        public TMP_InputField verificationEmailInput; // 인증 이메일 입력

        // ============================================
        // Password Section - Input Fields
        // ============================================
        [Header("Password Section - Inputs")]
        public TMP_InputField passwordEmailInput;        // 비밀번호 설정 이메일 입력 (읽기 전용)
        public TMP_InputField passwordInput;             // 비밀번호 입력
        public TMP_InputField passwordConfirmInput;      // 비밀번호 확인 입력

        // ============================================
        // Nickname Section - Input Fields
        // ============================================
        [Header("Nickname Section - Inputs")]
        public TMP_InputField nicknameInput;        // 닉네임 입력

        // ============================================
        // Social Login Buttons
        // ============================================
        [Header("Social Login Buttons")]
        public Button guestLoginButton;             // 게스트 로그인 버튼
        public Button emailLoginModeButton;         // "이메일 로그인" 전환 버튼
        public Button googleLoginButton;            // Google 로그인 버튼
        public Button kakaoLoginButton;             // Kakao 로그인 버튼

        // ============================================
        // Login Section Buttons
        // ============================================
        [Header("Login Section Buttons")]
        public Button emailLoginExecuteButton;      // "로그인" 실행 버튼
        public Button emailSignupButton;            // "회원가입" 모드 전환 버튼

        // ============================================
        // Email Verification Section Buttons
        // ============================================
        [Header("Email Verification Section Buttons")]
        public Button verifyEmailButton;            // "인증하기/인증확인" 버튼
        public Button resendEmailButton;            // "재발송" 버튼

        // ============================================
        // Password Section Buttons
        // ============================================
        [Header("Password Section Buttons")]
        public Button passwordConfirmButton;        // 비밀번호 설정 "확인" 버튼

        // ============================================
        // Nickname Section Buttons
        // ============================================
        [Header("Nickname Section Buttons")]
        public Button nicknameConfirmButton;        // 닉네임 설정 "확인" 버튼

        // ============================================
        // Other Buttons
        // ============================================
        [Header("Other Buttons")]
        public Button backToSocialButton;           // "다른 방식으로 로그인하기"

        // ============================================
        // UI Text
        // ============================================
        [Header("UI Text")]
        public TextMeshProUGUI validationErrorText; // 검증 오류 메시지 텍스트

        private bool isProcessing = false;
        private Coroutine photonTimeoutCoroutine = null;

        // Photon 연결 상태 추적
        private bool isTrackingPhotonConnection = false;
        private float photonConnectionStartTime = 0f;
        private float loadingScreenStartTime = 0f; // 로딩스크린 시작 시간

        // Tab 키로 InputField 순회
        private TMP_InputField[] inputFieldOrder;
        private Photon.Realtime.ClientState lastClientState;

        private const float PHOTON_CONNECT_TIMEOUT = 10f; // Photon 연결 타임아웃 (10초)
        private const float TASK_TIMEOUT = 10f; // Firebase Task 타임아웃 (10초)
        private const float MIN_LOADING_DURATION = 1.5f; // 최소 로딩 시간 (페이드인 + 여유)

        // 재발송 쿨다운
        private System.DateTime lastResendTime;
        private const int RESEND_COOLDOWN = 60; // 60초
        #endregion

        #region Unity Lifecycle
        void Start()
        {
#if UNITY_EDITOR
            // 에디터에서 빌드 중일 때는 실행하지 않음
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                return;
            }
#endif

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

            // UI 초기화 - 소셜 로그인 화면부터 시작
            // (기본적으로 SocialLoginButtonsPanel이 활성화되어 있음)

            // InputField 순서 초기화
            UpdateInputFieldOrder();

            // 플랫폼별 UI 설정 (PC에서는 SNS 버튼 숨김)
            ConfigurePlatformSpecificUI();

            // 재발송 버튼 초기 비활성화 (이메일 발송 후에만 활성화)
            if (resendEmailButton != null)
            {
                resendEmailButton.gameObject.SetActive(false);
            }

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
            }
        }

        void Update()
        {
            // Tab 키로 다음 InputField로 이동
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HandleTabKey();
            }

            // Enter 키로 현재 활성 섹션의 버튼 클릭
            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                HandleEnterKey();
            }

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
            }
        }

        /// <summary>
        /// 로비 진입 성공 시점에 호출
        /// </summary>
        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();

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
                yield return new WaitForSeconds(remainingTime);
            }

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
            // 버튼 활성화 (구식 시스템 제거됨)
        }
        #endregion

        #region Button Events

        /// <summary>
        /// 이메일 로그인 모드 전환 버튼 클릭
        /// </summary>
        public void OnClickEmailLoginModeButton()
        {
            // 소셜 로그인 패널 숨기기
            if (socialLoginButtonsPanel != null)
            {
                socialLoginButtonsPanel.SetActive(false);
            }

            // 이메일 로그인 패널 보이기
            if (emailLoginPanel != null)
            {
                emailLoginPanel.SetActive(true);
            }

            // 로그인 섹션 활성화 (기본값)
            if (loginSection != null)
            {
                loginSection.SetActive(true);
            }

            // 나머지 섹션들은 비활성화
            if (emailVerificationSection != null)
            {
                emailVerificationSection.SetActive(false);
            }
            if (passwordSection != null)
            {
                passwordSection.SetActive(false);
            }
            if (nicknameSection != null)
            {
                nicknameSection.SetActive(false);
            }

            // "다른 방식으로 로그인하기" 버튼 표시
            if (backToSocialButton != null)
            {
                backToSocialButton.gameObject.SetActive(true);
            }

            // 로그인 섹션의 첫 번째 입력 필드에 포커스
            if (loginEmailInput != null)
            {
                loginEmailInput.ActivateInputField();
            }
        }

        /// <summary>
        /// 게스트 로그인 버튼 클릭
        /// </summary>
        public void OnClickGuestLoginButton()
        {
            // 확인 팝업 표시
            UI.Shared.ConfirmationPopup.Show(
                "게스트로 시작하시겠습니까?\n\n주의사항\n• 앱 삭제 시 데이터가 손실됩니다\n• 이메일/구글 계정과 연동 가능합니다",
                onConfirm: async () => await OnGuestLoginConfirm(),
                onCancel: () => { }, // 빈 액션 전달 (취소 버튼 표시)
                confirmText: "확인",
                cancelText: "취소"
            );
        }

        /// <summary>
        /// 게스트 로그인 확인 (팝업에서 확인 버튼 클릭 시)
        /// </summary>
        private async Task OnGuestLoginConfirm()
        {
            if (isProcessing) return;

            isProcessing = true;
            DisableAllButtons();

            bool shouldResetButtons = true;

            try
            {
                // Firebase 초기화 대기
                if (!AuthManager.Instance.IsInitialized)
                {
                    SystemMessageManager.Instance?.ShowMessage("InitializingFirebase");
                    bool authReady = await AuthManager.Instance.WaitForInitialization(10f);
                    if (!authReady)
                    {
                        SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                        return;
                    }
                }

                if (!SessionManager.Instance.IsInitialized)
                {
                    bool sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                    if (!sessionReady)
                    {
                        SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                        return;
                    }
                }

                // 게스트 로그인 시도
                bool success = await AuthManager.Instance.SignInAnonymously();

                if (!success)
                {
                    // 에러 팝업
                    UI.Shared.ConfirmationPopup.Show(
                        "게스트 로그인에 실패했습니다.\n\n네트워크 연결을 확인하고\n다시 시도해주세요.",
                        onConfirm: null,
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                    return;
                }

                // 로그인 성공 - 세션 생성 및 로비 진입
                string uid = AuthManager.Instance.CurrentUserUID;

                // 세션 생성 (익명 계정도 세션 필요)
                bool sessionCreated = await SessionManager.Instance.CreateSession(uid);
                if (!sessionCreated)
                {
                    SystemMessageManager.Instance?.ShowMessage("SessionCreateFailed");
                    await AuthManager.Instance.DeleteAccount(); // 생성된 익명 계정 삭제
                    return;
                }

                SessionManager.Instance.StartSessionMonitoring(uid);
                await System.Threading.Tasks.Task.Delay(500);

                // Photon 연결 및 로비 진입
                StartCoroutine(OnLoginSuccessCoroutine());
                shouldResetButtons = false; // 성공 시 씬 전환되므로 버튼 리셋 불필요
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JoinManager] 게스트 로그인 예외: {ex.Message}");
                UI.Shared.ConfirmationPopup.Show(
                    "게스트 로그인 중 오류가 발생했습니다.\n다시 시도해주세요.",
                    onConfirm: null,
                    onCancel: null,
                    confirmText: "확인",
                    cancelText: null
                );
            }
            finally
            {
                isProcessing = false;
                if (shouldResetButtons)
                {
                    EnableAllButtons();
                }
            }
        }

        /// <summary>
        /// Google 로그인 버튼 클릭
        /// </summary>
        public async void OnClickGoogleLoginButton()
        {
            if (isProcessing) return;

            isProcessing = true;
            DisableAllButtons();

            bool shouldResetButtons = true; // 씬 전환 시 버튼 리셋 스킵용 플래그

            try
            {
                // Firebase 초기화 대기
                if (!AuthManager.Instance.IsInitialized)
                {
                    SystemMessageManager.Instance?.ShowMessage("InitializingFirebase");
                    bool authReady = await AuthManager.Instance.WaitForInitialization(10f);
                    if (!authReady)
                    {
                        SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                        return;
                    }
                }

                if (!SessionManager.Instance.IsInitialized)
                {
                    bool sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                    if (!sessionReady)
                    {
                        SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                        return;
                    }
                }

                // Google 로그인 시도 (타임아웃 30초)
                SystemMessageManager.Instance?.ShowMessage("GoogleLoginInProgress");

                var loginTask = AuthManager.Instance.LoginWithGoogle();
                var timeoutTask = System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(30));

                var completedTask = await System.Threading.Tasks.Task.WhenAny(loginTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    SystemMessageManager.Instance?.ShowMessage("GoogleLoginFailed");
                    return;
                }

                var result = await loginTask;

                if (!result.success)
                {
                    // 사용자 취소 - 조용히 처리
                    if (result.message == "CANCELED")
                    {
                        return;
                    }

                    // 타임아웃 발생 (60초 초과)
                    if (result.message == "TIMEOUT")
                    {
                        SystemMessageManager.Instance?.ShowMessage("GoogleLoginTimeout");
                        return;
                    }

                    // 계정이 이미 존재 (다른 방법으로 가입됨)
                    if (result.message == "ACCOUNT_EXISTS")
                    {
                        SystemMessageManager.Instance?.ShowMessage("AccountExistsWithDifferentProvider");
                        return;
                    }

                    // 기타 에러
                    SystemMessageManager.Instance?.ShowMessage("GoogleLoginFailed");
                    return;
                }

                // Google 로그인 성공
                await HandleGoogleLoginSuccess(result.email);
                shouldResetButtons = false; // 성공 시 씬 전환되므로 버튼 리셋 불필요
            }
            finally
            {
                isProcessing = false;
                if (shouldResetButtons)
                {
                    EnableAllButtons();
                }
            }
        }

        /// <summary>
        /// 이메일 로그인 실행 버튼 클릭
        /// </summary>
        public async void OnClickEmailLoginExecuteButton()
        {
            await ExecuteLogin();
        }

        /// <summary>
        /// 회원가입 모드 전환 버튼 클릭
        /// </summary>
        public void OnClickEmailSignupButton()
        {
            // 로그인 섹션 숨기기
            if (loginSection != null)
            {
                loginSection.SetActive(false);
            }

            // 이메일 인증 섹션 보이기 (Step 1)
            if (emailVerificationSection != null)
            {
                emailVerificationSection.SetActive(true);
            }

            // 입력 필드 초기화
            if (verificationEmailInput != null)
            {
                verificationEmailInput.text = "";
                verificationEmailInput.ActivateInputField();
            }
        }

        // 회원가입 상태 추적
        private bool isVerificationEmailSent = false;
        private string verifiedEmail = "";

        /// <summary>
        /// 이메일 인증 버튼 클릭 ("인증하기" 또는 "인증확인")
        /// Step 1: 이메일 발송 → "인증확인" 버튼으로 변경
        /// Step 2: 인증 확인 → 비밀번호 섹션으로 이동
        /// </summary>
        public async void OnClickVerifyEmailButton()
        {
            if (isProcessing) return;

            string email = verificationEmailInput?.text ?? "";

            if (string.IsNullOrEmpty(email))
            {
                SystemMessageManager.Instance?.ShowMessage("EmailRequired");
                return;
            }

            // 이메일 형식 검증
            if (!IsValidEmail(email))
            {
                if (validationErrorText != null)
                {
                    validationErrorText.text = "올바른 이메일 형식이 아닙니다";
                    validationErrorText.color = Color.red;
                }
                return;
            }

            isProcessing = true;
            DisableAllButtons();

            try
            {
                // Firebase 초기화 대기
                if (!AuthManager.Instance.IsInitialized)
                {
                    SystemMessageManager.Instance?.ShowMessage("InitializingFirebase");
                    bool authReady = await AuthManager.Instance.WaitForInitialization(10f);
                    if (!authReady)
                    {
                        SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                        return;
                    }
                }

                // 인증 이메일이 이미 발송된 경우 → 인증 확인만 수행
                if (isVerificationEmailSent && email == verifiedEmail)
                {
                    await CheckEmailVerificationStatus();
                    return;
                }

                // 첫 클릭: Firestore 중복 확인
                SystemMessageManager.Instance?.ShowMessage("CheckingEmail");
                bool isRegistered = await DatabaseManager.Instance.IsEmailRegistered(email);

                if (isRegistered)
                {
                    if (validationErrorText != null)
                    {
                        validationErrorText.text = "이미 등록된 이메일입니다";
                        validationErrorText.color = Color.red;
                    }
                    return;
                }

                // 임시 비밀번호로 계정 생성
                string tempPassword = System.Guid.NewGuid().ToString();
                var signupResult = await AuthManager.Instance.RegisterWithEmail(email, tempPassword);

                if (!signupResult.success)
                {
                    SystemMessageManager.Instance?.ShowMessage(signupResult.message, MessageType.Error);
                    return;
                }

                // 인증 이메일 발송
                var sendResult = await AuthManager.Instance.SendVerificationEmail();

                if (sendResult.success)
                {
                    // 상태 저장
                    isVerificationEmailSent = true;
                    verifiedEmail = email;

                    // 재발송 시간 초기화
                    lastResendTime = System.DateTime.Now;

                    // 성공 팝업
                    UI.Shared.ConfirmationPopup.Show(
                        $"인증 메일이 '{email}'로 발송되었습니다.\n\n" +
                        "이메일의 인증 링크를 클릭한 후\n" +
                        "\"인증확인\" 버튼을 눌러주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );

                    // 버튼 텍스트 변경 및 재발송 버튼 표시
                    if (verifyEmailButton != null)
                    {
                        var buttonText = verifyEmailButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null) buttonText.text = "인증확인";
                    }

                    if (resendEmailButton != null)
                    {
                        resendEmailButton.gameObject.SetActive(true);
                    }
                }
                else
                {
                    UI.Shared.ConfirmationPopup.Show(
                        "인증 메일 발송에 실패했습니다.\n\n" +
                        "잠시 후 다시 시도해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );

                    // 실패 시 생성된 계정 삭제
                    await AuthManager.Instance.DeleteAccount();
                }
            }
            finally
            {
                isProcessing = false;
                EnableAllButtons();
            }
        }

        /// <summary>
        /// 이메일 형식 검증
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 이메일 재발송 버튼 클릭
        /// </summary>
        public async void OnClickResendEmailButton()
        {
            await ResendVerificationEmail();
        }

        /// <summary>
        /// 비밀번호 설정 확인 버튼 클릭 (Step 2 완료)
        /// </summary>
        public async void OnClickPasswordConfirmButton()
        {
            if (isProcessing) return;

            string password = passwordInput?.text ?? "";
            string passwordConfirm = passwordConfirmInput?.text ?? "";

            // 입력 검증
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordConfirm))
            {
                SystemMessageManager.Instance?.ShowMessage("InputPasswordConfirm");
                return;
            }

            // 비밀번호 최소 길이 검증 (Firebase 기본: 6자)
            if (password.Length < 6)
            {
                if (validationErrorText != null)
                {
                    validationErrorText.text = "비밀번호는 최소 6자 이상이어야 합니다";
                    validationErrorText.color = Color.red;
                }
                return;
            }

            if (password != passwordConfirm)
            {
                if (validationErrorText != null)
                {
                    validationErrorText.text = "비밀번호가 일치하지 않습니다";
                    validationErrorText.color = Color.red;
                }
                return;
            }

            isProcessing = true;
            DisableAllButtons();

            try
            {
                // Firebase UpdatePassword 호출
                SystemMessageManager.Instance?.ShowMessage("UpdatingPassword");
                bool updateSuccess = await AuthManager.Instance.UpdatePassword(password);

                if (!updateSuccess)
                {
                    SystemMessageManager.Instance?.ShowMessage(
                        "비밀번호 설정에 실패했습니다.\n다시 시도해주세요.",
                        MessageType.Error
                    );
                    return;
                }

                SystemMessageManager.Instance?.ShowMessage("PasswordSetSuccess");

                // 비밀번호 섹션 숨기기
                if (passwordSection != null)
                {
                    passwordSection.SetActive(false);
                }

                // 닉네임 섹션 보이기 (Step 3)
                if (nicknameSection != null)
                {
                    nicknameSection.SetActive(true);
                }

                // 닉네임 입력 필드에 포커스
                if (nicknameInput != null)
                {
                    nicknameInput.text = "";
                    nicknameInput.ActivateInputField();

                    // 실시간 검증 리스너 등록
                    nicknameInput.onValueChanged.RemoveListener(OnNicknameInputChanged);
                    nicknameInput.onValueChanged.AddListener(OnNicknameInputChanged);
                }
            }
            finally
            {
                isProcessing = false;
                EnableAllButtons();
            }
        }

        /// <summary>
        /// 닉네임 설정 확인 버튼 클릭 (회원가입 최종 완료)
        /// </summary>
        public async void OnClickNicknameConfirmButton()
        {
            await ExecuteSignup();
        }

        /// <summary>
        /// "다른 방식으로 로그인하기" 버튼 클릭 (소셜 로그인 화면으로 복귀)
        /// 회원가입 진행 중인 경우 생성된 Firebase 계정 삭제
        /// </summary>
        public async void OnClickBackToSocialButton()
        {
            // 회원가입 진행 중이고 로그인 상태인 경우 계정 삭제
            if (AuthManager.Instance.IsLoggedIn &&
                (emailVerificationSection.activeSelf || passwordSection.activeSelf || nicknameSection.activeSelf))
            {
                UI.Shared.ConfirmationPopup.Show(
                    "회원가입을 중단하시겠습니까?\n\n" +
                    "진행 중인 계정 정보가 삭제됩니다.",
                    onConfirm: async () =>
                    {
                        // 계정 삭제
                        SystemMessageManager.Instance?.ShowMessage("AccountDeletionInProgress");
                        await AuthManager.Instance.DeleteAccount();
                        SystemMessageManager.Instance?.ShowMessage("AccountDeletionComplete");

                        // UI 초기화
                        ResetToSocialLoginPanel();
                    },
                    onCancel: () => { },
                    confirmText: "확인",
                    cancelText: "취소"
                );
            }
            else
            {
                // 단순히 로그인 화면인 경우 바로 복귀
                ResetToSocialLoginPanel();
            }
        }

        /// <summary>
        /// 소셜 로그인 패널로 UI 초기화
        /// </summary>
        private void ResetToSocialLoginPanel()
        {
            // 회원가입 상태 초기화
            isVerificationEmailSent = false;
            verifiedEmail = "";

            // 이메일 로그인 패널 숨기기
            if (emailLoginPanel != null)
            {
                emailLoginPanel.SetActive(false);
            }

            // 소셜 로그인 패널 보이기
            if (socialLoginButtonsPanel != null)
            {
                socialLoginButtonsPanel.SetActive(true);
            }

            // "다른 방식으로 로그인하기" 버튼 숨기기
            if (backToSocialButton != null)
            {
                backToSocialButton.gameObject.SetActive(false);
            }

            // 모든 섹션 숨기기
            if (loginSection != null) loginSection.SetActive(false);
            if (emailVerificationSection != null) emailVerificationSection.SetActive(false);
            if (passwordSection != null) passwordSection.SetActive(false);
            if (nicknameSection != null) nicknameSection.SetActive(false);

            // 모든 입력 필드 초기화
            if (loginEmailInput != null) loginEmailInput.text = "";
            if (loginPasswordInput != null) loginPasswordInput.text = "";
            if (verificationEmailInput != null) verificationEmailInput.text = "";
            if (passwordEmailInput != null) passwordEmailInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
            if (passwordConfirmInput != null) passwordConfirmInput.text = "";
            if (nicknameInput != null) nicknameInput.text = "";

            // 검증 에러 텍스트 초기화
            if (validationErrorText != null)
            {
                validationErrorText.text = "";
            }

            // 버튼 텍스트 초기화 (인증하기로 복원)
            if (verifyEmailButton != null)
            {
                var buttonText = verifyEmailButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "인증하기";
            }

            // 재발송 버튼 숨기기
            if (resendEmailButton != null)
            {
                resendEmailButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 이메일 로그인 섹션으로 전환 (회원가입 완료 후 로그인 유도)
        /// </summary>
        private void ShowEmailLoginSection()
        {
            // 회원가입 상태 초기화
            isVerificationEmailSent = false;
            verifiedEmail = "";

            // 소셜 로그인 패널 숨기기
            if (socialLoginButtonsPanel != null)
            {
                socialLoginButtonsPanel.SetActive(false);
            }

            // 이메일 로그인 패널 보이기
            if (emailLoginPanel != null)
            {
                emailLoginPanel.SetActive(true);
            }

            // 로그인 섹션만 활성화
            if (loginSection != null)
            {
                loginSection.SetActive(true);
            }

            // 나머지 섹션들은 비활성화
            if (emailVerificationSection != null)
            {
                emailVerificationSection.SetActive(false);
            }
            if (passwordSection != null)
            {
                passwordSection.SetActive(false);
            }
            if (nicknameSection != null)
            {
                nicknameSection.SetActive(false);
            }

            // "다른 방식으로 로그인하기" 버튼 표시
            if (backToSocialButton != null)
            {
                backToSocialButton.gameObject.SetActive(true);
            }

            // 입력 필드 초기화
            if (loginEmailInput != null)
            {
                loginEmailInput.text = "";
                loginEmailInput.ActivateInputField();
            }
            if (loginPasswordInput != null)
            {
                loginPasswordInput.text = "";
            }

            // 검증 에러 텍스트 초기화
            if (validationErrorText != null)
            {
                validationErrorText.text = "";
            }

            // 회원가입 관련 입력 필드 초기화
            if (verificationEmailInput != null) verificationEmailInput.text = "";
            if (passwordEmailInput != null) passwordEmailInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
            if (passwordConfirmInput != null) passwordConfirmInput.text = "";
            if (nicknameInput != null) nicknameInput.text = "";

            // 버튼 텍스트 초기화 (인증하기로 복원)
            if (verifyEmailButton != null)
            {
                var buttonText = verifyEmailButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "인증하기";
            }

            // 재발송 버튼 숨기기
            if (resendEmailButton != null)
            {
                resendEmailButton.gameObject.SetActive(false);
            }
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
                // 버튼 활성화 (구식 시스템 제거됨)
                yield break;
            }

            bool profileExists = checkTask.Result;
            UserProfile profile = null;

            if (!profileExists)
            {
                // ⚠️ 프로필이 없는 경우: 정상적인 경우 발생하지 않아야 함
                // (회원가입 시 프로필 생성하므로)
                Debug.LogError($"[JoinManager] 프로필이 존재하지 않습니다: {uid}");
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("ProfileNotFound");
                }
                // 버튼 활성화 (구식 시스템 제거됨)
                yield break;
            }

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
                // 버튼 활성화 (구식 시스템 제거됨)
                yield break;
            }

            profile = loadTask.Result;

            if (profile != null)
            {
                // ✅ 이메일 인증 상태 업데이트 (익명 로그인이 아닌 경우만 true로 설정)
                // 익명 로그인은 EmailVerified가 항상 false여야 함
                if (!AuthManager.Instance.IsAnonymous)
                {
                    var updateEmailVerifiedTask = DatabaseManager.Instance.UpdateEmailVerified(uid, true);
                    elapsedTime = 0f;

                    while (!updateEmailVerifiedTask.IsCompleted && elapsedTime < TASK_TIMEOUT)
                    {
                        yield return new WaitForSeconds(0.1f);
                        elapsedTime += 0.1f;
                    }

                    // 업데이트 실패해도 로그인은 진행 (치명적 아님)
                    if (!updateEmailVerifiedTask.IsCompleted)
                    {
                        Debug.LogWarning($"⏱️ 마지막 로그인 업데이트 타임아웃 ({TASK_TIMEOUT}초) - 무시하고 진행");
                    }
                }

                // Photon 닉네임 설정
                PhotonNetwork.NickName = profile.Nickname;

                // Photon Custom Property에 Firebase UID 저장
                ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "FirebaseUID", uid }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

                // 이미 로비에 있는지 확인
                if (PhotonNetwork.InLobby)
                {

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

                // 로딩스크린 상태 업데이트
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.UpdateProgress(0.1f, "로비 연결 준비 중...");
                }

                // Photon 로비 진입
                PhotonNetwork.JoinLobby();

                // 버튼 활성화 (구식 시스템 제거됨)
            }
            else
            {
                // 로딩스크린 숨김
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.FadeOutManually();
                }

                SystemMessageManager.Instance?.ShowMessage("ProfileLoadFailed");
                // 버튼 활성화 (구식 시스템 제거됨)
            }
        }

        /// <summary>
        /// UI 모드 설정 (로그인/회원가입) - 하위 호환성용
        /// </summary>

        /// <summary>
        /// 이메일 인증 상태 확인 (자동 새로고침)
        /// 인증 완료 시 비밀번호 섹션으로 전환
        /// </summary>
        private async Task CheckEmailVerificationStatus()
        {
            DisableAllButtons();

            try
            {
                await AuthManager.Instance.ReloadUserInfo();

                if (AuthManager.Instance.IsEmailVerified)
                {
                    UI.Shared.ConfirmationPopup.Show(
                        "이메일 인증이 완료되었습니다!\n\n" +
                        "비밀번호를 설정해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );

                    // 이메일 인증 섹션 숨기기
                    if (emailVerificationSection != null)
                    {
                        emailVerificationSection.SetActive(false);
                    }

                    // 비밀번호 섹션 보이기 (Step 2)
                    if (passwordSection != null)
                    {
                        passwordSection.SetActive(true);
                    }

                    // 비밀번호 섹션의 이메일 필드에 인증된 이메일 표시 (읽기 전용)
                    if (passwordEmailInput != null && verificationEmailInput != null)
                    {
                        passwordEmailInput.text = verificationEmailInput.text;
                        passwordEmailInput.interactable = false;
                    }

                    // 비밀번호 입력 필드에 포커스
                    if (passwordInput != null)
                    {
                        passwordInput.text = "";
                        passwordInput.ActivateInputField();
                    }

                    if (passwordConfirmInput != null)
                    {
                        passwordConfirmInput.text = "";
                    }
                }
                else
                {
                    UI.Shared.ConfirmationPopup.Show(
                        "이메일 인증이 아직 완료되지 않았습니다.\n\n" +
                        "이메일의 인증 링크를 클릭한 후\n다시 시도해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                }
            }
            finally
            {
                EnableAllButtons();
            }
        }

        /// <summary>
        /// 인증 이메일 재발송
        /// 회원가입 중에는 이미 로그인 상태이므로 단순히 인증 이메일만 재발송
        /// </summary>
        private async Task ResendVerificationEmail()
        {
            if (!AuthManager.Instance.IsLoggedIn)
            {
                UI.Shared.ConfirmationPopup.Show(
                    "오류가 발생했습니다.\n다시 시도해주세요.",
                    onConfirm: () => { },
                    onCancel: null,
                    confirmText: "확인",
                    cancelText: null
                );
                return;
            }

            // 60초 쿨다운 체크
            if (lastResendTime != default(System.DateTime))
            {
                var timeSinceLastResend = (System.DateTime.Now - lastResendTime).TotalSeconds;
                if (timeSinceLastResend < RESEND_COOLDOWN)
                {
                    int remainingSeconds = (int)(RESEND_COOLDOWN - timeSinceLastResend);
                    UI.Shared.ConfirmationPopup.Show(
                        $"재발송은 {remainingSeconds}초 후에\n다시 시도할 수 있습니다.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                    return;
                }
            }

            DisableAllButtons();

            try
            {
                // 인증 이메일 재발송
                var result = await AuthManager.Instance.SendVerificationEmail();

                if (result.success)
                {
                    // 재발송 시간 기록
                    lastResendTime = System.DateTime.Now;

                    UI.Shared.ConfirmationPopup.Show(
                        "인증 메일이 재발송되었습니다.\n\n" +
                        "이메일의 인증 링크를 클릭한 후\n" +
                        "\"인증확인\" 버튼을 눌러주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                }
                else
                {
                    UI.Shared.ConfirmationPopup.Show(
                        "인증 메일 재발송에 실패했습니다.\n\n" +
                        "잠시 후 다시 시도해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                }
            }
            finally
            {
                EnableAllButtons();
            }
        }

        /// <summary>
        /// 로그인 실행
        /// </summary>
        private async Task ExecuteLogin()
        {
            if (isProcessing) return;

            string email = loginEmailInput?.text ?? "";
            string password = loginPasswordInput?.text ?? "";

            // 입력 검증
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SystemMessageManager.Instance?.ShowMessage("InputEmailPassword");
                return;
            }

            isProcessing = true;
            DisableAllButtons();

            try
            {
                // Firebase 초기화 대기
                if (!AuthManager.Instance.IsInitialized)
                {
                    SystemMessageManager.Instance?.ShowMessage("InitializingFirebase");

                    bool authReady = await AuthManager.Instance.WaitForInitialization(10f);
                    if (!authReady)
                    {
                        AuthManager.Instance.RetryInitialization();
                        authReady = await AuthManager.Instance.WaitForInitialization(10f);
                        if (!authReady)
                        {
                            SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                            Debug.LogError("[JoinManager] AuthManager 초기화 재시도 실패");
                            return;
                        }
                    }
                }

                if (!SessionManager.Instance.IsInitialized)
                {
                    bool sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                    if (!sessionReady)
                    {
                        SessionManager.Instance.RetryInitialization();
                        sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                        if (!sessionReady)
                        {
                            SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                            Debug.LogError("[JoinManager] SessionManager 초기화 재시도 실패");
                            return;
                        }
                    }
                }

                // 로그인 처리
                SystemMessageManager.Instance?.ShowMessage("LoggingIn");
                var result = await AuthManager.Instance.LoginWithEmail(email, password);

                if (result.success)
                {
                    // 이메일 인증 체크
                    if (!AuthManager.Instance.IsEmailVerified)
                    {
                        UI.Shared.ConfirmationPopup.Show(
                            "이메일 인증이 필요합니다.\n\n" +
                            $"{email}으로 발송된\n" +
                            "이메일의 인증 링크를 클릭해주세요.\n\n" +
                            "이메일을 받지 못하셨나요?",
                            onConfirm: async () =>
                            {
                                await ResendVerificationEmail();
                            },
                            onCancel: () => { },
                            confirmText: "재발송",
                            cancelText: "닫기"
                        );

                        AuthManager.Instance.SignOutWithoutSessionClear();
                        return;
                    }

                    // 세션 체크 (중복 로그인 확인)
                    string uid = AuthManager.Instance.CurrentUserUID;
                    var sessionCheck = await SessionManager.Instance.CheckSession(uid);

                    if (sessionCheck.isDuplicate)
                    {
                        UI.Shared.ConfirmationPopup.Show(
                            "이미 로그인 중인 계정입니다.\n기존 접속을 해제하고 로그인하시겠습니까?",
                            onConfirm: async () =>
                            {
                                SystemMessageManager.Instance?.ShowMessage("ForceLoginInProgress");
                                bool forceLoginSuccess = await SessionManager.Instance.ForceLogin(uid);

                                if (forceLoginSuccess)
                                {
                                    SessionManager.Instance.StartSessionMonitoring(uid);
                                    SystemMessageManager.Instance?.ShowMessage("LoginSuccess");
                                    await System.Threading.Tasks.Task.Delay(500);
                                    StartCoroutine(OnLoginSuccessCoroutine());
                                }
                                else
                                {
                                    SystemMessageManager.Instance?.ShowMessage("SessionCreateFailed");
                                    AuthManager.Instance.Logout();
                                }

                                isProcessing = false;
                                // 버튼 활성화 (구식 시스템 제거됨)
                            },
                            onCancel: () =>
                            {
                                AuthManager.Instance.SignOutWithoutSessionClear();
                                SystemMessageManager.Instance?.ShowMessage("LoginCanceled");
                                isProcessing = false;
                                // 버튼 활성화 (구식 시스템 제거됨)
                            },
                            confirmText: "확인",
                            cancelText: "취소"
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

                    SessionManager.Instance.StartSessionMonitoring(uid);
                    SystemMessageManager.Instance?.ShowMessage("LoginSuccess");
                    await System.Threading.Tasks.Task.Delay(500);
                    StartCoroutine(OnLoginSuccessCoroutine());
                }
                else
                {
                    // ⭐ 소셜 로그인 전용 계정 체크
                    if (result.message.StartsWith("SOCIAL_LOGIN_ONLY::"))
                    {
                        string provider = result.message.Split("::")[1];

                        UI.Shared.ConfirmationPopup.Show(
                            $"이 계정은 {provider} 로그인 전용입니다.\n\n" +
                            $"'{email}'은(는)\n" +
                            $"{provider}로 가입된 계정입니다.\n\n" +
                            $"{provider} 버튼을 사용해주세요.",
                            onConfirm: () => { },
                            onCancel: null,
                            confirmText: "확인",
                            cancelText: null
                        );
                        return;
                    }

                    SystemMessageManager.Instance?.ShowMessage(result.message, MessageType.Error);
                }
            }
            finally
            {
                isProcessing = false;
                EnableAllButtons();
            }
        }

        /// <summary>
        /// 회원가입 실행 (3단계 시스템의 Step 3: 닉네임 설정 완료)
        /// Step 1에서 이미 계정 생성, Step 2에서 비밀번호 설정 완료 상태
        /// </summary>
        private async Task ExecuteSignup()
        {
            if (isProcessing) return;

            string nickname = nicknameInput?.text ?? "";

            // 닉네임 입력 검증
            if (string.IsNullOrEmpty(nickname))
            {
                SystemMessageManager.Instance?.ShowMessage("InputNickname");
                return;
            }

            // 픽셀 길이 검증
            int pixelLength = CalculatePixelLength(nickname);
            if (pixelLength > 24)
            {
                if (validationErrorText != null)
                {
                    validationErrorText.text = "닉네임이 너무 깁니다";
                    validationErrorText.color = Color.red;
                }
                return;
            }

            isProcessing = true;
            DisableAllButtons();

            try
            {
                // Firebase 초기화 대기
                if (!AuthManager.Instance.IsInitialized)
                {
                    SystemMessageManager.Instance?.ShowMessage("InitializingFirebase");
                    bool authReady = await AuthManager.Instance.WaitForInitialization(10f);
                    if (!authReady)
                    {
                        AuthManager.Instance.RetryInitialization();
                        authReady = await AuthManager.Instance.WaitForInitialization(10f);
                        if (!authReady)
                        {
                            SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                            return;
                        }
                    }
                }

                if (!SessionManager.Instance.IsInitialized)
                {
                    bool sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                    if (!sessionReady)
                    {
                        SessionManager.Instance.RetryInitialization();
                        sessionReady = await SessionManager.Instance.WaitForInitialization(10f);
                        if (!sessionReady)
                        {
                            SystemMessageManager.Instance?.ShowMessage("FirebaseInitTimeout");
                            return;
                        }
                    }
                }

                // 닉네임 중복 확인
                bool isNicknameAvailable = await DatabaseManager.Instance.IsNicknameAvailable(nickname);
                if (!isNicknameAvailable)
                {
                    UI.Shared.ConfirmationPopup.Show(
                        $"'{nickname}' 닉네임은 이미 사용 중입니다.\n\n다른 닉네임을 입력해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                    return;
                }

                // ⭐ Step 1에서 이미 계정 생성되었으므로 UID 가져오기
                string uid = AuthManager.Instance.CurrentUserUID;
                string email = verifiedEmail; // Step 1에서 저장한 이메일

                // Firestore 프로필 생성 (이메일은 이미 인증 완료 상태)
                bool profileCreated = await DatabaseManager.Instance.CreateUserProfile(uid, email, nickname, emailVerified: true);
                if (!profileCreated)
                {
                    UI.Shared.ConfirmationPopup.Show(
                        "프로필 생성에 실패했습니다.\n\n다시 시도해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                    return;
                }

                // ⭐ 회원가입 완료 - 세션 생성 없이 로그인 유도
                UI.Shared.ConfirmationPopup.Show(
                    "회원가입을 환영합니다!\n\n로그인을 진행해주세요.",
                    onConfirm: () =>
                    {
                        // 회원가입 완료 후 로그아웃 처리
                        AuthManager.Instance.SignOutWithoutSessionClear();

                        // 이메일 로그인 화면으로 복귀
                        ShowEmailLoginSection();
                    },
                    onCancel: null,
                    confirmText: "확인",
                    cancelText: null
                );
            }
            finally
            {
                isProcessing = false;
                EnableAllButtons();
            }
        }

        /// <summary>
        /// Google 로그인 성공 후 처리
        /// </summary>
        private async Task HandleGoogleLoginSuccess(string email)
        {
            SystemMessageManager.Instance?.ShowMessage("GoogleLoginSuccess");

            string uid = AuthManager.Instance.CurrentUserUID;

            // 이메일이 비어있는 경우 ProviderData에서 가져오기 (Play Games 로그인 fallback)
            if (string.IsNullOrEmpty(email))
            {
                email = AuthManager.Instance.GetCurrentUserEmailFromProvider();
            }

            // 프로필 존재 여부 확인
            bool profileExists = await DatabaseManager.Instance.UserProfileExists(uid);

            if (!profileExists)
            {
                // 신규 사용자 - 닉네임 입력 팝업 표시
                UI.Shared.InputFieldPopup.ShowNicknameInput(
                    onConfirm: async (nickname) =>
                    {
                        // 프로필 생성
                        bool profileCreated = await DatabaseManager.Instance.CreateSocialUserProfile(
                            uid,
                            email,
                            nickname,
                            "Google"
                        // AuthManager.Instance.CurrentUser.PhotoUrl?.ToString() ?? ""
                        );

                        if (!profileCreated)
                        {
                            SystemMessageManager.Instance?.ShowMessage("ProfileCreateFailed");
                            AuthManager.Instance.SignOutWithoutSessionClear();
                            return;
                        }

                        // 세션 생성 및 로비 진입
                        await ProceedToLobby(uid);
                    },
                    onCancel: () =>
                    {
                        // 닉네임 입력 취소 - 로그아웃
                        AuthManager.Instance.SignOutWithoutSessionClear();
                    }
                );
            }
            else
            {
                // 기존 사용자 - 세션 체크 및 로비 진입
                // Photon 닉네임 설정
                var profile = await Utils.ProfileExtensions.LoadProfileWithNullCheck(uid);
                if (profile == null)
                {
                    return;
                }

                PhotonNetwork.NickName = profile.Nickname;

                // Photon Custom Property에 Firebase UID 저장
                ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "FirebaseUID", uid }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

                await ProceedToLobby(uid);
            }
        }

        /// <summary>
        /// 세션 체크 후 로비 진입
        /// </summary>
        private async Task ProceedToLobby(string uid)
        {
            // 세션 체크 (중복 로그인 확인)
            var sessionCheck = await SessionManager.Instance.CheckSession(uid);

            if (sessionCheck.isDuplicate)
            {
                UI.Shared.ConfirmationPopup.Show(
                    "이미 로그인 중인 계정입니다.\n기존 접속을 해제하고 로그인하시겠습니까?",
                    onConfirm: async () =>
                    {
                        SystemMessageManager.Instance?.ShowMessage("ForceLoginInProgress");
                        bool forceLoginSuccess = await SessionManager.Instance.ForceLogin(uid);

                        if (forceLoginSuccess)
                        {
                            SessionManager.Instance.StartSessionMonitoring(uid);
                            SystemMessageManager.Instance?.ShowMessage("LoginSuccess");
                            await System.Threading.Tasks.Task.Delay(500);
                            StartCoroutine(OnLoginSuccessCoroutine());
                        }
                        else
                        {
                            SystemMessageManager.Instance?.ShowMessage("SessionCreateFailed");
                            AuthManager.Instance.Logout();
                        }
                    },
                    onCancel: () =>
                    {
                        AuthManager.Instance.SignOutWithoutSessionClear();
                    },
                    confirmText: "확인",
                    cancelText: "취소"
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

            SessionManager.Instance.StartSessionMonitoring(uid);
            SystemMessageManager.Instance?.ShowMessage("LoginSuccess");
            await System.Threading.Tasks.Task.Delay(500);
            StartCoroutine(OnLoginSuccessCoroutine());
        }

        /// <summary>
        /// Tab 키로 다음 InputField로 포커스 이동
        /// </summary>
        private void HandleTabKey()
        {
            if (inputFieldOrder == null || inputFieldOrder.Length == 0)
                return;

            // 현재 포커스된 InputField 찾기
            int currentIndex = -1;
            for (int i = 0; i < inputFieldOrder.Length; i++)
            {
                if (inputFieldOrder[i] != null && inputFieldOrder[i].isFocused)
                {
                    currentIndex = i;
                    break;
                }
            }

            // 다음 InputField로 포커스 이동
            if (currentIndex >= 0)
            {
                int nextIndex = (currentIndex + 1) % inputFieldOrder.Length;
                inputFieldOrder[nextIndex]?.ActivateInputField();
            }
            else
            {
                // 포커스된 필드가 없으면 첫 번째 필드로
                inputFieldOrder[0]?.ActivateInputField();
            }
        }

        /// <summary>
        /// Enter 키로 현재 활성 섹션의 확인/다음 버튼 클릭
        /// </summary>
        private void HandleEnterKey()
        {
            // 로그인 섹션 활성화 시 - 로그인 버튼 클릭
            if (loginSection != null && loginSection.activeSelf)
            {
                // 이메일과 비밀번호가 입력되었는지 확인
                if (loginEmailInput != null && !string.IsNullOrWhiteSpace(loginEmailInput.text) &&
                    loginPasswordInput != null && !string.IsNullOrWhiteSpace(loginPasswordInput.text))
                {
                    if (emailLoginExecuteButton != null && emailLoginExecuteButton.interactable)
                    {
                        emailLoginExecuteButton.onClick?.Invoke();
                    }
                }
            }
            // 이메일 인증 섹션 활성화 시 - 인증하기 버튼 클릭
            else if (emailVerificationSection != null && emailVerificationSection.activeSelf)
            {
                // 이메일이 입력되었는지 확인
                if (verificationEmailInput != null && !string.IsNullOrWhiteSpace(verificationEmailInput.text))
                {
                    if (verifyEmailButton != null && verifyEmailButton.interactable)
                    {
                        verifyEmailButton.onClick?.Invoke();
                    }
                }
            }
            // 비밀번호 설정 섹션 활성화 시 - 확인 버튼 클릭
            else if (passwordSection != null && passwordSection.activeSelf)
            {
                // 비밀번호와 비밀번호 확인이 입력되었는지 확인
                if (passwordInput != null && !string.IsNullOrWhiteSpace(passwordInput.text) &&
                    passwordConfirmInput != null && !string.IsNullOrWhiteSpace(passwordConfirmInput.text))
                {
                    // 비밀번호 일치 여부 확인
                    if (passwordInput.text == passwordConfirmInput.text)
                    {
                        if (passwordConfirmButton != null && passwordConfirmButton.interactable)
                        {
                            passwordConfirmButton.onClick?.Invoke();
                        }
                    }
                }
            }
            // 닉네임 설정 섹션 활성화 시 - 확인 버튼 클릭
            else if (nicknameSection != null && nicknameSection.activeSelf)
            {
                // 닉네임이 입력되었는지 확인
                if (nicknameInput != null && !string.IsNullOrWhiteSpace(nicknameInput.text))
                {
                    if (nicknameConfirmButton != null && nicknameConfirmButton.interactable)
                    {
                        nicknameConfirmButton.onClick?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// InputField 순서 업데이트 (새 시스템에서는 모든 필드 포함)
        /// </summary>
        private void UpdateInputFieldOrder()
        {
            // 새 시스템에서는 모든 인풋 필드를 포함 (섹션별로 활성화/비활성화됨)
            inputFieldOrder = new TMP_InputField[] {
                loginEmailInput, loginPasswordInput,
                verificationEmailInput,
                passwordEmailInput, passwordInput, passwordConfirmInput,
                nicknameInput
            };
        }


        /// <summary>
        /// 모든 버튼 비활성화 (비동기 작업 중 중복 클릭 방지)
        /// </summary>
        private void DisableAllButtons()
        {
            if (guestLoginButton != null) guestLoginButton.interactable = false;
            if (emailLoginModeButton != null) emailLoginModeButton.interactable = false;
            if (googleLoginButton != null) googleLoginButton.interactable = false;
            if (kakaoLoginButton != null) kakaoLoginButton.interactable = false;
            if (emailLoginExecuteButton != null) emailLoginExecuteButton.interactable = false;
            if (emailSignupButton != null) emailSignupButton.interactable = false;
            if (verifyEmailButton != null) verifyEmailButton.interactable = false;
            if (resendEmailButton != null) resendEmailButton.interactable = false;
            if (passwordConfirmButton != null) passwordConfirmButton.interactable = false;
            if (nicknameConfirmButton != null) nicknameConfirmButton.interactable = false;
            if (backToSocialButton != null) backToSocialButton.interactable = false;
        }

        /// <summary>
        /// 모든 버튼 활성화
        /// </summary>
        private void EnableAllButtons()
        {
            if (guestLoginButton != null) guestLoginButton.interactable = true;
            if (emailLoginModeButton != null) emailLoginModeButton.interactable = true;
            if (googleLoginButton != null) googleLoginButton.interactable = true;
            if (kakaoLoginButton != null) kakaoLoginButton.interactable = true;
            if (emailLoginExecuteButton != null) emailLoginExecuteButton.interactable = true;
            if (emailSignupButton != null) emailSignupButton.interactable = true;
            if (verifyEmailButton != null) verifyEmailButton.interactable = true;
            if (resendEmailButton != null) resendEmailButton.interactable = true;
            if (passwordConfirmButton != null) passwordConfirmButton.interactable = true;
            if (nicknameConfirmButton != null) nicknameConfirmButton.interactable = true;
            if (backToSocialButton != null) backToSocialButton.interactable = true;
        }

        /// <summary>
        /// 닉네임 입력 실시간 검증 (픽셀 기반)
        /// </summary>
        private void OnNicknameInputChanged(string input)
        {
            if (validationErrorText == null) return;

            // 빈 입력
            if (string.IsNullOrEmpty(input))
            {
                validationErrorText.text = "";
                return;
            }

            // 픽셀 길이 계산
            int pixelLength = CalculatePixelLength(input);

            // 24픽셀 초과 시 경고
            if (pixelLength > 24)
            {
                validationErrorText.text = $"닉네임이 너무 깁니다 ({pixelLength}/24)";
                validationErrorText.color = Color.red;
            }
            else
            {
                validationErrorText.text = $"{pixelLength}/24";
                validationErrorText.color = Color.blue;
            }
        }

        /// <summary>
        /// 닉네임 픽셀 길이 계산
        /// 한글: 2px, 영문/숫자/특수문자: 1px
        /// </summary>
        private int CalculatePixelLength(string input)
        {
            int totalPixels = 0;

            foreach (char c in input)
            {
                // 한글 유니코드 범위: AC00-D7A3
                if (c >= 0xAC00 && c <= 0xD7A3)
                {
                    totalPixels += 2; // 한글 2px
                }
                else
                {
                    totalPixels += 1; // 영문/숫자/특수문자 1px
                }
            }

            return totalPixels;
        }

        /// <summary>
        /// 플랫폼별 UI 설정
        /// PC Standalone에서는 SNS 로그인 버튼 숨김 (에디터 포함)
        /// </summary>
        private void ConfigurePlatformSpecificUI()
        {
#if UNITY_ANDROID || UNITY_IOS
            // 모바일 플랫폼: SNS 버튼 표시
            if (googleLoginButton != null)
            {
                googleLoginButton.gameObject.SetActive(true);
            }

            if (kakaoLoginButton != null)
            {
                kakaoLoginButton.gameObject.SetActive(true);
            }
#else
            // PC/에디터: SNS 버튼 숨김
            if (googleLoginButton != null)
            {
                googleLoginButton.gameObject.SetActive(false);
            }

            if (kakaoLoginButton != null)
            {
                kakaoLoginButton.gameObject.SetActive(false);
            }
#endif
        }

        #endregion
    }
}

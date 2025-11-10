using Objects;
using Objects.Data;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // 한국 리전 설정
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";

            // 서버 연결 (AppId, 버전, 서버에 요청)
            PhotonNetwork.ConnectUsingSettings();

            // UI 초기화
            SetLoginMode(true);

            // 버튼 이벤트는 Unity Editor에서 등록
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
        }

        /// <summary>
        /// 로비 진입 성공 시점에 호출
        /// </summary>
        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();

            // 로비로 이동
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowThenLoadLocal(SceneNameExtensions.GetSceneName(SceneName.LobbyScene));
            }
            else
            {
                PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.LobbyScene));
            }
        }
        #endregion

        #region Button Events
        /// <summary>
        /// [Deprecated] 기존 메서드 - 사용하지 말것
        /// </summary>
        [System.Obsolete("Use OnClickActionButton instead")]
        public void OnClickButton()
        {
            // 이 메서드는 더 이상 사용하지 않습니다.
            // Firebase 인증을 사용하려면 OnClickActionButton을 사용하세요.
            Debug.LogWarning("OnClickButton()은 더 이상 사용되지 않습니다. OnClickActionButton()을 사용하세요.");
        }

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
                if (isLoginMode)
                {
                    // 로그인 처리
                    SystemMessageManager.Instance?.ShowMessage("LoggingIn");
                    var result = await AuthManager.Instance.LoginWithEmail(email, password);

                    if (result.success)
                    {
                        // 세션 체크 (중복 로그인 확인)
                        string uid = AuthManager.Instance.CurrentUserUID;
                        var sessionCheck = await SessionManager.Instance.CheckSession(uid);

                        if (sessionCheck.isDuplicate)
                        {
                            // 중복 로그인 감지 - Firebase에서 오는 커스텀 메시지 사용
                            SystemMessageManager.Instance?.ShowMessage(sessionCheck.message, MessageType.Error);

                            // Firebase 로그아웃 처리
                            AuthManager.Instance.Logout();
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
                        SystemMessageManager.Instance?.ShowMessage("RegisterSuccess");
                        await System.Threading.Tasks.Task.Delay(800); // 0.8초 대기

                        // 회원가입 성공 시 자동으로 로그인되므로 프로필 생성
                        SystemMessageManager.Instance?.ShowMessage("CreatingProfile");
                        string uid = AuthManager.Instance.CurrentUserUID;
                        await DatabaseManager.Instance.CreateUserProfile(uid, email, nickname);

                        await System.Threading.Tasks.Task.Delay(500); // 0.5초 대기

                        // 세션 생성
                        bool sessionCreated = await SessionManager.Instance.CreateSession(uid);

                        if (!sessionCreated)
                        {
                            SystemMessageManager.Instance?.ShowMessage("SessionCreateFailed");
                            AuthManager.Instance.Logout();
                            return;
                        }

                        // Coroutine으로 로비 진입
                        StartCoroutine(OnLoginSuccessCoroutine());
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
        /// 로그인 성공 후 처리 (Coroutine 버전 - 빌드 호환성)
        /// </summary>
        private IEnumerator OnLoginSuccessCoroutine()
        {
            string uid = AuthManager.Instance.CurrentUserUID;
            string email = AuthManager.Instance.CurrentUserEmail;

            // 프로필 존재 여부 확인
            var checkTask = DatabaseManager.Instance.UserProfileExists(uid);
            yield return new WaitUntil(() => checkTask.IsCompleted);

            bool profileExists = checkTask.Result;
            UserProfile profile = null;

            if (!profileExists)
            {
                // 프로필이 없으면 생성 (기존 계정의 경우)
                SystemMessageManager.Instance?.ShowMessage("CreatingProfile");
                string defaultNickname = email.Split('@')[0]; // 이메일 앞부분을 닉네임으로

                var createTask = DatabaseManager.Instance.CreateUserProfile(uid, email, defaultNickname);
                yield return new WaitUntil(() => createTask.IsCompleted);

                var loadTask = DatabaseManager.Instance.GetUserProfile(uid);
                yield return new WaitUntil(() => loadTask.IsCompleted);
                profile = loadTask.Result;
            }
            else
            {
                // 프로필 로드
                var loadTask = DatabaseManager.Instance.GetUserProfile(uid);
                yield return new WaitUntil(() => loadTask.IsCompleted);
                profile = loadTask.Result;
            }

            if (profile != null)
            {
                // 마지막 로그인 시간 업데이트
                var updateTask = DatabaseManager.Instance.UpdateLastLogin(uid);
                yield return new WaitUntil(() => updateTask.IsCompleted);

                // Photon 닉네임 설정
                PhotonNetwork.NickName = profile.Nickname;

                // Photon Custom Property에 Firebase UID 저장
                ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "FirebaseUID", uid }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

                // Photon 로비 진입 (메시지 없이 자동 진입)
                PhotonNetwork.JoinLobby();

                SetButtonsInteractable(true);
            }
            else
            {
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
        #endregion
    }
}

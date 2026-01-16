using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using Utills;
using Objects;

namespace Manager
{
    /// <summary>
    /// Firebase Authentication을 관리하는 싱글톤 매니저
    /// 파사드 패턴으로 각 서비스에 위임
    /// </summary>
    public class AuthManager : SingletonDontDestroy<AuthManager>
    {
        #region Fields and Properties
        private FirebaseAuth auth;
        private FirebaseUser currentUser;
        private bool isInitialized = false;

        private const float INIT_TIMEOUT = 10f;

        private string currentSessionLoginProvider = string.Empty;
        private const string PREF_KEY_LOGIN_PROVIDER = "LastLoginProvider";

        // 서비스 인스턴스
        private EmailAuthService emailAuthService;
        private GoogleAuthService googleAuthService;
        private AnonymousAuthService anonymousAuthService;
        private AccountLinkingService accountLinkingService;

        // Public Properties
        public FirebaseAuth Auth => auth;
        public FirebaseUser CurrentUser => currentUser;
        public bool IsLoggedIn => currentUser != null;
        public bool IsInitialized => isInitialized;
        public string CurrentUserUID => currentUser?.UserId ?? string.Empty;
        public string CurrentUserEmail => currentUser?.Email ?? string.Empty;
        public bool IsEmailVerified => currentUser?.IsEmailVerified ?? false;
        public bool IsAnonymous => currentUser?.IsAnonymous ?? false;

        // 이벤트
        public event Action OnAccountLinked;
        public event Action OnEmailLinked;
        public event Action OnGoogleLinked;
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
                return;
#endif

            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            if (isInitialized)
                return;

            if (FirebaseInitializer.IsFirebaseReady)
            {
                InitializeAuthInstance();
            }
            else
            {
                StartCoroutine(WaitForFirebaseInitializer());
            }
        }

        private void InitializeAuthInstance()
        {
            try
            {
                auth = FirebaseAuth.DefaultInstance;
                auth.StateChanged += OnAuthStateChanged;
                OnAuthStateChanged(this, null);

                // 서비스 인스턴스 생성
                emailAuthService = new EmailAuthService(this);
                googleAuthService = new GoogleAuthService(this);
                anonymousAuthService = new AnonymousAuthService(this);
                accountLinkingService = new AccountLinkingService(this);

                LoadLoginProvider();
                isInitialized = true;

                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.OnForceLogout += HandleForceLogout;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuthManager] Auth 인스턴스 초기화 실패: {ex.Message}");
                isInitialized = false;
            }
        }

        private System.Collections.IEnumerator WaitForFirebaseInitializer()
        {
            float elapsedTime = 0f;
            const float timeout = 15f;

            while (!FirebaseInitializer.IsFirebaseReady && elapsedTime < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }

            if (FirebaseInitializer.IsFirebaseReady)
            {
                InitializeAuthInstance();
            }
            else
            {
                Debug.LogError($"[AuthManager] FirebaseInitializer 초기화 타임아웃 ({timeout}초)");
                isInitialized = false;
            }
        }

        public void RetryInitialization()
        {
            if (!isInitialized)
                InitializeFirebase();
        }

        public async Task<bool> WaitForInitialization(float timeout = INIT_TIMEOUT)
        {
            float elapsedTime = 0f;

            while (!isInitialized && elapsedTime < timeout)
            {
                await Task.Delay(100);
                elapsedTime += 0.1f;
            }

            if (!isInitialized)
            {
                Debug.LogError($"AuthManager 초기화 타임아웃 ({timeout}초)");
            }

            return isInitialized;
        }

        protected override void OnDestroy()
        {
            if (auth != null)
            {
                auth.StateChanged -= OnAuthStateChanged;
            }

            var sessionManager = FindAnyObjectByType<SessionManager>();
            if (sessionManager != null)
            {
                sessionManager.OnForceLogout -= HandleForceLogout;
            }

            base.OnDestroy();
        }

        private void OnAuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (auth.CurrentUser != currentUser)
            {
                bool signedIn = currentUser != auth.CurrentUser && auth.CurrentUser != null;
                currentUser = auth.CurrentUser;
            }
        }
        #endregion

        #region Internal Helper Methods (서비스용)
        /// <summary>
        /// 서비스에서 currentUser를 설정할 수 있도록 함
        /// </summary>
        internal void SetCurrentUser(FirebaseUser user)
        {
            currentUser = user;
        }

        /// <summary>
        /// 로그인 방식 저장 (내부용)
        /// </summary>
        internal void SaveLoginProvider(string providerId)
        {
            currentSessionLoginProvider = providerId;
            UnityEngine.PlayerPrefs.SetString(PREF_KEY_LOGIN_PROVIDER, providerId);
            UnityEngine.PlayerPrefs.Save();
        }

        /// <summary>
        /// 현재 세션 로그인 Provider 가져오기 (AccountLinkingService용)
        /// </summary>
        internal string GetCurrentSessionLoginProvider()
        {
            return currentSessionLoginProvider;
        }

        private void LoadLoginProvider()
        {
            currentSessionLoginProvider = UnityEngine.PlayerPrefs.GetString(PREF_KEY_LOGIN_PROVIDER, string.Empty);
        }

        private void ClearLoginProvider()
        {
            currentSessionLoginProvider = string.Empty;
            UnityEngine.PlayerPrefs.DeleteKey(PREF_KEY_LOGIN_PROVIDER);
            UnityEngine.PlayerPrefs.Save();
        }
        #endregion

        #region Public Methods - Event Notifications
        public void NotifyEmailLinked()
        {
            OnEmailLinked?.Invoke();
            OnAccountLinked?.Invoke();
        }

        public void NotifyGoogleLinked()
        {
            OnGoogleLinked?.Invoke();
            OnAccountLinked?.Invoke();
        }

        public void NotifyAccountLinked()
        {
            OnAccountLinked?.Invoke();
        }
        #endregion

        #region Public Methods - Anonymous Login (파사드)
        public Task<bool> SignInAnonymously()
        {
            return anonymousAuthService.SignInAnonymously();
        }

        public Task<bool> DeleteAccount()
        {
            return anonymousAuthService.DeleteAccount();
        }

        public Task<(bool success, string message)> ConvertGuestToGoogle()
        {
            return anonymousAuthService.ConvertGuestToGoogle();
        }

        public Task<(bool success, string message)> ConvertGuestToEmail(string email, string password, string nickname = null)
        {
            return anonymousAuthService.ConvertGuestToEmail(email, password, nickname);
        }
        #endregion

        #region Public Methods - Email Auth (파사드)
        public Task<(bool success, string message)> RegisterWithEmail(string email, string password)
        {
            return emailAuthService.RegisterWithEmail(email, password);
        }

        public Task<(bool success, string message)> LoginWithEmail(string email, string password)
        {
            return emailAuthService.LoginWithEmail(email, password);
        }

        public Task<(bool success, string message)> SendEmailVerification()
        {
            return emailAuthService.SendEmailVerification();
        }

        public Task<(bool success, string message)> SendVerificationEmail()
        {
            return emailAuthService.SendEmailVerification();
        }

        public Task<(bool success, string message)> SendPasswordResetEmail(string email)
        {
            return emailAuthService.SendPasswordResetEmail(email);
        }

        public Task<bool> ReloadUserInfo()
        {
            return emailAuthService.ReloadUserInfo();
        }

        public Task<bool> UpdatePassword(string newPassword)
        {
            return emailAuthService.UpdatePassword(newPassword);
        }
        #endregion

        #region Public Methods - Google Login (파사드)
        public Task<(bool success, string message, string email)> LoginWithGoogle()
        {
            return googleAuthService.LoginWithGoogle();
        }

        public Task<List<string>> GetProvidersForEmail(string email)
        {
            return googleAuthService.GetProvidersForEmail(email);
        }
        #endregion

        #region Public Methods - Account Linking (파사드)
        public List<string> GetCurrentUserProviders()
        {
            return accountLinkingService.GetCurrentUserProviders();
        }

        public string GetCurrentUserEmailFromProvider()
        {
            return accountLinkingService.GetCurrentUserEmailFromProvider();
        }

        public bool IsGoogleLogin()
        {
            return accountLinkingService.IsGoogleLogin();
        }

        public bool CanLinkPasswordToGoogle()
        {
            return accountLinkingService.CanLinkPasswordToGoogle();
        }

        public bool CanLinkGoogleToEmail()
        {
            return accountLinkingService.CanLinkGoogleToEmail();
        }

        public bool IsAccountFullyLinked()
        {
            return accountLinkingService.IsAccountFullyLinked();
        }

        public bool IsEmailOnlyAccount()
        {
            return accountLinkingService.IsEmailOnlyAccount();
        }

        public Task<(bool success, string message)> LinkPasswordToGoogle(string email, string password)
        {
            return accountLinkingService.LinkPasswordToGoogle(email, password);
        }

        public Task<(bool success, string message)> LinkGoogleToEmail()
        {
            return accountLinkingService.LinkGoogleToEmail();
        }

        public Task<(bool success, string message, string email)> LinkEmailToGoogle(string email, string password, string nickname = null)
        {
            return accountLinkingService.LinkEmailToGoogle(email, password, nickname);
        }

        public string GetLoginMethodDisplayName(bool showCurrentSessionOnly = false)
        {
            return accountLinkingService.GetLoginMethodDisplayName(showCurrentSessionOnly);
        }

        public bool CanLinkEmailToGuest()
        {
            if (currentUser == null)
                return false;

            var providers = GetCurrentUserProviders();
            return IsAnonymous && !providers.Contains("password");
        }
        #endregion

        #region Public Methods - Logout
        public async void Logout()
        {
            if (auth != null && currentUser != null)
            {
                string uid = currentUser.UserId;

                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.StopSessionMonitoring();
                }

                if (SessionManager.Instance != null && SessionManager.Instance.IsInitialized)
                {
                    await SessionManager.Instance.ClearSession(uid);
                }

                auth.SignOut();
                currentUser = null;
                ClearLoginProvider();
            }
        }

        public void SignOutWithoutSessionClear()
        {
            if (auth != null && currentUser != null)
            {
                auth.SignOut();
                currentUser = null;
            }
        }
        #endregion

        #region Public Methods - Auto Login
        public bool CanAutoLogin()
        {
            if (!isInitialized)
                return false;

            if (auth != null && auth.CurrentUser != null)
            {
                currentUser = auth.CurrentUser;
                return true;
            }

            return false;
        }
        #endregion

        #region Force Logout Handler
        private void HandleForceLogout()
        {
            if (SettingsManager.Instance != null && SettingsManager.Instance.IsSettingsOpen)
            {
                SettingsManager.Instance.HideSettings();
            }

            SignOutWithoutSessionClear();

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedForMessage;

            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowThenLoadLocal(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
            }
        }

        private void OnSceneLoadedForMessage(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedForMessage;
            UnityEngine.MonoBehaviour coroutineRunner = this;
            coroutineRunner.StartCoroutine(ShowForceLogoutMessageDelayed());
        }

        private System.Collections.IEnumerator ShowForceLogoutMessageDelayed()
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);

            if (SystemMessageManager.Instance != null)
            {
                SystemMessageManager.Instance.ShowMessage("DuplicateLoginDetected");
            }
        }
        #endregion

        #region Static Helper (호환성 유지)
        /// <summary>
        /// Firebase 에러 메시지를 한글로 변환 (static - 외부에서 사용 가능)
        /// </summary>
        public static string GetFirebaseErrorMessage(FirebaseException ex)
        {
            return FirebaseAuthHelper.GetFirebaseErrorMessage(ex);
        }
        #endregion
    }
}

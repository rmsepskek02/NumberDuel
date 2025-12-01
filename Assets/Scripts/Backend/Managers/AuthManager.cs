using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using Utills;
using Objects;

namespace Manager
{
    /// <summary>
    /// Firebase Authentication을 관리하는 싱글톤 매니저
    /// 이메일/비밀번호 로그인 및 회원가입 담당
    /// </summary>
    public class AuthManager : SingletonDontDestroy<AuthManager>
    {
        #region Fields and Properties
        private FirebaseAuth auth;
        private FirebaseUser currentUser;
        private bool isInitialized = false;

        private const float INIT_TIMEOUT = 10f; // 초기화 타임아웃 (10초)

        /// <summary>
        /// 현재 로그인한 사용자
        /// </summary>
        public FirebaseUser CurrentUser => currentUser;

        /// <summary>
        /// 로그인 상태 확인
        /// </summary>
        public bool IsLoggedIn => currentUser != null;

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 현재 사용자의 UID
        /// </summary>
        public string CurrentUserUID => currentUser?.UserId ?? string.Empty;

        /// <summary>
        /// 현재 사용자의 이메일
        /// </summary>
        public string CurrentUserEmail => currentUser?.Email ?? string.Empty;
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            // 이미 초기화 중이거나 완료된 경우 중복 호출 방지
            if (isInitialized)
            {
                Debug.Log("[AuthManager] 이미 초기화 완료됨");
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;

                    // 인증 상태 변경 이벤트 등록
                    auth.StateChanged += OnAuthStateChanged;

                    // 이미 로그인되어 있는지 확인
                    OnAuthStateChanged(this, null);

                    isInitialized = true; // 초기화 완료 플래그 설정
                    Debug.Log("✅ AuthManager 초기화 완료");

                    // SessionManager의 강제 로그아웃 이벤트 구독
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.OnForceLogout += HandleForceLogout;
                        Debug.Log("✅ SessionManager 강제 로그아웃 이벤트 구독 완료");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Firebase 초기화 실패: {task.Result}");
                    isInitialized = false;
                }
            });
        }

        /// <summary>
        /// Firebase 재초기화 시도 (초기화 실패 시 호출)
        /// </summary>
        public void RetryInitialization()
        {
            if (isInitialized)
            {
                Debug.Log("[AuthManager] 이미 초기화 완료됨 - 재초기화 불필요");
                return;
            }

            Debug.Log("[AuthManager] Firebase 재초기화 시도...");
            InitializeFirebase();
        }

        /// <summary>
        /// Firebase 초기화 완료 대기 (타임아웃 적용)
        /// </summary>
        /// <param name="timeout">타임아웃 시간 (초)</param>
        /// <returns>초기화 성공 여부</returns>
        public async Task<bool> WaitForInitialization(float timeout = INIT_TIMEOUT)
        {
            float elapsedTime = 0f;

            while (!isInitialized && elapsedTime < timeout)
            {
                await Task.Delay(100); // 0.1초마다 체크
                elapsedTime += 0.1f;
            }

            if (!isInitialized)
            {
                Debug.LogError($"⏱️ AuthManager 초기화 타임아웃 ({timeout}초)");
            }

            return isInitialized;
        }

        private void OnDestroy()
        {
            if (auth != null)
            {
                auth.StateChanged -= OnAuthStateChanged;
            }

            // SessionManager 이벤트 구독 해제
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnForceLogout -= HandleForceLogout;
            }
        }

        private void OnAuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (auth.CurrentUser != currentUser)
            {
                bool signedIn = currentUser != auth.CurrentUser && auth.CurrentUser != null;

                if (!signedIn && currentUser != null)
                {
                    Debug.Log("사용자 로그아웃");
                }

                currentUser = auth.CurrentUser;

                if (signedIn)
                {
                    Debug.Log($"사용자 로그인: {currentUser.Email} (UID: {currentUser.UserId})");
                }
            }
        }
        #endregion

        #region Public Methods - Registration
        /// <summary>
        /// 이메일과 비밀번호로 회원가입
        /// </summary>
        /// <param name="email">이메일</param>
        /// <param name="password">비밀번호</param>
        /// <returns>성공 여부와 메시지</returns>
        public async Task<(bool success, string message)> RegisterWithEmail(string email, string password)
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return (false, "이메일과 비밀번호를 입력해주세요.");
                }

                if (password.Length < 6)
                {
                    return (false, "비밀번호는 최소 6자 이상이어야 합니다.");
                }

                // Firebase 회원가입
                AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

                if (result.User != null)
                {
                    currentUser = result.User;

                    // 이메일 인증 발송
                    await SendEmailVerification();

                    Debug.Log($"✅ 회원가입 성공: {email}");
                    return (true, "회원가입이 완료되었습니다. 이메일 인증을 진행해주세요.");
                }

                return (false, "회원가입에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"회원가입 에러: {ex.Message}");
                return (false, GetFirebaseErrorMessage(ex));
            }
        }

        /// <summary>
        /// 이메일 인증 발송
        /// </summary>
        public async Task<bool> SendEmailVerification()
        {
            if (currentUser == null)
            {
                Debug.LogError("로그인된 사용자가 없습니다.");
                return false;
            }

            try
            {
                await currentUser.SendEmailVerificationAsync();
                Debug.Log("✅ 이메일 인증 발송 완료");
                return true;
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"이메일 인증 발송 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Public Methods - Login
        /// <summary>
        /// 이메일과 비밀번호로 로그인
        /// </summary>
        /// <param name="email">이메일</param>
        /// <param name="password">비밀번호</param>
        /// <returns>성공 여부와 메시지</returns>
        public async Task<(bool success, string message)> LoginWithEmail(string email, string password)
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return (false, "이메일과 비밀번호를 입력해주세요.");
                }

                // Firebase 로그인
                AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);

                if (result.User != null)
                {
                    currentUser = result.User;

                    // 이메일 인증 확인 (선택사항)
                    // if (!currentUser.IsEmailVerified)
                    // {
                    //     return (false, "이메일 인증이 완료되지 않았습니다.");
                    // }

                    Debug.Log($"✅ 로그인 성공: {email}");
                    return (true, "로그인 성공!");
                }

                return (false, "로그인에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"로그인 에러: {ex.Message}");
                return (false, GetFirebaseErrorMessage(ex));
            }
        }
        #endregion

        #region Public Methods - Logout
        /// <summary>
        /// 로그아웃
        /// </summary>
        public async void Logout()
        {
            if (auth != null && currentUser != null)
            {
                string uid = currentUser.UserId;

                // Firebase 세션 종료
                auth.SignOut();
                currentUser = null;

                // Firestore 세션 정리
                if (SessionManager.Instance != null && SessionManager.Instance.IsInitialized)
                {
                    await SessionManager.Instance.ClearSession(uid);
                    Debug.Log("✅ 로그아웃 및 세션 정리 완료");
                }
                else
                {
                    Debug.Log("✅ 로그아웃 완료 (세션 정리 생략)");
                }
            }
        }
        #endregion

        #region Public Methods - Auto Login
        /// <summary>
        /// Firebase 세션을 통한 자동 로그인 체크
        /// </summary>
        /// <returns>자동 로그인 가능 여부</returns>
        public bool CanAutoLogin()
        {
            // Firebase 초기화 완료 여부 확인
            if (!isInitialized)
            {
                Debug.Log("[AuthManager] Firebase 초기화 대기 중...");
                return false;
            }

            // Firebase에 이미 로그인되어 있는지 확인
            if (auth != null && auth.CurrentUser != null)
            {
                currentUser = auth.CurrentUser;
                Debug.Log($"[AuthManager] 자동 로그인 가능: {currentUser.Email}");
                return true;
            }

            Debug.Log("[AuthManager] 자동 로그인 불가: 저장된 세션 없음");
            return false;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Firebase 에러 메시지를 한글로 변환
        /// </summary>
        private string GetFirebaseErrorMessage(FirebaseException ex)
        {
            string errorCode = ex.Message;

            if (errorCode.Contains("EMAIL_EXISTS") || errorCode.Contains("already in use"))
                return "이미 사용 중인 이메일입니다.";

            if (errorCode.Contains("INVALID_EMAIL") || errorCode.Contains("badly formatted"))
                return "올바르지 않은 이메일 형식입니다.";

            if (errorCode.Contains("WEAK_PASSWORD"))
                return "비밀번호가 너무 약합니다. 6자 이상 입력해주세요.";

            if (errorCode.Contains("USER_NOT_FOUND") || errorCode.Contains("no user record"))
                return "존재하지 않는 계정입니다.";

            if (errorCode.Contains("WRONG_PASSWORD") || errorCode.Contains("password is invalid"))
                return "비밀번호가 올바르지 않습니다.";

            if (errorCode.Contains("TOO_MANY_ATTEMPTS"))
                return "너무 많은 시도가 있었습니다. 잠시 후 다시 시도해주세요.";

            if (errorCode.Contains("NETWORK_ERROR"))
                return "네트워크 연결을 확인해주세요.";

            return $"오류가 발생했습니다: {errorCode}";
        }
        #endregion

        #region Force Logout Handler
        /// <summary>
        /// 강제 로그아웃 처리 (다른 곳에서 로그인하여 세션이 종료될 때 호출)
        /// </summary>
        private void HandleForceLogout()
        {
            Debug.LogWarning("⚠️ 다른 곳에서 로그인되어 강제 로그아웃됩니다.");

            // Firebase 로그아웃 처리
            Logout();

            // 시스템 메시지 표시 준비
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedForMessage;

            // JoinScene으로 이동
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowThenLoadLocal(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
            }
        }

        /// <summary>
        /// 씬 로드 완료 후 시스템 메시지 표시
        /// </summary>
        private void OnSceneLoadedForMessage(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // 이벤트 구독 해제
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedForMessage;

            // 시스템 메시지 표시 (씬이 로드된 후 약간의 지연을 두고 표시)
            UnityEngine.MonoBehaviour coroutineRunner = this;
            coroutineRunner.StartCoroutine(ShowForceLogoutMessageDelayed());
        }

        /// <summary>
        /// 강제 로그아웃 메시지 표시 (지연)
        /// </summary>
        private System.Collections.IEnumerator ShowForceLogoutMessageDelayed()
        {
            // 0.5초 대기 (씬 전환 완료 후 메시지 표시)
            yield return new UnityEngine.WaitForSeconds(0.5f);

            // 시스템 메시지 표시
            if (SystemMessageManager.Instance != null)
            {
                SystemMessageManager.Instance.ShowMessage("DuplicateLoginDetected");
            }
            else
            {
                Debug.LogWarning("⚠️ SystemMessageManager를 찾을 수 없습니다.");
            }
        }
        #endregion
    }
}

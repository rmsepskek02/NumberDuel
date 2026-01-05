using System;
using System.Collections.Generic;
using System.Linq;
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
        /// 현재 세션에서 사용한 로그인 방식 저장
        /// "google", "password", "anonymous" 등
        /// </summary>
        private string currentSessionLoginProvider = string.Empty;

        private const string PREF_KEY_LOGIN_PROVIDER = "LastLoginProvider";

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

        /// <summary>
        /// 이메일 인증 여부 확인
        /// </summary>
        public bool IsEmailVerified => currentUser?.IsEmailVerified ?? false;

        /// <summary>
        /// 익명 로그인 여부 확인
        /// </summary>
        public bool IsAnonymous => currentUser?.IsAnonymous ?? false;

        /// <summary>
        /// 이메일/비밀번호 전용 계정 여부 확인
        /// (소셜 로그인 없이 이메일로만 로그인 가능한 계정)
        /// </summary>
        public bool IsEmailOnlyAccount()
        {
            if (currentUser == null || currentUser.IsAnonymous)
                return false;

            bool hasPasswordProvider = false;
            bool hasSocialProvider = false;

            foreach (var provider in currentUser.ProviderData)
            {
                if (provider.ProviderId == "password")
                    hasPasswordProvider = true;
                else if (provider.ProviderId == "google.com" ||
                         provider.ProviderId == "playgames.google.com")
                    hasSocialProvider = true;
            }

            // 이메일 provider만 있고 소셜 provider 없음
            return hasPasswordProvider && !hasSocialProvider;
        }

        /// <summary>
        /// 계정 연동 완료 이벤트
        /// 게스트 → 이메일, 게스트 → Google, 이메일 → Google 등 모든 계정 연동 시 발생
        /// </summary>
        public event Action OnAccountLinked;

        /// <summary>
        /// 이메일 계정 연동 완료 이벤트
        /// 게스트 → 이메일 연동 시에만 발생
        /// </summary>
        public event Action OnEmailLinked;

        /// <summary>
        /// Google 계정 연동 완료 이벤트
        /// 게스트 → Google, 이메일 → Google 연동 시에만 발생
        /// </summary>
        public event Action OnGoogleLinked;

        /// <summary>
        /// 이메일 연동 완료 알림
        /// </summary>
        public void NotifyEmailLinked()
        {
            OnEmailLinked?.Invoke();
            OnAccountLinked?.Invoke();
        }

        /// <summary>
        /// Google 연동 완료 알림
        /// </summary>
        public void NotifyGoogleLinked()
        {
            OnGoogleLinked?.Invoke();
            OnAccountLinked?.Invoke();
        }

        /// <summary>
        /// 계정 연동 완료 알림 (일반)
        /// </summary>
        public void NotifyAccountLinked()
        {
            OnAccountLinked?.Invoke();
        }

        /// <summary>
        /// 로그인 방식 저장 (PlayerPrefs)
        /// </summary>
        private void SaveLoginProvider(string providerId)
        {
            currentSessionLoginProvider = providerId;
            UnityEngine.PlayerPrefs.SetString(PREF_KEY_LOGIN_PROVIDER, providerId);
            UnityEngine.PlayerPrefs.Save();
        }

        /// <summary>
        /// 저장된 로그인 방식 불러오기 (PlayerPrefs)
        /// </summary>
        private void LoadLoginProvider()
        {
            currentSessionLoginProvider = UnityEngine.PlayerPrefs.GetString(PREF_KEY_LOGIN_PROVIDER, string.Empty);
        }

        /// <summary>
        /// 저장된 로그인 방식 삭제 (로그아웃 시)
        /// </summary>
        private void ClearLoginProvider()
        {
            currentSessionLoginProvider = string.Empty;
            UnityEngine.PlayerPrefs.DeleteKey(PREF_KEY_LOGIN_PROVIDER);
            UnityEngine.PlayerPrefs.Save();
        }
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            // 에디터에서 빌드 중일 때는 Firebase 초기화 스킵
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                return;
            }
#endif

            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            // 이미 초기화 중이거나 완료된 경우 중복 호출 방지
            if (isInitialized)
            {
                return;
            }

            // FirebaseInitializer가 이미 초기화를 완료했는지 확인
            if (FirebaseInitializer.IsFirebaseReady)
            {
                // 즉시 사용 가능
                InitializeAuthInstance();
            }
            else
            {
                // FirebaseInitializer 초기화 대기
                StartCoroutine(WaitForFirebaseInitializer());
            }
        }

        /// <summary>
        /// FirebaseAuth 인스턴스 초기화 (FirebaseInitializer 초기화 완료 후 호출)
        /// </summary>
        private void InitializeAuthInstance()
        {
            try
            {
                auth = FirebaseAuth.DefaultInstance;

                // 인증 상태 변경 이벤트 등록
                auth.StateChanged += OnAuthStateChanged;

                // 이미 로그인되어 있는지 확인
                OnAuthStateChanged(this, null);

                // ⭐ 저장된 로그인 방식 불러오기
                LoadLoginProvider();

                isInitialized = true; // 초기화 완료 플래그 설정

                // SessionManager의 강제 로그아웃 이벤트 구독
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.OnForceLogout += HandleForceLogout;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuthManager] ❌ Auth 인스턴스 초기화 실패: {ex.Message}");
                isInitialized = false;
            }
        }

        /// <summary>
        /// FirebaseInitializer 초기화 완료 대기
        /// </summary>
        private System.Collections.IEnumerator WaitForFirebaseInitializer()
        {
            float elapsedTime = 0f;
            const float timeout = 15f; // 15초 타임아웃

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
                Debug.LogError($"[AuthManager] ⏱️ FirebaseInitializer 초기화 타임아웃 ({timeout}초)");
                Debug.LogError($"[AuthManager]    마지막 상태: {FirebaseInitializer.DependencyStatus}");
                isInitialized = false;
            }
        }

        /// <summary>
        /// Firebase 재초기화 시도 (초기화 실패 시 호출)
        /// </summary>
        public void RetryInitialization()
        {
            if (isInitialized)
            {
                return;
            }

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

        protected override void OnDestroy()
        {
            if (auth != null)
            {
                auth.StateChanged -= OnAuthStateChanged;
            }

            // SessionManager 이벤트 구독 해제
            // FindAnyObjectByType 사용으로 OnDestroy 중 GameObject 재생성 방지
            var sessionManager = FindAnyObjectByType<SessionManager>();
            if (sessionManager != null)
            {
                sessionManager.OnForceLogout -= HandleForceLogout;
            }

            // 베이스 클래스의 OnDestroy 호출 (싱글톤 인스턴스 정리)
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

        #region Public Methods - Anonymous Login
        /// <summary>
        /// 익명 로그인
        /// </summary>
        /// <returns>성공 여부</returns>
        public async Task<bool> SignInAnonymously()
        {
            try
            {
                // Firebase 초기화 확인
                if (!isInitialized || auth == null)
                {
                    Debug.LogError("[AuthManager] Firebase가 초기화되지 않았습니다.");
                    return false;
                }

                // 시스템 메시지: 로그인 중
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("GuestLoginInProgress");
                }

                // Firebase 익명 로그인
                var result = await auth.SignInAnonymouslyAsync();

                if (result.User != null)
                {
                    currentUser = result.User;
                    SaveLoginProvider("anonymous"); // 세션 로그인 방식 저장
                    string userId = currentUser.UserId;

                    // Firestore에 익명 사용자 프로필 생성
                    string nickname = await GenerateUniqueGuestNickname();
                    bool profileCreated = await CreateAnonymousUserProfile(userId, nickname);

                    if (!profileCreated)
                    {
                        Debug.LogError("[AuthManager] 익명 사용자 프로필 생성 실패");
                        return false;
                    }

                    // 시스템 메시지: 로그인 성공
                    if (SystemMessageManager.Instance != null)
                    {
                        SystemMessageManager.Instance.ShowMessage("GuestLoginSuccess");
                    }

                    return true;
                }

                return false;
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AuthManager] 익명 로그인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 고유한 게스트 닉네임 생성 (Guest_xxxxxx 형식, 6자리 영숫자)
        /// Firestore에서 중복 확인
        /// </summary>
        private async Task<string> GenerateUniqueGuestNickname()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            int maxAttempts = 10; // 최대 10번 시도

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 6자리 랜덤 문자열 생성
                string randomSuffix = "";
                for (int i = 0; i < 6; i++)
                {
                    randomSuffix += chars[UnityEngine.Random.Range(0, chars.Length)];
                }

                string nickname = $"Guest_{randomSuffix}";

                // Firestore에서 중복 확인
                if (DatabaseManager.Instance != null)
                {
                    bool isAvailable = await DatabaseManager.Instance.IsNicknameAvailable(nickname);

                    if (isAvailable)
                    {
                        return nickname;
                    }
                }
            }

            // 최대 시도 횟수 초과 시 타임스탬프 추가
            long timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string fallbackNickname = $"Guest_{timestamp.ToString().Substring(timestamp.ToString().Length - 6)}";
            return fallbackNickname;
        }

        /// <summary>
        /// 익명 사용자 Firestore 프로필 생성
        /// </summary>
        private async Task<bool> CreateAnonymousUserProfile(string userId, string nickname)
        {
            try
            {
                if (DatabaseManager.Instance != null)
                {
                    // DatabaseManager의 CreateUserProfile 메서드 사용
                    // 이메일은 빈 문자열, emailVerified는 false, authProvider는 "Guest"
                    bool created = await DatabaseManager.Instance.CreateUserProfile(
                        userId,
                        "", // 익명 계정은 이메일 없음
                        nickname,
                        emailVerified: false,
                        authProvider: "Guest" // 게스트 로그인
                    );

                    return created;
                }

                Debug.LogError("[AuthManager] DatabaseManager를 찾을 수 없습니다.");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuthManager] 익명 사용자 프로필 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 계정 삭제 (익명 계정 탈퇴 및 일반 계정 탈퇴)
        /// </summary>
        public async Task<bool> DeleteAccount()
        {
            var user = currentUser;

            if (user == null)
            {
                Debug.LogError("[AuthManager] 로그인된 사용자가 없습니다.");
                return false;
            }

            try
            {
                // 시스템 메시지: 처리 중
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("AccountDeletionInProgress");
                }

                string userId = user.UserId;

                // Firebase Auth 계정 삭제
                await user.DeleteAsync();
                currentUser = null;

                // 시스템 메시지: 탈퇴 완료
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("AccountDeletionComplete");
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AuthManager] 계정 삭제 실패: {e.Message}");
                return false;
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
                    SaveLoginProvider("password"); // 세션 로그인 방식 저장

                    // 이메일 인증 확인 (선택사항)
                    // if (!currentUser.IsEmailVerified)
                    // {
                    //     return (false, "이메일 인증이 완료되지 않았습니다.");
                    // }

                    return (true, "로그인 성공!");
                }

                return (false, "로그인에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"로그인 에러: {ex.Message}");

                // ⭐ 소셜 로그인 전용 계정 체크
                if (ex.Message.Contains("INVALID_PASSWORD") || ex.Message.Contains("password is invalid"))
                {
                    // Provider 확인
                    var providers = await GetProvidersForEmail(email);

                    if (providers.Contains("google.com") && !providers.Contains("password"))
                    {
                        // Google로만 가입된 계정
                        return (false, "SOCIAL_LOGIN_ONLY::Google");
                    }
                }

                return (false, GetFirebaseErrorMessage(ex));
            }
        }
        #endregion

        #region Public Methods - Logout
        /// <summary>
        /// 로그아웃 (수동 로그아웃 - 로그아웃 버튼 클릭 시)
        /// </summary>
        public async void Logout()
        {
            if (auth != null && currentUser != null)
            {
                string uid = currentUser.UserId;

                // ✅ 1. 먼저 SessionManager 리스너 중지 (자기 자신의 세션 삭제를 감지하지 않도록)
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.StopSessionMonitoring();
                }

                // ✅ 2. Firestore 세션 정리
                if (SessionManager.Instance != null && SessionManager.Instance.IsInitialized)
                {
                    await SessionManager.Instance.ClearSession(uid);
                }

                // ✅ 3. Firebase 세션 종료
                auth.SignOut();
                currentUser = null;

                // ✅ 4. 저장된 로그인 방식 삭제
                ClearLoginProvider();
            }
        }

        /// <summary>
        /// Firebase에서만 로그아웃 (Firestore 세션은 유지)
        /// 중복 로그인 팝업에서 취소 버튼 클릭 시 사용
        /// Client B가 로그인을 취소해도 Client A의 세션이 삭제되지 않도록 함
        /// </summary>
        public void SignOutWithoutSessionClear()
        {
            if (auth != null && currentUser != null)
            {

                // Firebase 세션만 종료
                auth.SignOut();
                currentUser = null;

                // ⚠️ 로그인 방식은 유지 (재로그인 시 복구용)
                // ClearLoginProvider()를 호출하지 않음
            }
        }

        /// <summary>
        /// 현재 사용자에게 이메일 인증 메일 발송
        /// </summary>
        /// <returns>성공 여부와 메시지</returns>
        public async Task<(bool success, string message)> SendVerificationEmail()
        {
            if (currentUser == null)
            {
                return (false, "로그인이 필요합니다");
            }

            if (currentUser.IsEmailVerified)
            {
                return (false, "이미 인증된 이메일입니다");
            }

            try
            {
                await currentUser.SendEmailVerificationAsync();
                return (true, "인증 이메일을 발송했습니다");
            }
            catch (FirebaseException ex)
            {
                string errorMessage = GetFirebaseErrorMessage(ex);
                Debug.LogError($"[AuthManager] 인증 이메일 발송 실패: {errorMessage}");

                // Rate Limiting 에러인 경우 (이미 이메일이 발송되었을 가능성 높음)
                if (errorMessage.Contains("unusual activity") || errorMessage.Contains("blocked"))
                {
                    return (true, "인증 이메일을 발송했습니다");
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 인증 이메일 발송 실패: {ex.Message}");
                return (false, "인증 이메일 발송에 실패했습니다");
            }
        }

        /// <summary>
        /// 비밀번호 재설정 이메일 발송
        /// </summary>
        /// <param name="email">비밀번호를 재설정할 이메일 주소</param>
        /// <returns>성공 여부와 메시지</returns>
        public async Task<(bool success, string message)> SendPasswordResetEmail(string email)
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrEmpty(email))
                {
                    return (false, "이메일을 입력해주세요.");
                }

                // Firebase 초기화 확인
                if (!isInitialized || auth == null)
                {
                    return (false, "Firebase가 초기화되지 않았습니다.");
                }

                // 비밀번호 재설정 이메일 발송
                await auth.SendPasswordResetEmailAsync(email);
                return (true, "비밀번호 재설정 이메일을 발송했습니다");
            }
            catch (FirebaseException ex)
            {
                string errorMessage = GetFirebaseErrorMessage(ex);
                Debug.LogError($"[AuthManager] 비밀번호 재설정 이메일 발송 실패: {errorMessage}");

                // Rate Limiting 에러인 경우 (이미 이메일이 발송되었을 가능성 높음)
                if (errorMessage.Contains("unusual activity") || errorMessage.Contains("blocked"))
                {
                    return (true, "비밀번호 재설정 이메일을 발송했습니다");
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 비밀번호 재설정 이메일 발송 실패: {ex.Message}");
                return (false, "비밀번호 재설정 이메일 발송에 실패했습니다");
            }
        }

        /// <summary>
        /// 사용자 정보 새로고침 (인증 상태 업데이트)
        /// </summary>
        /// <returns>성공 여부</returns>
        public async Task<bool> ReloadUserInfo()
        {
            if (currentUser == null)
            {
                Debug.LogWarning("[AuthManager] 로그인된 사용자가 없습니다");
                return false;
            }

            try
            {
                await currentUser.ReloadAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 사용자 정보 새로고침 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 사용자의 비밀번호 업데이트
        /// Step 2에서 사용 (이메일 인증 후 비밀번호 설정)
        /// </summary>
        /// <param name="newPassword">새로운 비밀번호</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> UpdatePassword(string newPassword)
        {
            if (currentUser == null)
            {
                Debug.LogError("[AuthManager] 로그인된 사용자가 없습니다");
                return false;
            }

            try
            {
                await currentUser.UpdatePasswordAsync(newPassword);
                return true;
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AuthManager] 비밀번호 업데이트 실패: {ex.Message}");
                return false;
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
                return false;
            }

            // Firebase에 이미 로그인되어 있는지 확인
            if (auth != null && auth.CurrentUser != null)
            {
                currentUser = auth.CurrentUser;
                return true;
            }

            return false;
        }
        #endregion

        #region Public Methods - Google Login
        /// <summary>
        /// Google 계정으로 로그인
        /// </summary>
        /// <returns>성공 여부와 메시지</returns>
        public async Task<(bool success, string message, string email)> LoginWithGoogle()
        {
            try
            {
                // Firebase 초기화 확인
                if (!isInitialized || auth == null)
                {
                    return (false, "Firebase가 초기화되지 않았습니다.", string.Empty);
                }

                // GoogleAuthProvider 생성
                var googleProvider = new Objects.Auth.GoogleAuthProvider(auth);

                // Google 로그인 시도
                var result = await googleProvider.SignIn();

                if (!result.Success)
                {
                    // 취소된 경우 조용히 처리
                    if (result.Message == "CANCELED")
                    {
                        return (false, "CANCELED", string.Empty);
                    }

                    // 계정이 이미 다른 방법으로 존재하는 경우
                    if (result.Message == "ACCOUNT_EXISTS")
                    {
                        return (false, "ACCOUNT_EXISTS", result.Email);
                    }

                    return (false, result.Message, string.Empty);
                }

                // 현재 사용자 업데이트
                currentUser = auth.CurrentUser;

                if (currentUser == null)
                {
                    return (false, "로그인에 실패했습니다.", string.Empty);
                }

                // ⭐ 세션 로그인 방식 저장 (google.com 또는 playgames.google.com)
                var providers = GetCurrentUserProviders();
                if (providers.Contains("playgames.google.com"))
                    SaveLoginProvider("playgames.google.com");
                else if (providers.Contains("google.com"))
                    SaveLoginProvider("google.com");

                // ⭐ Google 로그인 시 이메일은 ProviderData에서 가져와야 함
                string email = GetCurrentUserEmailFromProvider();
                if (string.IsNullOrEmpty(email))
                    email = currentUser.Email ?? string.Empty;

                return (true, "Google 로그인 성공", email);
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AuthManager] Google 로그인 에러: {ex.Message}");
                return (false, GetFirebaseErrorMessage(ex), string.Empty);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Google 로그인 예외: {ex.Message}");
                return (false, "Google 로그인 중 오류가 발생했습니다.", string.Empty);
            }
        }

        /// <summary>
        /// 이메일로 등록된 Provider 확인
        /// </summary>
        public async Task<List<string>> GetProvidersForEmail(string email)
        {
            try
            {
                if (!isInitialized || auth == null)
                {
                    return new List<string>();
                }

                var providers = await auth.FetchProvidersForEmailAsync(email);
                return providers?.ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthManager] Provider 조회 실패: {ex.Message}");
                return new List<string>();
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Firebase 에러 메시지를 한글로 변환 (static으로 외부에서도 사용 가능)
        /// </summary>
        public static string GetFirebaseErrorMessage(FirebaseException ex)
        {
            string errorCode = ex.Message;

            // 계정 이미 존재 (다른 Provider로 가입됨)
            if (errorCode.Contains("account-exists-with-different-credential"))
                return "ACCOUNT_EXISTS"; // 특별한 코드 반환

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

            if (errorCode.Contains("TOO_MANY_ATTEMPTS") || errorCode.Contains("too-many-requests"))
                return "너무 많은 시도가 있었습니다. 잠시 후 다시 시도해주세요.";

            if (errorCode.Contains("NETWORK_ERROR") || errorCode.Contains("network"))
                return "네트워크 연결을 확인해주세요.";

            // 유효하지 않은 Credential
            if (errorCode.Contains("invalid-credential"))
                return "인증 정보가 유효하지 않습니다. 다시 시도해주세요.";

            // 팝업 차단
            if (errorCode.Contains("popup-blocked"))
                return "팝업이 차단되었습니다. 브라우저 설정을 확인해주세요.";

            return $"오류가 발생했습니다: {errorCode}";
        }
        #endregion

        #region Force Logout Handler
        /// <summary>
        /// 강제 로그아웃 처리 (다른 곳에서 로그인하여 세션이 종료될 때 호출)
        /// </summary>
        private void HandleForceLogout()
        {
            // 설정 UI가 열려있으면 닫기
            if (SettingsManager.Instance != null && SettingsManager.Instance.IsSettingsOpen)
            {
                SettingsManager.Instance.HideSettings();
            }

            // 🔥 중요: Logout() 대신 SignOutWithoutSessionClear() 사용!
            // 이유: 세션은 이미 다른 클라이언트(Client B)가 사용 중이므로 삭제하면 안됨
            // Logout()을 호출하면 ClearSession()이 실행되어 Client B의 세션까지 삭제됨
            SignOutWithoutSessionClear();

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
        }
        #endregion

        #region Public Methods - Account Linking

        /// <summary>
        /// 현재 사용자의 로그인 Provider 목록 가져오기
        /// </summary>
        public List<string> GetCurrentUserProviders()
        {
            if (currentUser == null)
                return new List<string>();

            return currentUser.ProviderData
                .Select(provider => provider.ProviderId)
                .ToList();
        }

        /// <summary>
        /// 현재 사용자의 이메일 (Google 또는 email/password에서)
        /// </summary>
        public string GetCurrentUserEmailFromProvider()
        {
            if (currentUser == null)
                return string.Empty;

            // Google provider에서 이메일 가져오기
            var googleProvider = currentUser.ProviderData
                .FirstOrDefault(p => p.ProviderId == "google.com");

            if (googleProvider != null)
                return googleProvider.Email;

            // email/password provider에서 이메일 가져오기
            var emailProvider = currentUser.ProviderData
                .FirstOrDefault(p => p.ProviderId == "password");

            if (emailProvider != null)
                return emailProvider.Email;

            // Firebase User 이메일
            return currentUser.Email ?? string.Empty;
        }

        /// <summary>
        /// 현재 Google 로그인 상태인지 확인
        /// </summary>
        /// <returns>Google 또는 Play Games로 로그인한 경우 true</returns>
        public bool IsGoogleLogin()
        {
            if (currentUser == null)
                return false;

            var providers = currentUser.ProviderData;
            return providers?.Any(p => p.ProviderId == "playgames.google.com" ||
                                      p.ProviderId == "google.com") ?? false;
        }

        /// <summary>
        /// Google 계정에 비밀번호 연동 가능 여부 확인
        /// (Google 또는 Play Games로 로그인했지만 아직 password provider가 없는 경우)
        /// </summary>
        public bool CanLinkPasswordToGoogle()
        {
            if (currentUser == null)
                return false;

            var providers = GetCurrentUserProviders();

            // Google 또는 Play Games는 있고, password는 없어야 함
            bool hasGoogle = providers.Contains("google.com") || providers.Contains("playgames.google.com");
            bool hasPassword = providers.Contains("password");

            return hasGoogle && !hasPassword;
        }

        /// <summary>
        /// 이메일 계정에 Google 연동 가능 여부 확인
        /// (email/password로 로그인했지만 아직 Google 또는 Play Games provider가 없는 경우)
        /// </summary>
        public bool CanLinkGoogleToEmail()
        {
            if (currentUser == null)
                return false;

            var providers = GetCurrentUserProviders();

            // password는 있고, Google/Play Games는 없어야 함
            bool hasGoogle = providers.Contains("google.com") || providers.Contains("playgames.google.com");
            bool hasPassword = providers.Contains("password");

            return hasPassword && !hasGoogle;
        }

        /// <summary>
        /// Google 계정에 비밀번호 연동
        /// (모바일: Google 로그인 → PC에서도 플레이하기)
        /// </summary>
        /// <param name="email">연동할 이메일 주소 (Play Games는 이메일 미제공이므로 사용자 입력 필요)</param>
        /// <param name="password">설정할 비밀번호</param>
        /// <returns>성공 여부, 메시지</returns>
        public async Task<(bool success, string message)> LinkPasswordToGoogle(string email, string password)
        {
            try
            {
                // 로그인 확인
                if (currentUser == null)
                {
                    return (false, "로그인이 필요합니다.");
                }

                // Google provider 확인
                if (!CanLinkPasswordToGoogle())
                {
                    return (false, "Google 계정이 아니거나 이미 연동되어 있습니다.");
                }

                // 이메일 검증
                if (string.IsNullOrEmpty(email))
                {
                    return (false, "이메일을 입력해주세요.");
                }

                // 비밀번호 검증
                if (string.IsNullOrEmpty(password))
                {
                    return (false, "비밀번호를 입력해주세요.");
                }

                if (password.Length < 6)
                {
                    return (false, "비밀번호는 최소 6자 이상이어야 합니다.");
                }

                // Credential 생성 (사용자가 입력한 이메일 사용)
                Firebase.Auth.Credential credential = Firebase.Auth.EmailAuthProvider.GetCredential(email, password);

                // 연동 시도
                var result = await currentUser.LinkWithCredentialAsync(credential);

                if (result.User != null)
                {
                    // 사용자 정보 새로고침
                    await ReloadUserInfo();

                    // ⭐ Google 계정은 이미 인증되어 있으므로 별도 이메일 인증 불필요
                    return (true, "PC 로그인 연동이 완료되었습니다!");
                }

                return (false, "계정 연동에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AuthManager] 계정 연동 에러: {ex.Message}");

                // 이미 연동된 경우
                if (ex.Message.Contains("PROVIDER_ALREADY_LINKED"))
                {
                    return (false, "이미 연동되어 있습니다.");
                }

                // 이메일 중복 (다른 계정에서 사용 중)
                if (ex.Message.Contains("EMAIL_EXISTS") || ex.Message.Contains("already in use"))
                {
                    return (false, "이 이메일은 다른 계정에서 사용 중입니다.");
                }

                return (false, GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 계정 연동 예외: {ex.Message}");
                return (false, "계정 연동 중 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 이메일 계정에 Google 연동
        /// (PC: 이메일 로그인 → 모바일에서 Google 연동)
        /// </summary>
        /// <returns>성공 여부, 메시지</returns>
        public async Task<(bool success, string message)> LinkGoogleToEmail()
        {
            try
            {
                // 로그인 확인
                if (currentUser == null)
                {
                    return (false, "로그인이 필요합니다.");
                }

                // email/password provider 확인
                if (!CanLinkGoogleToEmail())
                {
                    return (false, "이메일 계정이 아니거나 이미 연동되어 있습니다.");
                }

                // 현재 계정의 이메일
                string currentEmail = GetCurrentUserEmailFromProvider();

                if (string.IsNullOrEmpty(currentEmail))
                {
                    return (false, "계정 이메일을 찾을 수 없습니다.");
                }

                // GoogleAuthProvider를 통해 Google 로그인 수행
                var googleProvider = new Objects.Auth.GoogleAuthProvider(auth);
                var googleResult = await googleProvider.SignIn();

                if (!googleResult.Success)
                {
                    // 취소된 경우
                    if (googleResult.Message == "CANCELED")
                    {
                        return (false, "Google 로그인이 취소되었습니다.");
                    }

                    return (false, googleResult.Message);
                }

#if UNITY_ANDROID
                // Android (Play Games)는 이메일을 제공하지 않으므로 이메일 확인 건너뜀
#else
                // PC/iOS는 Firebase OAuth를 사용하므로 이메일 일치 확인
                string googleEmail = googleResult.Email;

                if (string.IsNullOrEmpty(googleEmail))
                {
                    return (false, "Google 계정 이메일을 가져올 수 없습니다.");
                }

                // 이메일 일치 여부 확인
                if (!currentEmail.Equals(googleEmail, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"이메일이 일치하지 않습니다.\n\n현재 계정: {currentEmail}\nGoogle 계정: {googleEmail}");
                }
#endif

                // Google Credential로 연동
                if (googleResult.Credential != null)
                {
                    var result = await currentUser.LinkWithCredentialAsync(googleResult.Credential);

                    if (result.User != null)
                    {
                        // 사용자 정보 새로고침
                        await ReloadUserInfo();

                        return (true, "Google 계정 연동이 완료되었습니다!");
                    }
                }

                return (false, "Google 계정 연동에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AuthManager] Google 연동 에러: {ex.Message}");

                // 이미 연동된 경우
                if (ex.Message.Contains("PROVIDER_ALREADY_LINKED"))
                {
                    return (false, "이미 Google 계정이 연동되어 있습니다.");
                }

                // 다른 계정에서 사용 중
                if (ex.Message.Contains("CREDENTIAL_ALREADY_IN_USE"))
                {
                    return (false, "이 Google 계정은 다른 사용자가 사용 중입니다.");
                }

                return (false, GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Google 연동 예외: {ex.Message}");
                return (false, "Google 계정 연동 중 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// Google 계정에 이메일 provider 연동 (게스트 전환과 동일한 프로세스)
        /// Step 1: 이메일/비밀번호 입력
        /// Step 2: 닉네임 설정 (선택)
        /// Step 3: 이메일 인증
        /// (모바일: Google 로그인 → PC에서도 플레이하기)
        /// </summary>
        /// <param name="email">연동할 이메일</param>
        /// <param name="password">설정할 비밀번호</param>
        /// <param name="nickname">닉네임 (선택)</param>
        /// <returns>성공 여부, 메시지, 이메일</returns>
        public async Task<(bool success, string message, string email)> LinkEmailToGoogle(string email, string password, string nickname = null)
        {
            try
            {
                // 1. 로그인 확인
                if (currentUser == null)
                {
                    return (false, "로그인이 필요합니다.", null);
                }

                // 2. Google provider 확인
                if (!CanLinkPasswordToGoogle())
                {
                    return (false, "Google 계정이 아니거나 이미 연동되어 있습니다.", null);
                }

                // 3. 이메일/비밀번호 검증
                if (string.IsNullOrEmpty(email))
                {
                    return (false, "이메일을 입력해주세요.", null);
                }

                if (string.IsNullOrEmpty(password) || password.Length < 6)
                {
                    return (false, "비밀번호는 최소 6자 이상이어야 합니다.", null);
                }

                // 4. Credential 생성
                Firebase.Auth.Credential credential = Firebase.Auth.EmailAuthProvider.GetCredential(email, password);

                // 5. Email provider 연동
                var result = await currentUser.LinkWithCredentialAsync(credential);

                if (result.User == null)
                {
                    return (false, "계정 연동에 실패했습니다.", null);
                }

                // 6. 닉네임 업데이트 (제공된 경우)
                if (!string.IsNullOrEmpty(nickname))
                {
                    string uid = currentUser.UserId;
                    bool nicknameUpdated = await DatabaseManager.Instance.UpdateNickname(uid, nickname);

                    if (nicknameUpdated)
                    {
                        // Firebase User의 DisplayName도 업데이트
                        var profile = new Firebase.Auth.UserProfile { DisplayName = nickname };
                        await currentUser.UpdateUserProfileAsync(profile);
                    }
                }

                // 7. 이메일 인증 메일 발송
                await currentUser.SendEmailVerificationAsync();

                // 8. 사용자 정보 새로고침
                await ReloadUserInfo();

                return (true, "이메일 인증 메일이 발송되었습니다.", email);
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AuthManager] Google → Email 연동 에러: {ex.Message}");

                if (ex.Message.Contains("PROVIDER_ALREADY_LINKED"))
                {
                    return (false, "이미 연동되어 있습니다.", null);
                }

                if (ex.Message.Contains("EMAIL_EXISTS") || ex.Message.Contains("already in use"))
                {
                    return (false, "이 이메일은 다른 계정에서 사용 중입니다.", null);
                }

                return (false, GetFirebaseErrorMessage(ex), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Google → Email 연동 예외: {ex.Message}");
                return (false, "계정 연동 중 오류가 발생했습니다.", null);
            }
        }

        /// <summary>
        /// 현재 로그인 방식 표시용 문자열 반환
        /// - "구글": Play Games Services로 로그인
        /// - "이메일": 이메일/비밀번호로 로그인
        /// - "구글 + 이메일": 두 방식 모두 연동됨
        /// - "손님": 게스트 로그인
        /// </summary>
        public string GetLoginMethodDisplayName(bool showCurrentSessionOnly = false)
        {
            if (currentUser == null)
                return "로그인 안됨";

            // 익명(게스트) 로그인 체크
            if (IsAnonymous)
                return "손님";

            var providers = GetCurrentUserProviders();

            bool hasGoogle = providers.Contains("google.com");
            bool hasPassword = providers.Contains("password");
            bool hasPlayGames = providers.Contains("playgames.google.com");

            // 두 방식 모두 연동된 경우
            if ((hasGoogle || hasPlayGames) && hasPassword)
            {
                if (showCurrentSessionOnly)
                {
                    // ⭐ 현재 세션에서 사용한 로그인 방식만 표시
                    if (currentSessionLoginProvider == "password")
                        return "이메일";
                    else if (currentSessionLoginProvider == "google.com" || currentSessionLoginProvider == "playgames.google.com")
                        return "구글";
                    else
                        // 세션 정보가 없으면 Email 우선 (PC 로그인 가능)
                        return "이메일";
                }
                else
                {
                    // 둘 다 표시
                    return "구글 + 이메일";
                }
            }

            // Google (Play Games) 로그인
            if (hasGoogle || hasPlayGames)
                return "구글";

            // 이메일/비밀번호 로그인
            if (hasPassword)
                return "이메일";

            // 예외 상황
            return "알 수 없음";
        }

        /// <summary>
        /// 연동 완료 여부 확인
        /// </summary>
        public bool IsAccountFullyLinked()
        {
            if (currentUser == null)
                return false;

            var providers = GetCurrentUserProviders();

            // Google (또는 Play Games)과 password 둘 다 있으면 완전 연동
            bool hasGoogle = providers.Contains("google.com") || providers.Contains("playgames.google.com");
            bool hasPassword = providers.Contains("password");

            return hasGoogle && hasPassword;
        }

        /// <summary>
        /// 게스트 계정을 Google 계정으로 전환
        /// (Anonymous Auth → Google Auth with Link)
        /// </summary>
        public async Task<(bool success, string message)> ConvertGuestToGoogle()
        {
            try
            {
                // 1. 게스트 확인
                if (currentUser == null)
                    return (false, "로그인이 필요합니다.");

                if (!IsAnonymous)
                    return (false, "게스트 계정이 아닙니다.");

                // 2. 이미 Google 연동되어 있는지 체크
                var providers = currentUser.ProviderData;
                foreach (var provider in providers)
                {
                    if (provider.ProviderId == "google.com" || provider.ProviderId == "playgames.google.com")
                    {
                        return (false, "이미 Google 계정이 연동되어 있습니다.");
                    }
                }

                // 3. 게스트 UID 저장 (연동 후에도 유지됨)
                string guestUID = currentUser.UserId;
                Debug.Log($"[AuthManager] 게스트 UID: {guestUID}");

                // 4. Google Credential만 획득 (새 계정 생성 안 함)
                var googleProvider = new Objects.Auth.GoogleAuthProvider(auth);
                var googleResult = await googleProvider.GetCredentialOnly();

                if (!googleResult.Success)
                    return (false, googleResult.Message);

                var googleCredential = googleResult.Credential;

                if (googleCredential == null)
                    return (false, "Google 인증 정보를 가져올 수 없습니다.");

                // 5. 게스트 계정에 Google Credential 연결 (UID 유지)
                var linkResult = await currentUser.LinkWithCredentialAsync(googleCredential);

                if (linkResult == null || linkResult.User == null)
                    return (false, "Google 계정 전환에 실패했습니다.");

                // 6. currentUser 업데이트 및 UID 유지 확인
                currentUser = linkResult.User;
                Debug.Log($"[AuthManager] 연동 후 UID: {currentUser.UserId} (유지: {guestUID == currentUser.UserId})");

                // 7. Firebase Display Name 업데이트 (Play Games Display Name)
                if (!string.IsNullOrEmpty(googleResult.DisplayName))
                {
                    var profile = new UserProfile { DisplayName = googleResult.DisplayName };
                    await currentUser.UpdateUserProfileAsync(profile);
                    Debug.Log($"[AuthManager] Display Name 업데이트: {googleResult.DisplayName}");
                }

                // 8. Firebase Database의 AuthProvider 업데이트 (같은 UID)
                if (DatabaseManager.Instance != null)
                {
                    // UserProfile의 AuthProvider를 "Google"로 변경
                    await DatabaseManager.Instance.UpdateUserAuthProvider(guestUID, "Google");

                    Debug.Log($"[AuthManager] 게스트 계정이 Google 계정으로 전환되었습니다. UID: {guestUID}");
                }

                return (true, "Google 계정으로 전환되었습니다.");
            }
            catch (FirebaseException ex)
            {
                // 중복 연동 에러 체크
                if (ex.ErrorCode == (int)AuthError.ProviderAlreadyLinked)
                {
                    return (false, "이미 Google 계정이 연동되어 있습니다.");
                }

                Debug.LogError($"[AuthManager] Google 전환 실패: {ex.Message}");
                return (false, GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Google 전환 실패: {ex.Message}");
                return (false, $"오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 게스트 계정을 이메일 계정으로 전환
        /// (Anonymous Auth → Email Auth with Link)
        /// </summary>
        /// <param name="email">연동할 이메일</param>
        /// <param name="password">설정할 비밀번호</param>
        /// <param name="nickname">새 닉네임 (null이면 기존 닉네임 유지)</param>
        public async Task<(bool success, string message)> ConvertGuestToEmail(
            string email,
            string password,
            string nickname = null)
        {
            try
            {
                // 1. 게스트 확인
                if (currentUser == null)
                    return (false, "로그인이 필요합니다.");

                if (!IsAnonymous)
                    return (false, "게스트 계정이 아닙니다.");

                // 2. 입력 검증
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    return (false, "이메일과 비밀번호를 입력해주세요.");

                if (password.Length < 6)
                    return (false, "비밀번호는 최소 6자 이상이어야 합니다.");

                // 3. 이메일 중복 확인 (Firestore)
                if (DatabaseManager.Instance != null)
                {
                    bool isRegistered = await DatabaseManager.Instance.IsEmailRegistered(email);
                    if (isRegistered)
                        return (false, "이미 사용 중인 이메일입니다.");
                }

                // 4. 닉네임 중복 확인 (새 닉네임이 제공된 경우만)
                if (!string.IsNullOrEmpty(nickname) && DatabaseManager.Instance != null)
                {
                    bool isAvailable = await DatabaseManager.Instance.IsNicknameAvailable(nickname);
                    if (!isAvailable)
                        return (false, "이미 사용 중인 닉네임입니다.");
                }

                // 5. 게스트 UID 저장 (연동 후에도 유지됨)
                string guestUID = currentUser.UserId;
                Debug.Log($"[AuthManager] 게스트 UID: {guestUID}");

                // 6. 이메일/비밀번호 Credential 생성
                var emailCredential = Firebase.Auth.EmailAuthProvider.GetCredential(email, password);

                // 7. 게스트 계정에 이메일 연동 (UID 유지)
                var linkResult = await currentUser.LinkWithCredentialAsync(emailCredential);

                if (linkResult == null || linkResult.User == null)
                    return (false, "이메일 계정 연동에 실패했습니다.");

                // 8. currentUser 업데이트 및 UID 유지 확인
                currentUser = linkResult.User;
                Debug.Log($"[AuthManager] 연동 후 UID: {currentUser.UserId} (유지: {guestUID == currentUser.UserId})");

                // 9. Firestore 업데이트
                if (DatabaseManager.Instance != null)
                {
                    // AuthProvider 변경 (Guest → Email)
                    await DatabaseManager.Instance.UpdateUserAuthProvider(guestUID, "Email");

                    // 이메일 업데이트
                    await DatabaseManager.Instance.UpdateUserEmail(guestUID, email);

                    // 닉네임 업데이트 (새 닉네임이 제공된 경우만)
                    if (!string.IsNullOrEmpty(nickname))
                    {
                        await DatabaseManager.Instance.UpdateUserNickname(guestUID, nickname);
                    }

                    Debug.Log($"[AuthManager] 게스트 계정이 이메일 계정으로 전환되었습니다. UID: {guestUID}");
                }

                // 10. 이메일 인증 발송 (선택사항)
                await SendEmailVerification();

                return (true, "이메일 계정으로 전환되었습니다.");
            }
            catch (FirebaseException ex)
            {
                // 중복 연동 에러 체크
                if (ex.ErrorCode == (int)AuthError.ProviderAlreadyLinked)
                {
                    return (false, "이미 이메일 계정이 연동되어 있습니다.");
                }

                // 이메일 중복 (Firebase Auth 레벨)
                if (ex.Message.Contains("EMAIL_EXISTS") || ex.Message.Contains("already in use"))
                {
                    return (false, "이미 사용 중인 이메일입니다.");
                }

                Debug.LogError($"[AuthManager] 이메일 전환 실패: {ex.Message}");
                return (false, GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 이메일 전환 실패: {ex.Message}");
                return (false, $"오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 이메일 계정에 연동 가능 여부 확인
        /// (게스트 계정이고 아직 이메일 provider가 없는 경우)
        /// </summary>
        public bool CanLinkEmailToGuest()
        {
            if (currentUser == null)
                return false;

            var providers = GetCurrentUserProviders();
            return IsAnonymous && !providers.Contains("password");
        }

        #endregion
    }
}

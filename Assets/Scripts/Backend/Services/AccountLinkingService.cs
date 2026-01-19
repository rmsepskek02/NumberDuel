using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 계정 연동 서비스
    /// - Google ↔ Email 계정 연동
    /// - Provider 상태 확인
    /// - 로그인 방식 표시
    /// </summary>
    public class AccountLinkingService
    {
        private readonly AuthManager authManager;

        private FirebaseAuth Auth => authManager.Auth;
        private FirebaseUser CurrentUser => authManager.CurrentUser;

        public AccountLinkingService(AuthManager authManager)
        {
            this.authManager = authManager;
        }

        #region Provider Status
        /// <summary>
        /// 현재 사용자의 로그인 Provider 목록 가져오기
        /// </summary>
        public List<string> GetCurrentUserProviders()
        {
            if (CurrentUser == null)
                return new List<string>();

            return CurrentUser.ProviderData
                .Select(provider => provider.ProviderId)
                .ToList();
        }

        /// <summary>
        /// 현재 사용자의 이메일 (Google 또는 email/password에서)
        /// </summary>
        public string GetCurrentUserEmailFromProvider()
        {
            if (CurrentUser == null)
                return string.Empty;

            // Google provider에서 이메일 가져오기
            var googleProvider = CurrentUser.ProviderData
                .FirstOrDefault(p => p.ProviderId == "google.com");

            if (googleProvider != null)
                return googleProvider.Email;

            // email/password provider에서 이메일 가져오기
            var emailProvider = CurrentUser.ProviderData
                .FirstOrDefault(p => p.ProviderId == "password");

            if (emailProvider != null)
                return emailProvider.Email;

            // Firebase User 이메일
            return CurrentUser.Email ?? string.Empty;
        }

        /// <summary>
        /// 현재 Google 로그인 상태인지 확인
        /// </summary>
        public bool IsGoogleLogin()
        {
            if (CurrentUser == null)
                return false;

            var providers = GetCurrentUserProviders();
            return FirebaseAuthHelper.HasGoogleProvider(providers);
        }

        /// <summary>
        /// Google 계정에 비밀번호 연동 가능 여부 확인
        /// </summary>
        public bool CanLinkPasswordToGoogle()
        {
            if (CurrentUser == null)
                return false;

            var providers = GetCurrentUserProviders();
            return FirebaseAuthHelper.HasGoogleProvider(providers) && !FirebaseAuthHelper.HasPasswordProvider(providers);
        }

        /// <summary>
        /// 이메일 계정에 Google 연동 가능 여부 확인
        /// </summary>
        public bool CanLinkGoogleToEmail()
        {
            if (CurrentUser == null)
                return false;

            var providers = GetCurrentUserProviders();
            return FirebaseAuthHelper.HasPasswordProvider(providers) && !FirebaseAuthHelper.HasGoogleProvider(providers);
        }

        /// <summary>
        /// 연동 완료 여부 확인
        /// </summary>
        public bool IsAccountFullyLinked()
        {
            if (CurrentUser == null)
                return false;

            var providers = GetCurrentUserProviders();
            return FirebaseAuthHelper.HasGoogleProvider(providers) && FirebaseAuthHelper.HasPasswordProvider(providers);
        }

        /// <summary>
        /// 이메일 전용 계정 여부 확인
        /// </summary>
        public bool IsEmailOnlyAccount()
        {
            if (CurrentUser == null || CurrentUser.IsAnonymous)
                return false;

            var providers = GetCurrentUserProviders();
            return FirebaseAuthHelper.HasPasswordProvider(providers) && !FirebaseAuthHelper.HasGoogleProvider(providers);
        }
        #endregion

        #region Link Password to Google
        /// <summary>
        /// Google 계정에 비밀번호 연동
        /// </summary>
        public async Task<(bool success, string message)> LinkPasswordToGoogle(string email, string password)
        {
            try
            {
                if (CurrentUser == null)
                    return (false, "로그인이 필요합니다.");

                if (!CanLinkPasswordToGoogle())
                    return (false, "Google 계정이 아니거나 이미 연동되어 있습니다.");

                // 이메일과 비밀번호 검증
                var (isValidEmail, emailError) = ValidationHelper.ValidateEmail(email);
                if (!isValidEmail)
                    return (false, emailError);

                var (isValidPassword, passwordError) = ValidationHelper.ValidatePassword(password);
                if (!isValidPassword)
                    return (false, passwordError);

                // Credential 생성
                Firebase.Auth.Credential credential = Firebase.Auth.EmailAuthProvider.GetCredential(email, password);

                // 연동 시도
                var result = await CurrentUser.LinkWithCredentialAsync(credential);

                if (result.User != null)
                {
                    // 사용자 정보 새로고침
                    await CurrentUser.ReloadAsync();

                    return (true, "PC 로그인 연동이 완료되었습니다!");
                }

                return (false, "계정 연동에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AccountLinkingService] 계정 연동 에러: {ex.Message}");

                if (ex.Message.Contains("PROVIDER_ALREADY_LINKED"))
                    return (false, "이미 연동되어 있습니다.");

                if (ex.Message.Contains("EMAIL_EXISTS") || ex.Message.Contains("already in use"))
                    return (false, "이 이메일은 다른 계정에서 사용 중입니다.");

                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountLinkingService] 계정 연동 예외: {ex.Message}");
                return (false, "계정 연동 중 오류가 발생했습니다.");
            }
        }
        #endregion

        #region Link Google to Email
        /// <summary>
        /// 이메일 계정에 Google 연동
        /// </summary>
        public async Task<(bool success, string message)> LinkGoogleToEmail()
        {
            try
            {
                if (CurrentUser == null)
                    return (false, "로그인이 필요합니다.");

                if (!CanLinkGoogleToEmail())
                    return (false, "이메일 계정이 아니거나 이미 연동되어 있습니다.");

                string currentEmail = GetCurrentUserEmailFromProvider();
                if (string.IsNullOrEmpty(currentEmail))
                    return (false, "계정 이메일을 찾을 수 없습니다.");

                // GoogleAuthProvider를 통해 Google Credential만 획득 (로그인 없이)
                var googleProvider = new Objects.Auth.GoogleAuthProvider(Auth);
                var googleResult = await googleProvider.GetCredentialOnly();

                if (!googleResult.Success)
                {
                    if (googleResult.Message == "CANCELED")
                        return (false, "Google 로그인이 취소되었습니다.");

                    return (false, googleResult.Message);
                }

#if UNITY_ANDROID
                // Android (Play Games)는 이메일을 제공하지 않으므로 이메일 확인 건너뜀
#else
                // PC/iOS는 Firebase OAuth를 사용하므로 이메일 일치 확인
                string googleEmail = googleResult.Email;

                if (string.IsNullOrEmpty(googleEmail))
                    return (false, "Google 계정 이메일을 가져올 수 없습니다.");

                // 이메일 일치 여부 확인
                if (!currentEmail.Equals(googleEmail, StringComparison.OrdinalIgnoreCase))
                    return (false, $"이메일이 일치하지 않습니다.\n\n현재 계정: {currentEmail}\nGoogle 계정: {googleEmail}");
#endif

                // Google Credential로 연동
                if (googleResult.Credential != null)
                {
                    var result = await CurrentUser.LinkWithCredentialAsync(googleResult.Credential);

                    if (result.User != null)
                    {
                        await CurrentUser.ReloadAsync();
                        return (true, "Google 계정 연동이 완료되었습니다!");
                    }
                }

                return (false, "Google 계정 연동에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AccountLinkingService] Google 연동 에러: {ex.Message}");

                if (ex.Message.Contains("PROVIDER_ALREADY_LINKED"))
                    return (false, "이미 Google 계정이 연동되어 있습니다.");

                if (ex.Message.Contains("CREDENTIAL_ALREADY_IN_USE"))
                    return (false, "이 Google 계정은 다른 사용자가 사용 중입니다.");

                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountLinkingService] Google 연동 예외: {ex.Message}");
                return (false, "Google 계정 연동 중 오류가 발생했습니다.");
            }
        }
        #endregion

        #region Link Email to Google
        /// <summary>
        /// Google 계정에 이메일 provider 연동
        /// </summary>
        public async Task<(bool success, string message, string email)> LinkEmailToGoogle(string email, string password, string nickname = null)
        {
            try
            {
                if (CurrentUser == null)
                    return (false, "로그인이 필요합니다.", null);

                if (!CanLinkPasswordToGoogle())
                    return (false, "Google 계정이 아니거나 이미 연동되어 있습니다.", null);

                // 이메일/비밀번호 검증
                var (isValidEmail, emailError) = ValidationHelper.ValidateEmail(email);
                if (!isValidEmail)
                    return (false, emailError, null);

                var (isValidPassword, passwordError) = ValidationHelper.ValidatePassword(password);
                if (!isValidPassword)
                    return (false, passwordError, null);

                // Credential 생성
                Firebase.Auth.Credential credential = Firebase.Auth.EmailAuthProvider.GetCredential(email, password);

                // Email provider 연동
                var result = await CurrentUser.LinkWithCredentialAsync(credential);

                if (result.User == null)
                    return (false, "계정 연동에 실패했습니다.", null);

                // 닉네임 업데이트
                if (!string.IsNullOrEmpty(nickname))
                {
                    string uid = CurrentUser.UserId;
                    bool nicknameUpdated = await DatabaseManager.Instance.UpdateNickname(uid, nickname);

                    if (nicknameUpdated)
                    {
                        var profile = new Firebase.Auth.UserProfile { DisplayName = nickname };
                        await CurrentUser.UpdateUserProfileAsync(profile);
                    }
                }

                // 이메일 인증 메일 발송
                await CurrentUser.SendEmailVerificationAsync();

                // 사용자 정보 새로고침
                await CurrentUser.ReloadAsync();

                return (true, "이메일 인증 메일이 발송되었습니다.", email);
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AccountLinkingService] Google → Email 연동 에러: {ex.Message}");

                if (ex.Message.Contains("PROVIDER_ALREADY_LINKED"))
                    return (false, "이미 연동되어 있습니다.", null);

                if (ex.Message.Contains("EMAIL_EXISTS") || ex.Message.Contains("already in use"))
                    return (false, "이 이메일은 다른 계정에서 사용 중입니다.", null);

                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountLinkingService] Google → Email 연동 예외: {ex.Message}");
                return (false, "계정 연동 중 오류가 발생했습니다.", null);
            }
        }
        #endregion

        #region Display Methods
        /// <summary>
        /// 현재 로그인 방식 표시용 문자열 반환
        /// </summary>
        public string GetLoginMethodDisplayName(bool showCurrentSessionOnly = false)
        {
            if (CurrentUser == null)
                return "로그인 안됨";

            if (CurrentUser.IsAnonymous)
                return "손님";

            var providers = GetCurrentUserProviders();

            bool hasGoogle = FirebaseAuthHelper.HasGoogleProvider(providers);
            bool hasPassword = FirebaseAuthHelper.HasPasswordProvider(providers);

            // 두 방식 모두 연동된 경우
            if (hasGoogle && hasPassword)
            {
                if (showCurrentSessionOnly)
                {
                    string currentProvider = authManager.GetCurrentSessionLoginProvider();

                    if (currentProvider == "password")
                        return "이메일";
                    else if (currentProvider == "google.com" || currentProvider == "playgames.google.com")
                        return "구글";
                    else
                        return "이메일";
                }
                else
                {
                    return "구글 + 이메일";
                }
            }

            if (hasGoogle)
                return "구글";

            if (hasPassword)
                return "이메일";

            return "알 수 없음";
        }
        #endregion
    }
}

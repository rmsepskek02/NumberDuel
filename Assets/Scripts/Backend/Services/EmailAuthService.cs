using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 이메일/비밀번호 인증 서비스
    /// - 회원가입
    /// - 로그인
    /// - 비밀번호 재설정
    /// - 이메일 인증
    /// </summary>
    public class EmailAuthService
    {
        private readonly AuthManager authManager;

        private FirebaseAuth Auth => authManager.Auth;
        private FirebaseUser CurrentUser => authManager.CurrentUser;

        public EmailAuthService(AuthManager authManager)
        {
            this.authManager = authManager;
        }

        #region Registration
        /// <summary>
        /// 이메일과 비밀번호로 회원가입
        /// </summary>
        public async Task<(bool success, string message)> RegisterWithEmail(string email, string password)
        {
            try
            {
                // 입력 검증
                var (isValidEmail, emailError) = ValidationHelper.ValidateEmail(email);
                if (!isValidEmail)
                    return (false, emailError);

                var (isValidPassword, passwordError) = ValidationHelper.ValidatePassword(password);
                if (!isValidPassword)
                    return (false, passwordError);

                // Firebase 회원가입
                AuthResult result = await Auth.CreateUserWithEmailAndPasswordAsync(email, password);

                if (result.User != null)
                {
                    authManager.SetCurrentUser(result.User);

                    // 이메일 인증 발송
                    await SendEmailVerificationInternal();

                    return (true, "회원가입이 완료되었습니다. 이메일 인증을 진행해주세요.");
                }

                return (false, "회원가입에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[EmailAuthService] 회원가입 에러: {ex.Message}");
                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
        }
        #endregion

        #region Login
        /// <summary>
        /// 이메일과 비밀번호로 로그인
        /// </summary>
        public async Task<(bool success, string message)> LoginWithEmail(string email, string password)
        {
            try
            {
                // 입력 검증
                var (isValidEmail, emailError) = ValidationHelper.ValidateEmail(email);
                if (!isValidEmail)
                    return (false, emailError);

                var (isValidPassword, passwordError) = ValidationHelper.ValidatePassword(password);
                if (!isValidPassword)
                    return (false, passwordError);

                // Firebase 로그인
                AuthResult result = await Auth.SignInWithEmailAndPasswordAsync(email, password);

                if (result.User != null)
                {
                    authManager.SetCurrentUser(result.User);
                    authManager.SaveLoginProvider("password"); // 세션 로그인 방식 저장

                    return (true, "로그인 성공!");
                }

                return (false, "로그인에 실패했습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[EmailAuthService] 로그인 에러: {ex.Message}");

                // 소셜 로그인 전용 계정 체크
                if (ex.Message.Contains("INVALID_PASSWORD") || ex.Message.Contains("password is invalid"))
                {
                    // Provider 확인
                    var providers = await GetProvidersForEmail(email);

                    if (FirebaseAuthHelper.HasGoogleProvider(providers) && !FirebaseAuthHelper.HasPasswordProvider(providers))
                    {
                        // Google로만 가입된 계정
                        return (false, "SOCIAL_LOGIN_ONLY::Google");
                    }
                }

                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
        }

        /// <summary>
        /// 이메일로 등록된 Provider 확인
        /// </summary>
        private async Task<System.Collections.Generic.List<string>> GetProvidersForEmail(string email)
        {
            try
            {
                if (Auth == null)
                    return new System.Collections.Generic.List<string>();

                var providers = await Auth.FetchProvidersForEmailAsync(email);
                return providers?.ToList() ?? new System.Collections.Generic.List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EmailAuthService] Provider 조회 실패: {ex.Message}");
                return new System.Collections.Generic.List<string>();
            }
        }
        #endregion

        #region Email Verification
        /// <summary>
        /// 이메일 인증 발송 (내부용)
        /// </summary>
        private async Task<bool> SendEmailVerificationInternal()
        {
            if (CurrentUser == null)
            {
                Debug.LogError("[EmailAuthService] 로그인된 사용자가 없습니다.");
                return false;
            }

            try
            {
                await CurrentUser.SendEmailVerificationAsync();
                return true;
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[EmailAuthService] 이메일 인증 발송 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 이메일 인증 발송 (외부 공개 API)
        /// </summary>
        public async Task<(bool success, string message)> SendEmailVerification()
        {
            if (CurrentUser == null)
                return (false, "로그인이 필요합니다");

            if (CurrentUser.IsEmailVerified)
                return (false, "이미 인증된 이메일입니다");

            try
            {
                await CurrentUser.SendEmailVerificationAsync();
                return (true, "인증 이메일을 발송했습니다");
            }
            catch (FirebaseException ex)
            {
                string errorMessage = FirebaseAuthHelper.GetFirebaseErrorMessage(ex);
                Debug.LogError($"[EmailAuthService] 인증 이메일 발송 실패: {errorMessage}");

                // Rate Limiting 에러인 경우 (이미 이메일이 발송되었을 가능성 높음)
                if (errorMessage.Contains("unusual activity") || errorMessage.Contains("blocked"))
                {
                    return (true, "인증 이메일을 발송했습니다");
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmailAuthService] 인증 이메일 발송 실패: {ex.Message}");
                return (false, "인증 이메일 발송에 실패했습니다");
            }
        }
        #endregion

        #region Password Reset
        /// <summary>
        /// 비밀번호 재설정 이메일 발송
        /// </summary>
        public async Task<(bool success, string message)> SendPasswordResetEmail(string email)
        {
            try
            {
                // 입력 검증
                var (isValid, errorMessage) = ValidationHelper.ValidateEmail(email);
                if (!isValid)
                    return (false, errorMessage);

                // Firebase 초기화 확인
                if (Auth == null)
                    return (false, "Firebase가 초기화되지 않았습니다.");

                // 비밀번호 재설정 이메일 발송
                await Auth.SendPasswordResetEmailAsync(email);
                return (true, "비밀번호 재설정 이메일을 발송했습니다");
            }
            catch (FirebaseException ex)
            {
                string errorMessage = FirebaseAuthHelper.GetFirebaseErrorMessage(ex);
                Debug.LogError($"[EmailAuthService] 비밀번호 재설정 이메일 발송 실패: {errorMessage}");

                // Rate Limiting 에러인 경우
                if (errorMessage.Contains("unusual activity") || errorMessage.Contains("blocked"))
                {
                    return (true, "비밀번호 재설정 이메일을 발송했습니다");
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmailAuthService] 비밀번호 재설정 이메일 발송 실패: {ex.Message}");
                return (false, "비밀번호 재설정 이메일 발송에 실패했습니다");
            }
        }
        #endregion

        #region User Info
        /// <summary>
        /// 사용자 정보 새로고침
        /// </summary>
        public async Task<bool> ReloadUserInfo()
        {
            if (CurrentUser == null)
            {
                Debug.LogWarning("[EmailAuthService] 로그인된 사용자가 없습니다");
                return false;
            }

            try
            {
                await CurrentUser.ReloadAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmailAuthService] 사용자 정보 새로고침 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 비밀번호 업데이트
        /// </summary>
        public async Task<bool> UpdatePassword(string newPassword)
        {
            if (CurrentUser == null)
            {
                Debug.LogError("[EmailAuthService] 로그인된 사용자가 없습니다");
                return false;
            }

            try
            {
                await CurrentUser.UpdatePasswordAsync(newPassword);
                return true;
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[EmailAuthService] 비밀번호 업데이트 실패: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}

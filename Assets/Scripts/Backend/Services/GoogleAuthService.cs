using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Google 로그인 서비스
    /// - Google 로그인 (PC: OAuth, Mobile: Play Games)
    /// - Provider 확인
    /// </summary>
    public class GoogleAuthService
    {
        private readonly AuthManager authManager;

        private FirebaseAuth Auth => authManager.Auth;
        private FirebaseUser CurrentUser => authManager.CurrentUser;

        public GoogleAuthService(AuthManager authManager)
        {
            this.authManager = authManager;
        }

        #region Google Login
        /// <summary>
        /// Google 계정으로 로그인
        /// </summary>
        public async Task<(bool success, string message, string email)> LoginWithGoogle()
        {
            try
            {
                // Firebase 초기화 확인
                if (Auth == null)
                    return (false, "Firebase가 초기화되지 않았습니다.", string.Empty);

                // GoogleAuthProvider 생성
                var googleProvider = new Objects.Auth.GoogleAuthProvider(Auth);

                // Google 로그인 시도
                var result = await googleProvider.SignIn();

                if (!result.Success)
                {
                    // 취소된 경우 조용히 처리
                    if (result.Message == "CANCELED")
                        return (false, "CANCELED", string.Empty);

                    // 계정이 이미 다른 방법으로 존재하는 경우
                    if (result.Message == "ACCOUNT_EXISTS")
                        return (false, "ACCOUNT_EXISTS", result.Email);

                    return (false, result.Message, string.Empty);
                }

                // 현재 사용자 업데이트
                authManager.SetCurrentUser(Auth.CurrentUser);

                if (CurrentUser == null)
                    return (false, "로그인에 실패했습니다.", string.Empty);

                // 세션 로그인 방식 저장
                var providers = GetCurrentUserProviders();
                if (providers.Contains("playgames.google.com"))
                    authManager.SaveLoginProvider("playgames.google.com");
                else if (providers.Contains("google.com"))
                    authManager.SaveLoginProvider("google.com");

                // Google 로그인 시 이메일은 ProviderData에서 가져와야 함
                string email = GetCurrentUserEmailFromProvider();
                if (string.IsNullOrEmpty(email))
                    email = CurrentUser.Email ?? string.Empty;

                return (true, "Google 로그인 성공", email);
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[GoogleAuthService] Google 로그인 에러: {ex.Message}");
                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex), string.Empty);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleAuthService] Google 로그인 예외: {ex.Message}");
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
                if (Auth == null)
                    return new List<string>();

                var providers = await Auth.FetchProvidersForEmailAsync(email);
                return providers?.ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleAuthService] Provider 조회 실패: {ex.Message}");
                return new List<string>();
            }
        }
        #endregion

        #region Provider Helpers
        /// <summary>
        /// 현재 사용자의 로그인 Provider 목록 가져오기
        /// </summary>
        private List<string> GetCurrentUserProviders()
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
        private string GetCurrentUserEmailFromProvider()
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
        #endregion
    }
}

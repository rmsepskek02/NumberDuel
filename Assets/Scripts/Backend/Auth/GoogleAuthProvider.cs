using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace Objects.Auth
{
    /// <summary>
    /// Google 로그인 처리 Provider
    /// Firebase Auth의 Google Provider를 사용한 Web OAuth 방식
    /// </summary>
    public class GoogleAuthProvider
    {
        private FirebaseAuth auth;

        public GoogleAuthProvider(FirebaseAuth firebaseAuth)
        {
            auth = firebaseAuth ?? throw new ArgumentNullException(nameof(firebaseAuth));
        }

        /// <summary>
        /// Google 로그인 시작
        /// Web OAuth 팝업을 통해 사용자 인증
        /// </summary>
        /// <returns>로그인 결과</returns>
        public async Task<SocialAuthResult> SignIn()
        {
            try
            {
                Debug.Log("[GoogleAuthProvider] Google 로그인 시작...");

                // Firebase Google Provider 생성
                var provider = new FederatedOAuthProvider();
                provider.SetProviderData(new FederatedOAuthProviderData
                {
                    ProviderId = "google.com"
                });

                // 추가 스코프 요청 (선택사항)
                // provider.SetScopes(new string[] { "profile", "email" });

                // Google 로그인 팝업 표시 및 인증
                var authResult = await auth.SignInWithProviderAsync(provider);

                if (authResult.User == null)
                {
                    return new SocialAuthResult
                    {
                        Success = false,
                        Message = "Google 로그인에 실패했습니다."
                    };
                }

                // 사용자 정보 추출
                var user = authResult.User;
                Debug.Log($"[GoogleAuthProvider] Google 로그인 성공: {user.Email}");

                return new SocialAuthResult
                {
                    Success = true,
                    Message = "Google 로그인 성공",
                    ProviderUserId = user.UserId,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.DisplayName ?? string.Empty,
                    PhotoUrl = user.PhotoUrl?.ToString() ?? string.Empty,
                    Credential = null // FederatedOAuthProvider는 Credential 직접 제공 안 함
                };
            }
            catch (FirebaseException ex) when (ex.Message.Contains("canceled") || ex.Message.Contains("CANCELED"))
            {
                // 사용자가 로그인 취소 - 조용히 처리
                Debug.Log("[GoogleAuthProvider] 사용자가 Google 로그인을 취소했습니다.");
                return new SocialAuthResult
                {
                    Success = false,
                    Message = "CANCELED" // 특별한 메시지로 취소 표시
                };
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[GoogleAuthProvider] Firebase 에러: {ex.Message}");
                return new SocialAuthResult
                {
                    Success = false,
                    Message = GetFirebaseErrorMessage(ex)
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleAuthProvider] 예외 발생: {ex.Message}");
                return new SocialAuthResult
                {
                    Success = false,
                    Message = "Google 로그인 중 오류가 발생했습니다."
                };
            }
        }

        /// <summary>
        /// Firebase 에러 메시지를 사용자 친화적인 메시지로 변환
        /// </summary>
        private string GetFirebaseErrorMessage(FirebaseException ex)
        {
            string errorCode = ex.Message;

            // 계정 이미 존재 (다른 Provider로 가입됨)
            if (errorCode.Contains("account-exists-with-different-credential"))
                return "ACCOUNT_EXISTS"; // 특별한 코드 반환

            // 유효하지 않은 Credential
            if (errorCode.Contains("invalid-credential"))
                return "인증 정보가 유효하지 않습니다. 다시 시도해주세요.";

            // 네트워크 오류
            if (errorCode.Contains("network"))
                return "네트워크 연결을 확인해주세요.";

            // Too many requests
            if (errorCode.Contains("too-many-requests"))
                return "너무 많은 시도가 있었습니다. 잠시 후 다시 시도해주세요.";

            // 팝업 차단
            if (errorCode.Contains("popup-blocked"))
                return "팝업이 차단되었습니다. 브라우저 설정을 확인해주세요.";

            // 기타
            return "Google 로그인 중 오류가 발생했습니다.";
        }

        /// <summary>
        /// 이메일로 등록된 Provider 목록 조회
        /// </summary>
        public async Task<List<string>> GetProvidersForEmail(string email)
        {
            try
            {
                var providers = await auth.FetchProvidersForEmailAsync(email);
                return providers?.ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleAuthProvider] Provider 조회 실패: {ex.Message}");
                return new List<string>();
            }
        }
    }
}

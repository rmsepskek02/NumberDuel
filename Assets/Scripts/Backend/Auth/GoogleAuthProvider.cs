using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace Objects.Auth
{
    /// <summary>
    /// Google 로그인 처리 Provider
    ///
    /// 플랫폼별 구현:
    /// - Android: Play Games Services v2 SDK (네이티브 Google 계정 선택 UI)
    /// - PC/iOS/WebGL: Firebase FederatedOAuthProvider (WebView 기반 OAuth 2.0)
    ///
    /// 주요 특징:
    /// - Android에서 네이티브 Google 계정 선택 UI 제공
    /// - Firebase Authentication과 통합되어 모든 플랫폼에서 동일한 사용자 계정 관리
    /// - Server Auth Code 방식으로 안전한 인증 처리 (Android)
    /// </summary>
    public class GoogleAuthProvider
    {
        private FirebaseAuth auth;

        public GoogleAuthProvider(FirebaseAuth firebaseAuth)
        {
            auth = firebaseAuth ?? throw new ArgumentNullException(nameof(firebaseAuth));
        }

        /// <summary>
        /// Google 로그인 시작 (플랫폼별 자동 분기)
        /// </summary>
        /// <returns>로그인 결과 (성공 여부, 사용자 정보, 에러 메시지 포함)</returns>
        public async Task<SocialAuthResult> SignIn()
        {
#if UNITY_ANDROID
            // Android: Play Games Services v2 네이티브 로그인
            // 시스템 레벨의 Google 계정 선택 UI 표시 (MapleStory 스타일)
            return await SignInWithPlayGames();
#else
            // PC/iOS/WebGL: Firebase Web OAuth 로그인
            // 앱 내 WebView를 통한 Google OAuth 2.0 인증
            return await SignInWithFirebase();
#endif
        }

#if UNITY_ANDROID
        /// <summary>
        /// Play Games Services v2를 사용한 네이티브 Google 로그인 (Android 전용)
        ///
        /// 인증 플로우:
        /// 1. Play Games Services 초기화 및 활성화
        /// 2. 네이티브 Google 계정 선택 UI 표시 (시스템 레벨)
        /// 3. Server Auth Code 요청 (Web Client ID 사용)
        /// 4. Firebase Credential 생성 (PlayGamesAuthProvider 사용)
        /// 5. Firebase Authentication으로 최종 로그인
        ///
        /// 중요 사항:
        /// - Server Auth Code 방식: ID Token 대신 Server Auth Code를 사용하는 이유는
        ///   Play Games Services v2가 ID Token을 직접 제공하지 않기 때문입니다.
        /// - PlayGamesAuthProvider 사용: GoogleAuthProvider가 아닌 PlayGamesAuthProvider를
        ///   사용해야 Firebase가 Play Games 인증을 올바르게 처리합니다.
        /// - Web Client ID: Unity Play Games Plugin 설정(GooglePlayGameSettings.txt)에
        ///   저장된 Web Client ID가 RequestServerSideAccess 호출 시 자동으로 사용됩니다.
        /// </summary>
        private async Task<SocialAuthResult> SignInWithPlayGames()
        {
            try
            {
                // Play Games Services 플랫폼 초기화
                // 이 메서드는 중복 호출해도 안전합니다 (내부에서 초기화 여부 확인)
                PlayGamesPlatform.Activate();

                // Server Auth Code 요청을 위한 비동기 작업 완료 소스
                var authCodeTaskSource = new TaskCompletionSource<string>();

                // Play Games 인증 시작 - 네이티브 Google 계정 선택 UI 표시
                PlayGamesPlatform.Instance.Authenticate((signInStatus) =>
                {
                    if (signInStatus == SignInStatus.Success)
                    {
                        // forceRefreshToken=true: 항상 새로운 Server Auth Code 발급
                        // Web Client ID는 GooglePlayGameSettings.txt에서 자동으로 사용됨
                        PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
                        {
                            if (!string.IsNullOrEmpty(authCode))
                            {
                                // Server Auth Code 획득 성공
                                authCodeTaskSource.SetResult(authCode);
                            }
                            else
                            {
                                // Server Auth Code가 null인 경우 (설정 오류 가능성)
                                Debug.LogError("[GoogleAuthProvider] Server Auth Code 반환값이 null입니다. " +
                                             "Unity Play Games Plugin 설정에서 Web Client ID가 올바르게 구성되었는지 확인하세요.");
                                authCodeTaskSource.SetResult(null);
                            }
                        });
                    }
                    else
                    {
                        // Play Games 인증 실패 (사용자 취소 또는 네트워크 오류)
                        Debug.LogError($"[GoogleAuthProvider] Play Games 인증 실패: {signInStatus}");
                        authCodeTaskSource.SetResult(null);
                    }
                });

                // Server Auth Code 획득 대기
                string serverAuthCode = await authCodeTaskSource.Task;

                if (string.IsNullOrEmpty(serverAuthCode))
                {
                    Debug.LogError("[GoogleAuthProvider] Server Auth Code를 가져올 수 없습니다.");
                    return new SocialAuthResult
                    {
                        Success = false,
                        Message = "Server Auth Code를 가져올 수 없습니다."
                    };
                }

                // Firebase Credential 생성
                // 중요: GoogleAuthProvider가 아닌 PlayGamesAuthProvider를 사용해야 합니다.
                // Play Games Services v2는 Google Sign-In과 다른 인증 메커니즘을 사용하므로
                // Firebase가 이를 올바르게 처리하려면 PlayGamesAuthProvider가 필요합니다.
                Credential credential = PlayGamesAuthProvider.GetCredential(serverAuthCode);

                // Firebase Authentication으로 최종 로그인
                var authResult = await auth.SignInWithCredentialAsync(credential);

                if (authResult == null)
                {
                    Debug.LogError("[GoogleAuthProvider] Firebase 인증 결과가 null입니다.");
                    return new SocialAuthResult
                    {
                        Success = false,
                        Message = "Firebase 인증에 실패했습니다."
                    };
                }

                // 사용자 정보 추출
                var user = authResult;

                // Play Games 프로필 정보 우선 사용 (Firebase에 표시 이름이 없을 수 있음)
                string displayName = user.DisplayName ??
                                    PlayGamesPlatform.Instance.GetUserDisplayName() ??
                                    string.Empty;

                // Play Games 로그인은 이메일을 제공하지 않음
                string email = string.Empty;

                return new SocialAuthResult
                {
                    Success = true,
                    Message = "Google 로그인 성공",
                    ProviderUserId = user.UserId,
                    Email = email,
                    DisplayName = displayName,
                    IdToken = serverAuthCode, // Server Auth Code 저장 (필요 시 서버 검증용)
                    Credential = credential
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleAuthProvider] Play Games 로그인 예외: {ex.Message}\n{ex.StackTrace}");
                return new SocialAuthResult
                {
                    Success = false,
                    Message = "Google 로그인 중 오류가 발생했습니다."
                };
            }
        }
#endif

        /// <summary>
        /// Firebase Web OAuth 방식 (PC/iOS/WebGL용)
        ///
        /// 인증 플로우:
        /// 1. FederatedOAuthProvider 생성 (Google OAuth 2.0 프로바이더)
        /// 2. 앱 내 WebView를 통해 Google 로그인 페이지 표시
        /// 3. 사용자가 Google 계정으로 로그인
        /// 4. Firebase가 OAuth 토큰을 자동으로 처리하여 사용자 인증
        ///
        /// 중요 사항:
        /// - Android와 달리 WebView 기반이므로 네이티브 UI가 아닙니다.
        /// - Firebase가 OAuth 2.0 플로우를 완전히 처리하므로 토큰 관리가 불필요합니다.
        /// - Credential 객체는 FederatedOAuthProvider에서 직접 제공되지 않습니다.
        /// - PC, iOS, WebGL 등 Play Games Services를 사용할 수 없는 플랫폼에서 사용됩니다.
        /// </summary>
        private async Task<SocialAuthResult> SignInWithFirebase()
        {
            try
            {
                // FederatedOAuthProvider 생성 및 구성
                // ProviderId "google.com"은 Firebase가 인식하는 Google OAuth 프로바이더입니다.
                var provider = new FederatedOAuthProvider();
                provider.SetProviderData(new FederatedOAuthProviderData
                {
                    ProviderId = "google.com"
                });

                // Google 로그인 WebView 표시 및 인증 처리
                // Firebase가 OAuth 2.0 플로우를 자동으로 관리합니다.
                var authResult = await auth.SignInWithProviderAsync(provider);

                if (authResult.User == null)
                {
                    Debug.LogError("[GoogleAuthProvider] Firebase Web OAuth 인증 결과에 사용자 정보가 없습니다.");
                    return new SocialAuthResult
                    {
                        Success = false,
                        Message = "Google 로그인에 실패했습니다."
                    };
                }

                // 사용자 정보 추출
                var user = authResult.User;

                return new SocialAuthResult
                {
                    Success = true,
                    Message = "Google 로그인 성공",
                    ProviderUserId = user.UserId,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.DisplayName ?? string.Empty,
                    Credential = null // FederatedOAuthProvider는 Credential 객체를 직접 제공하지 않음
                };
            }
            catch (FirebaseException ex) when (ex.Message.Contains("canceled") || ex.Message.Contains("CANCELED"))
            {
                // 사용자가 WebView에서 로그인을 취소한 경우
                return new SocialAuthResult
                {
                    Success = false,
                    Message = "CANCELED"
                };
            }
            catch (TaskCanceledException)
            {
                // 타임아웃 발생 (일반적으로 60초 초과 시)
                return new SocialAuthResult
                {
                    Success = false,
                    Message = "TIMEOUT"
                };
            }
            catch (FirebaseException ex) when (ex.Message.Contains("timeout") || ex.Message.Contains("TIMEOUT") || ex.Message.Contains("timed out"))
            {
                // Firebase 레벨 타임아웃
                return new SocialAuthResult
                {
                    Success = false,
                    Message = "TIMEOUT"
                };
            }
            catch (FirebaseException ex)
            {
                // Firebase 인증 에러 (네트워크 오류, 권한 문제 등)
                Debug.LogError($"[GoogleAuthProvider] Firebase 에러: {ex.Message}");
                return new SocialAuthResult
                {
                    Success = false,
                    Message = GetFirebaseErrorMessage(ex)
                };
            }
            catch (Exception ex)
            {
                // 예상치 못한 예외
                Debug.LogError($"[GoogleAuthProvider] 예상치 못한 예외 발생: {ex.Message}\n{ex.StackTrace}");
                return new SocialAuthResult
                {
                    Success = false,
                    Message = "Google 로그인 중 오류가 발생했습니다."
                };
            }
        }

        /// <summary>
        /// Firebase 에러 메시지를 사용자 친화적인 메시지로 변환
        ///
        /// AuthManager의 중앙화된 에러 메시지 처리 로직을 사용하여
        /// Firebase 예외를 일관된 사용자 메시지로 변환합니다.
        /// </summary>
        /// <param name="ex">Firebase 예외 객체</param>
        /// <returns>사용자에게 표시할 에러 메시지</returns>
        private string GetFirebaseErrorMessage(FirebaseException ex)
        {
            return Manager.AuthManager.GetFirebaseErrorMessage(ex);
        }

        /// <summary>
        /// 이메일 주소로 등록된 Provider 목록 조회
        ///
        /// 사용 사례:
        /// - 이메일 중복 확인 (이미 다른 Provider로 가입된 경우)
        /// - 로그인 방법 안내 (예: "이 이메일은 Google로 가입되어 있습니다")
        /// - 계정 연동 가이드 (여러 Provider 연결 시)
        /// </summary>
        /// <param name="email">조회할 이메일 주소</param>
        /// <returns>Provider ID 목록 (예: ["google.com", "password"])</returns>
        public async Task<List<string>> GetProvidersForEmail(string email)
        {
            try
            {
                var providers = await auth.FetchProvidersForEmailAsync(email);
                return providers?.ToList() ?? new List<string>();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }
}

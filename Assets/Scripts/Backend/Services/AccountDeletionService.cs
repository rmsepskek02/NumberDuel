using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 회원 탈퇴 서비스
    /// - 재인증 (이메일/Google)
    /// - 계정 및 데이터 삭제
    /// </summary>
    public class AccountDeletionService
    {
        private readonly AuthManager authManager;

        private FirebaseAuth Auth => authManager.Auth;
        private FirebaseUser CurrentUser => authManager.CurrentUser;

        public AccountDeletionService(AuthManager authManager)
        {
            this.authManager = authManager;
        }

        #region Reauthentication
        /// <summary>
        /// 이메일 계정 재인증 (비밀번호 사용)
        /// </summary>
        /// <param name="password">사용자 비밀번호</param>
        /// <returns>재인증 결과</returns>
        public async Task<(bool success, string message)> ReauthenticateWithEmail(string password)
        {
            if (CurrentUser == null)
                return (false, "로그인이 필요합니다.");

            if (string.IsNullOrEmpty(password))
                return (false, "비밀번호를 입력해주세요.");

            try
            {
                string email = CurrentUser.Email;
                if (string.IsNullOrEmpty(email))
                {
                    // ProviderData에서 이메일 가져오기
                    email = authManager.GetCurrentUserEmailFromProvider();
                }

                if (string.IsNullOrEmpty(email))
                    return (false, "이메일 정보를 찾을 수 없습니다.");

                var credential = EmailAuthProvider.GetCredential(email, password);
                await CurrentUser.ReauthenticateAsync(credential);

                Debug.Log("[AccountDeletionService] 이메일 재인증 성공");
                return (true, "인증 성공");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AccountDeletionService] 이메일 재인증 실패: {ex.Message}");

                if (ex.ErrorCode == (int)AuthError.WrongPassword)
                    return (false, "비밀번호가 일치하지 않습니다.");

                if (ex.ErrorCode == (int)AuthError.TooManyRequests)
                    return (false, "너무 많은 시도가 있었습니다. 잠시 후 다시 시도해주세요.");

                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountDeletionService] 이메일 재인증 예외: {ex.Message}");
                return (false, "인증 중 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// Google 계정 재인증
        /// Android: Play Games Services 사용
        /// PC: Firebase Web OAuth 사용
        /// </summary>
        /// <returns>재인증 결과</returns>
        public async Task<(bool success, string message)> ReauthenticateWithGoogle()
        {
            if (CurrentUser == null)
                return (false, "로그인이 필요합니다.");

            try
            {
#if UNITY_ANDROID
                // Android: Play Games Services 재인증
                var googleProvider = new Objects.Auth.GoogleAuthProvider(Auth);
                var result = await googleProvider.GetCredentialOnly();

                if (!result.Success)
                {
                    Debug.LogError($"[AccountDeletionService] Google Credential 획득 실패: {result.Message}");
                    return (false, result.Message);
                }

                if (result.Credential == null)
                    return (false, "Google 인증 정보를 가져올 수 없습니다.");

                await CurrentUser.ReauthenticateAsync(result.Credential);
                Debug.Log("[AccountDeletionService] Google 재인증 성공 (Android)");
                return (true, "Google 재인증 성공");
#else
                // PC/Editor: Firebase Web OAuth 재인증
                var provider = new FederatedOAuthProvider();
                provider.SetProviderData(new FederatedOAuthProviderData
                {
                    ProviderId = "google.com"
                });

                await CurrentUser.ReauthenticateWithProviderAsync(provider);
                Debug.Log("[AccountDeletionService] Google 재인증 성공 (PC)");
                return (true, "Google 재인증 성공");
#endif
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AccountDeletionService] Google 재인증 실패: {ex.Message}");

                if (ex.Message.Contains("canceled") || ex.Message.Contains("CANCELED"))
                    return (false, "CANCELED");

                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountDeletionService] Google 재인증 예외: {ex.Message}");
                return (false, "Google 인증 중 오류가 발생했습니다.");
            }
        }
        #endregion

        #region Account Deletion
        /// <summary>
        /// 계정 완전 삭제 (재인증 후 호출)
        /// 1. Firestore 프로필 삭제
        /// 2. Firestore 세션 삭제
        /// 3. Firebase Auth 계정 삭제
        /// </summary>
        /// <returns>삭제 결과</returns>
        public async Task<(bool success, string message)> DeleteAccountCompletely()
        {
            if (CurrentUser == null)
                return (false, "로그인이 필요합니다.");

            string uid = CurrentUser.UserId;

            try
            {
                // 1. Firestore 프로필 삭제
                if (DatabaseManager.Instance != null)
                {
                    bool profileDeleted = await DatabaseManager.Instance.DeleteUserProfile(uid);
                    if (!profileDeleted)
                    {
                        Debug.LogWarning("[AccountDeletionService] 프로필 삭제 실패 - 계속 진행");
                    }
                }

                // 2. Firestore 세션 삭제
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.StopSessionMonitoring();
                    await SessionManager.Instance.ClearSession(uid);
                }

                // 3. Firebase Auth 계정 삭제
                await CurrentUser.DeleteAsync();
                authManager.SetCurrentUser(null);

                Debug.Log($"[AccountDeletionService] 계정 완전 삭제 완료: {uid}");
                return (true, "탈퇴가 완료되었습니다.");
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[AccountDeletionService] 계정 삭제 실패: {ex.Message}");

                if (ex.ErrorCode == (int)AuthError.RequiresRecentLogin)
                    return (false, "보안을 위해 다시 로그인해주세요.");

                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountDeletionService] 계정 삭제 예외: {ex.Message}");
                return (false, "탈퇴 처리 중 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// Guest 계정 삭제 (재인증 불필요)
        /// </summary>
        /// <returns>삭제 결과</returns>
        public async Task<(bool success, string message)> DeleteGuestAccount()
        {
            if (CurrentUser == null)
                return (false, "로그인이 필요합니다.");

            if (!CurrentUser.IsAnonymous)
                return (false, "게스트 계정이 아닙니다.");

            // Guest 계정은 재인증 없이 바로 삭제
            return await DeleteAccountCompletely();
        }
        #endregion
    }
}

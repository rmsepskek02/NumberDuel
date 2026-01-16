using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 익명(게스트) 로그인 서비스
    /// - 게스트 로그인
    /// - 게스트 → Google 전환
    /// - 게스트 → Email 전환
    /// - 계정 삭제
    /// </summary>
    public class AnonymousAuthService
    {
        private readonly AuthManager authManager;

        private FirebaseAuth Auth => authManager.Auth;
        private FirebaseUser CurrentUser => authManager.CurrentUser;

        public AnonymousAuthService(AuthManager authManager)
        {
            this.authManager = authManager;
        }

        #region Anonymous Login
        /// <summary>
        /// 익명 로그인
        /// </summary>
        public async Task<bool> SignInAnonymously()
        {
            try
            {
                // Firebase 초기화 확인
                if (Auth == null)
                {
                    Debug.LogError("[AnonymousAuthService] Firebase가 초기화되지 않았습니다.");
                    return false;
                }

                // 시스템 메시지: 로그인 중
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("GuestLoginInProgress");
                }

                // Firebase 익명 로그인
                var result = await Auth.SignInAnonymouslyAsync();

                if (result.User != null)
                {
                    authManager.SetCurrentUser(result.User);
                    authManager.SaveLoginProvider("anonymous"); // 세션 로그인 방식 저장
                    string userId = CurrentUser.UserId;

                    // Firestore에 익명 사용자 프로필 생성
                    string nickname = await GenerateUniqueGuestNickname();
                    bool profileCreated = await CreateAnonymousUserProfile(userId, nickname);

                    if (!profileCreated)
                    {
                        Debug.LogError("[AnonymousAuthService] 익명 사용자 프로필 생성 실패");
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
                Debug.LogError($"[AnonymousAuthService] 익명 로그인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 고유한 게스트 닉네임 생성
        /// </summary>
        private async Task<string> GenerateUniqueGuestNickname()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                string randomSuffix = "";
                for (int i = 0; i < 6; i++)
                {
                    randomSuffix += chars[UnityEngine.Random.Range(0, chars.Length)];
                }

                string nickname = $"Guest_{randomSuffix}";

                if (DatabaseManager.Instance != null)
                {
                    bool isAvailable = await DatabaseManager.Instance.IsNicknameAvailable(nickname);
                    if (isAvailable)
                        return nickname;
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
                    bool created = await DatabaseManager.Instance.CreateUserProfile(
                        userId,
                        "",
                        nickname,
                        emailVerified: false,
                        authProvider: "Guest"
                    );

                    return created;
                }

                Debug.LogError("[AnonymousAuthService] DatabaseManager를 찾을 수 없습니다.");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnonymousAuthService] 익명 사용자 프로필 생성 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Account Deletion
        /// <summary>
        /// 계정 삭제
        /// </summary>
        public async Task<bool> DeleteAccount()
        {
            var user = CurrentUser;

            if (user == null)
            {
                Debug.LogError("[AnonymousAuthService] 로그인된 사용자가 없습니다.");
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
                authManager.SetCurrentUser(null);

                // 시스템 메시지: 탈퇴 완료
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("AccountDeletionComplete");
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnonymousAuthService] 계정 삭제 실패: {e.Message}");
                return false;
            }
        }
        #endregion

        #region Guest Conversion
        /// <summary>
        /// 게스트 계정을 Google 계정으로 전환
        /// </summary>
        public async Task<(bool success, string message)> ConvertGuestToGoogle()
        {
            try
            {
                if (CurrentUser == null)
                    return (false, "로그인이 필요합니다.");

                if (!CurrentUser.IsAnonymous)
                    return (false, "게스트 계정이 아닙니다.");

                // 이미 Google 연동되어 있는지 체크
                var providers = CurrentUser.ProviderData;
                foreach (var provider in providers)
                {
                    if (provider.ProviderId == "google.com" || provider.ProviderId == "playgames.google.com")
                        return (false, "이미 Google 계정이 연동되어 있습니다.");
                }

                string guestUID = CurrentUser.UserId;
                Debug.Log($"[AnonymousAuthService] 게스트 UID: {guestUID}");

                // Google Credential만 획득
                var googleProvider = new Objects.Auth.GoogleAuthProvider(Auth);
                var googleResult = await googleProvider.GetCredentialOnly();

                if (!googleResult.Success)
                    return (false, googleResult.Message);

                var googleCredential = googleResult.Credential;
                if (googleCredential == null)
                    return (false, "Google 인증 정보를 가져올 수 없습니다.");

                // 게스트 계정에 Google Credential 연결
                var linkResult = await CurrentUser.LinkWithCredentialAsync(googleCredential);

                if (linkResult == null || linkResult.User == null)
                    return (false, "Google 계정 전환에 실패했습니다.");

                authManager.SetCurrentUser(linkResult.User);
                Debug.Log($"[AnonymousAuthService] 연동 후 UID: {CurrentUser.UserId} (유지: {guestUID == CurrentUser.UserId})");

                // Firebase Display Name 업데이트
                if (!string.IsNullOrEmpty(googleResult.DisplayName))
                {
                    var profile = new UserProfile { DisplayName = googleResult.DisplayName };
                    await CurrentUser.UpdateUserProfileAsync(profile);
                    Debug.Log($"[AnonymousAuthService] Display Name 업데이트: {googleResult.DisplayName}");
                }

                // Firebase Database의 AuthProvider 업데이트
                if (DatabaseManager.Instance != null)
                {
                    await DatabaseManager.Instance.UpdateUserAuthProvider(guestUID, "Google");
                    Debug.Log($"[AnonymousAuthService] 게스트 계정이 Google 계정으로 전환되었습니다. UID: {guestUID}");
                }

                return (true, "Google 계정으로 전환되었습니다.");
            }
            catch (FirebaseException ex)
            {
                if (ex.ErrorCode == (int)AuthError.ProviderAlreadyLinked)
                    return (false, "이미 Google 계정이 연동되어 있습니다.");

                Debug.LogError($"[AnonymousAuthService] Google 전환 실패: {ex.Message}");
                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnonymousAuthService] Google 전환 실패: {ex.Message}");
                return (false, $"오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 게스트 계정을 이메일 계정으로 전환
        /// </summary>
        public async Task<(bool success, string message)> ConvertGuestToEmail(
            string email,
            string password,
            string nickname = null)
        {
            try
            {
                if (CurrentUser == null)
                    return (false, "로그인이 필요합니다.");

                if (!CurrentUser.IsAnonymous)
                    return (false, "게스트 계정이 아닙니다.");

                // 입력 검증
                var (isValidEmail, emailError) = Global.ValidateEmail(email);
                if (!isValidEmail)
                    return (false, emailError);

                var (isValidPassword, passwordError) = Global.ValidatePassword(password);
                if (!isValidPassword)
                    return (false, passwordError);

                // 이메일 중복 확인
                if (DatabaseManager.Instance != null)
                {
                    bool isRegistered = await DatabaseManager.Instance.IsEmailRegistered(email);
                    if (isRegistered)
                        return (false, "이미 사용 중인 이메일입니다.");
                }

                // 닉네임 중복 확인
                if (!string.IsNullOrEmpty(nickname) && DatabaseManager.Instance != null)
                {
                    bool isAvailable = await DatabaseManager.Instance.IsNicknameAvailable(nickname);
                    if (!isAvailable)
                        return (false, "이미 사용 중인 닉네임입니다.");
                }

                string guestUID = CurrentUser.UserId;
                Debug.Log($"[AnonymousAuthService] 게스트 UID: {guestUID}");

                // 이메일/비밀번호 Credential 생성
                var emailCredential = Firebase.Auth.EmailAuthProvider.GetCredential(email, password);

                // 게스트 계정에 이메일 연동
                var linkResult = await CurrentUser.LinkWithCredentialAsync(emailCredential);

                if (linkResult == null || linkResult.User == null)
                    return (false, "이메일 계정 연동에 실패했습니다.");

                authManager.SetCurrentUser(linkResult.User);
                Debug.Log($"[AnonymousAuthService] 연동 후 UID: {CurrentUser.UserId} (유지: {guestUID == CurrentUser.UserId})");

                // Firestore 업데이트
                if (DatabaseManager.Instance != null)
                {
                    await DatabaseManager.Instance.UpdateUserAuthProvider(guestUID, "Email");
                    await DatabaseManager.Instance.UpdateUserEmail(guestUID, email);

                    if (!string.IsNullOrEmpty(nickname))
                        await DatabaseManager.Instance.UpdateUserNickname(guestUID, nickname);

                    Debug.Log($"[AnonymousAuthService] 게스트 계정이 이메일 계정으로 전환되었습니다. UID: {guestUID}");
                }

                // 이메일 인증 발송
                await CurrentUser.SendEmailVerificationAsync();

                return (true, "이메일 계정으로 전환되었습니다.");
            }
            catch (FirebaseException ex)
            {
                if (ex.ErrorCode == (int)AuthError.ProviderAlreadyLinked)
                    return (false, "이미 이메일 계정이 연동되어 있습니다.");

                if (ex.Message.Contains("EMAIL_EXISTS") || ex.Message.Contains("already in use"))
                    return (false, "이미 사용 중인 이메일입니다.");

                Debug.LogError($"[AnonymousAuthService] 이메일 전환 실패: {ex.Message}");
                return (false, FirebaseAuthHelper.GetFirebaseErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnonymousAuthService] 이메일 전환 실패: {ex.Message}");
                return (false, $"오류가 발생했습니다: {ex.Message}");
            }
        }
        #endregion
    }
}

using System;
using System.Threading.Tasks;
using Objects;
using Objects.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Tabs
{
    /// <summary>
    /// 프로필 설정 탭 UI
    /// Firebase에서 프로필 정보 로드 및 표시
    /// </summary>
    public class ProfileSettingsTab : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI emailText;
        [SerializeField] private TextMeshProUGUI createdAtText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI totalGamesText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI winRateText;
        [SerializeField] private TextMeshProUGUI lastLoginText;

        [Header("Account Linking")]
        [SerializeField] private GameObject linkingSection;              // 계정 연동 섹션
        [SerializeField] private TextMeshProUGUI loginMethodText;       // "로그인 방식: Google"
        [SerializeField] private Button linkPasswordButton;             // "🖥️ PC에서도 플레이하기" 버튼
        [SerializeField] private Button linkSocialButton;               // "📱 SNS 계정 연동하기" 버튼
        [SerializeField] private TextMeshProUGUI linkedCompleteText;    // "연동 완료" 텍스트
        [SerializeField] private TextMeshProUGUI linkedDescriptionText; // "모든 플랫폼에서 로그인 가능" 설명

        [Header("Error UI")]
        [SerializeField] private GameObject errorPanel;

        [Header("Account Deletion")]
        [SerializeField] private Button deleteAccountButton;

        [Header("Privacy Policy")]
        [SerializeField] private Button privacyPolicyButton;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 버튼 이벤트는 Unity 에디터에서 수동으로 연결

            if (errorPanel != null)
                errorPanel.SetActive(false);
        }

        private void OnEnable()
        {
            // 계정 연동 이벤트 구독
            if (Manager.AuthManager.Instance != null)
            {
                Manager.AuthManager.Instance.OnAccountLinked += OnAccountLinkedEvent;
            }
            LoadProfile();
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (Manager.AuthManager.Instance != null)
            {
                Manager.AuthManager.Instance.OnAccountLinked -= OnAccountLinkedEvent;
            }
        }
        #endregion

        /// <summary>
        /// 계정 연동 완료 이벤트 핸들러
        /// </summary>
        private void OnAccountLinkedEvent()
        {
            // 프로필 재로드 및 UI 갱신
            LoadProfile();
            UpdateAccountLinkingUI();
        }

        #region Profile Loading
        /// <summary>
        /// Firebase에서 프로필 로드
        /// </summary>
        private async void LoadProfile()
        {
            if (Manager.AuthManager.Instance == null || !Manager.AuthManager.Instance.IsLoggedIn)
            {
                HideError();
                return;
            }

            if (Manager.DatabaseManager.Instance == null)
            {
                ShowError("데이터베이스 연결에 실패했습니다.");
                return;
            }

            HideError();

            try
            {
                string uid = Manager.AuthManager.Instance.CurrentUserUID;
                UserProfile profile = await Manager.DatabaseManager.Instance.GetUserProfile(uid);

                if (profile != null)
                {
                    UpdateUI(profile);
                    UpdateAccountLinkingUI();
                }
                else
                {
                    ShowError("프로필 정보를 불러올 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileSettingsTab] 프로필 로드 실패: {ex.Message}");
                ShowError("프로필 정보를 불러오는 중 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI(UserProfile profile)
        {
            if (profile == null)
                return;

            if (nicknameText != null)
                nicknameText.text = profile.Nickname;

            if (emailText != null)
            {
                // AuthProvider 기준으로 이메일 표시 분기
                if (profile.AuthProvider == "Guest")
                {
                    // 게스트 계정
                    emailText.text = "손님";
                }
                else
                {
                    // Google, Email, 또는 Google+Email 계정
                    // ⭐ ProviderData에서 실제 연동된 provider 확인
                    if (Manager.AuthManager.Instance != null)
                    {
                        var providers = Manager.AuthManager.Instance.GetCurrentUserProviders();
                        bool hasPassword = providers.Contains("password");
                        bool hasGoogle = providers.Contains("google.com") || providers.Contains("playgames.google.com");

                        if (hasPassword)
                        {
                            // Email provider가 있으면 해당 이메일 표시 (Google+Email 또는 Email만)
                            string email = Manager.AuthManager.Instance.GetCurrentUserEmailFromProvider();
                            emailText.text = !string.IsNullOrEmpty(email) ? email : "이메일";
                        }
                        else if (hasGoogle)
                        {
                            // Google만 있는 경우
                            emailText.text = "구글 이메일";
                        }
                        else
                        {
                            // Fallback: Firestore 프로필 이메일
                            emailText.text = !string.IsNullOrEmpty(profile.Email) ? profile.Email : "이메일 없음";
                        }
                    }
                    else
                    {
                        // AuthManager 없는 경우 Fallback
                        emailText.text = !string.IsNullOrEmpty(profile.Email) ? profile.Email : "이메일 없음";
                    }
                }
            }

            if (createdAtText != null)
                createdAtText.text = FormatDate(profile.CreatedAt);

            if (totalGamesText != null)
                totalGamesText.text = $"{profile.Stats.TotalGames} 게임";

            if (statsText != null)
            {
                string winColorHex = "17AE82";
                string lossColorHex = "BD383B";
                statsText.text = $"<color=#{winColorHex}>{profile.Stats.Wins} 승</color> <color=#{lossColorHex}>{profile.Stats.Losses} 패</color>";
            }

            if (winRateText != null)
                winRateText.text = $"{profile.Stats.WinRate:F1}%";

            if (lastLoginText != null)
                lastLoginText.text = $"최근 플레이 : {FormatDateTime(profile.LastLoginAt)}";
        }
        #endregion

        #region UI State Management
        private void ShowError(string message)
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(true);
                var errorText = errorPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (errorText != null)
                    errorText.text = message;
            }

            Debug.LogError($"[ProfileSettingsTab] {message}");
        }

        private void HideError()
        {
            if (errorPanel != null)
                errorPanel.SetActive(false);
        }
        #endregion

        #region Helper Methods
        private string FormatDate(DateTime dateTime) => dateTime.ToString("yyyy.MM.dd");

        private string FormatDateTime(DateTime dateTime) => dateTime.ToString("yyyy.MM.dd HH:mm");
        #endregion

        #region Account Linking
        /// <summary>
        /// 계정 연동 UI 업데이트 (상황에 맞게 동적 표시)
        /// </summary>
        private void UpdateAccountLinkingUI()
        {
            if (Manager.AuthManager.Instance == null)
                return;

            if (loginMethodText != null)
            {
                // ⭐ Google+Email 연동 계정의 경우 현재 세션 로그인 방식만 표시
                string method = Manager.AuthManager.Instance.GetLoginMethodDisplayName(showCurrentSessionOnly: true);
                loginMethodText.text = $"로그인 방식: {method}";
            }

            bool isAnonymous = Manager.AuthManager.Instance.IsAnonymous;

            if (isAnonymous)
            {
                // 게스트: "계정 연동하기" 버튼 표시
                ShowGuestLinkAccountButton();
            }
            else
            {
                var providers = Manager.AuthManager.Instance.GetCurrentUserProviders();
                bool hasGoogle = providers.Contains("google.com") || providers.Contains("playgames.google.com");
                bool hasPassword = providers.Contains("password");
                bool isAndroid = Application.platform == RuntimePlatform.Android;

                if (hasGoogle && !hasPassword)
                {
                    ShowLinkPasswordButton();
                }
                else if (!hasGoogle && hasPassword)
                {
                    if (isAndroid)
                        ShowLinkSocialButton();
                    else
                        ShowLinkUnavailableMessage();
                }
                else if (hasGoogle && hasPassword)
                {
                    ShowLinkedComplete();
                }
                else
                {
                    HideAllLinkingUI();
                }
            }
        }

        /// <summary>
        /// 게스트: "계정 연동하기" 버튼 표시
        /// </summary>
        private void ShowGuestLinkAccountButton()
        {
            // linkSocialButton을 재활용
            if (linkSocialButton != null)
            {
                linkSocialButton.gameObject.SetActive(true);

                // 버튼 텍스트 변경
                var buttonText = linkSocialButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "계정 연동하기";
            }

            if (linkPasswordButton != null)
                linkPasswordButton.gameObject.SetActive(false);

            if (linkedCompleteText != null)
                linkedCompleteText.gameObject.SetActive(false);

            if (linkedDescriptionText != null)
                linkedDescriptionText.gameObject.SetActive(false);
        }

        /// <summary>
        /// "PC에서도 플레이하기" 버튼 표시
        /// </summary>
        private void ShowLinkPasswordButton()
        {
            if (linkPasswordButton != null)
                linkPasswordButton.gameObject.SetActive(true);

            if (linkSocialButton != null)
                linkSocialButton.gameObject.SetActive(false);

            if (linkedCompleteText != null)
                linkedCompleteText.gameObject.SetActive(false);

            if (linkedDescriptionText != null)
                linkedDescriptionText.gameObject.SetActive(false);
        }

        /// <summary>
        /// "SNS 계정 연동하기" 버튼 표시
        /// </summary>
        private void ShowLinkSocialButton()
        {
            if (linkPasswordButton != null)
                linkPasswordButton.gameObject.SetActive(false);

            if (linkSocialButton != null)
                linkSocialButton.gameObject.SetActive(true);

            if (linkedCompleteText != null)
                linkedCompleteText.gameObject.SetActive(false);

            if (linkedDescriptionText != null)
                linkedDescriptionText.gameObject.SetActive(false);
        }

        /// <summary>
        /// "연동 완료" 텍스트 표시
        /// </summary>
        private void ShowLinkedComplete()
        {
            if (linkPasswordButton != null)
                linkPasswordButton.gameObject.SetActive(false);

            if (linkSocialButton != null)
                linkSocialButton.gameObject.SetActive(false);

            if (linkedCompleteText != null)
            {
                linkedCompleteText.gameObject.SetActive(true);
                linkedCompleteText.text = "연동 완료";
            }

            if (linkedDescriptionText != null)
            {
                linkedDescriptionText.gameObject.SetActive(true);
                linkedDescriptionText.text = "모든 플랫폼에서 로그인 가능";
            }
        }

        /// <summary>
        /// 모든 연동 UI 숨김
        /// </summary>
        private void HideAllLinkingUI()
        {
            if (linkPasswordButton != null)
                linkPasswordButton.gameObject.SetActive(false);

            if (linkSocialButton != null)
                linkSocialButton.gameObject.SetActive(false);

            if (linkedCompleteText != null)
                linkedCompleteText.gameObject.SetActive(false);

            if (linkedDescriptionText != null)
                linkedDescriptionText.gameObject.SetActive(false);
        }

        private void ShowLinkUnavailableMessage()
        {
            if (linkPasswordButton != null)
                linkPasswordButton.gameObject.SetActive(false);

            if (linkSocialButton != null)
                linkSocialButton.gameObject.SetActive(false);

            if (linkedCompleteText != null)
            {
                linkedCompleteText.gameObject.SetActive(true);
                linkedCompleteText.text = "SNS 연동은 모바일 앱에서만 가능합니다";
            }

            if (linkedDescriptionText != null)
            {
                linkedDescriptionText.gameObject.SetActive(true);
                linkedDescriptionText.text = "Android 앱에서 Google 계정을 연동하세요";
            }
        }

        /// <summary>
        /// linkPasswordButton 클릭
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public void OnLinkPasswordClicked()
        {
            // Google → Email Link (게스트 전환과 동일한 3단계 프로세스)
            UI.Shared.LinkEmailPopupUI.Show((success) =>
            {
                if (success)
                {
                    // 프로필 재로드 및 UI 갱신
                    LoadProfile();
                    UpdateAccountLinkingUI();
                }
            }, UI.Shared.ConversionMode.GoogleToEmail);
        }

        /// <summary>
        /// linkSocialButton 클릭 (게스트 또는 이메일 계정 모드 분기)
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public void OnLinkSocialClicked()
        {
            bool isAnonymous = Manager.AuthManager.Instance.IsAnonymous;

            if (isAnonymous)
            {
                // 게스트: "손님 계정" 텍스트와 게스트 모드 플래그 전달
                UI.Shared.LinkSocialPopupUI.Show("손님 계정", true, OnAccountLinked);
            }
            else
            {
                // 이메일 계정: 실제 이메일과 일반 모드 플래그 전달
                string currentEmail = Manager.AuthManager.Instance.GetCurrentUserEmailFromProvider();
                UI.Shared.LinkSocialPopupUI.Show(currentEmail, false, OnAccountLinked);
            }
        }

        /// <summary>
        /// 비밀번호 연동 완료 콜백
        /// </summary>
        private void OnPasswordLinked(bool success)
        {
            if (success)
            {
                // 프로필 재로드 및 UI 갱신
                LoadProfile();
                UpdateAccountLinkingUI();
            }
        }

        /// <summary>
        /// 계정 연동 완료 콜백 (게스트 → SNS 또는 이메일 → SNS)
        /// </summary>
        private void OnAccountLinked(bool success)
        {
            if (success)
            {
                // 프로필 재로드 및 UI 갱신
                LoadProfile();
                UpdateAccountLinkingUI();
            }
        }

        #endregion

        #region Account Deletion
        /// <summary>
        /// 회원 탈퇴 버튼 클릭
        /// </summary>
        public void OnDeleteAccountClicked()
        {
            if (Manager.AuthManager.Instance == null)
                return;

            bool isAnonymous = Manager.AuthManager.Instance.IsAnonymous;

            if (isAnonymous)
            {
                // Guest: 로그아웃 = 탈퇴 안내
                ShowGuestDeletionPopup();
            }
            else
            {
                // Email/Google: 재인증 후 탈퇴
                ShowDeletionConfirmPopup();
            }
        }

        /// <summary>
        /// Guest 계정 탈퇴 안내 팝업
        /// </summary>
        private void ShowGuestDeletionPopup()
        {
            Shared.ConfirmationPopupUI.Show(
                "손님 계정은 로그아웃 시\n자동으로 삭제됩니다.\n\n" +
                "[주의사항]\n" +
                "• 로그아웃하면 계정이 영구 삭제됩니다\n" +
                "• 게임 데이터가 모두 사라집니다\n" +
                "• 복구가 불가능합니다\n\n" +
                "계정을 유지하려면 먼저\n" +
                "이메일 또는 Google 계정을 연동해주세요.",
                onConfirm: async () =>
                {
                    await ExecuteGuestDeletion();
                },
                onCancel: () => { },
                confirmText: "로그아웃",
                cancelText: "취소"
            );
        }

        /// <summary>
        /// Email/Google 계정 탈퇴 확인 팝업 (1단계)
        /// </summary>
        private void ShowDeletionConfirmPopup()
        {
            Shared.ConfirmationPopupUI.Show(
                "정말 탈퇴하시겠습니까?\n\n" +
                "[주의사항]\n" +
                "• 모든 게임 데이터가 삭제됩니다\n" +
                "• 복구가 불가능합니다",
                onConfirm: () =>
                {
                    // 재인증 단계로 이동
                    ShowReauthPopup();
                },
                onCancel: () => { },
                confirmText: "탈퇴하기",
                cancelText: "취소"
            );
        }

        /// <summary>
        /// 재인증 팝업 표시 (2단계)
        /// </summary>
        private void ShowReauthPopup()
        {
            var providers = Manager.AuthManager.Instance.GetCurrentUserProviders();
            bool hasPassword = providers.Contains("password");
            bool hasGoogle = providers.Contains("google.com") || providers.Contains("playgames.google.com");

            // 현재 세션 로그인 방식 확인
            string currentProvider = Manager.AuthManager.Instance.GetLoginMethodDisplayName(showCurrentSessionOnly: true);

            if (currentProvider == "이메일" || (hasPassword && !hasGoogle))
            {
                // 비밀번호 재인증
                ShowPasswordReauthPopup();
            }
            else if (currentProvider == "Google" || hasGoogle)
            {
                // Google 재인증
                ExecuteGoogleReauth();
            }
        }

        /// <summary>
        /// 비밀번호 재인증 팝업
        /// </summary>
        private void ShowPasswordReauthPopup()
        {
            Shared.InputFieldPopupUI.Show(
                Shared.InputPopupType.Password,
                onConfirm: async (password) =>
                {
                    // validator에서 재인증 성공 시에만 호출됨
                    await ExecuteAccountDeletion();
                },
                onCancel: () => { Shared.InputFieldPopupUI.Hide(); },
                title: "비밀번호 확인",
                validator: ValidatePasswordReauth
            );
        }

        /// <summary>
        /// 비밀번호 재인증 검증 (validator)
        /// 재입력 가능한 에러: 검증 텍스트로 표시 + 팝업 유지
        /// 복구 불가능한 에러: 검증 텍스트로 표시 + 팝업 유지 (사용자가 취소 선택하도록)
        /// </summary>
        private async Task<(bool valid, string message)> ValidatePasswordReauth(string password)
        {
            var result = await Manager.AuthManager.Instance.ReauthenticateForDeletion(password);

            if (result.success)
            {
                return (true, string.Empty);
            }

            // 모든 에러는 검증 텍스트로 표시하고 팝업 유지
            return (false, result.message);
        }

        /// <summary>
        /// Google 재인증 실행
        /// </summary>
        private async void ExecuteGoogleReauth()
        {
            Manager.SystemMessageManager.Instance?.ShowMessage("GoogleReauthInProgress");

            var result = await Manager.AuthManager.Instance.ReauthenticateWithGoogleForDeletion();

            if (result.success)
            {
                await ExecuteAccountDeletion();
            }
            else
            {
                // 사용자 취소는 메시지 표시 안함
                if (result.message != "CANCELED")
                {
                    Manager.SystemMessageManager.Instance?.ShowMessage("ReauthenticationFailed");
                }
            }
        }

        /// <summary>
        /// Guest 계정 삭제 실행
        /// </summary>
        private async Task ExecuteGuestDeletion()
        {
            Manager.SystemMessageManager.Instance?.ShowMessage("AccountDeletionInProgress");

            var result = await Manager.AuthManager.Instance.DeleteGuestAccount();

            if (result.success)
            {
                Manager.SystemMessageManager.Instance?.ShowMessage("AccountDeletionComplete");
                NavigateToJoinScene();
            }
            else
            {
                Manager.SystemMessageManager.Instance?.ShowMessage("AccountDeletionFailed");
            }
        }

        /// <summary>
        /// 계정 삭제 실행 (재인증 후)
        /// </summary>
        private async Task ExecuteAccountDeletion()
        {
            Manager.SystemMessageManager.Instance?.ShowMessage("AccountDeletionInProgress");

            var result = await Manager.AuthManager.Instance.DeleteAccountCompletely();

            if (result.success)
            {
                Manager.SystemMessageManager.Instance?.ShowMessage("AccountDeletionComplete");
                NavigateToJoinScene();
            }
            else
            {
                Manager.SystemMessageManager.Instance?.ShowMessage("AccountDeletionFailed");
            }
        }

        /// <summary>
        /// JoinScene으로 이동
        /// </summary>
        private void NavigateToJoinScene()
        {
            // 설정 화면 닫기
            if (Manager.SettingsManager.Instance != null)
            {
                Manager.SettingsManager.Instance.HideSettings();
            }

            // JoinScene으로 이동
            if (Manager.LoadingScreenManager.Instance != null)
            {
                Manager.LoadingScreenManager.Instance.ShowThenLoadLocal(
                    Objects.SceneNameExtensions.GetSceneName(Objects.SceneName.JoinScene)
                );
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    Objects.SceneNameExtensions.GetSceneName(Objects.SceneName.JoinScene)
                );
            }
        }
        #endregion

        #region Privacy Policy
        /// <summary>
        /// 개인정보처리방침 버튼 클릭
        /// </summary>
        public void OnPrivacyPolicyClicked()
        {
            string content =
                "최종 수정일: 2026년 1월 19일\n\n" +
                "<b>1. 수집하는 개인정보</b>\n" +
                "• 이메일 주소 (이메일 로그인 시)\n" +
                "• Google 계정 정보 (Google 로그인 시)\n" +
                "• 닉네임\n\n" +
                "<b>2. 수집 목적</b>\n" +
                "• 회원 식별 및 로그인\n" +
                "• 멀티플레이어 게임 서비스 제공\n\n" +
                "<b>3. 보관 기간</b>\n" +
                "• 회원 탈퇴 시까지\n\n" +
                "<b>4. 제3자 제공</b>\n" +
                "본 앱은 다음 서비스를 이용합니다:\n" +
                "• Google Firebase (인증, 데이터베이스)\n" +
                "• Photon (멀티플레이어)\n\n" +
                "각 서비스의 개인정보처리방침:\n" +
                "• Google: policies.google.com/privacy\n" +
                "• Photon: photonengine.com/privacy\n\n" +
                "<b>5. 개인정보 삭제</b>\n" +
                "앱 내 설정 > 회원탈퇴 기능을 통해\n" +
                "모든 개인정보를 삭제할 수 있습니다.\n\n" +
                "<b>6. 문의</b>\n" +
                "이메일: rmsepskek02@gmail.com";

            Shared.ScrollableTextPopupUI.Show("NumberDuel 개인정보처리방침", content);
        }
        #endregion
    }
}

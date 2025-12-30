using System;
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
        [SerializeField] private TextMeshProUGUI linkedCompleteText;    // "✅ 연동 완료" 텍스트
        [SerializeField] private TextMeshProUGUI linkedDescriptionText; // "모든 플랫폼에서 로그인 가능" 설명

        [Header("Error UI")]
        [SerializeField] private GameObject errorPanel;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            linkPasswordButton?.onClick.AddListener(OnLinkPasswordClicked);
            linkSocialButton?.onClick.AddListener(OnLinkSocialClicked);

            if (errorPanel != null)
                errorPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            linkPasswordButton?.onClick.RemoveListener(OnLinkPasswordClicked);
            linkSocialButton?.onClick.RemoveListener(OnLinkSocialClicked);
        }

        private void OnEnable()
        {
            LoadProfile();
        }
        #endregion

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
                string email = profile.Email;

                // Google 로그인인 경우 하드코딩된 텍스트 표시
                if (Manager.AuthManager.Instance != null && Manager.AuthManager.Instance.IsGoogleLogin())
                {
                    emailText.text = "Google Email";
                }
                else
                {
                    // 이메일/비밀번호 로그인인 경우 실제 이메일 표시
                    if (Manager.AuthManager.Instance != null)
                    {
                        email = Manager.AuthManager.Instance.GetCurrentUserEmailFromProvider();

                        if (string.IsNullOrEmpty(email))
                            email = Manager.AuthManager.Instance.CurrentUserEmail;

                        if (string.IsNullOrEmpty(email))
                            email = profile.Email;
                    }
                    emailText.text = email ?? string.Empty;
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
                string method = Manager.AuthManager.Instance.GetLoginMethodDisplayName();
                loginMethodText.text = $"로그인 방식: {method}";
            }

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
                linkedCompleteText.text = "✅ 연동 완료";
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

        private void OnLinkPasswordClicked()
        {
            string googleEmail = Manager.AuthManager.Instance.GetCurrentUserEmailFromProvider();
            UI.Shared.LinkPasswordPopupManager.Show(googleEmail, OnPasswordLinked);
        }

        private void OnLinkSocialClicked()
        {
            string currentEmail = Manager.AuthManager.Instance.GetCurrentUserEmailFromProvider();
            UI.Shared.LinkSocialPopupManager.Show(currentEmail, OnSocialLinked);
        }

        private void OnPasswordLinked(bool success)
        {
            if (success)
                UpdateAccountLinkingUI();
        }

        private void OnSocialLinked(bool success)
        {
            if (success)
                UpdateAccountLinkingUI();
        }

        #endregion
    }
}

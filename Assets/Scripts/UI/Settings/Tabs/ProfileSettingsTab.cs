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

        [Header("Buttons")]
        // [SerializeField] private Button changeNicknameButton; // 보류: 추후 구현 예정
        // [SerializeField] private Button retryButton; // 보류: 에러 처리 간소화

        [Header("Account Linking")]
        [SerializeField] private GameObject linkingSection;              // 계정 연동 섹션
        [SerializeField] private TextMeshProUGUI loginMethodText;       // "로그인 방식: Google"
        [SerializeField] private Button linkPasswordButton;             // "🖥️ PC에서도 플레이하기" 버튼
        [SerializeField] private Button linkSocialButton;               // "📱 SNS 계정 연동하기" 버튼
        [SerializeField] private TextMeshProUGUI linkedCompleteText;    // "✅ 연동 완료" 텍스트
        [SerializeField] private TextMeshProUGUI linkedDescriptionText; // "모든 플랫폼에서 로그인 가능" 설명

        [Header("Loading/Error UI")]
        // [SerializeField] private GameObject loadingIndicator; // 보류: 로딩 표시 간소화
        [SerializeField] private GameObject errorPanel; // 에러 패널

        private UserProfile currentProfile;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 버튼 이벤트 등록
            // changeNicknameButton?.onClick.AddListener(OnChangeNicknameClicked); // 보류: 닉네임 변경 기능
            // retryButton?.onClick.AddListener(OnRetryClicked); // 보류: 재시도 버튼

            // 계정 연동 버튼 이벤트
            linkPasswordButton?.onClick.AddListener(OnLinkPasswordClicked);
            linkSocialButton?.onClick.AddListener(OnLinkSocialClicked);

            // 초기 상태 설정
            // if (loadingIndicator != null)
            //     loadingIndicator.SetActive(false); // 보류: 로딩 인디케이터

            if (errorPanel != null)
                errorPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            // changeNicknameButton?.onClick.RemoveListener(OnChangeNicknameClicked); // 보류: 닉네임 변경 기능
            // retryButton?.onClick.RemoveListener(OnRetryClicked); // 보류: 재시도 버튼

            linkPasswordButton?.onClick.RemoveListener(OnLinkPasswordClicked);
            linkSocialButton?.onClick.RemoveListener(OnLinkSocialClicked);
        }

        private void OnEnable()
        {
            // 탭 활성화 시 프로필 로드
            LoadProfile();
        }
        #endregion

        #region Profile Loading
        /// <summary>
        /// Firebase에서 프로필 로드
        /// </summary>
        private async void LoadProfile()
        {
            // AuthManager 확인
            if (Manager.AuthManager.Instance == null || !Manager.AuthManager.Instance.IsLoggedIn)
            {
                ShowError("로그인 정보를 찾을 수 없습니다.");
                return;
            }

            // DatabaseManager 확인
            if (Manager.DatabaseManager.Instance == null)
            {
                ShowError("데이터베이스 연결에 실패했습니다.");
                return;
            }

            // 로딩 표시
            // ShowLoading(true); // 보류: 로딩 인디케이터
            HideError();

            try
            {
                string uid = Manager.AuthManager.Instance.CurrentUserUID;

                // Firebase에서 프로필 로드
                UserProfile profile = await Manager.DatabaseManager.Instance.GetUserProfile(uid);

                if (profile != null)
                {
                    currentProfile = profile;
                    UpdateUI(profile);
                    // ShowLoading(false); // 보류: 로딩 인디케이터

                    // 계정 연동 상태 업데이트
                    UpdateAccountLinkingUI();
                }
                else
                {
                    ShowError("프로필 정보를 불러올 수 없습니다.");
                    // ShowLoading(false); // 보류: 로딩 인디케이터
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileSettingsTab] 프로필 로드 실패: {ex.Message}");
                ShowError("프로필 정보를 불러오는 중 오류가 발생했습니다.");
                // ShowLoading(false); // 보류: 로딩 인디케이터
            }
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI(UserProfile profile)
        {
            if (profile == null)
                return;

            // 플레이어 정보
            if (nicknameText != null)
                nicknameText.text = profile.Nickname;

            if (emailText != null)
                emailText.text = profile.Email;

            if (createdAtText != null)
                createdAtText.text = FormatDate(profile.CreatedAt);

            // 전적 정보
            if (totalGamesText != null)
                totalGamesText.text = $"{profile.Stats.TotalGames} 게임";

            // 승/패 텍스트를 Rich Text로 색상 적용
            if (statsText != null)
            {
                string winColorHex = "17AE82"; // 청록색
                string lossColorHex = "BD383B"; // 빨간색

                statsText.text = $"<color=#{winColorHex}>{profile.Stats.Wins} 승</color> <color=#{lossColorHex}>{profile.Stats.Losses} 패</color>";
            }

            if (winRateText != null)
                winRateText.text = $"{profile.Stats.WinRate:F1}%";

            if (lastLoginText != null)
                lastLoginText.text = $"최근 플레이 : {FormatDateTime(profile.LastLoginAt)}";
        }
        #endregion

        #region Button Handlers
        // 보류: 닉네임 변경 기능 - 추후 구현 예정
        /*
        /// <summary>
        /// 닉네임 변경 버튼 클릭
        /// </summary>
        private void OnChangeNicknameClicked()
        {
            // NicknameChangePopupUI 열기
            var nicknamePopup = FindAnyObjectByType<NicknameChangePopupUI>();

            if (nicknamePopup != null)
            {
                nicknamePopup.Show(OnNicknameChanged);
            }
            else
            {
                Debug.LogWarning("[ProfileSettingsTab] NicknameChangePopupUI를 찾을 수 없습니다!");
            }

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 닉네임 변경 완료 콜백
        /// </summary>
        private void OnNicknameChanged(string newNickname)
        {
            if (string.IsNullOrEmpty(newNickname))
                return;

            // UI 즉시 업데이트
            if (nicknameText != null)
                nicknameText.text = newNickname;

            // 프로필 다시 로드 (서버에서 최신 정보 가져오기)
            LoadProfile();

            Debug.Log($"[ProfileSettingsTab] 닉네임이 '{newNickname}'으로 변경되었습니다.");
        }
        */

        // 보류: 재시도 버튼 - 에러 처리 간소화
        /*
        /// <summary>
        /// 재시도 버튼 클릭
        /// </summary>
        private void OnRetryClicked()
        {
            LoadProfile();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }
        */
        #endregion

        #region UI State Management
        // 보류: 로딩 인디케이터 - 로딩 표시 간소화
        /*
        /// <summary>
        /// 로딩 표시/숨김
        /// </summary>
        private void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(show);
        }
        */

        /// <summary>
        /// 에러 표시
        /// </summary>
        private void ShowError(string message)
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(true);

                // 에러 패널 내부의 TextMeshProUGUI 찾아서 메시지 설정
                var errorText = errorPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (errorText != null)
                {
                    errorText.text = message;
                }
            }

            Debug.LogError($"[ProfileSettingsTab] {message}");
        }

        /// <summary>
        /// 에러 숨김
        /// </summary>
        private void HideError()
        {
            if (errorPanel != null)
                errorPanel.SetActive(false);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// DateTime을 날짜 형식으로 변환 (yyyy.MM.dd)
        /// </summary>
        private string FormatDate(DateTime dateTime)
        {
            return dateTime.ToString("yyyy.MM.dd");
        }

        /// <summary>
        /// DateTime을 날짜+시간 형식으로 변환 (yyyy.MM.dd HH:mm)
        /// </summary>
        private string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy.MM.dd HH:mm");
        }
        #endregion

        #region Account Linking

        /// <summary>
        /// 계정 연동 UI 업데이트 (상황에 맞게 동적 표시)
        /// </summary>
        private void UpdateAccountLinkingUI()
        {
            if (Manager.AuthManager.Instance == null)
                return;

            // 로그인 방식 표시
            if (loginMethodText != null)
            {
                string method = Manager.AuthManager.Instance.GetLoginMethodDisplayName();
                loginMethodText.text = $"로그인 방식: {method}";
            }

            // 연동 상태 확인
            bool hasGoogle = Manager.AuthManager.Instance.GetCurrentUserProviders().Contains("google.com");
            bool hasPassword = Manager.AuthManager.Instance.GetCurrentUserProviders().Contains("password");

            // 상태 A: Google만 (모바일에서 가입)
            if (hasGoogle && !hasPassword)
            {
                ShowLinkPasswordButton();
            }
            // 상태 B: 이메일만 (PC에서 가입)
            else if (!hasGoogle && hasPassword)
            {
                ShowLinkSocialButton();
            }
            // 상태 C: 모두 연동 완료
            else if (hasGoogle && hasPassword)
            {
                ShowLinkedComplete();
            }
            // 그 외: 연동 섹션 숨김
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

        /// <summary>
        /// "PC에서도 플레이하기" 버튼 클릭 (Google → 비밀번호)
        /// </summary>
        private void OnLinkPasswordClicked()
        {
            string googleEmail = Manager.AuthManager.Instance.GetCurrentUserEmailFromProvider();
            UI.Shared.LinkPasswordPopup.Show(googleEmail, OnPasswordLinked);
        }

        /// <summary>
        /// "SNS 계정 연동하기" 버튼 클릭 (이메일 → SNS)
        /// </summary>
        private void OnLinkSocialClicked()
        {
            string currentEmail = Manager.AuthManager.Instance.GetCurrentUserEmailFromProvider();
            UI.Shared.LinkSocialPopup.Show(currentEmail, OnSocialLinked);
        }

        /// <summary>
        /// 비밀번호 연동 완료 콜백
        /// </summary>
        private void OnPasswordLinked(bool success)
        {
            if (success)
            {
                UpdateAccountLinkingUI();
                Debug.Log("[ProfileSettingsTab] 비밀번호 연동 완료");
            }
        }

        /// <summary>
        /// SNS 연동 완료 콜백
        /// </summary>
        private void OnSocialLinked(bool success)
        {
            if (success)
            {
                UpdateAccountLinkingUI();
                Debug.Log("[ProfileSettingsTab] SNS 연동 완료");
            }
        }

        #endregion
    }
}

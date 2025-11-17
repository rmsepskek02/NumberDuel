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
        [SerializeField] private TextMeshProUGUI winsText;
        [SerializeField] private TextMeshProUGUI lossesText;
        [SerializeField] private TextMeshProUGUI winRateText;
        [SerializeField] private TextMeshProUGUI lastLoginText;

        [Header("Buttons")]
        [SerializeField] private Button changeNicknameButton;
        [SerializeField] private Button retryButton; // 재시도 버튼 (로드 실패 시 표시)

        [Header("Loading/Error UI")]
        [SerializeField] private GameObject loadingIndicator; // 로딩 인디케이터
        [SerializeField] private GameObject errorPanel; // 에러 패널

        private UserProfile currentProfile;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 버튼 이벤트 등록
            changeNicknameButton?.onClick.AddListener(OnChangeNicknameClicked);
            retryButton?.onClick.AddListener(OnRetryClicked);

            // 초기 상태 설정
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            if (errorPanel != null)
                errorPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            changeNicknameButton?.onClick.RemoveListener(OnChangeNicknameClicked);
            retryButton?.onClick.RemoveListener(OnRetryClicked);
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
            ShowLoading(true);
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
                    ShowLoading(false);
                }
                else
                {
                    ShowError("프로필 정보를 불러올 수 없습니다.");
                    ShowLoading(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileSettingsTab] 프로필 로드 실패: {ex.Message}");
                ShowError("프로필 정보를 불러오는 중 오류가 발생했습니다.");
                ShowLoading(false);
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

            if (winsText != null)
            {
                winsText.text = $"{profile.Stats.Wins} 승";
                winsText.color = new Color(0.09f, 0.68f, 0.51f); // 녹색 (Global.Green)
            }

            if (lossesText != null)
            {
                lossesText.text = $"{profile.Stats.Losses} 패";
                lossesText.color = new Color(0.74f, 0.22f, 0.23f); // 빨간색 (Global.Red)
            }

            if (winRateText != null)
                winRateText.text = $"{profile.Stats.WinRate:F1}%";

            if (lastLoginText != null)
                lastLoginText.text = FormatDateTime(profile.LastLoginAt);
        }
        #endregion

        #region Button Handlers
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
        /// 재시도 버튼 클릭
        /// </summary>
        private void OnRetryClicked()
        {
            LoadProfile();

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
        #endregion

        #region UI State Management
        /// <summary>
        /// 로딩 표시/숨김
        /// </summary>
        private void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(show);
        }

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
    }
}

using Objects;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Settings
{
    /// <summary>
    /// 설정 패널 Footer 버튼 컨트롤러
    /// 씬별로 버튼 표시를 동적으로 변경
    /// </summary>
    public class SettingsFooter : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Buttons")]
        [SerializeField] private Button exitButton; // "로비로 나가기" 또는 "게임 종료"
        [SerializeField] private Button logoutButton; // "로그아웃"

        [Header("Exit Button Text")]
        [SerializeField] private TextMeshProUGUI exitButtonText; // Exit 버튼 텍스트

        private string currentSceneName;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 현재 씬 이름 가져오기
            currentSceneName = SceneManager.GetActiveScene().name;

            // 버튼 이벤트 등록
            exitButton?.onClick.AddListener(OnExitButtonClicked);
            logoutButton?.onClick.AddListener(OnLogoutButtonClicked);

            // 씬별 버튼 텍스트 설정
            UpdateExitButtonText();
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            exitButton?.onClick.RemoveListener(OnExitButtonClicked);
            logoutButton?.onClick.RemoveListener(OnLogoutButtonClicked);
        }

        private void OnEnable()
        {
            // 활성화될 때마다 씬 확인 및 버튼 텍스트 업데이트
            currentSceneName = SceneManager.GetActiveScene().name;
            UpdateExitButtonText();
            UpdateLogoutButtonVisibility();
        }
        #endregion

        #region Button Handlers
        /// <summary>
        /// Exit 버튼 클릭 (씬별 분기)
        /// </summary>
        private void OnExitButtonClicked()
        {
            if (Manager.SettingsManager.Instance != null)
            {
                Manager.SettingsManager.Instance.OnExitButtonClicked();
            }

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 로그아웃 버튼 클릭
        /// </summary>
        private void OnLogoutButtonClicked()
        {
            if (Manager.SettingsManager.Instance != null)
            {
                Manager.SettingsManager.Instance.OnLogoutButtonClicked();
            }

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 씬별 Exit 버튼 텍스트 업데이트
        /// </summary>
        private void UpdateExitButtonText()
        {
            if (exitButtonText == null)
                return;

            if (currentSceneName == SceneName.GameScene.GetSceneName())
            {
                // GameScene: "방 나가기"
                exitButtonText.text = "방 나가기";
            }
            else
            {
                // 그 외: "게임 종료"
                exitButtonText.text = "게임 종료";
            }
        }

        /// <summary>
        /// 씬별 로그아웃 버튼 표시 여부 업데이트
        /// </summary>
        private void UpdateLogoutButtonVisibility()
        {
            if (logoutButton == null)
                return;

            // JoinScene에서는 로그아웃 버튼 숨김
            if (currentSceneName == SceneName.JoinScene.GetSceneName())
            {
                logoutButton.gameObject.SetActive(false);
            }
            else
            {
                logoutButton.gameObject.SetActive(true);
            }
        }
        #endregion
    }
}

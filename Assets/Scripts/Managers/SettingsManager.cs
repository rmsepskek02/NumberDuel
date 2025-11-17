using Objects;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utills;

namespace Manager
{
    /// <summary>
    /// 설정 시스템 중앙 관리자
    /// - 설정 패널 Show/Hide
    /// - ESC 키 감지
    /// - 씬별 Footer 액션 분기
    /// </summary>
    public class SettingsManager : SingletonDontDestroy<SettingsManager>
    {
        #region Fields and Properties
        [Header("UI References")]
        [SerializeField] private UI.Settings.SettingsPanelUI settingsPanelUI;
        [SerializeField] private UI.Shared.ConfirmationPopupUI confirmationPopup;

        // 설정 패널 열림 상태
        private bool isSettingsOpen = false;
        private bool isConfirmationOpen = false;

        /// <summary>
        /// 설정 패널이 열려있는지 확인
        /// </summary>
        public bool IsSettingsOpen => isSettingsOpen;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            // UI 참조가 없으면 씬에서 찾기
            if (settingsPanelUI == null)
            {
                settingsPanelUI = FindAnyObjectByType<UI.Settings.SettingsPanelUI>();
            }

            if (confirmationPopup == null)
            {
                confirmationPopup = FindAnyObjectByType<UI.Shared.ConfirmationPopupUI>();
            }
        }

        private void Update()
        {
            // ESC 키 처리
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }
        }
        #endregion

        #region ESC Key Handling
        /// <summary>
        /// ESC 키 입력 처리
        /// - 확인 팝업 열려있을 때: 확인 팝업만 닫기
        /// - 설정 패널 열려있을 때: 설정 닫기
        /// - 아무것도 없을 때: 설정 열기
        /// </summary>
        private void HandleEscapeKey()
        {
            if (isConfirmationOpen)
            {
                // 확인 팝업만 닫기
                HideConfirmation();
            }
            else if (isSettingsOpen)
            {
                // 설정 패널 닫기
                HideSettings();
            }
            else
            {
                // 설정 패널 열기
                ShowSettings();
            }
        }
        #endregion

        #region Settings Panel Control
        /// <summary>
        /// 설정 패널 표시
        /// </summary>
        public void ShowSettings()
        {
            if (settingsPanelUI == null)
            {
                Debug.LogError("[SettingsManager] SettingsPanelUI가 없습니다!");
                return;
            }

            if (isSettingsOpen)
                return;

            settingsPanelUI.Show();
            isSettingsOpen = true;

            // 사운드 재생
            SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 설정 패널 숨기기
        /// </summary>
        public void HideSettings()
        {
            if (settingsPanelUI == null)
                return;

            if (!isSettingsOpen)
                return;

            settingsPanelUI.Hide();
            isSettingsOpen = false;

            // 사운드 재생
            SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }
        #endregion

        #region Footer Actions
        /// <summary>
        /// "로비로 나가기" 또는 "게임 종료" 버튼 클릭
        /// (씬에 따라 분기)
        /// </summary>
        public void OnExitButtonClicked()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == SceneName.GameScene.GetSceneName())
            {
                // GameScene: 로비로 나가기
                ShowConfirmation(ConfirmationType.ExitToLobby);
            }
            else
            {
                // 그 외: 게임 종료
                ShowConfirmation(ConfirmationType.QuitGame);
            }
        }

        /// <summary>
        /// "로그아웃" 버튼 클릭
        /// </summary>
        public void OnLogoutButtonClicked()
        {
            ShowConfirmation(ConfirmationType.Logout);
        }
        #endregion

        #region Confirmation Popup
        /// <summary>
        /// 확인 팝업 표시
        /// </summary>
        private void ShowConfirmation(ConfirmationType type)
        {
            if (confirmationPopup == null)
            {
                Debug.LogError("[SettingsManager] ConfirmationPopupUI가 없습니다!");
                ExecuteConfirmationAction(type); // 팝업 없이 바로 실행
                return;
            }

            string message = GetConfirmationMessage(type);
            confirmationPopup.Show(message, () => ExecuteConfirmationAction(type));
            isConfirmationOpen = true;

            // 사운드 재생
            SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 확인 팝업 숨기기
        /// </summary>
        private void HideConfirmation()
        {
            if (confirmationPopup == null)
                return;

            confirmationPopup.Hide();
            isConfirmationOpen = false;
        }

        /// <summary>
        /// ConfirmationType에 따른 메시지 반환
        /// </summary>
        private string GetConfirmationMessage(ConfirmationType type)
        {
            return type switch
            {
                ConfirmationType.ExitToLobby => "게임에서 나가시겠습니까?\n진행 중인 게임은 패배 처리됩니다.",
                ConfirmationType.QuitGame => "게임을 종료하시겠습니까?",
                ConfirmationType.Logout => "로그아웃 하시겠습니까?",
                _ => "계속하시겠습니까?"
            };
        }

        /// <summary>
        /// 확인 팝업 액션 실행
        /// </summary>
        private void ExecuteConfirmationAction(ConfirmationType type)
        {
            switch (type)
            {
                case ConfirmationType.ExitToLobby:
                    ExitToLobby();
                    break;

                case ConfirmationType.QuitGame:
                    QuitGame();
                    break;

                case ConfirmationType.Logout:
                    Logout();
                    break;
            }

            // 확인 팝업 닫기
            HideConfirmation();
        }

        /// <summary>
        /// 로비로 나가기 (Photon 방 나가기)
        /// </summary>
        private void ExitToLobby()
        {
            Debug.Log("[SettingsManager] 로비로 나가기...");

            // Photon 방 나가기
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            // LoadingScreenManager가 있으면 사용, 없으면 직접 로드
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.LoadScene(SceneName.LobbyScene.GetSceneName());
            }
            else
            {
                SceneManager.LoadScene(SceneName.LobbyScene.GetSceneName());
            }

            // 설정 패널 닫기
            HideSettings();
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        private void QuitGame()
        {
            Debug.Log("[SettingsManager] 게임 종료...");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// 로그아웃
        /// </summary>
        private void Logout()
        {
            Debug.Log("[SettingsManager] 로그아웃...");

            // AuthManager 로그아웃
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.SignOut();
            }

            // Photon 연결 해제
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }

            // JoinScene으로 이동
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.LoadScene(SceneName.JoinScene.GetSceneName());
            }
            else
            {
                SceneManager.LoadScene(SceneName.JoinScene.GetSceneName());
            }

            // 설정 패널 닫기
            HideSettings();
        }
        #endregion

        #region Public API for ConfirmationPopup
        /// <summary>
        /// 확인 팝업이 닫힐 때 ConfirmationPopupUI에서 호출
        /// </summary>
        public void OnConfirmationClosed()
        {
            isConfirmationOpen = false;
        }
        #endregion
    }
}

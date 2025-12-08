using Objects;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private GameObject settingsButton; // Settings 버튼 (씬별 활성화/비활성화)
        // ConfirmationPopup은 Static Manager로 변경되어 필드 불필요

        // 설정 패널 열림 상태
        private bool isSettingsOpen = false;

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

            if (settingsButton == null)
            {
                settingsButton = GameObject.Find("SettingsButton");
            }

            // SettingsCanvas를 DontDestroyOnLoad로 설정
            if (settingsPanelUI != null)
            {
                GameObject settingsCanvas = settingsPanelUI.transform.root.gameObject;
                UIHelper.MakeDontDestroy(settingsCanvas);
            }

            // ConfirmationPopup은 Static Manager로 자동 관리

            // 씬 변경 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 현재 씬에서 SettingsButton 상태 초기화
            UpdateSettingsButtonVisibility(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            // 씬 변경 이벤트 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            // ESC 키 처리 (New Input System)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }
        }

        /// <summary>
        /// 씬 로드 시 호출 - SettingsButton 활성화/비활성화 제어
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateSettingsButtonVisibility(scene.name);
        }

        /// <summary>
        /// 씬별 SettingsButton 활성화/비활성화
        /// </summary>
        private void UpdateSettingsButtonVisibility(string sceneName)
        {
            if (settingsButton == null)
                return;

            // SplashScene에서만 비활성화, 나머지 씬에서는 활성화
            bool shouldShow = sceneName != SceneName.SplashScene.GetSceneName();
            settingsButton.SetActive(shouldShow);

            Debug.Log($"[SettingsManager] SettingsButton {(shouldShow ? "활성화" : "비활성화")} (씬: {sceneName})");
        }
        #endregion

        #region ESC Key Handling
        /// <summary>
        /// ESC 키 입력 처리
        /// - 설정 패널 열려있을 때: 설정 닫기
        /// - 아무것도 없을 때: 설정 열기
        /// (확인 팝업은 자체적으로 Cancel 버튼으로 닫기)
        /// </summary>
        private void HandleEscapeKey()
        {
            if (isSettingsOpen)
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

        /// <summary>
        /// 설정 버튼 클릭 핸들러 (Inspector에서 Button.OnClick에 연결)
        /// </summary>
        public void OnSettingsButtonClicked()
        {
            ShowSettings();
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
                // GameScene: 게임 진행 여부에 따라 다른 메시지
                bool isGameInProgress = TurnManager.Instance?.IsGameStarted ?? false;

                if (isGameInProgress)
                {
                    // 게임 진행 중
                    ShowConfirmation(ConfirmationType.ExitToLobby);
                }
                else
                {
                    // 게임 시작 전
                    ShowConfirmation(ConfirmationType.ExitToLobbyBeforeGame);
                }
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
        /// 확인 팝업 표시 (Static Manager 사용)
        /// </summary>
        private void ShowConfirmation(ConfirmationType type)
        {
            string message = GetConfirmationMessage(type);
            UI.Shared.ConfirmationPopup.Show(message, () => ExecuteConfirmationAction(type));
        }

        /// <summary>
        /// ConfirmationType에 따른 메시지 반환
        /// </summary>
        private string GetConfirmationMessage(ConfirmationType type)
        {
            return type switch
            {
                ConfirmationType.ExitToLobby => "게임에서 나가시겠습니까?\n진행 중인 게임은 패배 처리됩니다.",
                ConfirmationType.ExitToLobbyBeforeGame => "방을 나가시겠습니까?",
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
                case ConfirmationType.ExitToLobbyBeforeGame:
                    ExitToLobby();
                    break;

                case ConfirmationType.QuitGame:
                    QuitGame();
                    break;

                case ConfirmationType.Logout:
                    Logout();
                    break;
            }

            // 확인 팝업은 자동으로 닫힘 (ConfirmationPopupUI에서 처리)
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
                LoadingScreenManager.Instance.ShowThenLoadLocal(SceneName.LobbyScene.GetSceneName());
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
        /// 로그아웃 (수동 로그아웃)
        /// </summary>
        private void Logout()
        {
            Debug.Log("[SettingsManager] 로그아웃...");

            // AuthManager 로그아웃
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.Logout();
            }

            // ✅ 수동 로그아웃 시스템 메시지 표시
            if (SystemMessageManager.Instance != null)
            {
                SystemMessageManager.Instance.ShowMessage("로그아웃되었습니다", MessageType.Info);
            }

            // Photon 연결 해제
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }

            // JoinScene으로 이동
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowThenLoadLocal(SceneName.JoinScene.GetSceneName());
            }
            else
            {
                SceneManager.LoadScene(SceneName.JoinScene.GetSceneName());
            }

            // 설정 패널 닫기
            HideSettings();
        }
        #endregion
    }
}

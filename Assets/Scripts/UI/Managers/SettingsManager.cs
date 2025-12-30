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
    /// - UIStackManager를 통한 ESC 키 처리
    /// - 씬별 Footer 액션 분기
    /// </summary>
    public class SettingsManager : SingletonDontDestroy<SettingsManager>, UI.ICloseable
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

            // UIStackManager 초기화 (Instance 접근으로 자동 생성)
            var uiStackManager = UI.UIStackManager.Instance;

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
        }
        #endregion

        #region ICloseable Implementation
        /// <summary>
        /// ICloseable 구현: 설정 패널 닫기
        /// UIStackManager의 ESC 키 처리에서 호출됨
        /// </summary>
        public void Close()
        {
            HideSettings();
        }

        /// <summary>
        /// ICloseable 구현: 닫을 수 있는 상태인지 확인
        /// </summary>
        /// <returns>애니메이션 진행 중이 아니고 설정이 열려있으면 true</returns>
        public bool CanClose()
        {
            // 설정이 열려있지 않으면 닫을 수 없음
            if (!isSettingsOpen)
                return false;

            // 애니메이션 진행 중이면 닫을 수 없음
            if (settingsPanelUI != null && settingsPanelUI.IsAnimating)
                return false;

            return true;
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

            // 계정 연동 팝업 미리 로드 (최초 지연 방지)
            UI.Shared.LinkPasswordPopupManager.PreloadPopup();
            UI.Shared.LinkSocialPopupManager.PreloadPopup();

            settingsPanelUI.Show();
            isSettingsOpen = true;

            // UIStackManager에 등록
            UI.UIStackManager.Instance?.Push(this);

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

            // UIStackManager에서 제거
            UI.UIStackManager.Instance?.Pop();

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

            // 확인 + 취소 버튼 표시
            UI.Shared.ConfirmationPopup.Show(
                message,
                onConfirm: () => ExecuteConfirmationAction(type),
                onCancel: () => { } // 취소 시 팝업만 닫힘
            );
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
                SystemMessageManager.Instance.ShowMessage("LogoutComplete");
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

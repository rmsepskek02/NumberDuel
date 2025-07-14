using Objects;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    /// <summary>
    /// 인 게임에서 UI를 관리하는 매니저
    /// TurnManager 중심의 게임 플로우 관리
    /// </summary>
    public class InGameUIManager : Utills.Singleton<InGameUIManager>
    {
        [Header("UI 컴포넌트")]
        public Button startButton;
        public Button endButton;
        public Button leaveButton;
        public TextMeshProUGUI turn;

        [Header("버튼 스프라이트")]
        public Sprite enabledStartSprite;
        public Sprite enabledLeaveSprite;
        public Sprite enabledEndSprite;
        public Sprite disabledSprite;

        [Header("게임 상태")]
        public bool isStart;

        private PhotonManager pm;

        #region Unity Lifecycle
        void Start()
        {
            pm = FindAnyObjectByType<PhotonManager>();
            InitializeButtons();
            UpdateButtons(PhotonNetwork.CurrentRoom?.PlayerCount ?? 0);
        }

        void Update()
        {
            UpdateTurnDisplay();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 버튼 초기화 및 이벤트 등록
        /// </summary>
        private void InitializeButtons()
        {
            // 버튼 이벤트 등록
            startButton.onClick.AddListener(OnClickStart);
            endButton.onClick.AddListener(OnClickEnd);
            leaveButton.onClick.AddListener(OnClickLeave);

            // 초기 버튼 상태 설정
            SetButtonState(leaveButton, true, enabledLeaveSprite);
            SetButtonState(endButton, false, disabledSprite);
            SetButtonState(startButton, false, enabledStartSprite, false); // 비활성화 상태로 시작
        }

        /// <summary>
        /// 턴 표시 업데이트
        /// </summary>
        private void UpdateTurnDisplay()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsGameStarted)
            {
                turn.text = TurnManager.Instance.CurrentTurn.ToString();
            }
            else
            {
                turn.text = "대기 중";
            }
        }
        #endregion

        #region PhotonManager Integration
        /// <summary>
        /// PhotonManager에서 UI 이벤트 등록
        /// </summary>
        public void RegisterPhotonManager(PhotonManager photonManager)
        {
            photonManager.MyTurn += EnableEndTurnButton;
            photonManager.YourTurn += DisableEndTurnButton;
            photonManager.EnterPlayer += OnPlayerEnter;
            photonManager.LeavePlayer += OnPlayerLeave;
            photonManager.UpdatePlayerCount += UpdateButtons;
        }

        /// <summary>
        /// 내 턴일 때 턴 종료 버튼 활성화
        /// </summary>
        private void EnableEndTurnButton()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsGameStarted)
            {
                SetButtonState(endButton, true, enabledEndSprite);
            }
        }

        /// <summary>
        /// 상대 턴일 때 턴 종료 버튼 비활성화
        /// </summary>
        private void DisableEndTurnButton()
        {
            SetButtonState(endButton, false, enabledEndSprite);
        }

        /// <summary>
        /// 플레이어 입장 시 UI 업데이트
        /// </summary>
        private void OnPlayerEnter()
        {
            Debug.Log("[InGameUIManager] 플레이어 입장");
            UpdateButtons(PhotonNetwork.CurrentRoom?.PlayerCount ?? 0);
        }

        /// <summary>
        /// 플레이어 퇴장 시 UI 업데이트
        /// </summary>
        private void OnPlayerLeave()
        {
            Debug.Log("[InGameUIManager] 플레이어 퇴장");
            UpdateButtons(PhotonNetwork.CurrentRoom?.PlayerCount ?? 0);

            // 게임 중이었다면 게임 종료 처리
            if (isStart && TurnManager.Instance != null)
            {
                ResetUI();
            }
        }

        /// <summary>
        /// 인원 수에 따른 Start 버튼 활성화
        /// </summary>
        private void UpdateButtons(int playerCount)
        {
            bool canStart = playerCount == 2 && PhotonNetwork.IsMasterClient && !isStart;
            SetButtonState(startButton, canStart, enabledStartSprite, !isStart);

            Debug.Log($"[InGameUIManager] 플레이어 수: {playerCount}, Start 버튼 활성화: {canStart}");
        }
        #endregion

        #region Button Click Events
        /// <summary>
        /// 게임 시작 버튼 클릭
        /// </summary>
        public void OnClickStart()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[InGameUIManager] 방장만 게임을 시작할 수 있습니다.");
                return;
            }

            if (PhotonNetwork.CurrentRoom?.PlayerCount != 2)
            {
                Debug.LogWarning("[InGameUIManager] 2명의 플레이어가 필요합니다.");
                return;
            }

            Debug.Log("[InGameUIManager] 게임 시작 버튼 클릭");
            isStart = true;
            StartGame();
        }

        /// <summary>
        /// 턴 종료 버튼 클릭
        /// </summary>
        public void OnClickEnd()
        {
            if (TurnManager.Instance == null)
            {
                Debug.LogError("[InGameUIManager] TurnManager를 찾을 수 없습니다!");
                return;
            }

            if (!TurnManager.Instance.IsLocalPlayerTurn)
            {
                Debug.LogWarning("[InGameUIManager] 내 턴이 아닙니다.");
                return;
            }

            Debug.Log("[InGameUIManager] 턴 종료 버튼 클릭");
            TurnManager.Instance.EndTurn();
        }

        /// <summary>
        /// 방 나가기 버튼 클릭
        /// </summary>
        public void OnClickLeave()
        {
            Debug.Log("[InGameUIManager] 방 나가기 버튼 클릭");

            if (pm != null)
            {
                pm.OnLeaveRoom();
            }
            else
            {
                PhotonNetwork.LeaveRoom();
            }
        }
        #endregion

        #region Game Flow Management
        /// <summary>
        /// 게임 시작 처리 (TurnManager 연동)
        /// </summary>
        private void StartGame()
        {
            // Start 버튼 숨기기
            startButton.gameObject.SetActive(false);

            if (PhotonNetwork.IsMasterClient)
            {
                // TurnManager를 통한 게임 시작
                if (TurnManager.Instance != null)
                {
                    Debug.Log("[InGameUIManager] TurnManager.StartGame() 호출");
                    TurnManager.Instance.StartGame();
                }
                else
                {
                    Debug.LogError("[InGameUIManager] TurnManager를 찾을 수 없습니다!");
                    ResetUI(); // 실패 시 UI 복원
                }
            }
            else
            {
                Debug.Log("[InGameUIManager] 방장의 게임 시작을 대기 중...");
            }
        }

        /// <summary>
        /// UI 리셋 (게임 종료 또는 실패 시)
        /// </summary>
        public void ResetUI()
        {
            Debug.Log("[InGameUIManager] UI 리셋");

            // 게임 상태 초기화
            isStart = false;

            // Start 버튼 다시 표시 및 상태 설정
            int playerCount = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
            bool canStart = playerCount == 2 && PhotonNetwork.IsMasterClient;

            startButton.gameObject.SetActive(true);
            SetButtonState(startButton, canStart, enabledStartSprite);

            // 턴 종료 버튼 비활성화
            SetButtonState(endButton, false, enabledEndSprite);

            // 턴 표시 초기화
            turn.text = "대기 중";
        }

        /// <summary>
        /// 게임 승리/패배 UI 표시
        /// </summary>
        public void ShowGameResult(CardZone.OwnerType winner)
        {
            string resultText = winner == CardZone.OwnerType.Player ? "승리!" : "패배!";
            Debug.Log($"[InGameUIManager] 게임 결과: {resultText}");

            // TODO: 게임 결과 UI 패널 표시
            // 현재는 3초 후 자동으로 UI 리셋
            Invoke(nameof(ResetUI), 3f);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 버튼 상태 설정 (통합 메서드)
        /// </summary>
        /// <param name="button">대상 버튼</param>
        /// <param name="isInteractable">상호작용 가능 여부</param>
        /// <param name="enabledSprite">활성화 스프라이트</param>
        /// <param name="isActive">GameObject 활성화 여부</param>
        public void SetButtonState(Button button, bool isInteractable, Sprite enabledSprite, bool isActive = true)
        {
            if (button == null) return;

            button.gameObject.SetActive(isActive);
            button.interactable = isInteractable;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = isInteractable ? enabledSprite : disabledSprite;
            }
        }

        /// <summary>
        /// 현재 게임 상태 디버그 출력
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintState()
        {
            Debug.Log($"[InGameUIManager] 게임시작: {isStart}, " +
                     $"플레이어수: {PhotonNetwork.CurrentRoom?.PlayerCount}, " +
                     $"방장: {PhotonNetwork.IsMasterClient}");
        }
        #endregion

        #region Event Subscriptions (게임 종료 시 정리)
        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (startButton != null) startButton.onClick.RemoveListener(OnClickStart);
            if (endButton != null) endButton.onClick.RemoveListener(OnClickEnd);
            if (leaveButton != null) leaveButton.onClick.RemoveListener(OnClickLeave);
        }
        #endregion
    }
}
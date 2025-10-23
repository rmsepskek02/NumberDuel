using DG.Tweening;
using Objects;
using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
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
        public TextMeshProUGUI playerText;
        public TextMeshProUGUI opponentText;

        [Header("버튼 스프라이트")]
        public Sprite enabledStartSprite;
        public Sprite enabledLeaveSprite;
        public Sprite enabledEndSprite;
        public Sprite disabledSprite;

        [Header("게임 상태")]
        public bool isStart;

        [Header("애니메이션 설정")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float scaleUpDuration = 0.6f;
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float pulseDuration = 0.8f;
        [SerializeField] private int pulseCount = 3;
        [SerializeField] private Ease scaleEase = Ease.OutBack;

        private PhotonManager pm;
        [SerializeField] private string winText = "WIN";
        [SerializeField] private string loseText = "LOSE";
        [SerializeField] private Color winColor = Color.green;
        [SerializeField] private Color loseColor = Color.red;

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
            Debug.Log("[InGameUIManager] EnableEndTurnButton 호출됨");

            if (TurnManager.Instance != null && TurnManager.Instance.IsGameStarted)
            {
                SetButtonState(endButton, true, enabledEndSprite);
                Debug.Log("[InGameUIManager] End 버튼 활성화 완료");
            }
            else
            {
                Debug.LogWarning("[InGameUIManager] TurnManager가 없거나 게임이 시작되지 않음");
            }
        }

        /// <summary>
        /// 상대 턴일 때 턴 종료 버튼 비활성화
        /// </summary>
        private void DisableEndTurnButton()
        {
            Debug.Log("[InGameUIManager] DisableEndTurnButton 호출됨");
            SetButtonState(endButton, false, enabledEndSprite);
            Debug.Log("[InGameUIManager] End 버튼 비활성화 완료");
        }

        /// <summary>
        /// 플레이어 입장 시 UI 업데이트
        /// </summary>
        private void OnPlayerEnter()
        {
            UpdateButtons(PhotonNetwork.CurrentRoom?.PlayerCount ?? 0);
        }

        /// <summary>
        /// 플레이어 퇴장 시 UI 업데이트
        /// </summary>
        private void OnPlayerLeave()
        {
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
            bool shouldShowStart = PhotonNetwork.IsMasterClient && !isStart; // 방장이고 게임이 시작되지 않았을 때만 보이기

            SetButtonState(startButton, canStart, enabledStartSprite, shouldShowStart);

            Debug.Log($"[InGameUIManager] 플레이어 수: {playerCount}, Start 버튼 활성화: {canStart}, Start 버튼 표시: {shouldShowStart}");
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

            Debug.Log("[InGameUIManager] START 버튼 클릭");
            startButton.gameObject.SetActive(false);

            // 게임이 종료된 상태인지 확인
            if (InGameManager.Instance != null && InGameManager.Instance.IsGameEnded)
            {
                // 게임 재시작
                Debug.Log("[InGameUIManager] 게임 재시작 실행");
                isStart = true;
                InGameManager.Instance.RestartGame();
            }
            else
            {
                // 새 게임 시작
                Debug.Log("[InGameUIManager] 새 게임 시작");
                isStart = true;
                StartGame();
            }
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
            UpdateButtons(playerCount);

            // 턴 종료 버튼 비활성화
            SetButtonState(endButton, false, enabledEndSprite);

            // 턴 표시 초기화
            turn.text = "대기 중";
        }

        /// <summary>
        /// 게임 결과 표시 (애니메이션 포함)
        /// </summary>
        /// <param name="isWin">로컬 플레이어가 승리했는지 여부</param>
        public void ShowGameResult(bool isWin)
        {
            Debug.Log($"[InGameUIManager] 게임 결과 표시: {(isWin ? "WIN" : "LOSE")}");

            if (isWin)
            {
                ShowWinResult();
            }
            else
            {
                ShowLoseResult();
            }
        }

        /// <summary>
        /// 승리 결과 표시
        /// </summary>
        private void ShowWinResult()
        {
            // 플레이어 텍스트: WIN
            if (playerText != null)
            {
                playerText.text = winText;
                playerText.color = winColor;
                playerText.gameObject.SetActive(true);
                PlayResultAnimation(playerText, true);
            }

            // 상대 텍스트: LOSE
            if (opponentText != null)
            {
                opponentText.text = loseText;
                opponentText.color = loseColor;
                opponentText.gameObject.SetActive(true);
                PlayResultAnimation(opponentText, false);
            }
        }

        /// <summary>
        /// 패배 결과 표시
        /// </summary>
        private void ShowLoseResult()
        {
            // 플레이어 텍스트: LOSE
            if (playerText != null)
            {
                playerText.text = loseText;
                playerText.color = loseColor;
                playerText.gameObject.SetActive(true);
                PlayResultAnimation(playerText, false);
            }

            // 상대 텍스트: WIN
            if (opponentText != null)
            {
                opponentText.text = winText;
                opponentText.color = winColor;
                opponentText.gameObject.SetActive(true);
                PlayResultAnimation(opponentText, true);
            }
        }

        /// <summary>
        /// 결과 텍스트 애니메이션 재생
        /// </summary>
        /// <param name="textMesh">애니메이션을 적용할 텍스트</param>
        /// <param name="isWin">승리 텍스트인지 여부</param>
        private void PlayResultAnimation(TextMeshProUGUI textMesh, bool isWin)
        {
            // 초기 상태 설정
            textMesh.alpha = 0f;
            textMesh.transform.localScale = Vector3.zero;

            DG.Tweening.Sequence animSequence = DOTween.Sequence();

            // 1단계: 페이드 인 + 스케일 업
            animSequence.Append(textMesh.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
            animSequence.Join(textMesh.transform.DOScale(Vector3.one, scaleUpDuration).SetEase(scaleEase));

            // 2단계: 승리 텍스트는 펄스 효과 추가
            if (isWin)
            {
                animSequence.AppendInterval(0.2f); // 짧은 대기

                // 펄스 효과 (여러 번 반복)
                for (int i = 0; i < pulseCount; i++)
                {
                    animSequence.Append(textMesh.transform.DOScale(Vector3.one * pulseScale, pulseDuration / 2)
                        .SetEase(Ease.InOutQuad));
                    animSequence.Append(textMesh.transform.DOScale(Vector3.one, pulseDuration / 2)
                        .SetEase(Ease.InOutQuad));
                }
            }
            else
            {
                // 패배 텍스트는 약간 어두워지는 효과
                animSequence.AppendInterval(0.3f);
                Color darkenedColor = textMesh.color * 0.7f;
                darkenedColor.a = 1f;
                animSequence.Append(textMesh.DOColor(darkenedColor, 0.5f));
            }
        }

        /// <summary>
        /// 게임 결과 텍스트 숨김 (게임 재시작 시 사용)
        /// </summary>
        public void HideGameResultTexts()
        {
            if (playerText != null)
            {
                playerText.DOKill(); // 진행 중인 애니메이션 중지
                playerText.gameObject.SetActive(false);
                playerText.alpha = 0f;
                playerText.transform.localScale = Vector3.zero;
            }

            if (opponentText != null)
            {
                opponentText.DOKill(); // 진행 중인 애니메이션 중지
                opponentText.gameObject.SetActive(false);
                opponentText.alpha = 0f;
                opponentText.transform.localScale = Vector3.zero;
            }

            Debug.Log("[InGameUIManager] 게임 결과 텍스트 숨김 완료");
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

        #region Event Subscriptions
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
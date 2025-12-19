using DG.Tweening;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utills;

namespace Manager
{
    /// <summary>
    /// 인 게임에서 UI를 관리하는 매니저
    /// TurnManager 중심의 게임 플로우 관리
    /// </summary>
    public class InGameUIManager : Utills.Singleton<InGameUIManager>
    {
        #region Fields and Properties
        [Header("UI 컴포넌트")]
        public Button startButton;
        public Button endButton;
        public TextMeshProUGUI turn;
        public TextMeshProUGUI playerText;
        public TextMeshProUGUI opponentText;

        [Header("버튼 스프라이트")]
        public Sprite enabledStartSprite;
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

        [SerializeField] private string winText = "승리!!";
        [SerializeField] private string loseText = "패배..";
        [SerializeField] private Color winColor = Color.green;
        [SerializeField] private Color loseColor = Color.red;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // 모든 버튼에 클릭 사운드 자동 등록
            UIHelper.RegisterAllButtonSounds();

            pm = FindAnyObjectByType<PhotonManager>();
            InitializeButtons();
            UpdateButtons(PhotonNetwork.CurrentRoom?.PlayerCount ?? 0);
        }

        void Update()
        {
            UpdateTurnDisplay();
            UpdateEndButtonState();
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

            // 초기 버튼 상태 설정
            SetButtonState(endButton, false, disabledSprite);
            SetButtonState(startButton, false, enabledStartSprite, false);
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

        /// <summary>
        /// 턴 종료 버튼 상태 업데이트
        /// 턴 종료가 예약되었거나 턴 전환 중이거나 원격 액션 진행 중이면 버튼 비활성화
        /// </summary>
        private void UpdateEndButtonState()
        {
            if (TurnManager.Instance == null || endButton == null) return;

            // 게임이 시작되지 않았거나 내 턴이 아니면 처리하지 않음
            if (!TurnManager.Instance.IsGameStarted || !TurnManager.Instance.IsLocalPlayerTurn)
                return;

            // 턴 종료가 예약되어 있거나 턴 전환 중이거나 원격 액션 진행 중이면 버튼 비활성화
            bool hasPendingRemoteActions = NetworkGameManager.Instance != null &&
                                           NetworkGameManager.Instance.HasPendingRemoteActions;

            if (TurnManager.Instance.IsPendingEndTurn ||
                TurnManager.Instance.IsProcessingTurn ||
                hasPendingRemoteActions)
            {
                SetButtonState(endButton, false, enabledEndSprite);
            }
            else
            {
                // 모든 조건이 해제되면 활성화
                SetButtonState(endButton, true, enabledEndSprite);
            }
        }
        #endregion

        #region PhotonManager Integration
        /// <summary>
        /// PhotonManager에게 UI 이벤트 등록
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
        /// 인원 확인 후에 Start 버튼 활성화
        /// </summary>
        private void UpdateButtons(int playerCount)
        {
            bool canStart = playerCount == 2 && PhotonNetwork.IsMasterClient && !isStart;
            bool shouldShowStart = PhotonNetwork.IsMasterClient && !isStart;

            SetButtonState(startButton, canStart, enabledStartSprite, shouldShowStart);
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
                return;
            }

            if (PhotonNetwork.CurrentRoom?.PlayerCount != 2)
            {
                return;
            }

            startButton.gameObject.SetActive(false);

            // 시스템 메시지 표시
            if (SystemMessageManager.Instance != null)
            {
                SystemMessageManager.Instance.ShowMessage("GameStarting");
            }

            // 게임이 종료된 상태인지 확인
            if (InGameManager.Instance != null && InGameManager.Instance.IsGameEnded)
            {
                // 게임 재시작
                isStart = true;
                InGameManager.Instance.RestartGame();
            }
            else
            {
                // 새 게임 시작
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
                return;
            }

            if (!TurnManager.Instance.IsLocalPlayerTurn)
            {
                return;
            }

            TurnManager.Instance.EndTurn();
        }
        #endregion

        #region Game Flow Management
        /// <summary>
        /// 게임 시작 처리 (TurnManager 이용)
        /// </summary>
        private void StartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // TurnManager에 게임 시작 명령
                if (TurnManager.Instance != null)
                {
                    TurnManager.Instance.StartGame();
                }
                else
                {
                    ResetUI();
                }
            }
        }

        /// <summary>
        /// UI 리셋 (게임 종료 또는 재시작 시)
        /// </summary>
        public void ResetUI()
        {
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

            Sequence animSequence = DOTween.Sequence();

            // 1단계: 페이드 인 + 스케일 업
            animSequence.Append(textMesh.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
            animSequence.Join(textMesh.transform.DOScale(Vector3.one, scaleUpDuration).SetEase(scaleEase));

            // 2단계: 승리 텍스트에 펄스 효과 추가
            if (isWin)
            {
                animSequence.AppendInterval(0.2f);

                // 펄스 효과 (커짐 후 반복)
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
                // 패배 텍스트에 약간 어두워지는 효과
                animSequence.AppendInterval(0.3f);
                Color darkenedColor = textMesh.color * 0.7f;
                darkenedColor.a = 1f;
                animSequence.Append(textMesh.DOColor(darkenedColor, 0.5f));
            }
        }

        /// <summary>
        /// 게임 결과 텍스트 숨김 (게임 재시작 시 호출)
        /// </summary>
        public void HideGameResultTexts()
        {
            if (playerText != null)
            {
                playerText.DOKill();
                playerText.gameObject.SetActive(false);
                playerText.alpha = 0f;
                playerText.transform.localScale = Vector3.zero;
            }

            if (opponentText != null)
            {
                opponentText.DOKill();
                opponentText.gameObject.SetActive(false);
                opponentText.alpha = 0f;
                opponentText.transform.localScale = Vector3.zero;
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 버튼 상태 설정 (통합 메서드)
        /// </summary>
        /// <param name="button">대상 버튼</param>
        /// <param name="isInteractable">인터랙트 가능 여부</param>
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
        #endregion

        #region Event Subscriptions
        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (startButton != null) startButton.onClick.RemoveListener(OnClickStart);
            if (endButton != null) endButton.onClick.RemoveListener(OnClickEnd);
        }
        #endregion
    }
}

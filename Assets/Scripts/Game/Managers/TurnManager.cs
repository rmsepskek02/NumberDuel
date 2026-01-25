using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using Utills;
using Objects;

namespace Manager
{
    /// <summary>
    /// 게임 턴을 중심으로 한 관리 시스템
    /// PunTurnManager 사용하지 않으며 커스텀 RPC 기반 턴 관리 사용
    /// MonoBehaviourPun을 상속해 RPC 사용
    ///
    /// 주요 기능:
    /// - 턴 순서 관리 (홀수 턴: 선공, 짝수 턴: 후공)
    /// - 첫 라운드 관리 (첫 2턴은 공격 불가)
    /// - 게임 시작/종료/재시작 처리
    /// - 모든 클라이언트 간 턴 동기화
    /// </summary>
    public class TurnManager : MonoBehaviourPun
    {
        #region Singleton Implementation
        private static TurnManager instance;
        public static TurnManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindAnyObjectByType<TurnManager>();
                return instance;
            }
        }

        private void Awake()
        {
            // Singleton 설정
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        #endregion

        #region Fields and Properties
        // 턴 시스템 설정
        [Header("턴 시스템 설정")]
        // TODO: 턴 제한 시간 기능 구현 필요
        // [SerializeField] private float turnTimeLimit = 60f; // 턴 제한 시간 (초)

        // 게임 상태
        private bool isGameStarted = false;
        private bool isFirstRound = true; // 첫 2턴은 공격 불가
        private int currentTurn = 0; // 현재 턴 번호 (1부터 시작)
        private int firstPlayerActorNumber = -1; // 선공 플레이어 ActorNumber

        // 플레이어 역할 (게임 시작시 할당)
        private bool isLocalPlayerFirst = false; // 로컬 플레이어가 선공인지 여부

        // 프로세스 플래그
        private bool isProcessingTurn = false; // 턴 전환 처리 중
        private float lastTurnChangeTime = 0f; // 마지막 턴 변경 시간
        private bool pendingEndTurn = false; // 턴 종료 대기 플래그
        /// <summary>
        /// 게임이 시작되었는지 여부
        /// </summary>
        public bool IsGameStarted => isGameStarted;

        /// <summary>
        /// 첫 번째 라운드인지 여부 (공격 불가 구간)
        /// 첫 2턴 동안은 공격이 불가능하며, 3턴부터 공격 가능
        /// </summary>
        public bool IsFirstRound => isFirstRound;

        /// <summary>
        /// 현재 턴 번호 (1부터 시작)
        /// </summary>
        public int CurrentTurn => currentTurn;

        /// <summary>
        /// 로컬 플레이어의 턴인지 여부
        /// 로직: 선공은 홀수 턴(1, 3, 5...), 후공은 짝수 턴(2, 4, 6...)을 가짐
        ///
        /// 중요: 게임이 시작되지 않았으면 항상 false 반환
        /// </summary>
        public bool IsLocalPlayerTurn
        {
            get
            {
                if (!isGameStarted) return false;

                // 선공은 홀수 턴(1, 3, 5...), 후공은 짝수 턴(2, 4, 6...)을 담당
                bool isFirstPlayerTurn = (currentTurn % 2) == 1;

                // 로컬 플레이어가 선공이면 홀수 턴일 때 true, 후공이면 짝수 턴일 때 true
                return isLocalPlayerFirst == isFirstPlayerTurn;
            }
        }

        /// <summary>
        /// 로컬 플레이어가 선공인지 여부
        /// </summary>
        public bool IsLocalPlayerFirst => isLocalPlayerFirst;

        /// <summary>
        /// 로컬 플레이어의 역할 (Player 또는 Opponent)
        /// 선공이면 Player, 후공이면 Opponent
        /// </summary>
        public CardZone.OwnerType LocalPlayerRole =>
            isLocalPlayerFirst ? CardZone.OwnerType.Player : CardZone.OwnerType.Opponent;

        /// <summary>
        /// 상대 플레이어의 역할
        /// 로컬 플레이어의 역할과 반대
        /// </summary>
        public CardZone.OwnerType OpponentPlayerRole =>
            isLocalPlayerFirst ? CardZone.OwnerType.Opponent : CardZone.OwnerType.Player;

        /// <summary>
        /// 턴 종료가 예약되어 대기 중인지 여부
        /// UI에서 버튼 상태 업데이트에 사용
        /// </summary>
        public bool IsPendingEndTurn => pendingEndTurn;

        /// <summary>
        /// 턴 전환 처리 중인지 여부
        /// UI에서 버튼 상태 업데이트에 사용
        /// </summary>
        public bool IsProcessingTurn => isProcessingTurn;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 이벤트 구독
            SubscribeToEvents();

            // 게임 상태 초기화
            ResetGameState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Event Subscription
        /// <summary>
        /// 이벤트 구독 등록
        /// InGameManager의 게임 종료 이벤트 수신
        /// </summary>
        private void SubscribeToEvents()
        {
            InGameManager.OnGameEnded += OnGameEnded;
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            InGameManager.OnGameEnded -= OnGameEnded;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 게임 시작 (마스터 클라이언트만 호출 가능)
        ///
        /// 실행 순서:
        /// 1. 선공 플레이어 랜덤 결정
        /// 2. RPC로 모든 클라이언트에 게임 시작 알림
        /// 3. 덱 초기화 및 초기 카드 드로우
        /// 4. 첫 턴 시작
        /// </summary>
        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[TurnManager] 마스터 클라이언트만 게임을 시작할 수 있습니다.");
                return;
            }

            if (isGameStarted)
            {
                Debug.LogWarning("[TurnManager] 게임이 이미 시작되었습니다.");
                return;
            }

            if (PhotonNetwork.CurrentRoom.PlayerCount != 2)
            {
                Debug.LogWarning("[TurnManager] 2명의 플레이어가 필요합니다.");
                return;
            }

            StartCoroutine(StartGameSequence());
        }

        /// <summary>
        /// 턴 종료 (로컬 플레이어의 턴일 때만 호출 가능)
        ///
        /// 실행 순서:
        /// 1. 프로세스 진행 중이면 턴 종료 대기 플래그 설정 후 대기
        /// 2. 프로세스가 없으면 즉시 턴 종료 시퀀스 실행
        /// 3. 턴 종료 처리 (공격 상태 초기화, 연산자 모드 취소 등)
        /// 4. 턴 번호 증가 (RPC로 모든 클라이언트에 동기화)
        /// 5. 첫 라운드 종료 체크
        /// </summary>
        public void EndTurn()
        {
            if (!IsLocalPlayerTurn)
            {
                Debug.LogWarning("[TurnManager] 내 턴이 아닙니다.");
                return;
            }

            if (isProcessingTurn)
            {
                Debug.LogWarning("[TurnManager] 턴 처리 중입니다.");
                return;
            }

            // 프로세스 진행 중이면 턴 종료 대기 플래그 설정
            if (InGameManager.Instance.IsProcessing)
            {
                if (!pendingEndTurn)
                {
                    pendingEndTurn = true;
                }
                return;
            }

            // 즉시 턴 종료 실행
            ExecuteEndTurn();
        }

        /// <summary>
        /// 턴 종료 실제 실행
        /// EndTurn() 또는 CheckPendingEndTurn()에서 호출
        /// </summary>
        private void ExecuteEndTurn()
        {
            pendingEndTurn = false;
            StartCoroutine(EndTurnSequence());
        }

        /// <summary>
        /// 대기 중인 턴 종료 요청 체크 및 실행
        /// InGameManager.EndProcess()에서 호출
        /// </summary>
        public void CheckPendingEndTurn()
        {
            if (!pendingEndTurn) return;
            if (!IsLocalPlayerTurn) return;
            if (isProcessingTurn) return;
            if (InGameManager.Instance.IsProcessing) return;

            ExecuteEndTurn();
        }

        /// <summary>
        /// 게임 재시작 (마스터 클라이언트만 호출 가능)
        ///
        /// 실행 순서:
        /// 1. RPC로 모든 클라이언트 게임 상태 초기화
        /// 2. 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[TurnManager] 마스터 클라이언트만 재시작할 수 있습니다.");
                return;
            }

            StartCoroutine(RestartGameSequence());
        }

        /// <summary>
        /// 로컬 플레이어가 액션을 수행할 수 있는지 확인
        /// 게임 시작, 자신의 턴, 턴 전환 중이 아님, 다른 프로세스 진행 중이 아님, 원격 액션 진행 중이 아님 모두 만족해야 함
        /// </summary>
        public bool CanPerformAction()
        {
            bool hasPendingRemoteActions = NetworkGameManager.Instance != null &&
                                          NetworkGameManager.Instance.HasPendingRemoteActions;

            return isGameStarted &&
                   IsLocalPlayerTurn &&
                   !isProcessingTurn &&
                   !InGameManager.Instance.IsProcessing &&
                   !hasPendingRemoteActions;
        }

        /// <summary>
        /// 첫 라운드에서 공격이 가능한지 확인
        /// 첫 2턴(선공 1턴, 후공 1턴)은 공격 불가
        /// </summary>
        public bool CanAttackInFirstRound()
        {
            return !isFirstRound;
        }
        #endregion

        #region Game Sequence
        /// <summary>
        /// 게임 시작 시퀀스
        /// ExecuteInitialDraw() 호출은 RPC_StartGame에서 이동
        ///
        /// 실행 순서:
        /// 1. 선공 플레이어 랜덤 결정
        /// 2. RPC로 모든 클라이언트에 게임 시작 알림 (덱 초기화 및 카드 드로우)
        /// 3. 첫 턴 시작
        /// </summary>
        private IEnumerator StartGameSequence()
        {
            // 1단계: 첫 번째 플레이어 랜덤 결정
            int randomFirstPlayer = DetermineFirstPlayer();

            // 2단계: 모든 클라이언트에 게임 시작 알림 (초기 드로우 포함)
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_StartGame", RpcTarget.All, randomFirstPlayer);
            }

            // 3단계: 첫 턴 시작 (RPC로 모든 클라이언트에 동기화)
            // 덱 초기화와 카드 드로우는 동기적으로 즉시 완료되므로 지연 불필요
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_BeginFirstTurn", RpcTarget.All);
            }

            yield break;
        }

        /// <summary>
        /// 턴 종료 시퀀스
        ///
        /// 실행 순서:
        /// 1. 턴 종료 처리 (공격 상태 초기화, 연산자 모드 취소 등)
        /// 2. 다음 턴으로 전환 (RPC로 모든 클라이언트 동기화)
        /// 3. 첫 라운드 종료 체크 (턴 3 이상이면 공격 활성화)
        /// </summary>
        private IEnumerator EndTurnSequence()
        {
            isProcessingTurn = true;

            // 1단계: 턴 종료 처리
            yield return StartCoroutine(ProcessTurnEnd());

            // 2단계: 다음 턴으로 전환
            int nextTurn = currentTurn + 1;
            photonView.RPC("RPC_AdvanceTurn", RpcTarget.All, nextTurn);

            // 3단계: 첫 라운드 종료 체크
            if (isFirstRound && nextTurn > 2)
            {
                photonView.RPC("RPC_EndFirstRound", RpcTarget.All);
            }

            yield return new WaitForSeconds(0.3f);
            isProcessingTurn = false;
        }

        /// <summary>
        /// 게임 재시작 시퀀스
        ///
        /// 실행 순서:
        /// 1. RPC로 모든 클라이언트 게임 상태 초기화
        /// 2. 게임 상태 리셋
        /// 3. 새 게임 시작
        /// </summary>
        private IEnumerator RestartGameSequence()
        {
            // 1단계: RPC로 모든 클라이언트에 게임 초기화
            photonView.RPC("RPC_RestartGameForAll", RpcTarget.All);

            yield return new WaitForSeconds(1f);

            // 2단계: 게임 상태 리셋
            photonView.RPC("RPC_ResetGame", RpcTarget.All);

            yield return new WaitForSeconds(0.5f);

            // 3단계: 새 게임 시작
            StartGame();
        }

        /// <summary>
        /// 모든 클라이언트에서 게임 초기화 수행
        /// InGameManager의 로컬 재시작 로직 호출
        /// </summary>
        [PunRPC]
        private void RPC_RestartGameForAll()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.RestartGameLocal();
            }
        }
        #endregion

        #region Turn Processing
        /// <summary>
        /// 첫 번째 플레이어 결정 (랜덤)
        /// PhotonNetwork.PlayerList에서 랜덤 선택
        /// </summary>
        private int DetermineFirstPlayer()
        {
            Player[] players = PhotonNetwork.PlayerList;
            int randomIndex = UnityEngine.Random.Range(0, players.Length);
            return players[randomIndex].ActorNumber;
        }

        /// <summary>
        /// 초기 카드 드로우 실행
        ///
        /// 드로우 규칙:
        /// - 선공: 4장
        /// - 후공: 5장 (선공 불이익 보상)
        ///
        /// 중요: 모든 플레이어가 자신의 관점에서 Player 존으로 드로우함
        /// NetworkGameManager가 상대 클라이언트에는 Opponent 카드로 동기화
        /// </summary>
        private void ExecuteInitialDraw()
        {
            if (InGameManager.Instance == null)
            {
                Debug.LogError("[TurnManager] InGameManager 인스턴스를 찾을 수 없습니다!");
                return;
            }

            // 선공은 7장, 후공은 8장 드로우
            int drawCount = isLocalPlayerFirst ? 7 : 8;

            // 모든 플레이어가 Player 존으로 드로우 (로컬 관점에서 자신의 카드)
            InGameManager.Instance.DrawCardsToHand(drawCount, CardZone.OwnerType.Player);
        }

        /// <summary>
        /// 첫 턴 시작
        /// 턴 번호를 1로 설정하고 턴 시작 처리 호출
        /// </summary>
        private void BeginFirstTurn()
        {
            currentTurn = 1;
            lastTurnChangeTime = Time.time;

            OnTurnStart();
        }

        /// <summary>
        /// 턴 시작 처리
        ///
        /// 실행 내용:
        /// 1. 원격 액션이 완료될 때까지 대기
        /// 2. 모든 필드 카드의 턴 상태 초기화 (원격 액션 완료 후)
        /// 3. 로컬 플레이어 턴이면 카드 1장 드로우 (첫 턴 제외)
        /// 4. UI 업데이트 이벤트 발생
        /// </summary>
        private void OnTurnStart()
        {
            // 원격 액션이 진행 중이면 완료될 때까지 대기 후 턴 시작
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.HasPendingRemoteActions)
            {
                StartCoroutine(WaitForRemoteActionsAndStartTurn());
            }
            else
            {
                // 원격 액션이 없으면 즉시 턴 시작
                CompleteTurnStart();
            }
        }

        /// <summary>
        /// 원격 액션 완료 대기 후 턴 시작
        /// </summary>
        private IEnumerator WaitForRemoteActionsAndStartTurn()
        {
            // 원격 액션이 모두 완료될 때까지 대기
            while (NetworkGameManager.Instance != null && NetworkGameManager.Instance.HasPendingRemoteActions)
            {
                yield return null;
            }

            // 원격 액션 완료 후 턴 시작
            CompleteTurnStart();
        }

        /// <summary>
        /// 턴 시작 완료 (카드 상태 리셋, 드로우, UI 업데이트)
        /// 원격 액션 완료 후 호출되므로 GLOW 효과가 올바르게 표시됨
        /// </summary>
        private void CompleteTurnStart()
        {
            // 모든 필드 카드의 턴 상태 초기화 (원격 액션 완료 후)
            // 이 시점에서 HasPendingRemoteActions = false이므로 GLOW가 정상 표시됨
            ResetAllCardsForNewTurn();

            if (IsLocalPlayerTurn)
            {
                // 내 턴 시작 사운드 재생
                SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_TurnStart);

                // 턴 시작: 카드 1장 드로우 (첫 턴 제외)
                if (currentTurn > 1)
                {
                    // Player 존으로 드로우
                    InGameManager.Instance.StartTurn(CardZone.OwnerType.Player);
                }
            }

            // UI 업데이트 이벤트 발생
            NotifyTurnChange();
        }

        /// <summary>
        /// 턴 종료 처리
        ///
        /// 정리 내용:
        /// 1. InGameManager 프로세스 상태 초기화
        /// 2. 공격 상태 강제 리셋
        /// 3. 연산자 모드 취소
        /// </summary>
        private IEnumerator ProcessTurnEnd()
        {
            // 모든 프로세스 상태 종료
            if (InGameManager.Instance.IsProcessing)
            {
                InGameManager.Instance.EndProcess();
            }

            // 공격 상태 리셋
            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            if (attackManager != null)
            {
                attackManager.ForceResetAttackState();
            }

            // 연산자 모드 취소
            if (OperatorManager.Instance != null && OperatorManager.Instance.IsInOperatorMode)
            {
                OperatorManager.Instance.CancelOperatorMode();
            }

            yield return new WaitForSeconds(0.1f);
        }

        /// <summary>
        /// 모든 필드 카드의 턴 상태 초기화
        ///
        /// 초기화 항목:
        /// - WasPlayedThisTurn: 이번 턴에 배치됨 (false로 초기화)
        /// - HasAttackedThisTurn: 이번 턴에 공격함 (false로 초기화)
        /// - WasModifiedThisTurn: 이번 턴에 수정됨 (false로 초기화)
        /// </summary>
        private void ResetAllCardsForNewTurn()
        {
            if (InGameManager.Instance == null) return;

            var fieldCards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in fieldCards)
            {
                // 새로운 Card.cs API 사용
                card.ResetForNewTurn();
            }
        }

        /// <summary>
        /// 첫 라운드 종료 후 모든 카드의 GLOW 오버라이드 해제
        /// 공격 가능한 카드는 자동으로 초록색 GLOW 표시됨
        /// </summary>
        private void ClearAllCardGlowOverrides()
        {
            if (InGameManager.Instance == null) return;

            var fieldCards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in fieldCards)
            {
                // 새로운 Card.cs API 사용: 강제 GLOW 해제하여 일반 상태 복귀
                card.ClearGlowOverride();
            }
        }
        #endregion

        #region RPC Methods
        /// <summary>
        /// 게임 시작 RPC 핸들러
        ///
        /// 모든 클라이언트에서 실행되어 다음 작업 수행:
        /// 1. 선공 플레이어 결정 및 로컬 플레이어 역할 설정
        /// 2. 게임 상태 초기화
        /// 3. 카드 색상 동기화 대기
        /// 4. 덱 초기화 (카드 색상 동기화 완료 후 실행)
        /// 5. 초기 카드 드로우 (선공 4장, 후공 5장)
        /// </summary>
        [PunRPC]
        private void RPC_StartGame(int firstPlayerActorNumber)
        {
            this.firstPlayerActorNumber = firstPlayerActorNumber;
            isLocalPlayerFirst = (PhotonNetwork.LocalPlayer.ActorNumber == firstPlayerActorNumber);
            isGameStarted = true;
            isFirstRound = true;
            currentTurn = 0;

            // 색상 동기화가 완료될 때까지 대기 후 덱 초기화 및 카드 드로우
            StartCoroutine(WaitForColorSyncAndInitialize());
        }

        /// <summary>
        /// 색상 동기화 완료 대기 후 덱 초기화 및 카드 드로우
        /// ResourcesManager의 색상 동기화가 완료되어야 올바른 색상의 카드가 생성됨
        /// </summary>
        private IEnumerator WaitForColorSyncAndInitialize()
        {
            // ResourcesManager의 색상 동기화 완료 대기
            // 게스트 플레이어는 RPC_SyncStoredColors를 받을 때까지 대기
            float timeout = 5f; // 최대 5초 대기
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                // ResourcesManager가 색상 동기화를 완료했는지 확인
                if (ResourcesManager.Instance != null && ResourcesManager.Instance.IsColorSynchronized)
                {
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 타임아웃 경고
            if (elapsed >= timeout)
            {
                Debug.LogWarning("[TurnManager] 색상 동기화 타임아웃! 기본 색상으로 진행합니다.");
            }

            // 덱 초기화 (카드 색상 동기화 완료 후 실행)
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.InitializeDecks();
            }
            else
            {
                Debug.LogError("[TurnManager] DeckManager 인스턴스를 찾을 수 없습니다!");
            }

            // 모든 클라이언트에서 자신의 초기 카드 드로우 실행
            ExecuteInitialDraw();

            // 상대방 정보 캐싱 (연결 끊김 시 GameRecord 생성에 사용)
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.CacheOpponentInfo();
            }
        }

        /// <summary>
        /// 턴 진행 RPC 핸들러
        /// 모든 클라이언트의 턴 번호를 동기화하고 턴 시작 처리 호출
        /// </summary>
        [PunRPC]
        private void RPC_AdvanceTurn(int newTurn)
        {
            currentTurn = newTurn;
            lastTurnChangeTime = Time.time;

            // 턴 시작 처리
            OnTurnStart();
        }

        /// <summary>
        /// 첫 라운드 종료 RPC 핸들러
        ///
        /// 첫 라운드 종료 시 수행:
        /// 1. isFirstRound 플래그 해제 (공격 활성화)
        /// 2. 모든 카드의 GLOW 오버라이드 해제 (정상 공격 가능 표시)
        /// </summary>
        [PunRPC]
        private void RPC_EndFirstRound()
        {
            isFirstRound = false;

            // 첫 라운드 종료 후 모든 카드의 GLOW 오버라이드 해제
            ClearAllCardGlowOverrides();
        }

        /// <summary>
        /// 첫 턴 시작 RPC 핸들러
        /// BeginFirstTurn() 호출하여 턴 번호를 1로 설정하고 게임 시작
        /// </summary>
        [PunRPC]
        private void RPC_BeginFirstTurn()
        {
            BeginFirstTurn();
        }

        /// <summary>
        /// 게임 리셋 RPC 핸들러
        /// ResetGameState() 호출하여 게임 상태 초기화
        /// </summary>
        [PunRPC]
        private void RPC_ResetGame()
        {
            ResetGameState();
        }
        #endregion

        #region State Management
        /// <summary>
        /// 게임 상태 초기화
        ///
        /// 초기화 항목:
        /// - 게임 시작 플래그
        /// - 첫 라운드 플래그
        /// - 턴 번호
        /// - 선공 플레이어 ActorNumber
        /// - 로컬 플레이어 역할
        /// - 턴 처리 플래그
        /// - 턴 종료 대기 플래그
        /// </summary>
        public void ResetGameState()
        {
            isGameStarted = false;
            isFirstRound = true;
            currentTurn = 0;
            firstPlayerActorNumber = -1;
            isLocalPlayerFirst = false;
            isProcessingTurn = false;
            pendingEndTurn = false;
            lastTurnChangeTime = 0f;
        }

        /// <summary>
        /// 게임 종료 이벤트 핸들러
        /// InGameManager.OnGameEnded 이벤트 수신
        ///
        /// 처리 내용:
        /// 1. 게임 상태 플래그 해제
        /// 2. UI에 재시작 버튼 활성화 알림
        /// </summary>
        private void OnGameEnded(CardZone.OwnerType winner)
        {
            isGameStarted = false;
            isProcessingTurn = false;

            // UI에 재시작 버튼 활성화 알림
            NotifyGameEnded();
        }
        #endregion

        #region Event Notifications
        /// <summary>
        /// 턴 변경 알림
        ///
        /// PhotonManager를 통해 UI에 턴 변경 이벤트 전달:
        /// - 로컬 플레이어 턴: MyTurn 이벤트 발생
        /// - 상대 플레이어 턴: YourTurn 이벤트 발생
        ///
        /// InGameUIManager가 이 이벤트를 수신하여 UI 업데이트
        /// </summary>
        private void NotifyTurnChange()
        {
            // PhotonManager에 턴 변경 알림
            var photonManager = FindAnyObjectByType<PhotonManager>();
            if (photonManager != null)
            {
                if (IsLocalPlayerTurn)
                {
                    photonManager.TriggerMyTurn();
                }
                else
                {
                    photonManager.TriggerYourTurn();
                }
            }
            else
            {
                Debug.LogError("[TurnManager] PhotonManager를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 게임 종료 알림
        /// InGameUIManager에 재시작 버튼 활성화 요청
        /// </summary>
        private void NotifyGameEnded()
        {
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.ResetUI();
            }
        }
        #endregion
    }
}

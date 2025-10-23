using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using Utills;
using Objects;

namespace Manager
{
    /// <summary>
    /// 버그 방지 중심의 턴 관리 시스템
    /// PunTurnManager 의존성을 제거하고 독립적인 RPC 기반 턴 관리 제공
    /// MonoBehaviourPun을 사용한 RPC 방식
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

        #region Fields
        [Header("턴 시스템 설정")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private float turnTimeLimit = 60f; // 턴 제한 시간 (초)

        // 게임 상태
        private bool isGameStarted = false;
        private bool isFirstRound = true;
        private int currentTurn = 0;
        private int firstPlayerActorNumber = -1;

        // 플레이어 역할 (동적 할당)
        private bool isLocalPlayerFirst = false; // 로컬 플레이어가 선공인지

        // 안전장치
        private bool isProcessingTurn = false;
        private float lastTurnChangeTime = 0f;
        #endregion

        #region Properties
        /// <summary>
        /// 게임이 시작되었는지 여부
        /// </summary>
        public bool IsGameStarted => isGameStarted;

        /// <summary>
        /// 첫 번째 라운드인지 여부 (공격 제한)
        /// </summary>
        public bool IsFirstRound => isFirstRound;

        /// <summary>
        /// 현재 턴 번호
        /// </summary>
        public int CurrentTurn => currentTurn;

        /// <summary>
        /// 로컬 플레이어의 턴인지 여부
        /// 수정: 선공이 항상 첫 턴(턴 1)을 가지도록 수정
        /// </summary>
        public bool IsLocalPlayerTurn
        {
            get
            {
                if (!isGameStarted) return false;

                // 선공이 홀수 턴(1, 3, 5...), 후공이 짝수 턴(2, 4, 6...)을 가짐
                bool isFirstPlayerTurn = (currentTurn % 2) == 1;

                // 로컬 플레이어가 선공이면 홀수 턴이 내 턴, 후공이면 짝수 턴이 내 턴
                return isLocalPlayerFirst == isFirstPlayerTurn;
            }
        }

        /// <summary>
        /// 로컬 플레이어가 선공인지 여부
        /// </summary>
        public bool IsLocalPlayerFirst => isLocalPlayerFirst;

        /// <summary>
        /// 로컬 플레이어의 역할 (Player 또는 Opponent)
        /// </summary>
        public CardZone.OwnerType LocalPlayerRole =>
            isLocalPlayerFirst ? CardZone.OwnerType.Player : CardZone.OwnerType.Opponent;

        /// <summary>
        /// 상대 플레이어의 역할
        /// </summary>
        public CardZone.OwnerType OpponentPlayerRole =>
            isLocalPlayerFirst ? CardZone.OwnerType.Opponent : CardZone.OwnerType.Player;
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
        private void SubscribeToEvents()
        {
            // InGameManager 게임 종료 이벤트 구독
            InGameManager.OnGameEnded += OnGameEnded;
        }

        private void UnsubscribeFromEvents()
        {
            InGameManager.OnGameEnded -= OnGameEnded;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 게임 시작 (방장만 호출 가능)
        /// </summary>
        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[TurnManager] 방장만 게임을 시작할 수 있습니다.");
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
        /// 턴 종료
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

            if (InGameManager.Instance.IsProcessing)
            {
                Debug.LogWarning("[TurnManager] 다른 프로세스가 진행 중입니다.");
                return;
            }

            StartCoroutine(EndTurnSequence());
        }

        /// <summary>
        /// 게임 재시작 (방장만 호출 가능)
        /// </summary>
        public void RestartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[TurnManager] 방장만 게임을 재시작할 수 있습니다.");
                return;
            }

            StartCoroutine(RestartGameSequence());
        }

        /// <summary>
        /// 현재 플레이어가 액션을 수행할 수 있는지 확인
        /// </summary>
        public bool CanPerformAction()
        {
            return isGameStarted &&
                   IsLocalPlayerTurn &&
                   !isProcessingTurn &&
                   !InGameManager.Instance.IsProcessing;
        }

        /// <summary>
        /// 첫 라운드에서 공격이 가능한지 확인
        /// </summary>
        public bool CanAttackInFirstRound()
        {
            return !isFirstRound; // 첫 라운드에서는 공격 불가
        }
        #endregion

        #region Game Sequence
        /// <summary>
        /// 게임 시작 시퀀스
        /// ExecuteInitialDraw() 호출을 RPC_StartGame으로 이동
        /// </summary>
        private IEnumerator StartGameSequence()
        {
            // 1단계: 첫 턴 플레이어 랜덤 결정
            int randomFirstPlayer = DetermineFirstPlayer();

            // 2단계: 모든 클라이언트에 게임 시작 알림 (초기 드로우 포함)
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_StartGame", RpcTarget.All, randomFirstPlayer);
            }

            // 3단계: 덱 초기화 대기
            yield return new WaitForSeconds(0.5f);

            // 4단계: 첫 턴 시작 (RPC로 모든 클라이언트에 전송)
            yield return new WaitForSeconds(1f);
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_BeginFirstTurn", RpcTarget.All);
            }
        }

        /// <summary>
        /// 턴 종료 시퀀스
        /// </summary>
        private IEnumerator EndTurnSequence()
        {
            isProcessingTurn = true;

            // 1단계: 턴 종료 처리
            yield return StartCoroutine(ProcessTurnEnd());

            // 2단계: 다음 턴으로 진행
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
        /// </summary>
        private IEnumerator RestartGameSequence()
        {
            Debug.Log("[TurnManager] 게임 재시작 시퀀스 시작");

            // 1단계: 게임 상태 리셋
            photonView.RPC("RPC_ResetGame", RpcTarget.All);

            yield return new WaitForSeconds(0.5f);

            // 2단계: 새 게임 시작
            StartGame();
        }
        #endregion

        #region Turn Processing
        /// <summary>
        /// 첫 턴 플레이어 결정 (랜덤)
        /// </summary>
        private int DetermineFirstPlayer()
        {
            Player[] players = PhotonNetwork.PlayerList;
            int randomIndex = Random.Range(0, players.Length);
            return players[randomIndex].ActorNumber;
        }

        /// <summary>
        /// 초기 드로우 실행 (차등 드로우: 선공 4장, 후공 5장)
        /// 모든 플레이어가 Player 역할로 드로우하여 양방향 동기화 보장
        /// </summary>
        private void ExecuteInitialDraw()
        {
            if (InGameManager.Instance == null)
            {
                Debug.LogError("[TurnManager] InGameManager 인스턴스를 찾을 수 없습니다!");
                return;
            }

            // 선공은 4장, 후공은 5장 드로우
            int drawCount = isLocalPlayerFirst ? 4 : 5;
            string role = isLocalPlayerFirst ? "선공" : "후공";

            Debug.Log($"[TurnManager] 초기 드로우: {drawCount}장 ({role})");

            // 모든 플레이어가 Player 역할로 드로우 (각자 관점에서 자신의 카드)
            InGameManager.Instance.DrawCardsToHand(drawCount, CardZone.OwnerType.Player);
        }

        /// <summary>
        /// 첫 턴 시작
        /// </summary>
        private void BeginFirstTurn()
        {
            currentTurn = 1;
            lastTurnChangeTime = Time.time;

            // 디버그 로그 추가
            Debug.Log($"[TurnManager] BeginFirstTurn - currentTurn: {currentTurn}, " +
                      $"isLocalPlayerFirst: {isLocalPlayerFirst}, " +
                      $"IsLocalPlayerTurn: {IsLocalPlayerTurn}");

            OnTurnStart();
        }

        /// <summary>
        /// 턴 시작 처리
        /// </summary>
        private void OnTurnStart()
        {
            // 모든 필드 카드의 턴 상태 초기화
            ResetAllCardsForNewTurn();

            if (IsLocalPlayerTurn)
            {
                // 내 턴: 카드 1장 드로우 (첫 턴 제외)
                if (currentTurn > 1)
                {
                    // Player 역할로 통일
                    InGameManager.Instance.StartTurn(CardZone.OwnerType.Player);
                }
            }

            // UI 업데이트 이벤트 발생
            NotifyTurnChange();
        }

        /// <summary>
        /// 턴 종료 처리
        /// </summary>
        private IEnumerator ProcessTurnEnd()
        {
            // 모든 프로세스 강제 종료
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
        /// 모든 카드의 턴 상태 초기화
        /// </summary>
        private void ResetAllCardsForNewTurn()
        {
            if (InGameManager.Instance == null) return;

            var fieldCards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in fieldCards)
            {
                // 새로운 Card.cs API 사용
                card.ResetForNewTurn(); // WasPlayedThisTurn, HasAttackedThisTurn 초기화
            }
        }

        /// <summary>
        /// 첫 라운드 종료 시 모든 카드의 GLOW 오버라이드 해제
        /// </summary>
        private void ClearAllCardGlowOverrides()
        {
            if (InGameManager.Instance == null) return;

            var fieldCards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in fieldCards)
            {
                // 새로운 Card.cs API 사용: 강제 GLOW 해제하여 일반 모드로 복귀
                card.ClearGlowOverride();
            }
        }
        #endregion

        #region RPC Methods
        /// <summary>
        /// 게임 시작 RPC 수신 처리
        /// 모든 클라이언트에서 실행되어 각자 초기 드로우 수행
        /// </summary>
        [PunRPC]
        private void RPC_StartGame(int firstPlayerActorNumber)
        {
            this.firstPlayerActorNumber = firstPlayerActorNumber;
            isLocalPlayerFirst = (PhotonNetwork.LocalPlayer.ActorNumber == firstPlayerActorNumber);
            isGameStarted = true;
            isFirstRound = true;
            currentTurn = 0;

            // 덱 초기화 (색상 동기화 완료 후 실행)
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.InitializeDecks();
            }
            else
            {
                Debug.LogError("[TurnManager] DeckManager 인스턴스를 찾을 수 없습니다!");
            }

            string role = isLocalPlayerFirst ? "선공" : "후공";
            Debug.Log($"[TurnManager] 게임 시작 - 역할: {role}");

            // 모든 클라이언트에서 각자 초기 드로우 실행
            ExecuteInitialDraw();
        }

        /// <summary>
        /// 턴 진행 RPC 수신 처리
        /// </summary>
        [PunRPC]
        private void RPC_AdvanceTurn(int newTurn)
        {
            currentTurn = newTurn;
            lastTurnChangeTime = Time.time;

            Debug.Log($"[TurnManager] 턴 진행: {newTurn}, IsLocalPlayerTurn: {IsLocalPlayerTurn}");

            // 턴 시작 처리
            OnTurnStart();
        }

        /// <summary>
        /// 첫 라운드 종료 RPC 수신 처리
        /// </summary>
        [PunRPC]
        private void RPC_EndFirstRound()
        {
            isFirstRound = false;

            // 첫 라운드 종료 시 모든 카드의 GLOW 오버라이드 해제
            ClearAllCardGlowOverrides();

            Debug.Log("[TurnManager] 첫 라운드 종료 - 공격 활성화");
        }

        /// <summary>
        /// 첫 턴 시작 RPC 수신 처리
        /// </summary>
        [PunRPC]
        private void RPC_BeginFirstTurn()
        {
            BeginFirstTurn();
        }

        /// <summary>
        /// 게임 리셋 RPC 수신 처리
        /// </summary>
        [PunRPC]
        private void RPC_ResetGame()
        {
            ResetGameState();
        }
        #endregion

        #region State Management
        /// <summary>
        /// 게임 상태 초기화 (public으로 변경)
        /// </summary>
        public void ResetGameState()
        {
            isGameStarted = false;
            isFirstRound = true;
            currentTurn = 0;
            firstPlayerActorNumber = -1;
            isLocalPlayerFirst = false;
            isProcessingTurn = false;
            lastTurnChangeTime = 0f;

            Debug.Log("[TurnManager] 게임 상태 초기화 완료");
        }

        /// <summary>
        /// 게임 종료 이벤트 처리
        /// </summary>
        private void OnGameEnded(CardZone.OwnerType winner)
        {
            isGameStarted = false;
            isProcessingTurn = false;

            Debug.Log($"[TurnManager] 게임 종료 - 승자: {winner}");

            // UI에 재시작 버튼 활성화 알림
            NotifyGameEnded();
        }
        #endregion

        #region Event Notifications
        /// <summary>
        /// 턴 변경 알림
        /// PunTurnManager 대신 PhotonManager에 직접 이벤트 전송
        /// </summary>
        private void NotifyTurnChange()
        {
            // PhotonManager에 턴 변경 알림
            var photonManager = FindAnyObjectByType<PhotonManager>();
            if (photonManager != null)
            {
                Debug.Log($"[TurnManager] 턴 이벤트 발생 - IsLocalPlayerTurn: {IsLocalPlayerTurn}");

                if (IsLocalPlayerTurn)
                {
                    Debug.Log("[TurnManager] 내 턴 이벤트 호출");
                    photonManager.MyTurn?.Invoke();
                }
                else
                {
                    Debug.Log("[TurnManager] 상대 턴 이벤트 호출");
                    photonManager.YourTurn?.Invoke();
                }
            }
            else
            {
                Debug.LogError("[TurnManager] PhotonManager를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 게임 종료 알림
        /// </summary>
        private void NotifyGameEnded()
        {
            // InGameUIManager에 재시작 버튼 활성화 요청
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.ResetUI();
            }
        }
        #endregion

        #region Debug & Utility
        /// <summary>
        /// 현재 상태 디버그 출력
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintState()
        {
            Debug.Log($"[TurnManager] 게임상태: {isGameStarted}, 턴: {currentTurn}, " +
                     $"첫라운드: {isFirstRound}, 내턴: {IsLocalPlayerTurn}, 역할: {LocalPlayerRole}");
        }
        #endregion
    }
}
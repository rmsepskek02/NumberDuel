using Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 인 게임에서 필요한 기능들을 관리하는 매니저
    /// TurnManager와 연동하여 턴 기반 게임플레이 제공
    /// </summary>
    public class InGameManager : Singleton<InGameManager>
    {
        #region Variables
        public List<int> playerList = new List<int>();
        public List<string> playerADeck;
        public List<string> playerBDeck;
        public List<string> playerAHand;
        public List<string> playerBHand;
        public GameObject myHandCardList;
        public GameObject myFieldCardList;
        public GameObject yourHandCardList;
        public GameObject yourFieldCardList;
        public bool isStart;
        public GameObject uiInGame;
        public GameObject choice;
        public GameObject joker;
        public int clickedMyCardIdx;
        public string clickedMyCardNumber;
        public int clickedYourCardIdx;
        public string clickedYourCardNumber;
        public string firstCardNumber = "";
        public Transform firstCard;
        public string secondCardNumber = "";
        public bool isCopy;
        public bool isDelete;
        public bool isPlus;
        public bool isMinus;
        public bool isMultiple;
        public bool isDivision;

        private readonly List<Card> fieldCards = new List<Card>();
        #endregion

        #region Process Management
        /// <summary>
        /// 현재 진행 중인 프로세스
        /// </summary>
        private GameProcessState currentProcess = GameProcessState.Idle;

        /// <summary>
        /// 현재 프로세스 상태 (읽기 전용)
        /// </summary>
        public GameProcessState CurrentProcess => currentProcess;

        /// <summary>
        /// 프로세스가 진행 중인지 여부
        /// </summary>
        public bool IsProcessing => currentProcess != GameProcessState.Idle;

        /// <summary>
        /// 프로세스 시작
        /// </summary>
        public bool StartProcess(GameProcessState process)
        {
            if (IsProcessing)
            {
                Debug.LogWarning($"[InGameManager] 이미 {currentProcess} 프로세스가 진행 중입니다. {process} 시작 실패.");
                return false;
            }

            currentProcess = process;
            return true;
        }

        /// <summary>
        /// 프로세스 종료
        /// </summary>
        public void EndProcess()
        {
            currentProcess = GameProcessState.Idle;
        }

        /// <summary>
        /// 특정 프로세스가 진행 중인지 확인
        /// </summary>
        public bool IsProcessActive(GameProcessState process)
        {
            return currentProcess == process;
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            //StartCoroutine(FindHealthManagerLater());
        }

        private IEnumerator FindHealthManagerLater()
        {
            yield return new WaitForEndOfFrame(); // 모든 Start() 실행 후

            var healthManager = FindAnyObjectByType<HealthManager>();
            if (healthManager != null)
            {
                Debug.Log($"HealthManager 발견: {healthManager.gameObject.name}");
                Debug.Log($"경로: {GetGameObjectPath(healthManager.gameObject)}");
            }
            else
            {
                Debug.Log("HealthManager가 정말로 씬에 없습니다.");
            }
        }

        string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }
        #endregion

        #region UI Event Handlers
        /// <summary>
        /// 게임을 시작하는 버튼의 클릭 이벤트
        /// </summary>
        public void OnClickStart()
        {
            isStart = true;
        }

        /// <summary>
        /// 턴 종료 버튼 클릭 이벤트
        /// PunTurnManager 대신 TurnManager 사용
        /// </summary>
        public void OnClickEnd()
        {
            if (TurnManager.Instance == null)
            {
                Debug.LogError("[InGameManager] TurnManager를 찾을 수 없습니다!");
                return;
            }

            // TurnManager를 통한 턴 종료 처리
            TurnManager.Instance.EndTurn();
        }
        #endregion

        #region Card Management
        /// <summary>
        /// 필드에 들어간 카드를 등록합니다.
        /// </summary>
        public void RegisterFieldCard(Card card)
        {
            if (!fieldCards.Contains(card))
                fieldCards.Add(card);
        }

        /// <summary>
        /// 필드에서 제거된 카드를 해제합니다.
        /// </summary>
        public void UnregisterFieldCard(Card card)
        {
            if (fieldCards.Contains(card))
                fieldCards.Remove(card);
        }

        /// <summary>
        /// 현재 필드에 존재하는 모든 카드를 반환합니다.
        /// 복사본 반환
        /// </summary>
        public List<Card> GetAllFieldCards()
        {
            return new List<Card>(fieldCards);
        }

        /// <summary>
        /// 모든 필드 카드 제거
        /// </summary>
        private void ClearAllFieldCards()
        {
            var allFieldCards = GetAllFieldCards();

            foreach (var card in allFieldCards.ToArray()) // ToArray()로 복사본 생성
            {
                if (card != null && card.gameObject != null)
                {
                    var zone = card.GetComponentInParent<CardZone>();
                    if (zone != null)
                    {
                        zone.RemoveCard(card.transform);
                    }

                    Destroy(card.gameObject);
                }
            }

            // 필드 카드 리스트 정리
            fieldCards.Clear();
        }
        #endregion

        #region Game Flow Management
        /// <summary>
        /// 게임 시작 시 덱 초기화 및 초기 핸드 드로우
        /// </summary>
        public void StartGame()
        {
            // 덱 초기화
            DeckManager.Instance.InitializeDecks();

            // 초기 핸드 드로우 (각자 5장)
            DrawCardsToHand(5, CardZone.OwnerType.Player);
            DrawCardsToHand(5, CardZone.OwnerType.Opponent);

            Debug.Log("[InGameManager] 게임 시작 - 덱 초기화 및 초기 핸드 드로우 완료");
        }

        /// <summary>
        /// 턴 시작 시 카드 1장 드로우
        /// </summary>
        public void StartTurn(CardZone.OwnerType currentPlayer)
        {
            DrawCardsToHand(1, currentPlayer);
        }

        /// <summary>
        /// 게임 재시작 (방장만 호출)
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[InGameManager] 게임 재시작 시작");

            // 방장 체크
            if (!Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[InGameManager] 방장만 게임을 재시작할 수 있습니다.");
                return;
            }

            // TurnManager를 통한 게임 재시작 (RPC로 모든 클라이언트 초기화)
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RestartGame();
            }
        }

        /// <summary>
        /// 로컬 게임 초기화 (모든 클라이언트에서 실행)
        /// </summary>
        public void RestartGameLocal()
        {
            Debug.Log("[InGameManager] 로컬 게임 초기화 시작");

            // 1. 게임 상태 플래그 리셋
            IsGameEnded = false;
            GameWinner = null;
            currentProcess = GameProcessState.Idle;

            // 2. 모든 진행 중인 프로세스 강제 종료
            ForceEndAllProcesses();

            // 3. InGameUIManager 초기화 (WIN/LOSE 텍스트 숨김)
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.HideGameResultTexts();
            }

            // 4. HealthManager 초기화
            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.InitializeHealth();
            }

            // 5. HealthUI 초기화 (DOTween 애니메이션 정리)
            var healthUI = FindAnyObjectByType<HealthUI>();
            if (healthUI != null)
            {
                healthUI.ResetUI();
            }

            // 6. ExpressionZone 초기화
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.ResetAllSlots();
            }

            // 7. NetworkGameManager 카드 레지스트리 초기화
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.ClearAllRegisteredCards();
            }

            // 8. 모든 Zone의 카드 완전 제거
            ClearAllZones();

            // 9. 필드 카드 리스트 초기화
            fieldCards.Clear();

            // 10. DeckManager 초기화
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.ResetDecks();
                DeckManager.Instance.InitializeDecks();
            }

            Debug.Log("[InGameManager] 로컬 게임 초기화 완료");
        }

        /// <summary>
        /// 모든 Zone의 카드 완전 제거
        /// </summary>
        private void ClearAllZones()
        {
            if (CardZone.AllZonesRoot == null)
            {
                Debug.LogWarning("[InGameManager] AllZonesRoot가 null입니다.");
                return;
            }

            var allZones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            foreach (var zone in allZones)
            {
                // Zone 내 모든 카드 제거
                var childCount = zone.transform.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    var child = zone.transform.GetChild(i);
                    var card = child.GetComponent<Card>();

                    if (card != null)
                    {
                        zone.RemoveCard(child);
                        Destroy(child.gameObject);
                    }
                }
            }

            Debug.Log("[InGameManager] 모든 Zone 초기화 완료");
        }
        #endregion

        #region Card Draw System
        /// <summary>
        /// 지정된 플레이어의 손패로 카드 드로우
        /// NetworkGameManager를 통한 네트워크 동기화 포함
        /// NetworkCard 기반의 고유 ID 시스템 사용
        /// </summary>
        /// <param name="count">드로우할 카드 수</param>
        /// <param name="owner">카드 소유자 (Player 또는 Opponent)</param>
        public void DrawCardsToHand(int count, CardZone.OwnerType owner)
        {
            // 손패 Zone 찾기 및 검증
            CardZone handZone = FindHandZone(owner);
            if (handZone == null)
            {
                Debug.LogError($"[InGameManager] {owner} 손패 Zone을 찾을 수 없습니다.");
                return;
            }

            int drawnCount = 0;
            int destroyedCount = 0;

            // 요청된 수만큼 카드 드로우 시도
            for (int i = 0; i < count; i++)
            {
                // 현재 손패 개수 확인 (10장 제한)
                int currentHandCount = GetHandCardCount(handZone);

                // 덱에서 카드 데이터 드로우 (로컬 실행)
                var cardData = owner == CardZone.OwnerType.Player
                    ? DeckManager.Instance.DrawPlayerCard()
                    : DeckManager.Instance.DrawOpponentCard();

                // 덱이 비어있는지 확인
                if (!cardData.HasValue)
                {
                    Debug.LogWarning($"[InGameManager] {owner} 덱이 비어있어 더 이상 드로우할 수 없습니다.");
                    break;
                }

                // 손패 제한 체크 (최대 10장)
                if (currentHandCount >= 10)
                {
                    // 카드 데이터는 이미 덱에서 제거되었으므로 파괴 처리
                    destroyedCount++;
                    continue;
                }

                // 실제 카드 오브젝트 생성 및 손패에 배치
                GameObject cardObject = DeckManager.Instance.CreateCardObject(cardData.Value, owner, handZone);
                if (cardObject != null)
                {
                    // NetworkCard 컴포넌트 추가 (네트워크 동기화를 위한 고유 ID 설정)
                    var networkCard = cardObject.GetComponent<NetworkCard>();
                    if (networkCard == null)
                    {
                        networkCard = cardObject.AddComponent<NetworkCard>();
                    }

                    // 위치 정보 업데이트 (NetworkCard 시스템)
                    networkCard.UpdateLocationInfo();

                    drawnCount++;
                }
                else
                {
                    Debug.LogError($"[InGameManager] {owner} 카드 오브젝트 생성 실패");
                }
            }

            // 네트워크 동기화 전송 (실제로 드로우된 카드가 있을 때만)
            if (drawnCount > 0)
            {
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.SyncCardDraw(owner, drawnCount);
                }
                else
                {
                    Debug.LogWarning("[InGameManager] NetworkGameManager 인스턴스를 찾을 수 없어 네트워크 동기화를 건너뜁니다.");
                }
            }

            // 결과 로그 출력
            if (drawnCount > 0)
                Debug.Log($"[InGameManager] {owner} {drawnCount}장 드로우 완료");

            if (destroyedCount > 0)
                Debug.Log($"[InGameManager] {owner} {destroyedCount}장 손패 제한으로 파괴됨");
        }

        /// <summary>
        /// 손패의 현재 카드 개수 반환
        /// </summary>
        private int GetHandCardCount(CardZone handZone)
        {
            if (handZone == null) return 0;

            // Transform의 자식 개수로 카드 수 확인
            return handZone.transform.childCount;
        }

        /// <summary>
        /// 손패 Zone 찾기
        /// </summary>
        private CardZone FindHandZone(CardZone.OwnerType owner)
        {
            if (CardZone.AllZonesRoot == null) return null;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            return zones.FirstOrDefault(z =>
                z.Owner == owner &&
                z.Zone == CardZone.ZoneType.Hand);
        }

        /// <summary>
        /// 카드 정보 문자열 생성
        /// </summary>
        private string GetCardDescription(Manager.CardData cardData)
        {
            return cardData.cardType switch
            {
                CardType.Number => $"숫자({cardData.numberValue})",
                CardType.Operator => $"연산자({cardData.operatorType})",
                CardType.Joker => "조커",
                _ => "알 수 없음"
            };
        }
        #endregion

        #region Game End Management
        /// <summary>
        /// 게임 종료 상태
        /// </summary>
        public bool IsGameEnded { get; private set; } = false;

        /// <summary>
        /// 승리한 플레이어
        /// </summary>
        public CardZone.OwnerType? GameWinner { get; private set; } = null;

        /// <summary>
        /// 게임 종료 이벤트
        /// </summary>
        public static event Action<CardZone.OwnerType> OnGameEnded;

        /// <summary>
        /// 게임 종료 처리 (HealthUI에서 호출)
        /// </summary>
        /// <param name="defeatedPlayer">패배한 플레이어</param>
        public void OnGameEnd(CardZone.OwnerType defeatedPlayer)
        {
            if (IsGameEnded)
            {
                Debug.LogWarning("[InGameManager] 게임이 이미 종료되었습니다.");
                return;
            }

            IsGameEnded = true;
            GameWinner = defeatedPlayer == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            Debug.Log($"[InGameManager] 게임 종료! 승자: {GameWinner}, 패자: {defeatedPlayer}");

            // 모든 프로세스 강제 종료
            ForceEndAllProcesses();

            // 게임 종료 이벤트 발생
            OnGameEnded?.Invoke(GameWinner.Value);

            // 게임 종료 UI 표시 (추후 구현)
            ShowGameEndUI(GameWinner.Value, defeatedPlayer);
        }

        /// <summary>
        /// 모든 진행 중인 프로세스 강제 종료
        /// </summary>
        private void ForceEndAllProcesses()
        {
            Debug.Log("[InGameManager] 모든 프로세스 강제 종료");

            // 현재 프로세스 종료
            if (IsProcessing)
            {
                EndProcess();
            }

            // 공격 상태 초기화
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

            // 조커 UI 숨기기
            if (JokerModeSelector.Instance != null)
            {
                JokerModeSelector.Instance.Hide();
            }

            // ExpressionZone 초기화
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.ResetAllSlots();
            }
        }

        #region Game End Management
        /// <summary>
        /// 게임 종료 UI 표시
        /// </summary>
        /// <param name="winner">승리한 플레이어</param>
        /// <param name="loser">패배한 플레이어</param>
        private void ShowGameEndUI(CardZone.OwnerType winner, CardZone.OwnerType loser)
        {
            // 핵심: GameWinner는 OnGameEnd에서 이미 각 클라이언트 관점으로 설정됨
            // Player가 승자면 로컬 플레이어 승리, Opponent가 승자면 로컬 플레이어 패배
            bool localPlayerWon = (winner == CardZone.OwnerType.Player);

            Debug.Log($"[InGameManager] 게임 결과: {(localPlayerWon ? "WIN" : "LOSE")} (winner={winner}, loser={loser})");

            // InGameUIManager에 결과 전달
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.ShowGameResult(localPlayerWon);
            }
        }
        #endregion

        /// <summary>
        /// 재시작 옵션 표시 (임시)
        /// </summary>
        private System.Collections.IEnumerator ShowRestartOption()
        {
            yield return new WaitForSeconds(3f);

            Debug.Log("[InGameManager] 게임을 재시작하시겠습니까? (R키를 눌러 재시작)");

            // 간단한 키 입력 대기 (임시)
            while (!Input.GetKeyDown(KeyCode.R))
            {
                yield return null;
            }

            RestartGame();
        }

        /// <summary>
        /// 현재 게임이 플레이 가능한 상태인지 확인
        /// </summary>
        public bool CanPlayGame()
        {
            return !IsGameEnded && HealthManager.Instance != null && !HealthManager.Instance.IsGameOver();
        }
        #endregion
    }
}
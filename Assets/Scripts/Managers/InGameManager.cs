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
    /// 인게임에서 필요한 기능들을 관리하는 매니저
    /// TurnManager와 협력하여 턴 기반 게임플레이를 구현
    ///
    /// 주요 기능:
    /// - 카드 드로우 시스템 (최대 10장 제한)
    /// - 필드 카드 등록/관리
    /// - 게임 종료 처리
    /// - 게임 재시작 처리
    /// - 프로세스 상태 관리 (동시 액션 방지)
    /// </summary>
    public class InGameManager : Singleton<InGameManager>
    {
        #region Fields and Properties
        /// <summary>
        /// 현재 필드에 배치된 모든 카드 리스트
        /// RegisterFieldCard/UnregisterFieldCard로 관리
        /// </summary>
        private readonly List<Card> fieldCards = new List<Card>();

        /// <summary>
        /// 현재 진행 중인 프로세스
        /// 동시 액션 방지를 위한 상태 플래그
        /// </summary>
        private GameProcessState currentProcess = GameProcessState.Idle;

        /// <summary>
        /// 현재 프로세스 상태 (읽기 전용)
        /// </summary>
        public GameProcessState CurrentProcess => currentProcess;

        /// <summary>
        /// 프로세스가 진행 중인지 여부
        /// Idle이 아니면 true 반환
        /// </summary>
        public bool IsProcessing => currentProcess != GameProcessState.Idle;

        /// <summary>
        /// 게임 종료 여부
        /// </summary>
        public bool IsGameEnded { get; private set; } = false;

        /// <summary>
        /// 승리한 플레이어
        /// Player 또는 Opponent
        /// </summary>
        public CardZone.OwnerType? GameWinner { get; private set; } = null;

        /// <summary>
        /// 게임 종료 이벤트
        /// TurnManager가 구독하여 게임 상태 플래그 해제
        /// </summary>
        public static event Action<CardZone.OwnerType> OnGameEnded;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 필요시 초기화 로직 추가
        }
        #endregion

        #region Process Management
        /// <summary>
        /// 프로세스 시작
        /// 이미 다른 프로세스가 진행 중이면 false 반환
        ///
        /// 사용 예:
        /// if (!InGameManager.Instance.StartProcess(GameProcessState.CardAttackProcess))
        ///     return; // 다른 프로세스 진행 중
        /// </summary>
        public bool StartProcess(GameProcessState process)
        {
            if (IsProcessing)
            {
                return false;
            }

            currentProcess = process;
            return true;
        }

        /// <summary>
        /// 프로세스 종료
        /// 상태를 Idle로 복귀하고 대기 중인 턴 종료 요청 확인
        /// </summary>
        public void EndProcess()
        {
            currentProcess = GameProcessState.Idle;

            // 프로세스 종료 후 대기 중인 턴 종료 요청이 있는지 확인
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.CheckPendingEndTurn();
            }
        }

        /// <summary>
        /// 특정 프로세스가 진행 중인지 확인
        /// </summary>
        public bool IsProcessActive(GameProcessState process)
        {
            return currentProcess == process;
        }
        #endregion

        #region UI Event Handlers
        /// <summary>
        /// 턴 종료 버튼 클릭 이벤트
        /// TurnManager를 통해 턴 종료 처리
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
        /// 필드에 새 카드를 등록합니다.
        /// 턴 관리 시스템에서 필드 카드 상태 업데이트에 사용
        /// </summary>
        public void RegisterFieldCard(Card card)
        {
            if (!fieldCards.Contains(card))
                fieldCards.Add(card);
        }

        /// <summary>
        /// 필드에서 제거된 카드를 등록 해제합니다.
        /// 카드 파괴 시 호출
        /// </summary>
        public void UnregisterFieldCard(Card card)
        {
            if (fieldCards.Contains(card))
                fieldCards.Remove(card);
        }

        /// <summary>
        /// 현재 필드에 존재하는 모든 카드를 반환합니다.
        /// 복사본 반환하여 원본 리스트 보호
        /// </summary>
        public List<Card> GetAllFieldCards()
        {
            return new List<Card>(fieldCards);
        }

        /// <summary>
        /// 모든 필드 카드 제거
        /// 게임 재시작 시 호출
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

            // 필드 카드 리스트 초기화
            fieldCards.Clear();
        }
        #endregion

        #region Game Flow Management
        /// <summary>
        /// 게임 시작 시 덱 초기화 및 초기 카드 드로우
        /// (현재는 TurnManager에서 처리)
        /// </summary>
        public void StartGame()
        {
            // 덱 초기화
            DeckManager.Instance.InitializeDecks();

            // 초기 카드 드로우 (각각 5장)
            DrawCardsToHand(5, CardZone.OwnerType.Player);
            DrawCardsToHand(5, CardZone.OwnerType.Opponent);
        }

        /// <summary>
        /// 턴 시작 시 카드 1장 드로우
        /// TurnManager에서 호출
        /// </summary>
        public void StartTurn(CardZone.OwnerType currentPlayer)
        {
            DrawCardsToHand(1, currentPlayer);
        }

        /// <summary>
        /// 게임 재시작 (마스터 클라이언트만 호출)
        /// TurnManager를 통해 RPC로 모든 클라이언트 동기화
        /// </summary>
        public void RestartGame()
        {
            if (!Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[InGameManager] 마스터 클라이언트만 재시작할 수 있습니다.");
                return;
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RestartGame();
            }
        }

        /// <summary>
        /// 로컬 게임 상태 초기화 (모든 클라이언트에서 실행)
        /// TurnManager의 RPC_RestartGameForAll에서 호출
        ///
        /// 초기화 순서:
        /// 1. 게임 종료 플래그 해제
        /// 2. 모든 진행 중인 프로세스 강제 종료
        /// 3. UI 초기화 (WIN/LOSE 텍스트 숨김)
        /// 4. HealthManager 초기화
        /// 5. HealthUI 초기화
        /// 6. ExpressionZone 초기화
        /// 7. NetworkGameManager 카드 레지스트리 초기화
        /// 8. 모든 Zone의 카드 오브젝트 삭제
        /// 9. 필드 카드 리스트 초기화
        /// 10. DeckManager 초기화
        /// </summary>
        public void RestartGameLocal()
        {
            // 1. 게임 종료 플래그 해제
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

            // 5. HealthUI 초기화 (DOTween 애니메이션 중단)
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

            // 8. 모든 Zone의 카드 오브젝트 삭제
            ClearAllZones();

            // 9. 필드 카드 리스트 초기화
            fieldCards.Clear();

            // 10. DeckManager 초기화
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.ResetDecks();
                DeckManager.Instance.InitializeDecks();
            }
        }

        /// <summary>
        /// 모든 Zone의 카드 오브젝트 삭제
        /// 게임 재시작 시 호출하여 씬을 깨끗하게 정리
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
        }
        #endregion

        #region Card Draw System
        /// <summary>
        /// 지정된 플레이어의 손으로 카드 드로우
        /// NetworkGameManager를 통해 네트워크 동기화 수행
        /// NetworkCard 컴포넌트가 고유 ID 시스템 제공
        ///
        /// 카드 드로우 규칙:
        /// - 손에 최대 10장까지만 보유 가능
        /// - 10장 초과 시 드로우한 카드는 즉시 폐기
        /// - 실제로 드로우된 카드 수만큼 NetworkGameManager를 통해 상대에게 동기화
        ///
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
                // 현재 손패 카드 수 확인 (10장 제한)
                int currentHandCount = GetHandCardCount(handZone);

                // 덱에서 카드 데이터 드로우 (덱이 비었으면 null 반환)
                var cardData = owner == CardZone.OwnerType.Player
                    ? DeckManager.Instance.DrawPlayerCard()
                    : DeckManager.Instance.DrawOpponentCard();

                // 카드 유효성 확인
                if (!cardData.HasValue)
                {
                    break;
                }

                // 손패 제한 체크 (최대 10장)
                if (currentHandCount >= 10)
                {
                    // 카드 데이터는 이미 덱에서 제거되었으므로 파기 처리
                    destroyedCount++;
                    continue;
                }

                // 실제 카드 오브젝트 생성 및 손패에 배치
                GameObject cardObject = DeckManager.Instance.CreateCardObject(cardData.Value, owner, handZone);
                if (cardObject != null)
                {
                    // NetworkCard 컴포넌트 추가 (네트워크 동기화를 위한 고유 ID 부여)
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

            // 네트워크 동기화 수행 (실제로 드로우된 카드 수가 있을 때만)
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
        }

        /// <summary>
        /// 지정된 손패 카드 개수 반환
        /// Transform의 자식 수로 카드 수 확인
        /// </summary>
        private int GetHandCardCount(CardZone handZone)
        {
            if (handZone == null) return 0;

            return handZone.transform.childCount;
        }

        /// <summary>
        /// 손패 Zone 찾기
        /// AllZonesRoot에서 Owner와 ZoneType이 일치하는 Zone 반환
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
        /// 카드 타입 문자열 변환 (디버그용)
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
        /// 게임 종료 처리 (HealthUI에서 호출)
        ///
        /// 처리 내용:
        /// 1. 게임 종료 플래그 설정
        /// 2. 승자 결정
        /// 3. 모든 프로세스 강제 종료
        /// 4. 게임 종료 이벤트 발생 (TurnManager에 알림)
        /// 5. 게임 결과 UI 표시
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

            // 모든 프로세스 강제 종료
            ForceEndAllProcesses();

            // 게임 종료 이벤트 발생
            OnGameEnded?.Invoke(GameWinner.Value);

            // 게임 결과 UI 표시 (지연 없이)
            ShowGameEndUI(GameWinner.Value, defeatedPlayer);
        }

        /// <summary>
        /// 모든 진행 중인 프로세스 강제 종료
        ///
        /// 종료 대상:
        /// - 현재 프로세스
        /// - 공격 상태
        /// - 연산자 모드
        /// - 조커 UI
        /// - ExpressionZone
        /// </summary>
        private void ForceEndAllProcesses()
        {
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

            // 조커 UI 닫기
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

        /// <summary>
        /// 게임 결과 UI 표시
        ///
        /// 중요: GameWinner는 각 클라이언트 관점에서 다름
        /// - Player가 승자면 로컬 플레이어 승리
        /// - Opponent가 승자면 로컬 플레이어 패배
        /// </summary>
        /// <param name="winner">승리한 플레이어</param>
        /// <param name="loser">패배한 플레이어</param>
        private void ShowGameEndUI(CardZone.OwnerType winner, CardZone.OwnerType loser)
        {
            // 로컬 플레이어 승패 판정
            bool localPlayerWon = (winner == CardZone.OwnerType.Player);

            // InGameUIManager에 결과 전달
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.ShowGameResult(localPlayerWon);
            }
        }

        /// <summary>
        /// 게임 플레이 가능 여부 확인
        /// 게임이 종료되지 않았고 HP가 남아있는지 체크
        /// </summary>
        public bool CanPlayGame()
        {
            return !IsGameEnded && HealthManager.Instance != null && !HealthManager.Instance.IsGameOver();
        }
        #endregion
    }
}

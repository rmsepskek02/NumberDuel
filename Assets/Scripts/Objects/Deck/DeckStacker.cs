using System.Collections.Generic;
using UnityEngine;
using Manager;
using DG.Tweening;
using System.Collections;

namespace Objects
{
    /// <summary>
    /// 카드 덱 외형 생성 및 관리용 클래스
    /// DeckManager와 연동하여 시각적 덱 표현과 실제 카드 제거를 처리
    /// ResourcesManager 안전장치 추가
    /// </summary>
    public class DeckStacker : MonoBehaviour
    {
        #region Inspector Fields
        [Header("덱 설정")]
        [SerializeField] private int cardCount = 30; // 생성할 카드 수
        [SerializeField] private float yOffset = 0.02f; // 카드 간 Y축 간격

        [Header("덱 기준 위치")]
        [SerializeField] private Transform deckRoot;

        [Header("내 덱인지 여부")]
        [SerializeField] private bool isMyDeck = true;

        [Header("애니메이션 설정")]
        [SerializeField] private float removeAnimationDuration = 0.3f;
        [SerializeField] private Vector3 removeDirection = Vector3.right;
        [SerializeField] private float removeDistance = 2f;
        #endregion

        #region Private Fields
        private readonly List<GameObject> stackedCards = new List<GameObject>();
        private int currentCardCount;

        /// <summary>덱 생성 완료 여부</summary>
        private bool isDeckCreated = false;
        #endregion

        #region Properties
        /// <summary>
        /// 내 덱인지 여부 (DeckManager에서 참조)
        /// </summary>
        public bool IsMyDeck => isMyDeck;

        /// <summary>
        /// 현재 덱에 남은 카드 수
        /// </summary>
        public int CurrentCardCount => currentCardCount;

        /// <summary>
        /// 덱이 비어있는지 확인
        /// </summary>
        public bool IsEmpty => currentCardCount <= 0;

        /// <summary>
        /// 덱 생성 완료 여부
        /// </summary>
        public bool IsDeckCreated => isDeckCreated;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 안전한 덱 생성 시작
            StartCoroutine(SafeCreateDeckVisual());
        }

        private void OnDestroy()
        {
            // DOTween 정리
            transform.DOKill();
        }
        #endregion

        #region Safe Initialization
        /// <summary>
        /// 안전한 덱 시각화 생성
        /// ResourcesManager 준비 완료 및 색상 동기화까지 대기 후 덱 생성
        /// </summary>
        private IEnumerator SafeCreateDeckVisual()
        {
            Debug.Log($"[DeckStacker] 덱 생성 준비 중... (IsMyDeck: {isMyDeck})");

            // ResourcesManager 기본 초기화 완료 대기
            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (ResourcesManager.Instance != null && ResourcesManager.Instance.IsBasicInitialized)
                {
                    Debug.Log("[DeckStacker] ResourcesManager 준비 완료");
                    break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout)
            {
                Debug.LogError("[DeckStacker] ResourcesManager 대기 시간 초과, 덱 생성 실패");
                yield break;
            }

            // 비방장인 경우 색상 동기화 완료까지 추가 대기
            if (!Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[DeckStacker] 비방장 - 색상 동기화 대기 중...");
                elapsed = 0f;
                while (elapsed < 5f) // 5초 추가 대기
                {
                    if (ResourcesManager.Instance != null && ResourcesManager.Instance.IsColorSynchronized)
                    {
                        Debug.Log("[DeckStacker] 색상 동기화 완료, 덱 생성 시작");
                        break;
                    }
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }

                if (elapsed >= 5f)
                {
                    Debug.Log("[DeckStacker] 색상 동기화 대기 시간 초과, 기본 색상으로 진행");
                }
            }
            else
            {
                Debug.Log("[DeckStacker] 방장 - 즉시 덱 생성 진행");
            }

            // 안전한 덱 생성 실행
            try
            {
                CreateDeckVisual();
                RegisterToDeckManager();
                isDeckCreated = true;
                Debug.Log($"[DeckStacker] 덱 생성 완료 (IsMyDeck: {isMyDeck}, 카드 수: {currentCardCount})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DeckStacker] 덱 생성 중 오류: {ex.Message}");
            }
        }
        #endregion

        #region Deck Visual Management
        /// <summary>
        /// 카드 프리팹을 생성하고 시각적으로 덱처럼 쌓기 (안전장치 추가)
        /// </summary>
        private void CreateDeckVisual()
        {
            if (deckRoot == null)
            {
                Debug.LogWarning("[DeckStacker] DeckRoot가 지정되지 않았습니다.");
                return;
            }

            // ResourcesManager 안전성 재확인
            if (ResourcesManager.Instance == null)
            {
                Debug.LogError("[DeckStacker] ResourcesManager 인스턴스가 없습니다.");
                return;
            }

            // 카드 템플릿 가져오기 (null 체크 포함)
            GameObject template = null;
            try
            {
                template = isMyDeck
                    ? ResourcesManager.Instance.GetPlayerCardTemplate()
                    : ResourcesManager.Instance.GetOpponentCardTemplate();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DeckStacker] 카드 템플릿 가져오기 중 오류: {ex.Message}");
                return;
            }

            if (template == null)
            {
                Debug.LogError($"[DeckStacker] 카드 템플릿이 null입니다. (IsMyDeck: {isMyDeck})");
                return;
            }

            // 기존 카드들 정리
            ClearExistingCards();

            // 카드 생성
            int successCount = 0;
            for (int i = 0; i < cardCount; i++)
            {
                try
                {
                    GameObject card = CreateStackedCard(template, i);
                    if (card != null)
                    {
                        stackedCards.Add(card);
                        successCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[DeckStacker] 카드 {i} 생성 중 오류: {ex.Message}");
                }
            }

            currentCardCount = successCount;
            Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱 생성 완료: {successCount}/{cardCount}장");
        }

        /// <summary>
        /// 개별 쌓인 카드 생성 (안전장치 추가)
        /// </summary>
        private GameObject CreateStackedCard(GameObject template, int index)
        {
            if (template == null)
            {
                Debug.LogError($"[DeckStacker] CreateStackedCard: template이 null입니다. (index: {index})");
                return null;
            }

            if (deckRoot == null)
            {
                Debug.LogError($"[DeckStacker] CreateStackedCard: deckRoot가 null입니다. (index: {index})");
                return null;
            }

            try
            {
                GameObject card = Instantiate(template, deckRoot);
                if (card == null)
                {
                    Debug.LogError($"[DeckStacker] 카드 인스턴스화 실패 (index: {index})");
                    return null;
                }

                card.SetActive(true);
                card.name = $"DeckCard_{index}_{(isMyDeck ? "My" : "Opponent")}";
                card.transform.localPosition = new Vector3(0, index * yOffset, 0);
                card.transform.localRotation = Quaternion.identity;

                // 상호작용 비활성화
                DisableCardInteraction(card);

                return card;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DeckStacker] CreateStackedCard 오류 (index: {index}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 카드의 상호작용 기능들 비활성화 (안전장치 추가)
        /// </summary>
        private void DisableCardInteraction(GameObject card)
        {
            if (card == null)
            {
                Debug.LogWarning("[DeckStacker] DisableCardInteraction: card가 null입니다.");
                return;
            }

            try
            {
                // 텍스트 비활성화
                var tmp = card.GetComponentInChildren<TMPro.TextMeshPro>();
                if (tmp != null)
                    tmp.gameObject.SetActive(false);

                // 드래그 기능 제거
                var drag = card.GetComponentInChildren<DragHandler>();
                if (drag != null)
                    Destroy(drag);

                // Glow 효과 제거
                var glow = card.GetComponentInChildren<CardEffect>();
                if (glow != null)
                    Destroy(glow);

                // 마우스 이벤트 제거 (클릭 방지)
                var mouseEvent = card.GetComponentInChildren<ObjectMouseEvent>();
                if (mouseEvent != null)
                    Destroy(mouseEvent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DeckStacker] DisableCardInteraction 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 기존 카드들 정리 (안전장치 추가)
        /// </summary>
        private void ClearExistingCards()
        {
            try
            {
                foreach (var card in stackedCards)
                {
                    if (card != null)
                        Destroy(card);
                }
                stackedCards.Clear();
                Debug.Log("[DeckStacker] 기존 카드 정리 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DeckStacker] 기존 카드 정리 중 오류: {ex.Message}");
                stackedCards.Clear(); // 에러가 나도 리스트는 정리
            }
        }
        #endregion

        #region DeckManager Integration
        /// <summary>
        /// DeckManager에 자동 등록 (안전장치 추가)
        /// </summary>
        private void RegisterToDeckManager()
        {
            try
            {
                if (DeckManager.Instance != null)
                {
                    DeckManager.Instance.RegisterDeckStacker(this, isMyDeck);
                    Debug.Log($"[DeckStacker] DeckManager 등록 완료 (IsMyDeck: {isMyDeck})");
                }
                else
                {
                    Debug.LogWarning("[DeckStacker] DeckManager 인스턴스를 찾을 수 없습니다.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DeckStacker] DeckManager 등록 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 맨 위 카드를 애니메이션과 함께 제거 (드로우 시 호출)
        /// </summary>
        public void RemoveTopCard()
        {
            if (!isDeckCreated)
            {
                Debug.LogWarning("[DeckStacker] 덱이 아직 생성되지 않았습니다.");
                return;
            }

            if (IsEmpty)
            {
                Debug.LogWarning($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱이 비어있어 카드를 제거할 수 없습니다.");
                return;
            }

            // 맨 위 카드 가져오기 (가장 나중에 추가된 카드)
            int topIndex = currentCardCount - 1;
            if (topIndex >= 0 && topIndex < stackedCards.Count)
            {
                GameObject topCard = stackedCards[topIndex];
                if (topCard != null)
                {
                    StartCoroutine(AnimateCardRemoval(topCard));
                }

                currentCardCount--;
                Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱에서 카드 제거 (남은 수: {currentCardCount})");
            }
            else
            {
                Debug.LogError($"[DeckStacker] 잘못된 카드 인덱스: {topIndex}, 스택 크기: {stackedCards.Count}");
            }
        }

        /// <summary>
        /// 카드 제거 애니메이션 (안전장치 추가)
        /// </summary>
        private IEnumerator AnimateCardRemoval(GameObject card)
        {
            if (card == null)
            {
                Debug.LogWarning("[DeckStacker] AnimateCardRemoval: card가 null입니다.");
                yield break;
            }

            // 제거 방향 계산 (내 덱과 상대 덱에 따라 다른 방향)
            Vector3 targetPosition = card.transform.position + (removeDirection.normalized * removeDistance);

            // DOTween 애니메이션
            var moveTween = card.transform.DOMove(targetPosition, removeAnimationDuration).SetEase(Ease.OutQuart);
            var scaleTween = card.transform.DOScale(Vector3.zero, removeAnimationDuration).SetEase(Ease.InQuart);

            // 애니메이션 완료 대기
            yield return moveTween.WaitForCompletion();

            // 카드 파괴
            Destroy(card);
        }

        /// <summary>
        /// 덱을 원래 상태로 리셋 (게임 재시작 시 사용)
        /// </summary>
        public void ResetDeck()
        {
            try
            {
                isDeckCreated = false;
                CreateDeckVisual();
                isDeckCreated = true;
                Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱 리셋 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DeckStacker] 덱 리셋 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 덱 상태 정보 반환
        /// </summary>
        public (int current, int max) GetDeckInfo()
        {
            return (currentCardCount, cardCount);
        }
        #endregion

        #region Debug & Utility
        /// <summary>
        /// 덱 상태 출력 (디버깅용)
        /// </summary>
        [ContextMenu("덱 상태 출력")]
        public void PrintDeckStatus()
        {
            Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱 상태: " +
                     $"생성완료={isDeckCreated}, " +
                     $"현재카드수={currentCardCount}/{cardCount}장, " +
                     $"스택크기={stackedCards.Count}");
        }

        /// <summary>
        /// 테스트용 카드 제거 (디버깅용)
        /// </summary>
        [ContextMenu("카드 1장 제거 테스트")]
        public void TestRemoveCard()
        {
            RemoveTopCard();
        }

        /// <summary>
        /// 강제 덱 재생성 (디버깅용)
        /// </summary>
        [ContextMenu("덱 강제 재생성")]
        public void ForceRecreateDebug()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(SafeCreateDeckVisual());
            }
        }
        #endregion
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manager;
using DG.Tweening;

namespace Objects
{
    /// <summary>
    /// 카드 덱 시각 표현 및 관리를 담당하는 클래스
    /// DeckManager와 연동하여 시각적 덱 표현과 카드 개수 애니메이션 처리
    /// ResourcesManager 의존성 추가
    /// </summary>
    public class DeckStacker : MonoBehaviour
    {
        #region Inspector Fields
        [Header("덱 설정")]
        [SerializeField] private int cardCount = 30; // 초기에 카드 수
        [SerializeField] private float yOffset = 0.02f; // 카드 간 Y축 간격

        [Header("덱 루트 위치")]
        [SerializeField] private Transform deckRoot;

        [Header("덱 소유자 설정")]
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
        /// 덱 소유자 여부 (DeckManager에서 사용)
        /// </summary>
        public bool IsMyDeck => isMyDeck;

        /// <summary>
        /// 현재 남아 있는 카드 수
        /// </summary>
        public int CurrentCardCount => currentCardCount;

        /// <summary>
        /// 카드가 비어있는지 확인
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
        /// 안전한 덱 시각화 초기화
        /// ResourcesManager 준비 완료 후 덱을 초기화하여 에러 방지 가능
        /// </summary>
        private IEnumerator SafeCreateDeckVisual()
        {
            // ResourcesManager 기본 초기화 완료 대기
            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (ResourcesManager.Instance != null && ResourcesManager.Instance.IsBasicInitialized)
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout)
            {
                yield break;
            }

            // 게스트인 경우 색상 동기화 완료까지 추가 대기
            if (!Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                elapsed = 0f;
                while (elapsed < 5f) // 5초 추가 대기
                {
                    if (ResourcesManager.Instance != null && ResourcesManager.Instance.IsColorSynchronized)
                    {
                        break;
                    }
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }

            // 실제로 덱 생성 시작
            try
            {
                CreateDeckVisual();
                RegisterToDeckManager();
                isDeckCreated = true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckStacker] 덱 생성 중 오류: {ex.Message}");
            }
        }
        #endregion

        #region Deck Visual Management
        /// <summary>
        /// 카드 프리팹을 생성하고 시각적으로 쌓아 올리기 (안전성 추가)
        /// </summary>
        private void CreateDeckVisual()
        {
            if (deckRoot == null)
            {
                return;
            }

            // ResourcesManager 존재 확인
            if (ResourcesManager.Instance == null)
            {
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
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckStacker] 카드 템플릿 가져오기 중 오류: {ex.Message}");
                return;
            }

            if (template == null)
            {
                return;
            }

            // 기존 카드들 삭제
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
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[DeckStacker] 카드 {i} 생성 중 오류: {ex.Message}");
                }
            }

            currentCardCount = successCount;
        }

        /// <summary>
        /// 개별 스택 카드 생성 (안전성 추가)
        /// </summary>
        private GameObject CreateStackedCard(GameObject template, int index)
        {
            if (template == null)
            {
                return null;
            }

            if (deckRoot == null)
            {
                return null;
            }

            try
            {
                GameObject card = Instantiate(template, deckRoot);
                if (card == null)
                {
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
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckStacker] CreateStackedCard 오류 (index: {index}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 카드의 상호작용 관련 컴포넌트 비활성화 (안전성 추가)
        /// </summary>
        private void DisableCardInteraction(GameObject card)
        {
            if (card == null)
            {
                return;
            }

            try
            {
                // 텍스트 비활성화
                var tmp = card.GetComponentInChildren<TMPro.TextMeshPro>();
                if (tmp != null)
                    tmp.gameObject.SetActive(false);

                // 드래그 관련 제거
                var drag = card.GetComponentInChildren<DragHandler>();
                if (drag != null)
                    Destroy(drag);

                // Glow 효과 제거
                var glow = card.GetComponentInChildren<CardEffect>();
                if (glow != null)
                    Destroy(glow);

                // 마우스 이벤트 제거 (클릭 등)
                var mouseEvent = card.GetComponentInChildren<ObjectMouseEvent>();
                if (mouseEvent != null)
                    Destroy(mouseEvent);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckStacker] DisableCardInteraction 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 기존 카드들 삭제 (안전성 추가)
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
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckStacker] 기존 카드 삭제 중 오류: {ex.Message}");
                stackedCards.Clear(); // 에러나도 리스트는 초기화
            }
        }
        #endregion

        #region DeckManager Integration
        /// <summary>
        /// DeckManager에 자동 등록 (안전성 추가)
        /// </summary>
        private void RegisterToDeckManager()
        {
            try
            {
                if (DeckManager.Instance != null)
                {
                    DeckManager.Instance.RegisterDeckStacker(this, isMyDeck);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckStacker] DeckManager 등록 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 덱 맨 위 카드를 애니메이션과 함께 제거 (외부에서 호출)
        /// </summary>
        public void RemoveTopCard()
        {
            if (!isDeckCreated)
            {
                return;
            }

            if (IsEmpty)
            {
                return;
            }

            // 덱 맨 위 카드 가져오기 (가장 최근에 추가된 카드)
            int topIndex = currentCardCount - 1;
            if (topIndex >= 0 && topIndex < stackedCards.Count)
            {
                GameObject topCard = stackedCards[topIndex];
                if (topCard != null)
                {
                    StartCoroutine(AnimateCardRemoval(topCard));
                }

                currentCardCount--;
            }
        }

        /// <summary>
        /// 카드 제거 애니메이션 (안전성 추가)
        /// </summary>
        private IEnumerator AnimateCardRemoval(GameObject card)
        {
            if (card == null)
            {
                yield break;
            }

            // 목표 위치 계산 (덱 밖으로 날아 가도록 다른 방향)
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
        /// 덱을 초기 상태로 재생성 (게임 재시작 시 사용)
        /// </summary>
        public void ResetDeck()
        {
            try
            {
                isDeckCreated = false;
                CreateDeckVisual();
                isDeckCreated = true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckStacker] 덱 재생성 중 오류: {ex.Message}");
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
        /// 덱 상태 출력 (디버그)
        /// </summary>
        [ContextMenu("덱 상태 출력")]
        public void PrintDeckStatus()
        {
            UnityEngine.Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱 상태: " +
                     $"생성완료={isDeckCreated}, " +
                     $"남은카드={currentCardCount}/{cardCount}장, " +
                     $"리스트크기={stackedCards.Count}");
        }

        /// <summary>
        /// 테스트용 카드 제거 (디버그)
        /// </summary>
        [ContextMenu("카드 1장 제거 테스트")]
        public void TestRemoveCard()
        {
            RemoveTopCard();
        }

        /// <summary>
        /// 강제 덱 재생성 (디버그)
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

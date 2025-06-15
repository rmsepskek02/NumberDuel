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
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            CreateDeckVisual();
            RegisterToDeckManager();
        }

        private void OnDestroy()
        {
            // DOTween 정리
            transform.DOKill();
        }
        #endregion

        #region Deck Visual Management
        /// <summary>
        /// 카드 프리팹을 생성하고 시각적으로 덱처럼 쌓기
        /// </summary>
        private void CreateDeckVisual()
        {
            if (deckRoot == null)
            {
                Debug.LogWarning("[DeckStacker] DeckRoot가 지정되지 않았습니다.");
                return;
            }

            GameObject template = isMyDeck
                ? ResourcesManager.Instance.GetPlayerCardTemplate()
                : ResourcesManager.Instance.GetOpponentCardTemplate();

            if (template == null)
            {
                Debug.LogError("[DeckStacker] 카드 템플릿이 설정되지 않았습니다.");
                return;
            }

            // 기존 카드들 정리
            ClearExistingCards();

            for (int i = 0; i < cardCount; i++)
            {
                GameObject card = CreateStackedCard(template, i);
                stackedCards.Add(card);
            }

            currentCardCount = cardCount;
            Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱 생성 완료: {cardCount}장");
        }

        /// <summary>
        /// 개별 쌓인 카드 생성
        /// </summary>
        private GameObject CreateStackedCard(GameObject template, int index)
        {
            GameObject card = Instantiate(template, deckRoot);
            card.SetActive(true);
            card.name = $"DeckCard_{index}";
            card.transform.localPosition = new Vector3(0, index * yOffset, 0);
            card.transform.localRotation = Quaternion.identity;

            // 상호작용 비활성화
            DisableCardInteraction(card);

            return card;
        }

        /// <summary>
        /// 카드의 상호작용 기능들 비활성화
        /// </summary>
        private void DisableCardInteraction(GameObject card)
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

        /// <summary>
        /// 기존 카드들 정리
        /// </summary>
        private void ClearExistingCards()
        {
            foreach (var card in stackedCards)
            {
                if (card != null)
                    Destroy(card);
            }
            stackedCards.Clear();
        }
        #endregion

        #region DeckManager Integration
        /// <summary>
        /// DeckManager에 자동 등록
        /// </summary>
        private void RegisterToDeckManager()
        {
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.RegisterDeckStacker(this, isMyDeck);
            }
        }

        /// <summary>
        /// 맨 위 카드를 애니메이션과 함께 제거 (드로우 시 호출)
        /// </summary>
        public void RemoveTopCard()
        {
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
        }

        /// <summary>
        /// 카드 제거 애니메이션
        /// </summary>
        private IEnumerator AnimateCardRemoval(GameObject card)
        {
            if (card == null) yield break;

            // 제거 방향 계산 (내 덱과 상대 덱에 따라 다른 방향)
            Vector3 targetPosition = card.transform.position + (removeDirection.normalized * removeDistance);

            // DOTween 애니메이션
            var moveTween = card.transform.DOMove(targetPosition, removeAnimationDuration).SetEase(Ease.OutQuart);
            var fadeTween = card.transform.DOScale(Vector3.zero, removeAnimationDuration).SetEase(Ease.InQuart);

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
            CreateDeckVisual();
            Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱 리셋 완료");
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
            Debug.Log($"[DeckStacker] {(isMyDeck ? "플레이어" : "상대")} 덱 상태: {currentCardCount}/{cardCount}장");
        }

        /// <summary>
        /// 테스트용 카드 제거 (디버깅용)
        /// </summary>
        [ContextMenu("카드 1장 제거 테스트")]
        public void TestRemoveCard()
        {
            RemoveTopCard();
        }
        #endregion
    }
}
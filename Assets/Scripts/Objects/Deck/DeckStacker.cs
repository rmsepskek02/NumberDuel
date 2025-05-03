using System.Collections.Generic;
using UnityEngine;
using Manager;

namespace Objects
{
    /// <summary>
    /// 카드 덱 외형 생성용 클래스
    /// 카드들을 일정한 Y 오프셋으로 겹쳐 쌓아 덱처럼 보이게 한다
    /// 생성된 카드에서는 DragHandler 및 Glow를 제거하고 텍스트는 비활성화한다
    /// </summary>
    public class DeckStacker : MonoBehaviour
    {
        [Header("덱 설정")]
        [SerializeField] private int cardCount = 30; // 생성할 카드 수
        [SerializeField] private float yOffset = 0.02f; // 카드 간 Y축 간격

        [Header("덱 기준 위치")]
        [SerializeField] private Transform deckRoot;

        [Header("내 덱인지 여부")]
        [SerializeField] private bool isMyDeck = true;

        private readonly List<GameObject> stackedCards = new();

        private void Start()
        {
            CreateDeckVisual();
        }

        /// <summary>
        /// 카드 프리팹을 생성하고 시각적으로 덱처럼 쌓는다
        /// - 드래그/Glow 제거, 텍스트 비활성화
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

            for (int i = 0; i < cardCount; i++)
            {
                GameObject card = Instantiate(template, deckRoot);
                card.SetActive(true); // 비활성 템플릿에서 복사되므로 반드시 활성화

                card.transform.localPosition = new Vector3(0, i * yOffset, 0);
                card.transform.localRotation = Quaternion.identity;

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

                stackedCards.Add(card);
            }
        }
    }
}

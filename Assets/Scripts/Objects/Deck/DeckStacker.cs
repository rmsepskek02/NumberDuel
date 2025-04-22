using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드 덱 외형 생성용 클래스
    /// 카드들을 일정한 Y 오프셋으로 겹쳐 쌓아 덱처럼 보이게 한다
    /// 생성된 카드에서는 DragHandler를 제거하여 드래그 불가 상태로 만든다
    /// </summary>
    public class DeckStacker : MonoBehaviour
    {
        [Header("카드 프리팹")]
        [SerializeField] private GameObject cardPrefab;

        [Header("덱 설정")]
        [SerializeField] private int cardCount = 30; // 생성할 카드 수
        [SerializeField] private float yOffset = 0.02f; // 카드 간 Y축 간격

        [Header("덱 기준 위치")]
        [SerializeField] private Transform deckRoot; // 덱이 시작되는 위치

        private List<GameObject> stackedCards = new List<GameObject>();

        /// <summary>
        /// Start 시 덱 외형을 생성
        /// </summary>
        private void Start()
        {
            CreateDeckVisual();
        }

        /// <summary>
        /// 카드 프리팹을 여러 장 생성하고 덱처럼 쌓아 올린다
        /// 생성 후 DragHandler를 제거하여 드래그가 되지 않도록 한다
        /// </summary>
        private void CreateDeckVisual()
        {
            if (cardPrefab == null || deckRoot == null) return;

            for (int i = 0; i < cardCount; i++)
            {
                // 카드 프리팹 생성
                GameObject card = Instantiate(cardPrefab, deckRoot);

                // 위치 설정: Y축으로 i * yOffset 만큼 위로 올림
                Vector3 localPos = new Vector3(0, i * yOffset, 0);
                card.transform.localPosition = localPos;

                // 회전은 덱 기준 그대로 유지
                card.transform.localRotation = Quaternion.identity;

                // 드래그 기능 제거
                DragHandler dragHandler = card.GetComponent<DragHandler>();
                if (dragHandler != null)
                {
                    Destroy(dragHandler);
                }

                // 리스트에 추가 (필요 시 관리용)
                stackedCards.Add(card);
            }
        }
    }
}

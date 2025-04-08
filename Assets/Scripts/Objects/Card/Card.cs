using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Objects
{
    /// <summary>
    /// 개별 카드 오브젝트의 클릭 처리를 담당하는 클래스
    /// - ObjectMouseEvent를 통해 클릭 감지
    /// - 클릭 시 카드 이름 출력 및 외부 이벤트 호출
    /// - 카드마다 고유한 반응을 설정할 수 있도록 인스턴스 이벤트 제공
    /// </summary>
    [RequireComponent(typeof(ObjectMouseEvent))]
    public class Card : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshPro testText; // 클릭 시 이름을 출력할 텍스트

        [Header("Events")]
        public UnityEvent<Card> onClicked; // 외부에서 구독 가능한 카드 단위 클릭 이벤트

        private void Awake()
        {
            // ObjectMouseEvent와 연결하여 클릭 이벤트 처리
            var mouseEvent = GetComponent<ObjectMouseEvent>();
            mouseEvent.OnClicked += HandleClick;
        }

        /// <summary>
        /// ObjectMouseEvent를 통해 클릭되었을 때 호출되는 내부 처리 메서드
        /// </summary>
        private void HandleClick()
        {
            // 텍스트에 카드 이름 표시
            if (testText != null)
                testText.text = gameObject.name;

            // 디버그 로그 출력
            Debug.Log($"[Card] Clicked: {gameObject.name}");

            // 외부 이벤트 호출 (개별 카드 인스턴스용)
            onClicked?.Invoke(this);
        }
    }
}

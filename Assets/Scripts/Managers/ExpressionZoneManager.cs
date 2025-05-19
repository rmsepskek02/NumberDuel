using UnityEngine;
using Objects;
using System.Linq;
using Expression;

namespace Manager
{
    /// <summary>
    /// ExpressionZone에 배치된 5개의 ExpressionCard를 제어하여 수식을 시각적으로 구성하는 매니저 클래스
    /// - 카드 배치 및 정렬
    /// - 연산자 및 숫자 표시
    /// - 결과값 계산 및 표시
    /// - 스프라이트에 따라 텍스트 색상 동기화
    /// </summary>
    public class ExpressionZoneManager : MonoBehaviour
    {
        [Header("카드 정렬 대상 Zone")]
        [SerializeField] private CardZone expressionZone;

        // ExpressionCard: 연산식에 사용되는 5개의 카드 (순서 중요: 0~4)
        private ExpressionCard[] expressionCards;

        private void Start()
        {
            // ExpressionZone이 할당되지 않은 경우, 부모에서 자동 검색
            if (expressionZone == null)
            {
                expressionZone = GetComponentInParent<CardZone>();
            }

            // 자식 오브젝트 중 ExpressionCard가 붙은 오브젝트를 이름순으로 정렬하여 수집
            expressionCards = GetComponentsInChildren<ExpressionCard>(includeInactive: true)
                .OrderBy(card => card.name)
                .ToArray();

            // 수집된 카드를 expressionZone에 등록하여 배치 정렬 수행
            if (expressionZone != null)
            {
                foreach (var card in expressionCards)
                {
                    if (card != null)
                    {
                        expressionZone.AddCard(card.transform);
                    }
                }

                expressionZone.UpdateLayout(); // 카드 정렬 실행
            }
            else
            {
                Debug.LogWarning("[ExpressionZoneManager] ExpressionZone이 연결되지 않았습니다.");
            }

            // 고정된 기호 카드 설정
            SetOperator("-");
            SetEqualSymbol();
        }

        /// <summary>
        /// 첫 번째 카드(0번 슬롯)에 플레이어 카드의 값을 표시한다.
        /// Sprite와 텍스트 색상도 함께 설정된다.
        /// </summary>
        public void SetMyCard(Card card)
        {
            var cardText = card.GetComponentInChildren<CardText>();
            if (cardText == null) return;

            var sprite = ResourcesManager.Instance.GetPlayerSprite();

            expressionCards[0].SetValue(cardText.TextValue);
            expressionCards[0].SetSprite(sprite);
            expressionCards[0].SetTextColor(Global.GetColorByName(sprite.name));
            expressionCards[0].SetTextVisible(true);
        }

        /// <summary>
        /// 수식존 첫 번째 카드 (플레이어 카드 영역)를 초기화합니다.
        /// - 중립 스프라이트 사용
        /// - 텍스트 숨김
        /// </summary>
        public void ClearMyCard()
        {
            var card = expressionCards[0];
            var neutralSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);

            if (card != null)
            {
                card.SetSprite(neutralSprite);
                card.SetValue("");
                card.SetTextVisible(false);
            }
        }

        /// <summary>
        /// 세 번째 카드(2번 슬롯)에 상대 카드의 값을 표시한다.
        /// Sprite와 텍스트 색상도 함께 설정된다.
        /// </summary>
        public void SetOpponentCard(Card card)
        {
            var cardText = card.GetComponentInChildren<CardText>();
            if (cardText == null) return;

            var sprite = ResourcesManager.Instance.GetOpponentSprite();

            expressionCards[2].SetValue(cardText.TextValue);
            expressionCards[2].SetSprite(sprite);
            expressionCards[2].SetTextColor(Global.GetColorByName(sprite.name));
            expressionCards[2].SetTextVisible(true); // 텍스트 다시 보이도록
        }

        /// <summary>
        /// 두 번째 카드(1번 슬롯)에 연산 기호(기본은 "-")를 표시한다.
        /// </summary>
        public void SetOperator(string symbol)
        {
            expressionCards[1].SetSymbol(symbol);
        }

        /// <summary>
        /// 네 번째 카드(3번 슬롯)에 "=" 기호를 고정으로 표시한다.
        /// </summary>
        public void SetEqualSymbol()
        {
            expressionCards[3].SetSymbol("=");
        }

        /// <summary>
        /// 다섯 번째 카드(4번 슬롯)에 연산 결과를 표시한다.
        /// 결과값의 부호에 따라 Sprite와 텍스트 색상을 설정한다.
        /// 결과가 0이면 중립 스프라이트 및 흰색 텍스트 사용.
        /// </summary>
        public void DisplayResult(Card myCard, Card opponentCard)
        {
            var myText = myCard.GetComponentInChildren<CardText>();
            var oppText = opponentCard.GetComponentInChildren<CardText>();

            if (myText == null || oppText == null) return;

            long myValue = myText.RawValue;
            long opponentValue = oppText.RawValue;

            long result = myValue - opponentValue;
            string display = Mathf.Abs(result).ToString();

            // 결과가 0인 경우: 중립 스프라이트 + 흰색 텍스트
            if (result == 0)
            {
                Sprite neutralSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);

                expressionCards[4].SetValue(display);
                expressionCards[4].SetSprite(neutralSprite);
                expressionCards[4].SetTextColor(Color.white);
                expressionCards[4].SetTextVisible(true); // 누락된 텍스트 표시
                return;
            }

            // 양수/음수일 경우: 플레이어 or 상대 스프라이트
            var sprite = result > 0
                ? ResourcesManager.Instance.GetPlayerSprite()
                : ResourcesManager.Instance.GetOpponentSprite();

            expressionCards[4].SetValue(display);
            expressionCards[4].SetSprite(sprite);
            expressionCards[4].SetTextColor(Global.GetColorByName(sprite.name));
            expressionCards[4].SetTextVisible(true); // 누락된 텍스트 표시
        }

        /// <summary>
        /// 전체 수식을 한 번에 구성하고 표시할 때 사용.
        /// 내부적으로 SetMyCard, SetOpponentCard, DisplayResult를 호출한다.
        /// </summary>
        public void DisplayFullExpression(Card myCard, Card opponentCard)
        {
            SetMyCard(myCard);
            SetOpponentCard(opponentCard);
            DisplayResult(myCard, opponentCard);
        }

        /// <summary>
        /// 수식존의 상대 카드(3번 슬롯)와 결과 카드(5번 슬롯)를 초기화한다.
        /// - 중립 스프라이트로 설정
        /// - 텍스트는 비우고 숨긴다
        /// </summary>
        public void ClearOpponentAndResult()
        {
            // 3번 슬롯: 상대 카드
            var opponentCard = expressionCards[2];
            var neutralSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);

            opponentCard.SetSprite(neutralSprite);
            opponentCard.SetValue("");
            opponentCard.SetTextVisible(false);

            // 5번 슬롯: 결과 카드
            var resultCard = expressionCards[4];
            resultCard.SetSprite(neutralSprite);
            resultCard.SetValue("");
            resultCard.SetTextVisible(false);
        }

    }
}

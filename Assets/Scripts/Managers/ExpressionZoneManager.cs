using UnityEngine;
using Objects;
using System.Linq;
using Expression;
using Utills;

namespace Manager
{
    /// <summary>
    /// ExpressionZone에 배치된 5개의 ExpressionCard를 제어하여 수식을 시각적으로 구성하는 매니저 클래스
    /// - 카드 배치 및 정렬
    /// - 연산자 및 숫자 표시
    /// - 결과값 계산 및 표시
    /// - 스프라이트에 따라 텍스트 색상 동기화
    /// </summary>
    public class ExpressionZoneManager : Singleton<ExpressionZoneManager>
    {
        [Header("카드 정렬 대상 Zone")]
        [SerializeField] private CardZone expressionZone;

        private ExpressionCard[] expressionCards;
        private Sprite neutralSprite;

        /// <summary>
        /// 시작 시 표현식 카드 정렬 및 초기화 수행
        /// </summary>
        private void Start()
        {
            if (expressionZone == null)
                expressionZone = GetComponentInParent<CardZone>();

            // 슬롯들을 이름 기준으로 정렬
            expressionCards = GetComponentsInChildren<ExpressionCard>(includeInactive: true)
                .OrderBy(card => card.name)
                .ToArray();

            // Zone에 정렬 등록
            foreach (var card in expressionCards)
            {
                expressionZone.AddCard(card.transform);
            }

            expressionZone.UpdateLayout();

            // 중립 스프라이트 로드
            neutralSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);

            // 수식존 초기화 (연산자 제외)
            ConfigureSlot(0, "", null, false); // 내 카드
            ConfigureSlot(2, "", null, false); // 상대 카드
            ConfigureSlot(4, "", null, false); // 결과
            SetEqualSymbol();
        }

        /// <summary>
        /// 지정한 슬롯에 텍스트/스프라이트/텍스트 활성 여부를 일괄 설정합니다.
        /// </summary>
        /// <param name="index">슬롯 인덱스 (0~4)</param>
        /// <param name="symbolText">표시할 텍스트나 기호</param>
        /// <param name="sprite">표시할 카드 Sprite (null이면 중립 사용)</param>
        /// <param name="showText">텍스트 표시 여부</param>
        public void ConfigureSlot(int index, string symbolText, Sprite sprite = null, bool showText = true)
        {
            if (index < 0 || index >= expressionCards.Length)
            {
                Debug.LogWarning($"[ExpressionZoneManager] 잘못된 슬롯 인덱스: {index}");
                return;
            }

            var slot = expressionCards[index];
            slot.SetValue(symbolText);
            slot.SetSprite(sprite ?? neutralSprite);
            slot.SetTextVisible(showText);

            // 텍스트 색상 설정 (Sprite가 있을 경우 색상 반영)
            if (sprite != null)
                slot.SetTextColor(Global.GetColorByName(sprite.name));
            else
                slot.SetTextColor(Color.white);
        }

        /// <summary>
        /// 내 카드의 값과 Sprite를 0번 슬롯에 표시합니다.
        /// </summary>
        public void SetMyCard(Card card)
        {
            var text = card.GetComponentInChildren<CardText>()?.TextValue;
            var sprite = card.GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (text == null || sprite == null) return;

            ConfigureSlot(0, text, sprite, true);
        }

        /// <summary>
        /// 상대 카드의 값과 Sprite를 2번 슬롯에 표시합니다.
        /// </summary>
        public void SetOpponentCard(Card card)
        {
            var text = card.GetComponentInChildren<CardText>()?.TextValue;
            var sprite = card.GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (text == null || sprite == null) return;

            ConfigureSlot(2, text, sprite, true);
        }

        /// <summary>
        /// 연산자 카드(OperatorType)에 해당하는 기호와 스프라이트를 1번 슬롯에 표시합니다.
        /// </summary>
        public void SetOperatorCard(Card operatorCard)
        {
            if (operatorCard == null || operatorCard.CardType != CardType.Operator)
            {
                Debug.LogWarning("[ExpressionZoneManager] 잘못된 연산자 카드가 전달됨.");
                return;
            }

            string symbol = operatorCard.OperatorType switch
            {
                OperatorType.Plus => "+",
                OperatorType.Minus => "-",
                OperatorType.Multiply => "×",
                OperatorType.Divide => "÷",
                _ => "?"
            };

            var sprite = operatorCard.GetComponentInChildren<SpriteRenderer>()?.sprite;
            ConfigureSlot(1, symbol, sprite, true);
            SetEqualSymbol();
        }

        /// <summary>
        /// 연산자 없이 수동으로 기호만 1번 슬롯에 표시합니다. (공격 프로세스용)
        /// </summary>
        public void SetOperatorSymbol(string symbol)
        {
            ConfigureSlot(1, symbol, null, true);
        }

        /// <summary>
        /// 수식 표현용 '=' 기호를 3번 슬롯에 고정 표시합니다.
        /// </summary>
        public void SetEqualSymbol()
        {
            ConfigureSlot(3, "=", null, true);
        }

        /// <summary>
        /// 연산 또는 공격 결과를 4번 슬롯에 표시합니다.
        /// - type이 null이면 공격 처리 방식 (절댓값, 색상 분기)
        /// - type이 있으면 연산자 처리 방식 (결과 그대로 출력)
        /// </summary>
        public void DisplayResult(long a, long b, OperatorType? type = null, Sprite forceSprite = null)
        {
            long result = type switch
            {
                OperatorType.Plus => a + b,
                OperatorType.Minus => a - b,
                OperatorType.Multiply => a * b,
                OperatorType.Divide => b != 0 ? a / b : 0,
                _ => a - b // type == null이면 기본 공격 연산
            };

            // 텍스트 포맷: 연산자는 부호 포함, 공격은 절댓값
            string text = type == null ? Mathf.Abs(result).ToString() : result.ToString();

            // 스프라이트 처리
            Sprite sprite = forceSprite ?? (
                result == 0 ? neutralSprite :
                result > 0 ? ResourcesManager.Instance.GetPlayerSprite() :
                             ResourcesManager.Instance.GetOpponentSprite()
            );

            ConfigureSlot(4, text, sprite, true);
        }
    }
}

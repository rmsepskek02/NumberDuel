using System;
using Objects;

namespace Manager.Network.Data
{
    /// <summary>
    /// 카드 색상 동기화 데이터
    /// 모든 클라이언트가 동일한 카드 색상을 사용하도록 보장
    /// </summary>
    [Serializable]
    public class CardColorData
    {
        /// <summary>플레이어 카드 스프라이트 이름</summary>
        public string playerSpriteName;

        /// <summary>상대방 카드 스프라이트 이름</summary>
        public string opponentSpriteName;

        /// <summary>
        /// CardColorData 생성자
        /// </summary>
        /// <param name="playerSprite">플레이어 스프라이트 이름</param>
        /// <param name="opponentSprite">상대방 스프라이트 이름</param>
        public CardColorData(string playerSprite, string opponentSprite)
        {
            playerSpriteName = playerSprite;
            opponentSpriteName = opponentSprite;
        }
    }

    /// <summary>
    /// 카드 드로우 네트워크 데이터
    /// 상대방에게 드로우 행위를 알리기 위한 구조체
    /// </summary>
    [Serializable]
    public class CardDrawData
    {
        /// <summary>카드 소유자 (0=Player, 1=Opponent)</summary>
        public int ownerType;

        /// <summary>드로우할 카드 수</summary>
        public int count;

        /// <summary>덱에서 제거 애니메이션 표시 여부</summary>
        public bool showAnimation;

        /// <summary>
        /// CardDrawData 생성자
        /// </summary>
        /// <param name="owner">카드 소유자</param>
        /// <param name="drawCount">드로우할 카드 수</param>
        /// <param name="animate">애니메이션 표시 여부</param>
        public CardDrawData(CardZone.OwnerType owner, int drawCount, bool animate = true)
        {
            ownerType = (int)owner;
            count = drawCount;
            showAnimation = animate;
        }
    }

    /// <summary>
    /// 카드 배치 네트워크 데이터
    /// 상대방 화면에 카드 배치를 동기화하기 위한 구조체
    /// </summary>
    [Serializable]
    public class CardPlacementData
    {
        /// <summary>카드 타입 (Number, Operator, Joker)</summary>
        public CardType cardType;

        /// <summary>숫자 카드의 값</summary>
        public long numberValue;

        /// <summary>연산자 카드의 타입</summary>
        public OperatorType operatorType;

        /// <summary>카드 소유자 (0=Player, 1=Opponent)</summary>
        public int ownerType;

        /// <summary>배치될 Zone 타입 (0=Hand, 1=Field)</summary>
        public int zoneType;

        /// <summary>Secret 모드 여부</summary>
        public bool isSecret;

        /// <summary>카드 고유 ID (NetworkCard 기반)</summary>
        public string uniqueId;

        /// <summary>Zone 내에서의 배치 인덱스</summary>
        public int targetIndex;

        /// <summary>
        /// CardPlacementData 생성자
        /// </summary>
        /// <param name="cardData">배치할 카드 데이터</param>
        /// <param name="owner">카드 소유자</param>
        /// <param name="zone">배치될 Zone</param>
        /// <param name="secret">Secret 모드 여부</param>
        /// <param name="id">고유 ID</param>
        /// <param name="index">배치 인덱스</param>
        public CardPlacementData(Manager.CardData cardData, CardZone.OwnerType owner,
                               CardZone.ZoneType zone, bool secret, string id, int index = -1)
        {
            cardType = cardData.cardType;
            numberValue = cardData.numberValue;
            operatorType = cardData.operatorType;
            ownerType = (int)owner;
            zoneType = (int)zone;
            isSecret = secret;
            uniqueId = id;
            targetIndex = index;
        }

        /// <summary>
        /// CardPlacementData를 Manager.CardData로 변환
        /// </summary>
        /// <returns>변환된 CardData</returns>
        public Manager.CardData ToCardData()
        {
            switch (cardType)
            {
                case CardType.Number:
                    return new Manager.CardData(numberValue);
                case CardType.Operator:
                    return new Manager.CardData(operatorType);
                case CardType.Joker:
                    return Manager.CardData.CreateJoker();
                default:
                    return new Manager.CardData(1);
            }
        }
    }

    /// <summary>
    /// 덱 상태 동기화 데이터
    /// 양쪽 덱의 남은 카드 수를 동기화하기 위한 구조체
    /// </summary>
    [Serializable]
    public class DeckSyncData
    {
        /// <summary>플레이어 덱 남은 카드 수</summary>
        public int playerDeckCount;

        /// <summary>상대방 덱 남은 카드 수</summary>
        public int opponentDeckCount;

        /// <summary>
        /// DeckSyncData 생성자
        /// </summary>
        /// <param name="playerCount">플레이어 덱 카드 수</param>
        /// <param name="opponentCount">상대방 덱 카드 수</param>
        public DeckSyncData(int playerCount, int opponentCount)
        {
            playerDeckCount = playerCount;
            opponentDeckCount = opponentCount;
        }
    }

    /// <summary>
    /// 전투 액션 데이터 구조체
    /// </summary>
    [Serializable]
    public class CombatActionData
    {
        public string attackerCardId;
        public string defenderCardId;
        public float attackerValue;
        public float defenderValue;
        public bool attackerWasSecret;
        public bool defenderWasSecret;
        public int damageToOpponent;
        public bool isEmptyFieldAttack;

        // 전투 결과로 제거될 카드들
        public bool destroyAttacker;
        public bool destroyDefender;

        // 추가: 전투 후 변경될 카드 값
        public float newAttackerValue;  // 공격자 승리 시 새로운 값
        public float newDefenderValue;  // 방어자 승리 시 새로운 값

        // 전투 아이콘 표시 상태
        public bool showAttackerSwordIcon;   // 공격자에게 칼 아이콘 표시
        public bool showDefenderShieldIcon;  // 방어자에게 방패 아이콘 표시

        public CombatActionData(string attId, string defId, float attVal, float defVal,
                               bool attSecret, bool defSecret, int damage,
                               bool destroyAtt = false, bool destroyDef = false,
                               float newAttVal = 0f, float newDefVal = 0f)
        {
            attackerCardId = attId;
            defenderCardId = defId;
            attackerValue = attVal;
            defenderValue = defVal;
            attackerWasSecret = attSecret;
            defenderWasSecret = defSecret;
            damageToOpponent = damage;
            isEmptyFieldAttack = string.IsNullOrEmpty(defId);
            destroyAttacker = destroyAtt;
            destroyDefender = destroyDef;
            newAttackerValue = newAttVal;
            newDefenderValue = newDefVal;

            // 아이콘 상태 설정: 공격자는 항상 칼, 방어자가 있을 때만 방패
            showAttackerSwordIcon = true;
            showDefenderShieldIcon = !string.IsNullOrEmpty(defId);
        }
    }

    /// <summary>
    /// 연산자 사용 결과 동기화 데이터
    /// </summary>
    [Serializable]
    public class OperationData
    {
        public int operatorType;        // OperatorType
        public string firstCardId;      // 결과가 적용될 카드
        public string secondCardId;     // 두 번째 카드 (삭제될 수 있음)
        public float firstCardValue;    // 첫 번째 카드의 원래 값
        public float secondCardValue;   // 두 번째 카드의 원래 값
        public float result;            // 연산 결과
        public float remainder;         // 나머지 (Divide만)
        public bool destroySecondCard;  // 두 번째 카드 삭제 여부

        public OperationData(OperatorType op, string first, string second,
                            float firstVal, float secondVal, float res, float rem = 0)
        {
            operatorType = (int)op;
            firstCardId = first;
            secondCardId = second;
            firstCardValue = firstVal;
            secondCardValue = secondVal;
            result = res;
            remainder = rem;
            destroySecondCard = false;
        }
    }

    /// <summary>
    /// 조커 효과 동기화 데이터
    /// </summary>
    [Serializable]
    public class JokerData
    {
        public int effectType;          // JokerEffectType
        public string[] targetCardIds;  // 대상 카드 ID 배열
        public float[] cardValues;      // 카드 값 배열 (Swap용)

        public JokerData(JokerEffectType type, string[] targets = null, float[] values = null)
        {
            effectType = (int)type;
            targetCardIds = targets ?? new string[0];
            cardValues = values ?? new float[0];
        }
    }
}

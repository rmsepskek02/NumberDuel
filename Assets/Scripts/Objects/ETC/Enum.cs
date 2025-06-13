/// <summary>
/// 게임 전체에서 사용되는 Enum을 관리하는 스크립트
/// </summary>
namespace Objects
{
    /// <summary>
    /// 카드 타입
    /// </summary>
    public enum CardType
    {
        Number,
        Operator,
        Joker
    }

    /// <summary>
    /// 연산자 타입
    /// </summary>
    public enum OperatorType
    {
        Plus,
        Minus,
        Multiply,
        Divide
    }

    /// <summary>
    /// 조커 효과 타입
    /// </summary>
    public enum JokerEffectType
    {
        Draw,
        Delete,
        Swap
    }

    /// <summary>
    /// 조커 대상 선택 모드
    /// </summary>
    public enum JokerTargetMode
    {
        None,           // 선택 모드 아님
        Delete,         // 삭제할 카드 선택
        SwapFirst,      // 교환할 첫 번째 카드 선택
        SwapSecond      // 교환할 두 번째 카드 선택
    }

    /// <summary>
    /// 카드 모드 타입 (Open/Secret)
    /// </summary>
    public enum CardModeType
    {
        Open,
        Secret
    }

    /// <summary>
    /// 게임 프로세스 상태
    /// </summary>
    public enum GameProcessState
    {
        Idle,                   // 대기 상태
        JokerDeleteProcess,     // 조커 삭제 진행 중
        JokerSwapProcess,       // 조커 교환 진행 중
        JokerDrawProcess,       // 조커 드로우 진행 중
        OperatorCalculation,    // 연산자 계산 진행 중
        CardAttackProcess,      // 카드 공격 진행 중
        CardPlacementProcess    // 카드 배치 진행 중
    }
}
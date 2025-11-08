/// <summary>
/// 게임 전체에서 사용되는 Enum을 정의하는 스크립트
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
        None,           // 대상 선택 아님
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
        Idle,                   // 유휴 상태
        JokerDeleteProcess,     // 조커 삭제 진행 중
        JokerSwapProcess,       // 조커 교환 진행 중
        JokerDrawProcess,       // 조커 드로우 진행 중
        OperatorCalculation,    // 연산자 계산 진행 중
        CardAttackProcess,      // 카드 공격 진행 중
        CardPlacementProcess    // 카드 배치 진행 중
    }

    /// <summary>
    /// 카드 아이콘 타입 (직관적인 게임 상태 표시용)
    /// </summary>
    public enum CardIconType
    {
        None,           // 아이콘 없음
        Attack,         // 공격 가능 (검)
        Defense,        // 방어/대상 (방패)
        Plus,           // 덧셈 연산자
        Minus,          // 뺄셈 연산자
        Multiply,       // 곱셈 연산자
        Divide,         // 나눗셈 연산자
        Delete,         // 삭제 효과 (금지 표시)
        Swap,           // 교환 효과 (교환 화살표)
        Draw,           // 드로우 효과
        Selection       // 시크릿 오픈 선택 표시
    }

    /// <summary>
    /// 시스템 메시지 타입
    /// </summary>
    public enum MessageType
    {
        Info,       // 일반 정보 (파란색/흰색)
        Warning,    // 경고 (노란색)
        Error,      // 오류 (빨간색)
        Success     // 성공 (초록색)
    }

    /// <summary>
    /// 씬 이름을 관리하는 enum — 하드코딩된 문자열 사용을 방지합니다.
    /// 빌드 세팅에 등록된 씬 이름과 정확히 일치하도록 값의 매핑을 유지하세요.
    /// </summary>
    public enum SceneName
    {
        SplashScene,
        JoinScene,
        LobbyScene,
        GameScene
    }

    /// <summary>
    /// SceneName enum에 대한 문자열 변환 확장 메서드
    /// 사용 예: var name = SceneName.SplashScene.GetSceneName();
    /// </summary>
    public static class SceneNameExtensions
    {
        public static string GetSceneName(this SceneName scene)
        {
            return scene switch
            {
                SceneName.SplashScene => "SplashScene",
                SceneName.JoinScene => "JoinScene",
                SceneName.LobbyScene => "LobbyScene",
                SceneName.GameScene => "GameScene",
                _ => scene.ToString()
            };
        }
    }
}

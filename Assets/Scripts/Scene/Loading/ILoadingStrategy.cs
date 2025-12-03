using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// 로딩 화면 동작 전략 인터페이스 (Strategy Pattern)
    ///
    /// LoadingScreenManager는 씬 로드 완료 시 이 전략을 통해 자동 페이드아웃 여부를 결정합니다.
    /// 씬별로 다른 로딩 동작을 구현하여 유연한 로딩 화면 제어가 가능합니다.
    ///
    /// [전략 구현체]
    /// - AutoFadeOutStrategy: 항상 자동 페이드아웃 (기본값)
    /// - ManualControlStrategy: 수동 제어 (FadeOutManually 호출 필요)
    /// </summary>
    public interface ILoadingStrategy
    {
        /// <summary>
        /// 씬 로드 완료 시 자동으로 페이드아웃할지 여부
        /// </summary>
        /// <param name="loadedScene">로드된 씬</param>
        /// <returns>true: 자동 페이드아웃, false: 수동 제어 대기</returns>
        bool ShouldAutoFadeOut(Scene loadedScene);

        /// <summary>
        /// 전략 이름 (디버깅용)
        /// </summary>
        string StrategyName { get; }
    }

    /// <summary>
    /// 자동 페이드아웃 전략
    /// 기본 동작: 씬 로드 완료 시 자동으로 페이드아웃
    /// 사용처: 일반적인 씬 전환 (SplashScene → JoinScene 등)
    /// </summary>
    public class AutoFadeOutStrategy : ILoadingStrategy
    {
        public string StrategyName => "AutoFadeOut";

        public bool ShouldAutoFadeOut(Scene loadedScene)
        {
            // 항상 자동 페이드아웃
            return true;
        }
    }

    /// <summary>
    /// 수동 제어 전략
    /// 씬 로드 완료 후에도 페이드아웃하지 않음
    /// 명시적으로 FadeOutManually() 호출 필요
    /// 사용처: Photon 연결 추적 (JoinScene), 재연결 확인 (LobbyScene)
    /// </summary>
    public class ManualControlStrategy : ILoadingStrategy
    {
        public string StrategyName => "ManualControl";

        public bool ShouldAutoFadeOut(Scene loadedScene)
        {
            // 자동 페이드아웃 안 함 (수동 제어)
            return false;
        }
    }
}

# LoadingScreenManager 리팩토링 요약

## 개요
LoadingScreenManager의 복잡한 플래그 의존성과 불명확한 책임을 개선하기 위해 **상태 머신 패턴**과 **전략 패턴**을 도입한 전면 리팩토링

**리팩토링 날짜**: 2025-12-02
**우선순위**: 안전성 최우선
**접근 방법**: 점진적 마이그레이션 (기존 API 호환성 유지)

---

## Phase 1: 상태 머신 도입

### 문제점
- `isShowing` boolean 플래그로만 상태 관리
- 여러 코루틴이 동시 실행될 경우 상태 불일치 가능
- 디버깅 시 현재 상태 파악 어려움

### 해결책
**LoadingState Enum 추가**:
```csharp
public enum LoadingState
{
    Hidden,      // 완전히 숨김 (alpha=0)
    FadingIn,    // 페이드인 진행 중
    Visible,     // 완전히 표시됨 (alpha=1, 로딩바 진행 가능)
    FadingOut    // 페이드아웃 진행 중
}
```

**상태 전환 검증 로직**:
- `CanTransitionTo()`: 유효한 전환만 허용
  - Hidden → FadingIn ✅
  - FadingIn → Visible ✅
  - Visible → FadingOut ✅
  - FadingOut → Hidden ✅
  - 기타 전환 ❌ (에러 로그)
- `TransitionTo()`: 안전한 상태 전환 + 로깅

**변경 파일**: `LoadingScreenManager.cs`

### 효과
✅ 명확한 상태 흐름
✅ 잘못된 상태 전환 시 즉시 감지
✅ 디버깅 용이 (모든 상태 전환 로그 출력)

---

## Phase 2: 전략 패턴 도입

### 문제점
- `autoFadeOutOnSceneLoad` boolean 플래그로 동작 제어
- 플래그 리셋 타이밍 실수로 버그 발생
- 씬별 다른 동작 구현 시 if-else 증가
- 코드 의도 파악 어려움

### 해결책
**ILoadingStrategy 인터페이스**:
```csharp
public interface ILoadingStrategy
{
    bool ShouldAutoFadeOut(Scene loadedScene);
    string StrategyName { get; }
}
```

**전략 구현체**:
1. **AutoFadeOutStrategy**: 씬 로드 시 자동 페이드아웃
   - 사용처: 일반적인 씬 전환 (SplashScene → JoinScene 등)

2. **ManualControlStrategy**: 수동 제어 (FadeOutManually 호출 필요)
   - 사용처: Photon 연결 추적 (JoinScene), 재연결 확인 (LobbyScene)

3. **ConditionalFadeOutStrategy**: 특정 씬에서만 수동 제어
   - 확장 가능성 확보

**변경 파일**:
- `ILoadingStrategy.cs` (신규)
- `LoadingScreenManager.cs`

**자동 전략 설정**:
- `ShowThenLoadLocal()` → AutoFadeOutStrategy
- `ShowManual()` → ManualControlStrategy
- `DisableAutoFadeOut()` → ManualControlStrategy

**자동 전략 리셋**:
- `FadeOutAndHide()` 완료 후 → AutoFadeOutStrategy
- `HideImmediate()` 실행 시 → AutoFadeOutStrategy

### 효과
✅ `autoFadeOutOnSceneLoad` 플래그 완전 제거
✅ 명확한 의도 표현 (로그에 전략 이름 표시)
✅ 확장 가능 (새 전략 추가 용이)
✅ 플래그 리셋 실수 방지

---

## Phase 3: 공개 API 단순화

### 문제점
- `DisableAutoFadeOut()` 이름이 직관적이지 않음
- 메서드 책임 및 사용 시나리오 불명확
- 긴급 복구용 메서드와 정상 흐름 메서드 구분 부족

### 해결책
**새 메서드 추가**:
- `SetManualControl()`: 수동 제어 모드 전환 (명확한 이름)

**Obsolete 처리**:
```csharp
[System.Obsolete("Use SetManualControl() instead for clarity", false)]
public void DisableAutoFadeOut()
{
    SetManualControl();
}
```

**주석 대폭 개선**:
- 모든 공개 메서드에 사용 사례, 흐름, 전략 명시
- 긴급 복구용 메서드(`ForceHide`, `CancelLoading`) 경고 표시
- 클래스 레벨 문서화 (사용법 가이드)

**변경 파일**: `LoadingScreenManager.cs`

### 효과
✅ API 의도 명확화
✅ 잘못된 사용 방지
✅ 신규 개발자 온보딩 용이

---

## Phase 4: isCancelled 플래그 개선

### 문제점
- `isCancelled` 플래그 역할 불명확
- 리셋 타이밍 문서화 부족

### 해결책
**주석 개선**:
```csharp
// 취소 플래그 (사용자 명시적 취소 시에만 사용)
// CancelLoading() 호출 시 true로 설정되며, OnSceneLoaded에서 페이드아웃 스킵용
// 일시적 플래그로, OnSceneLoaded 또는 HideImmediate에서 false로 리셋됨
private bool isCancelled = false;
```

**OnSceneLoaded 단순화**:
- 3단계로 명확히 구분: 취소 체크 → 상태 체크 → 전략 실행
- 각 단계마다 명확한 주석

**HideImmediate 개선**:
- `isCancelled` 플래그도 함께 리셋
- 모든 상태를 초기화하여 다음 사용 준비

**변경 파일**: `LoadingScreenManager.cs`

### 효과
✅ 플래그 역할 명확화
✅ 리셋 로직 일관성 확보
✅ 버그 발생 가능성 감소

---

## Phase 5: 최종 정리

### 작업 내용
1. **ILoadingStrategy.cs 문서화 개선**
   - 인터페이스 역할 상세 설명
   - 전략 구현체 목록 명시

2. **LoadingScreenManager.cs 클래스 주석 개선**
   - 상태 머신 흐름 다이어그램
   - 기본/고급 사용법 가이드
   - 공개 API 목록

3. **코드 일관성 확보**
   - 로그 메시지 형식 통일 (`[LoadingScreen]`)
   - 주석 스타일 일관성

4. **리팩토링 문서 작성**
   - 이 문서 (REFACTORING_SUMMARY.md)

---

## 최종 구조

### 파일 구조
```
Assets/Scripts/Scene/Loading/
├── LoadingScreenManager.cs        (메인 로딩 화면 매니저)
├── ILoadingStrategy.cs            (전략 인터페이스 + 구현체 3종)
└── REFACTORING_SUMMARY.md         (이 문서)
```

### 공개 API (사용자가 호출하는 메서드)
| 메서드 | 설명 | 전략 |
|--------|------|------|
| `ShowThenLoadLocal(string)` | 씬 전환 (자동 페이드아웃) | Auto |
| `ShowManual(string)` | 수동 제어 시작 | Manual |
| `UpdateProgress(float, string)` | 진행률 업데이트 | - |
| `FadeOutManually()` | 수동 페이드아웃 | - |
| `SetManualControl()` | 수동 제어 모드 전환 | Manual |
| `CancelLoading()` | 사용자 취소 | - |
| `ForceHide()` | 긴급 복구 | - |
| ~~`DisableAutoFadeOut()`~~ | [Obsolete] SetManualControl 사용 | - |

### 상태 전이 다이어그램
```
Hidden ──[Show]──> FadingIn ──[완료]──> Visible ──[FadeOut]──> FadingOut ──[완료]──> Hidden
  ↑                                                                                   |
  └─────────────────────────── [ForceHide/CancelLoading] ─────────────────────────────┘
```

---

## 호환성

### 기존 코드 영향
✅ **JoinManager.cs**: 14개 호출 지점 - 모두 호환됨
✅ **LobbyManager.cs**: 12개 호출 지점 - 모두 호환됨
✅ **기타 Manager**: 영향 없음

### 주의사항
⚠️ `DisableAutoFadeOut()` 사용 시 Obsolete 경고 발생 (동작은 정상)
→ 추후 `SetManualControl()`로 변경 권장

---

## 성과

### 코드 품질 개선
- **복잡도**: 높음 → 중간 (boolean 플래그 → 명시적 상태/전략)
- **가독성**: 낮음 → 높음 (명확한 이름, 풍부한 주석)
- **유지보수성**: 낮음 → 높음 (확장 가능한 구조)
- **디버깅**: 어려움 → 용이함 (모든 전환 로그)

### 버그 방지
✅ 잘못된 상태 전환 감지
✅ 전략 자동 리셋으로 플래그 리셋 실수 방지
✅ 명확한 API로 잘못된 사용 방지

### 확장성
✅ 새 전략 추가 용이 (ILoadingStrategy 구현)
✅ 씬별 다른 동작 구현 간편
✅ 테스트 코드 작성 용이 (전략 Mocking 가능)

---

## 테스트 가이드

### 테스트 시나리오

#### 1. 일반 씬 전환 (자동 페이드아웃)
```csharp
LoadingScreenManager.Instance.ShowThenLoadLocal("LobbyScene");
// 예상: Hidden → FadingIn → Visible → [씬 로드] → FadingOut → Hidden
```

#### 2. Photon 연결 추적 (수동 제어)
```csharp
LoadingScreenManager.Instance.ShowManual("서버 연결 중...");
// [연결 진행 중]
LoadingScreenManager.Instance.UpdateProgress(0.5f, "인증 중...");
// [연결 완료]
LoadingScreenManager.Instance.FadeOutManually();
// 예상: Hidden → FadingIn → Visible → [수동 업데이트] → FadingOut → Hidden
```

#### 3. 재연결 확인 (LobbyScene)
```csharp
// LobbyManager.Start()
LoadingScreenManager.Instance.SetManualControl();
// [재연결 체크]
if (PhotonNetwork.IsConnectedAndReady)
{
    LoadingScreenManager.Instance.FadeOutManually();
}
```

#### 4. 긴급 복구 (로딩 화면이 멈춘 경우)
```csharp
LoadingScreenManager.Instance.ForceHide();
// 예상: 어떤 상태든 즉시 Hidden으로 전환
```

### 검증 포인트
✅ 모든 씬 전환 시 로딩 화면 정상 동작
✅ Photon 연결 진행률 실시간 표시
✅ LobbyScene 재연결 시나리오 정상 처리
✅ 상태 전환 로그 정상 출력
✅ 예외 상황에서 ForceHide 정상 동작

---

## 향후 개선 사항 (선택)

### 고려 사항
1. **전략을 ScriptableObject로 분리**
   - 런타임 생성 대신 에디터에서 설정
   - 디자이너가 전략 조합 가능

2. **전략을 씬 메타데이터에 저장**
   - 각 씬에 기본 전략 미리 지정
   - LoadingScreenManager가 자동 선택

3. **테스트 코드 작성**
   - 상태 전환 단위 테스트
   - 전략 패턴 Mock 테스트

4. **취소 기능 개선**
   - `isCancelled` 플래그 → Cancelled 상태로 승격?
   - 취소 이벤트 시스템 도입?

---

## 참고 자료

### 디자인 패턴
- **State Pattern**: https://refactoring.guru/design-patterns/state
- **Strategy Pattern**: https://refactoring.guru/design-patterns/strategy

### Unity 관련
- **SceneManager.sceneLoaded**: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html
- **Coroutines**: https://docs.unity3d.com/Manual/Coroutines.html

---

## Phase 6: 코드 최적화 및 정리 (2025-12-03)

### 문제점
- ConditionalFadeOutStrategy: 구현되어 있으나 미사용
- DisableAutoFadeOut(): Obsolete 상태로 유지 중
- AnimateLoadingText(): 취약한 조건문 (`text.StartsWith("로딩")`)
- 과도한 디버그 로그로 콘솔 가독성 저하

### 해결책

#### 1. 미사용 코드 제거
**ConditionalFadeOutStrategy 제거**:
- ILoadingStrategy.cs에서 완전 제거
- AutoFadeOutStrategy와 ManualControlStrategy만으로 충분
- 코드 복잡도 감소

**DisableAutoFadeOut() 제거**:
- LobbyManager.cs에서 `SetManualControl()`로 교체
- Obsolete 메서드 완전 제거
- API 간소화

#### 2. AnimateLoadingText() 조건문 개선
**변경 전**:
```csharp
if (statusText != null && statusText.text.StartsWith("로딩"))
```

**변경 후**:
```csharp
if (statusText != null)
```

**이유**:
- 이 코루틴은 FadeInThenLoadLocalRoutine과 FadeInThenActionRoutine에서만 시작
- 둘 다 "로딩 중"으로 초기화
- `StartsWith` 조건이 없어도 안전하고 더 단순함

#### 3. 디버그 로그 정리
**제거된 로그**:
- ShowThenLoadLocal의 전략 설정 로그
- OnSceneLoaded의 상세 로그 (Scene, State, Strategy, Cancelled)
- 각 단계별 정보성 로그
- FadeOutAndHide의 전략 리셋 로그
- ShowManual, SetManualControl, ForceHide, HideImmediate의 정보성 로그
- CancelLoading의 상태 로그

**유지된 로그**:
- ✅ TransitionTo의 상태 전환 로그 (디버깅 핵심)
- ✅ 상태 전환 에러 로그 (문제 감지)
- ✅ 폰트 관련 경고 로그 (설정 오류 방지)

### 효과
✅ **코드 라인 수 감소**: ~60줄 감소
✅ **복잡도 감소**: 미사용 전략 클래스 제거
✅ **가독성 향상**: 불필요한 로그 제거로 핵심만 표시
✅ **API 단순화**: Obsolete 메서드 제거
✅ **안정성 유지**: 모든 기능 정상 동작 확인

### 변경 파일
- `ILoadingStrategy.cs`: ConditionalFadeOutStrategy 제거
- `LoadingScreenManager.cs`: DisableAutoFadeOut 제거, 로그 정리, 조건문 개선
- `LobbyManager.cs`: DisableAutoFadeOut → SetManualControl 교체

---

**작성자**: Claude (Anthropic)
**리뷰어**: 리팩토링 완료 후 팀 리뷰 필요
**버전**: 1.1 (2025-12-03 최적화 완료)

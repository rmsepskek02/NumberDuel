# JoinManager 스크립트 정리 후 Unity 에디터 작업 사항

## 개요
JoinManager.cs 스크립트를 리팩토링하고 정리한 후, Unity 에디터에서 확인하고 작업해야 할 사항들을 정리한 문서입니다.

---

## 1. Inspector 필드 확인

### 중요: Serialized Field 누락 확인
스크립트 정리 과정에서 `public` 필드들의 선언 순서와 Header가 변경되었습니다. Unity 에디터에서 JoinManager 컴포넌트를 확인하여 모든 필드가 올바르게 할당되어 있는지 확인하세요.

#### 확인 필요 필드 목록:

**Panels**
- `emailLoginPanel`
- `socialLoginButtonsPanel`

**Sections**
- `loginSection`
- `emailVerificationSection`
- `passwordSection`
- `nicknameSection`

**Input Fields - Login Section**
- `loginEmailInput`
- `loginPasswordInput`

**Input Fields - Email Verification Section**
- `verificationEmailInput`

**Input Fields - Password Section**
- `passwordEmailInput`
- `passwordInput`
- `passwordConfirmInput`

**Input Fields - Nickname Section**
- `nicknameInput`

**Buttons - Social Login**
- `guestLoginButton`
- `emailLoginModeButton`
- `googleLoginButton`
- `kakaoLoginButton`

**Buttons - Login Section**
- `emailLoginExecuteButton`
- `emailSignupButton`

**Buttons - Email Verification Section**
- `verifyEmailButton`
- `resendEmailButton`

**Buttons - Password Section**
- `passwordConfirmButton`

**Buttons - Nickname Section**
- `nicknameConfirmButton`

**Buttons - Other**
- `backToSocialButton`
- `findPasswordButton`

**UI Text**
- `validationErrorText`

---

## 2. Button Event 연결 확인

### Unity Editor에서 버튼 이벤트 재연결 필요
일부 버튼 메서드가 제거되었으므로, 다음 버튼들의 onClick 이벤트가 올바르게 연결되어 있는지 확인하세요:

#### Social Login Buttons
- **Guest Login Button** → `OnClickGuestLoginButton()`
- **Email Login Mode Button** → `OnClickEmailLoginModeButton()`
- **Google Login Button** → `OnClickGoogleLoginButton()`
- **Kakao Login Button** (현재 미구현)

#### Login Section Buttons
- **Email Login Execute Button** → `OnClickEmailLoginExecuteButton()`
- **Email Signup Button** → `OnClickEmailSignupButton()`

#### Email Verification Section Buttons
- **Verify Email Button** → `OnClickVerifyEmailButton()`
- **Resend Email Button** → `OnClickResendEmailButton()`

#### Password Section Buttons
- **Password Confirm Button** → `OnClickPasswordConfirmButton()`

#### Nickname Section Buttons
- **Nickname Confirm Button** → `OnClickNicknameConfirmButton()`

#### Other Buttons
- **Back To Social Button** → `OnClickBackToSocialButton()`
- **Find Password Button** → `OnFindPasswordButtonClicked()`

---

## 3. 씬 재저장 권장
스크립트 변경으로 인한 직렬화 데이터 문제를 방지하기 위해:

1. JoinScene을 열어서 JoinManager가 있는 GameObject를 선택
2. Inspector에서 모든 필드가 올바르게 할당되었는지 확인
3. 씬을 저장 (Ctrl+S)
4. Unity를 재시작하여 변경사항이 올바르게 적용되었는지 확인

---

## 4. 테스트 체크리스트

### 기능 테스트
스크립트 정리 후 다음 기능들이 정상 작동하는지 테스트하세요:

#### 소셜 로그인
- [ ] 게스트 로그인 팝업 표시 및 실행
- [ ] 이메일 로그인 모드 전환
- [ ] Google 로그인 (모바일 빌드에서만)

#### 이메일 로그인
- [ ] 이메일/비밀번호 입력 후 로그인
- [ ] 이메일 인증 안 된 계정 로그인 시 경고
- [ ] 소셜 로그인 전용 계정 이메일 로그인 시도 시 경고
- [ ] 중복 로그인 체크 및 강제 로그인

#### 이메일 회원가입 (3단계)
- [ ] Step 1: 이메일 입력 및 인증 메일 발송
- [ ] Step 1: 인증 메일 재발송 (60초 쿨다운)
- [ ] Step 1: 이메일 인증 확인
- [ ] Step 2: 비밀번호 설정 (최소 6자 검증)
- [ ] Step 3: 닉네임 설정 (픽셀 길이 24 제한)
- [ ] 회원가입 완료 후 로그인 화면 복귀

#### 네비게이션
- [ ] "다른 방식으로 로그인하기" 버튼
- [ ] 회원가입 중 뒤로가기 시 계정 삭제 확인

#### 입력 필드 편의성
- [ ] Tab 키로 다음 입력 필드 이동
- [ ] Enter 키로 현재 섹션 버튼 실행
- [ ] Enter 키 쿨다운 (0.3초)

#### 로딩 및 연결
- [ ] Photon 연결 진행률 표시
- [ ] Photon 연결 타임아웃 (10초)
- [ ] Firebase 초기화 대기 및 재시도
- [ ] 로그인 성공 후 로비 진입

---

## 5. 변경사항 요약

### 코드 구조 개선
- **상수 그룹화**: 모든 매직 넘버를 상수로 정의
- **변수 카테고리화**: 필드를 기능별로 region으로 분리
- **버튼 이벤트 재정리**: Navigation, Social Login, Email Login, Email Signup으로 분류
- **헬퍼 메서드 추가**:
  - `WaitForFirebaseInitialization()`: Firebase 초기화 대기 로직 통합
  - `ShowSimpleAlert()`: 간단한 알림 팝업 표시

### 제거된 항목
- **메서드 제거**: `OnConnected()` (사용하지 않는 Photon 콜백)
- **주석 제거**: "버튼 활성화 (구식 시스템 제거됨)" 중복 주석
- **이모지 제거**: ⭐, ⏱️, ✅, ⚠️ 등 모든 이모지

### 매직 넘버 → 상수 변환
- `0.3f` → `ENTER_KEY_COOLDOWN`
- `10f` → `PHOTON_CONNECT_TIMEOUT`, `TASK_TIMEOUT`
- `1.5f` → `MIN_LOADING_DURATION`
- `60` → `RESEND_COOLDOWN`
- `500ms` → `LOGIN_TRANSITION_DELAY * 1000`
- `24` → `MAX_NICKNAME_PIXEL_LENGTH`
- `6` → `MIN_PASSWORD_LENGTH`

### 코드 가독성 개선
- 불필요한 주석 제거
- 긴 메서드 주석 간소화
- 중복 코드 헬퍼 메서드로 추출

---

## 6. 주의사항

### 빌드 테스트
PC와 모바일(Android/iOS) 빌드 모두에서 테스트하세요:
- **PC 빌드**: SNS 로그인 버튼이 숨겨져야 함
- **모바일 빌드**: Google 로그인이 정상 작동해야 함

### 디버그 로그 확인
콘솔에서 다음 로그들을 확인하세요:
- `[JoinManager] AuthManager 초기화 재시도 실패`
- `[JoinManager] SessionManager 초기화 재시도 실패`
- `Photon 연결 타임아웃 (10초)`
- `프로필 존재 확인 타임아웃 (10초)`
- `프로필 로드 타임아웃 (10초)`

---

## 7. 추가 권장 사항

### 향후 개선 가능 항목
1. **async void → async Task 변환**: Unity 이벤트 핸들러가 아닌 메서드들을 Task로 변환하여 예외 처리 개선
2. **Validation 로직 분리**: 이메일/비밀번호/닉네임 검증 로직을 별도 클래스로 분리
3. **UI 상태 관리 개선**: 섹션 전환 로직을 State Machine 패턴으로 개선
4. **에러 처리 통합**: SystemMessageManager 의존성을 줄이고 에러 처리 일관성 개선

---

## 문의사항
스크립트 정리 과정에서 문제가 발생하거나 추가 질문이 있으면 개발팀에 문의하세요.

**작성일**: 2026-01-06
**작성자**: Claude Code (AI Assistant)
**버전**: 1.0

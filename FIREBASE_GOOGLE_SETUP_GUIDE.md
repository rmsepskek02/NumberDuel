# Firebase Console Google 로그인 설정 가이드

## 🔧 Firebase Console 설정

### 1. Firebase Console 접속
1. [Firebase Console](https://console.firebase.google.com/) 접속
2. **NumberDuel** 프로젝트 선택
3. 좌측 메뉴에서 **Authentication** 클릭

---

### 2. Google 로그인 방법 활성화

#### 2-1. Sign-in Method 탭
1. Authentication 페이지에서 **Sign-in method** 탭 클릭
2. **Sign-in providers** 목록에서 **Google** 찾기
3. Google 행의 **연필 아이콘** 클릭 (또는 행 클릭)

#### 2-2. Google 제공업체 활성화
1. **사용 설정** 토글을 **ON**으로 변경
2. **프로젝트 지원 이메일** 선택 (Firebase 프로젝트 소유자 이메일)
3. **저장** 버튼 클릭

✅ Google 로그인이 활성화되었습니다!

---

### 3. OAuth 동의 화면 설정 (선택사항이지만 권장)

Google 로그인 시 사용자에게 표시되는 동의 화면을 커스터마이징할 수 있습니다.

#### 3-1. Google Cloud Console 접속
1. Firebase Console의 **프로젝트 설정** → **서비스 계정** 탭
2. **Google Cloud Console에서 권한 관리** 링크 클릭
3. 또는 [Google Cloud Console](https://console.cloud.google.com/) 직접 접속

#### 3-2. OAuth 동의 화면 구성
1. 좌측 메뉴에서 **API 및 서비스** → **OAuth 동의 화면** 클릭
2. **User Type** 선택:
   - **외부(External)**: 누구나 Google 계정으로 로그인 가능 (권장)
   - **내부(Internal)**: Google Workspace 조직 내부만 (테스트용)
3. **만들기** 클릭

#### 3-3. 앱 정보 입력
**OAuth 동의 화면** 페이지에서 다음 정보 입력:

| 필드 | 입력 값 |
|------|---------|
| **앱 이름** | `NumberDuel` |
| **사용자 지원 이메일** | 개발자 이메일 주소 |
| **앱 로고** | (선택) NumberDuel 로고 이미지 업로드 (120x120px 이상) |
| **앱 도메인** | (선택) 추후 웹사이트 생기면 입력 |
| **승인된 도메인** | (선택) Firebase 호스팅 도메인 |
| **개발자 연락처 정보** | 개발자 이메일 주소 |

4. **저장 후 계속** 클릭

#### 3-4. 범위(Scopes) 설정
1. **범위 추가 또는 삭제** 클릭
2. 기본 범위 확인 (이미 선택되어 있음):
   - `userinfo.email` - 이메일 주소 보기
   - `userinfo.profile` - 개인정보 보기
3. **업데이트** → **저장 후 계속** 클릭

#### 3-5. 테스트 사용자 추가 (외부 앱인 경우)
앱이 **게시되지 않은 상태**에서는 테스트 사용자만 로그인 가능합니다.

1. **테스트 사용자** 섹션에서 **ADD USERS** 클릭
2. 테스트에 사용할 Google 계정 이메일 입력 (쉼표로 구분)
3. **추가** 클릭
4. **저장 후 계속** 클릭

#### 3-6. 요약 확인
- 설정 내용 검토 후 **대시보드로 돌아가기** 클릭

---

### 4. 게시 상태 변경 (프로덕션 배포 시)

현재는 **테스트** 상태이므로 테스트 사용자만 로그인 가능합니다.
**모든 사용자가 로그인**할 수 있도록 하려면:

1. OAuth 동의 화면 페이지에서 **앱 게시** 버튼 클릭
2. 확인 대화상자에서 **확인** 클릭
3. **게시 상태**: `프로덕션`으로 변경됨

⚠️ **주의**:
- 앱을 게시하면 모든 Google 계정 사용자가 로그인 가능
- 민감한 범위를 요청하는 경우 Google의 검토 필요 (기본 범위는 검토 불필요)

---

## 🎮 Unity Editor 최종 설정

### 1. InputFieldPopup.prefab 생성
`UI_PREFAB_SETUP_GUIDE.md` 파일을 참고하여 Prefab 생성:
1. Hierarchy에서 UI 구조 생성
2. `InputFieldPopupUI.cs` 스크립트 연결
3. 모든 필드 연결 (titleText, inputField, validationText, 버튼 등)
4. **PopupPanel**에 **CanvasGroup** 컴포넌트 추가
5. `Assets/Resources/Prefabs/UI/InputFieldPopup.prefab`으로 저장

**중요**: 반드시 `Resources/Prefabs/UI/` 폴더에 저장!

---

### 2. Google 로그인 버튼 추가

#### 2-1. JoinScene 열기
1. Project 창에서 `Assets/Scenes/JoinScene.unity` 열기
2. Hierarchy에서 **Canvas** → **LoginPanel** 찾기

#### 2-2. SocialLoginContainer 생성
LoginPanel 하위에 새로운 GameObject 생성:

```
LoginPanel
├─ EmailInputField
├─ PasswordInputField
├─ ButtonContainer (기존 버튼들)
└─ SocialLoginContainer (새로 추가)
    ├─ DividerText
    └─ GoogleLoginButton
```

**SocialLoginContainer 설정**:
- **RectTransform**:
  - Anchor: Bottom Center
  - Width: `500`, Height: `120`
  - Position: (0, 80, 0)

#### 2-3. DividerText 생성
- **TextMeshProUGUI**:
  - Text: `─── 또는 ───`
  - Font Size: `16`
  - Alignment: Center + Middle
  - Color: RGBA(0.7, 0.7, 0.7, 1) - 회색
- **RectTransform**:
  - Anchor: Top Center
  - Width: `400`, Height: `30`
  - Position: (0, -20, 0)

#### 2-4. GoogleLoginButton 생성
- **RectTransform**:
  - Anchor: Bottom Center
  - Width: `400`, Height: `50`
  - Position: (0, 10, 0)
- **Image**:
  - Source Image: Google 로고 스프라이트 (아래 참조)
  - Color: RGBA(1, 1, 1, 1) - 흰색 배경
- **Button**:
  - Interactable: ✓
  - Transition: Color Tint
  - OnClick(): `JoinManager` → `OnClickGoogleLoginButton()`

#### 2-5. GoogleLoginButton > Text
- **TextMeshProUGUI**:
  - Text: `Google로 시작하기`
  - Font: Maplestory Bold
  - Font Size: `18`
  - Alignment: Center + Middle
  - Color: RGBA(0.3, 0.3, 0.3, 1) - 어두운 회색

---

### 3. Google 로고 에셋 추가

#### 3-1. Google 브랜딩 가이드라인
- [Google Identity Branding Guidelines](https://developers.google.com/identity/branding-guidelines)
- **중요**: Google의 브랜딩 가이드를 준수해야 합니다!

#### 3-2. 권장 디자인
**옵션 1: 공식 로고 사용**
1. [Google Branding Assets](https://about.google/brand-resource-center/) 방문
2. "Google 'G' Logo" 다운로드
3. `Assets/Resources/Sprites/Social/` 폴더에 저장
4. Inspector에서 Texture Type: `Sprite (2D and UI)` 설정

**옵션 2: 버튼 이미지 직접 제작**
- 흰색 배경 + 검정 텍스트 (Google 가이드라인)
- 텍스트: "Google로 시작하기" 또는 "Sign in with Google"
- 왼쪽에 Google "G" 로고 배치

#### 3-3. JoinManager 연결
1. Hierarchy에서 **JoinManager** GameObject 선택
2. Inspector에서 **Google Login Button** 필드에 GoogleLoginButton 드래그

---

### 4. SystemMessages.asset에 메시지 추가

`GOOGLE_LOGIN_MESSAGES.md` 파일을 참고하여 메시지 추가:

1. Project 창에서 `Assets/Resources/Data/SystemMessages.asset` 선택
2. Inspector에서 **Messages** 리스트 펼치기
3. 다음 6개 메시지 추가 (`+` 버튼):

| Message Key | Message Text | Type | Duration | Color |
|-------------|--------------|------|----------|-------|
| `GoogleLoginInProgress` | `Google 로그인 중...` | Info | 3 | RGB(1, 1, 1) |
| `GoogleLoginSuccess` | `Google 로그인 성공!` | Success | 2 | RGB(0.3, 1, 0.3) |
| `GoogleLoginFailed` | `Google 로그인에 실패했습니다` | Error | 3 | RGB(1, 0.3, 0.3) |
| `AccountExistsWithDifferentProvider` | `이미 다른 방법으로 가입된 이메일입니다.\n\n해당 로그인 방법을 사용해주세요.` | Warning | 4 | RGB(1, 0.92, 0.016) |
| `ProfileCreateFailed` | `프로필 생성에 실패했습니다.\n다시 시도해주세요.` | Error | 3 | RGB(1, 0.3, 0.3) |
| `SessionCreateFailed` | `세션 생성에 실패했습니다.` | Error | 3 | RGB(1, 0.3, 0.3) |

4. **Ctrl+S**로 저장

---

## 🧪 테스트 시나리오

### 시나리오 1: 신규 Google 사용자
1. JoinScene에서 **Google로 시작하기** 버튼 클릭
2. Google 로그인 팝업 표시 → 계정 선택
3. 닉네임 입력 팝업 표시
4. 닉네임 입력 후 확인 → LobbyScene 이동
5. Firestore `users` 컬렉션에 프로필 생성 확인

**예상 결과**:
- Firestore UserProfile: `AuthProvider = "Google"`
- PhotoUrl 필드에 Google 프로필 사진 URL 저장
- SessionManager에 세션 생성됨

---

### 시나리오 2: 기존 이메일 계정이 Google 로그인 시도
1. 이미 이메일로 가입된 계정의 이메일 사용
2. **Google로 시작하기** 버튼 클릭
3. Google 로그인 팝업에서 해당 이메일 선택

**예상 결과**:
- Firebase가 `account-exists-with-different-credential` 에러 반환
- 경고 팝업 표시: "이미 다른 방법으로 가입된 이메일입니다."
- 사용자는 이메일 로그인으로 전환 필요

---

### 시나리오 3: 기존 Google 계정이 이메일 로그인 시도
1. 이미 Google로 가입된 계정의 이메일 입력
2. 임의의 비밀번호 입력 후 로그인 시도

**예상 결과**:
- AuthManager가 `INVALID_PASSWORD` 감지
- GetProvidersForEmail() 호출 → `google.com` 반환
- `SOCIAL_LOGIN_ONLY::Google` 메시지 반환
- 팝업 표시: "이 계정은 Google 로그인만 가능합니다."

---

### 시나리오 4: 사용자 취소
1. **Google로 시작하기** 버튼 클릭
2. Google 로그인 팝업에서 **취소** 또는 **X** 클릭

**예상 결과**:
- `GoogleAuthProvider`가 "CANCELED" 반환
- UI에 에러 메시지 표시 없음 (조용히 처리)

---

### 시나리오 5: 네트워크 오류
1. 인터넷 연결 끊기
2. **Google로 시작하기** 버튼 클릭

**예상 결과**:
- Firebase 네트워크 오류 발생
- 팝업 표시: "네트워크 연결을 확인해주세요."

---

### 시나리오 6: 닉네임 중복
1. 신규 Google 사용자로 로그인
2. 닉네임 입력 팝업에서 이미 사용 중인 닉네임 입력
3. 확인 버튼 클릭

**예상 결과**:
- `IsNicknameAvailable()` 검사 실패
- 빨간색 메시지 표시: "이미 사용 중인 닉네임입니다."
- 팝업이 닫히지 않고 재입력 가능

---

## 🔍 디버깅 팁

### Console 로그 확인
Google 로그인 과정에서 다음 로그가 출력됩니다:

```
[GoogleAuthProvider] Google 로그인 시작...
[GoogleAuthProvider] Google 로그인 성공: user@example.com
[JoinManager] Google 로그인 성공: user@example.com
[JoinManager] 프로필이 존재하지 않음, 닉네임 입력 요청
[InputFieldPopup] 팝업이 성공적으로 로드되었습니다.
[DatabaseManager] 소셜 로그인 프로필 생성 완료: user@example.com
[SessionManager] 세션 생성 완료: <uid>
```

### Firebase Console에서 확인
1. **Authentication** → **Users** 탭
2. Google로 로그인한 사용자가 목록에 표시되는지 확인
3. **Sign-in provider**: `google.com` 표시 확인

### Firestore에서 확인
1. **Firestore Database** → **users** 컬렉션
2. UID로 문서 검색
3. 필드 확인:
   - `AuthProvider`: `"Google"`
   - `PhotoUrl`: Google 프로필 사진 URL (빈 문자열일 수도 있음)
   - `Email`, `Nickname` 정상 저장 확인

---

## ⚠️ 주의사항

### 1. 테스트 사용자 제한
OAuth 동의 화면이 **테스트** 상태일 때:
- 최대 100명의 테스트 사용자만 추가 가능
- 테스트 사용자 외에는 로그인 불가
- 실제 배포 전 **게시** 상태로 변경 필요

### 2. Google 계정 사진 권한
- 일부 사용자는 프로필 사진 접근 거부 가능
- `PhotoUrl`이 빈 문자열일 수 있음
- UI 구현 시 기본 아바타 이미지 준비 필요

### 3. 이메일 없는 Google 계정
- 매우 드물지만 이메일이 없는 Google 계정 존재 가능
- `user.Email`이 null인 경우 처리 필요 (현재 코드는 빈 문자열로 변환)

### 4. 중복 세션 감지
- `SessionManager.CreateSession()`에서 기존 세션 확인
- 다른 기기에서 로그인 시 기존 세션 종료
- 이중 로그인 방지 로직 작동

---

## 📋 최종 체크리스트

### Firebase Console
- [ ] Authentication → Sign-in method에서 Google 활성화
- [ ] OAuth 동의 화면 구성 완료 (앱 이름, 이메일 등)
- [ ] 테스트 사용자 추가 (테스트 단계)
- [ ] 필요 시 앱 게시 (프로덕션 단계)

### Unity Editor
- [ ] `InputFieldPopup.prefab` 생성 및 Resources 폴더 저장
- [ ] `InputFieldPopupUI` 스크립트 연결 및 필드 매핑
- [ ] JoinScene에 Google 로그인 버튼 추가
- [ ] `JoinManager.googleLoginButton` 필드 연결
- [ ] `SystemMessages.asset`에 6개 메시지 추가
- [ ] Google 로고 에셋 추가 (선택)

### 코드 검증
- [ ] 모든 스크립트 컴파일 오류 없음
- [ ] `GoogleAuthProvider.cs` 존재 확인
- [ ] `SocialAuthResult.cs` 존재 확인
- [ ] `InputFieldPopupUI.cs`, `InputFieldPopup.cs` 존재 확인
- [ ] `AuthManager`, `DatabaseManager`, `JoinManager` 수정 완료

### 테스트
- [ ] 시나리오 1: 신규 Google 사용자 로그인
- [ ] 시나리오 2: 기존 이메일 계정이 Google 시도
- [ ] 시나리오 3: 기존 Google 계정이 이메일 시도
- [ ] 시나리오 4: 사용자 취소
- [ ] 시나리오 5: 네트워크 오류
- [ ] 시나리오 6: 닉네임 중복

---

## 🎉 완료!

모든 설정이 완료되었습니다!

**다음 단계**:
1. Unity Editor에서 위 체크리스트 항목 완료
2. 테스트 빌드 실행 후 모든 시나리오 테스트
3. 문제 발생 시 Console 로그 및 Firebase Console 확인
4. 프로덕션 배포 전 OAuth 동의 화면 게시

**추후 작업 (선택사항)**:
- 프로필 사진 UI 표시 기능 추가
- Kakao 로그인 구현 (유사한 패턴 적용)
- 계정 연동 기능 (이메일+Google 동시 사용)
- Google Play Games 로그인 (Android 전용)

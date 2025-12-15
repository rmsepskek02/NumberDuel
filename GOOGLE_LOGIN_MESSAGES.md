# Google 로그인 관련 시스템 메시지 추가 가이드

## Unity Editor에서 추가할 메시지 목록

`Assets/Resources/Data/SystemMessages.asset` 파일을 선택하고 Inspector에서 다음 메시지들을 추가하세요.

---

### 1. GoogleLoginInProgress
- **Message Key**: `GoogleLoginInProgress`
- **Message Text**: `Google 로그인 중...`
- **Message Type**: `Info`
- **Display Duration**: `3`
- **Text Color**: RGB(1, 1, 1) - 하얀색

---

### 2. GoogleLoginSuccess
- **Message Key**: `GoogleLoginSuccess`
- **Message Text**: `Google 로그인 성공!`
- **Message Type**: `Success`
- **Display Duration**: `2`
- **Text Color**: RGB(0.3, 1, 0.3) - 초록색

---

### 3. GoogleLoginFailed
- **Message Key**: `GoogleLoginFailed`
- **Message Text**: `Google 로그인에 실패했습니다`
- **Message Type**: `Error`
- **Display Duration**: `3`
- **Text Color**: RGB(1, 0.3, 0.3) - 빨간색

---

### 4. AccountExistsWithDifferentProvider
- **Message Key**: `AccountExistsWithDifferentProvider`
- **Message Text**: `이미 다른 방법으로 가입된 이메일입니다.\n\n해당 로그인 방법을 사용해주세요.`
- **Message Type**: `Warning`
- **Display Duration**: `4`
- **Text Color**: RGB(1, 0.92, 0.016) - 노란색

---

### 5. ProfileCreateFailed (이미 있을 수 있음 - 확인 후 없으면 추가)
- **Message Key**: `ProfileCreateFailed`
- **Message Text**: `프로필 생성에 실패했습니다.\n다시 시도해주세요.`
- **Message Type**: `Error`
- **Display Duration**: `3`
- **Text Color**: RGB(1, 0.3, 0.3) - 빨간색

---

### 6. SessionCreateFailed (이미 있을 수 있음 - 확인 후 없으면 추가)
- **Message Key**: `SessionCreateFailed`
- **Message Text**: `세션 생성에 실패했습니다.`
- **Message Type**: `Error`
- **Display Duration**: `3`
- **Text Color**: RGB(1, 0.3, 0.3) - 빨간색

---

## 추가 방법

1. Unity Editor 실행
2. Project 창에서 `Assets/Resources/Data/SystemMessages.asset` 선택
3. Inspector 창에서 **Messages** 리스트 펼치기
4. 리스트 하단의 `+` 버튼 클릭하여 새 항목 추가
5. 위의 정보대로 각 필드 입력
6. Ctrl+S로 저장

---

## 색상 값 참고

- **하얀색 (Info)**: R=1, G=1, B=1, A=1
- **초록색 (Success)**: R=0.3, G=1, B=0.3, A=1
- **빨간색 (Error)**: R=1, G=0.3, B=0.3, A=1
- **노란색 (Warning)**: R=1, G=0.92, B=0.016, A=1

---

## 확인 방법

추가 후 JoinManager.cs에서 다음과 같이 호출됩니다:

```csharp
SystemMessageManager.Instance?.ShowMessage("GoogleLoginInProgress");
SystemMessageManager.Instance?.ShowMessage("AccountExistsWithDifferentProvider");
```

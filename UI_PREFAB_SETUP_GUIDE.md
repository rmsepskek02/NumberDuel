# Google 로그인 UI Prefab 설정 가이드

## 📦 필요한 Prefab: InputFieldPopup

**위치**: `Assets/Resources/Prefabs/UI/InputFieldPopup.prefab`

---

## 🎨 Prefab 구조

```
InputFieldPopup (GameObject)
├─ Canvas (Canvas, CanvasScaler, GraphicRaycaster)
│   ├─ Background (Image) - 어두운 배경 (반투명)
│   └─ PopupPanel (Image, RectTransform)
│       ├─ TitleText (TextMeshProUGUI)
│       ├─ InputFieldContainer (GameObject)
│       │   └─ InputField (TMP_InputField)
│       │       ├─ Placeholder (TextMeshProUGUI)
│       │       └─ Text (TextMeshProUGUI)
│       ├─ ValidationText (TextMeshProUGUI)
│       └─ ButtonContainer (GameObject)
│           ├─ ConfirmButton (Button)
│           │   └─ Text (TextMeshProUGUI)
│           └─ CancelButton (Button)
│               └─ Text (TextMeshProUGUI)
└─ InputFieldPopupUI (Script Component)
```

---

## 🔧 상세 설정

### 1. InputFieldPopup (Root GameObject)
- **Transform**: Position (0, 0, 0), Rotation (0, 0, 0), Scale (1, 1, 1)
- **Component 추가**: `InputFieldPopupUI` 스크립트

---

### 2. Canvas
**컴포넌트 설정:**
- **Canvas**
  - Render Mode: `Screen Space - Overlay`
  - Sort Order: `100` (다른 UI보다 위)
- **CanvasScaler**
  - UI Scale Mode: `Scale With Screen Size`
  - Reference Resolution: `1280 x 720`
  - Match: `0.5` (Width와 Height 중간)
- **GraphicRaycaster**: 기본 설정

---

### 3. Background (Canvas의 자식)
- **RectTransform**:
  - Anchor: Stretch both (Left=0, Top=0, Right=0, Bottom=0)
  - Position: (0, 0, 0)
- **Image**:
  - Color: RGBA(0, 0, 0, 0.8) - 검정 반투명
  - Raycast Target: ✓ (체크)

---

### 4. PopupPanel (Canvas의 자식)
- **RectTransform**:
  - Anchor: Middle Center
  - Width: `500`, Height: `300`
  - Position: (0, 0, 0)
- **Image**:
  - Source Image: `UI/Skin/Background.psd` (또는 흰색 스프라이트)
  - Color: RGBA(1, 1, 1, 1) - 하얀색
  - Raycast Target: ✓
- **Shadow / Outline** (선택사항): 팝업 그림자 효과

---

### 5. TitleText (PopupPanel의 자식)
- **RectTransform**:
  - Anchor: Top Center
  - Width: `450`, Height: `60`
  - Position: (0, -40, 0)
- **TextMeshProUGUI**:
  - Text: "닉네임을 설정해주세요"
  - Font: Maplestory Bold (또는 프로젝트 폰트)
  - Font Size: `24`
  - Alignment: Center + Middle
  - Color: RGBA(0, 0, 0, 1) - 검정색

---

### 6. InputFieldContainer (PopupPanel의 자식)
- **RectTransform**:
  - Anchor: Middle Center
  - Width: `450`, Height: `60`
  - Position: (0, 0, 0)

---

### 7. InputField (InputFieldContainer의 자식)
- **RectTransform**:
  - Anchor: Stretch
  - Left=0, Top=0, Right=0, Bottom=0
- **Image**:
  - Source Image: `UI/Skin/InputFieldBackground.psd` (또는 박스 스프라이트)
  - Color: RGBA(0.9, 0.9, 0.9, 1) - 밝은 회색
- **TMP_InputField**:
  - Text Component: `Text` 자식 오브젝트
  - Placeholder: `Placeholder` 자식 오브젝트
  - Character Limit: `12` (닉네임 최대 길이)
  - Content Type: `Standard` (런타임에 변경됨)
  - Line Type: `Single Line`

#### InputField > Placeholder (TextMeshProUGUI)
- Text: "닉네임 입력"
- Font Size: `20`
- Color: RGBA(0.5, 0.5, 0.5, 0.5) - 회색 반투명
- Alignment: Left + Middle

#### InputField > Text (TextMeshProUGUI)
- Text: ""
- Font Size: `20`
- Color: RGBA(0, 0, 0, 1) - 검정색
- Alignment: Left + Middle

---

### 8. ValidationText (PopupPanel의 자식)
- **RectTransform**:
  - Anchor: Middle Center
  - Width: `450`, Height: `40`
  - Position: (0, -70, 0)
- **TextMeshProUGUI**:
  - Text: ""
  - Font Size: `16`
  - Alignment: Center + Middle
  - Color: RGBA(1, 1, 1, 1) - 기본 흰색 (런타임에 변경)
  - Wrapping: Enabled

---

### 9. ButtonContainer (PopupPanel의 자식)
- **RectTransform**:
  - Anchor: Bottom Center
  - Width: `450`, Height: `50`
  - Position: (0, 30, 0)
- **Horizontal Layout Group** (선택사항):
  - Spacing: `20`
  - Child Alignment: Middle Center

---

### 10. ConfirmButton (ButtonContainer의 자식)
- **RectTransform**:
  - Anchor: Middle Center
  - Width: `200`, Height: `50`
  - Position: (110, 0, 0)
- **Image**:
  - Source Image: `UI/Skin/Button.psd` (또는 버튼 스프라이트)
  - Color: RGBA(0.3, 1, 0.3, 1) - 초록색
- **Button**:
  - Interactable: ✓
  - Transition: Color Tint
  - Target Graphic: Image

#### ConfirmButton > Text (TextMeshProUGUI)
- Text: "확인"
- Font Size: `20`
- Alignment: Center + Middle
- Color: RGBA(1, 1, 1, 1) - 흰색

---

### 11. CancelButton (ButtonContainer의 자식)
- **RectTransform**:
  - Anchor: Middle Center
  - Width: `200`, Height: `50`
  - Position: (-110, 0, 0)
- **Image**:
  - Source Image: `UI/Skin/Button.psd`
  - Color: RGBA(0.7, 0.7, 0.7, 1) - 회색
- **Button**:
  - Interactable: ✓
  - Transition: Color Tint

#### CancelButton > Text (TextMeshProUGUI)
- Text: "취소"
- Font Size: `20`
- Alignment: Center + Middle
- Color: RGBA(1, 1, 1, 1) - 흰색

---

## 📎 InputFieldPopupUI 스크립트 연결

**Root GameObject에 `InputFieldPopupUI` 컴포넌트를 추가하고 다음과 같이 연결:**

| 필드 이름 | 연결할 오브젝트 |
|----------|----------------|
| `titleText` | PopupPanel/TitleText |
| `inputField` | PopupPanel/InputFieldContainer/InputField |
| `placeholderText` | PopupPanel/InputFieldContainer/InputField/Placeholder |
| `validationText` | PopupPanel/ValidationText |
| `confirmButton` | PopupPanel/ButtonContainer/ConfirmButton |
| `cancelButton` | PopupPanel/ButtonContainer/CancelButton |
| `canvasGroup` | PopupPanel (CanvasGroup 컴포넌트 추가 필요) |
| `popupRect` | PopupPanel (RectTransform) |

**PopupPanel에 CanvasGroup 컴포넌트 추가:**
- Inspector에서 PopupPanel 선택
- Add Component → `CanvasGroup`
- Alpha: `1`
- Interactable: ✓
- Block Raycasts: ✓

---

## ✅ Prefab 저장 위치

생성 완료 후 다음 위치에 Prefab으로 저장:
```
Assets/Resources/Prefabs/UI/InputFieldPopup.prefab
```

**중요**: 반드시 `Resources/Prefabs/UI/` 폴더에 저장해야 `InputFieldPopup.cs`의 `Resources.Load()` 가 작동합니다!

---

## 🎨 디자인 참고

기존 `ConfirmationPopup.prefab`과 유사한 스타일로 제작하세요:
1. Project 창에서 `Assets/Resources/Prefabs/UI/ConfirmationPopup.prefab` 열기
2. 구조 및 색상 참고
3. 동일한 폰트, 스프라이트 사용

---

## 🧪 테스트 방법

1. JoinScene에 임시 버튼 추가
2. 버튼 클릭 시 다음 코드 실행:
```csharp
UI.Shared.InputFieldPopup.ShowNicknameInput(
    onConfirm: (nickname) => Debug.Log($"닉네임: {nickname}"),
    onCancel: () => Debug.Log("취소됨")
);
```
3. 팝업이 정상적으로 표시되는지 확인
4. 닉네임 입력 및 검증 동작 확인
5. 테스트 버튼 제거

---

## 📝 Google 로그인 버튼 추가

JoinScene에 Google 로그인 버튼도 추가해야 합니다:

### Google 로그인 버튼 위치
```
JoinScene
└─ Canvas
    └─ LoginPanel
        ├─ EmailInputField
        ├─ PasswordInputField
        ├─ ButtonContainer (기존 버튼들)
        └─ SocialLoginContainer (새로 추가)
            ├─ DividerText ("─── 또는 ───")
            └─ GoogleLoginButton
```

### Google 로그인 버튼 설정
- **Image**: Google 로고 + "Google로 시작하기" 텍스트
- **Width**: `400`, **Height**: `50`
- **Color**: 흰색 배경, 검정 텍스트 (Google 가이드라인 준수)
- **OnClick**: JoinManager → `OnClickGoogleLoginButton()`

### Google 로고 에셋
- [Google Identity Branding Guidelines](https://developers.google.com/identity/branding-guidelines)
- 로고 다운로드 후 `Assets/Resources/Sprites/Google/` 에 저장
- Inspector에서 `googleLoginButton` 필드에 연결

---

## ✅ 최종 체크리스트

- [ ] InputFieldPopup.prefab 생성 완료
- [ ] InputFieldPopupUI 스크립트 연결 완료
- [ ] Resources/Prefabs/UI/ 폴더에 저장 완료
- [ ] CanvasGroup 컴포넌트 추가 완료
- [ ] 모든 필드 연결 완료
- [ ] JoinScene에 Google 로그인 버튼 추가 완료
- [ ] JoinManager의 googleLoginButton 필드 연결 완료
- [ ] SystemMessages.asset에 메시지 추가 완료

---

모든 설정이 완료되면 다음 가이드(Firebase Console 설정)로 이동하세요!

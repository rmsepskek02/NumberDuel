using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Manager;
using Objects;

namespace UI.Shared
{
    /// <summary>
    /// 게스트 계정 → 이메일 계정 연동 팝업 UI
    /// Step 1: 이메일 + 비밀번호 입력
    /// Step 2: 닉네임 입력
    /// Step 3: 이메일 인증 (필수)
    /// </summary>
    public enum LinkEmailStep
    {
        EmailAndPassword = 1,  // Step 1: 이메일 + 비밀번호
        Nickname = 2,          // Step 2: 닉네임
        EmailVerification = 3  // Step 3: 이메일 인증
    }

    public class LinkEmailPopupUI : MonoBehaviour
    {
        [Header("Popup Root")]
        [SerializeField] private GameObject popupRoot;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI stepText; // "Step 1/3"
        [SerializeField] private Image[] progressDots; // ●○○
        [SerializeField] private Color activeDotColor = Color.white;
        [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("Step Sections")]
        [SerializeField] private GameObject emailDuplicateSection; // Step 1: 이메일 입력 섹션
        [SerializeField] private GameObject passwordSection;        // Step 1: 비밀번호 입력 섹션
        [SerializeField] private GameObject nicknameSection;        // Step 2: 닉네임 섹션
        [SerializeField] private GameObject emailVerifySection;     // Step 3: 이메일 인증 섹션

        [Header("Message")]
        [SerializeField] private TextMeshProUGUI messageText; // 동적 메시지 텍스트

        [Header("Step 1: Email & Password Input Fields")]
        [SerializeField] private TMP_InputField emailInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField confirmPasswordInputField;

        [Header("Step 2: Nickname")]
        [SerializeField] private TextMeshProUGUI currentNicknameText;
        [SerializeField] private TMP_InputField newNicknameInputField;

        [Header("Step 3: Email Verification")]
        [SerializeField] private TMP_InputField emailVerificationField; // 읽기 전용
        [SerializeField] private Button verifyEmailButton; // "인증하기"
        [SerializeField] private Button resendEmailButton; // "재발송"

        [Header("Navigation Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI backButtonText; // "취소" / "뒤로가기"
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI nextButtonText; // "다음" / "완료"

        [Header("Validation Settings")]
        [SerializeField] private int maxNicknamePixelLength = 24; // 픽셀 기반 (한글 2px, 영문/숫자 1px)

        // 데이터 저장
        private LinkEmailStep currentStep = LinkEmailStep.EmailAndPassword;
        private string enteredEmail;
        private string enteredPassword;
        private string enteredNickname;
        private string currentGuestNickname;
        private Action<bool> onComplete;

        // 계정 전환 상태
        private bool isAccountConverted = false;

        // 재발송 쿨다운
        private System.DateTime lastResendTime;
        private const int RESEND_COOLDOWN = 60; // 60초

        private void Awake()
        {
            // 실시간 비밀번호 검증
            passwordInputField.onValueChanged.AddListener(OnPasswordChanged);
            confirmPasswordInputField.onValueChanged.AddListener(OnPasswordChanged);

            // 실시간 닉네임 픽셀 길이 표시
            newNicknameInputField.onValueChanged.AddListener(OnNicknameInputChanged);

            // 모바일 키보드 대응 (Android/iOS만)
            RegisterInputFieldsToKeyboardManager();

            Hide();
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        public void Show(Action<bool> callback)
        {
            onComplete = callback;
            popupRoot.SetActive(true);

            // 초기화
            currentStep = LinkEmailStep.EmailAndPassword;
            enteredEmail = "";
            enteredPassword = "";
            enteredNickname = "";
            isAccountConverted = false;

            // 입력 필드 초기화
            emailInputField.text = "";
            passwordInputField.text = "";
            confirmPasswordInputField.text = "";
            newNicknameInputField.text = "";

            // 현재 게스트 닉네임 로드
            LoadCurrentNickname();

            // UI 업데이트
            UpdateUI();
        }

        /// <summary>
        /// 팝업 숨김
        /// </summary>
        public void Hide()
        {
            popupRoot.SetActive(false);
            onComplete = null;
        }

        /// <summary>
        /// 현재 게스트 닉네임 로드
        /// </summary>
        private async void LoadCurrentNickname()
        {
            string userId = AuthManager.Instance.CurrentUserUID;
            var profile = await DatabaseManager.Instance.GetUserProfile(userId);

            if (profile != null)
            {
                currentGuestNickname = profile.Nickname;
            }
        }

        /// <summary>
        /// UI 업데이트 (Step 변경 시)
        /// </summary>
        private void UpdateUI()
        {
            // 섹션 활성화/비활성화
            // Step 1: EmailDuplicateSection + PasswordSection
            emailDuplicateSection.SetActive(currentStep == LinkEmailStep.EmailAndPassword);
            passwordSection.SetActive(currentStep == LinkEmailStep.EmailAndPassword);

            // Step 2: NicknameSection
            nicknameSection.SetActive(currentStep == LinkEmailStep.Nickname);

            // Step 3: EmailVerifySection
            emailVerifySection.SetActive(currentStep == LinkEmailStep.EmailVerification);

            // Progress 업데이트
            UpdateProgress();

            // Step별 UI 설정
            switch (currentStep)
            {
                case LinkEmailStep.EmailAndPassword:
                    backButtonText.text = "취소";
                    nextButtonText.text = "다음";
                    messageText.text = "";
                    emailInputField.Select();
                    break;

                case LinkEmailStep.Nickname:
                    backButtonText.text = "뒤로가기";
                    nextButtonText.text = "다음";
                    messageText.text = "";
                    // 현재 닉네임 표시
                    if (currentNicknameText != null)
                    {
                        currentNicknameText.text = $"현재 닉네임: {currentGuestNickname}";
                    }

                    newNicknameInputField.Select();
                    break;

                case LinkEmailStep.EmailVerification:
                    backButtonText.text = "뒤로가기";
                    nextButtonText.text = "완료";

                    // 이메일 필드 설정 (읽기 전용)
                    if (emailVerificationField != null)
                    {
                        emailVerificationField.text = enteredEmail;
                        emailVerificationField.interactable = false;
                    }

                    // MessageText
                    messageText.text = "이메일 인증을 완료해야 계정을 사용할 수 있습니다.\n메일함을 확인하고 인증 링크를 클릭해주세요.";
                    // 버튼 설정
                    verifyEmailButton.gameObject.SetActive(true);
                    resendEmailButton.gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// Progress Dots 업데이트
        /// </summary>
        private void UpdateProgress()
        {
            stepText.text = $"Step {(int)currentStep}/3";

            for (int i = 0; i < progressDots.Length; i++)
            {
                progressDots[i].color = i < (int)currentStep ? activeDotColor : inactiveDotColor;
            }
        }

        #region Button Handlers

        /// <summary>
        /// [뒤로가기] 버튼 클릭
        /// Unity Editor에서 이벤트 연결
        /// </summary>
        public void OnBackClicked()
        {
            // 계정 전환 후에는 뒤로가기 불가 (방어 코드)
            if (isAccountConverted)
            {
                ConfirmationPopup.Show(
                    "이미 계정 전환이 완료되었습니다.\n취소할 수 없으니 계속 진행해주세요.",
                    onConfirm: () => { },
                    onCancel: null,
                    confirmText: "확인"
                );
                return;
            }

            // 이전 단계로
            switch (currentStep)
            {
                case LinkEmailStep.Nickname:
                    currentStep = LinkEmailStep.EmailAndPassword;
                    UpdateUI();
                    break;

                case LinkEmailStep.EmailVerification:
                    currentStep = LinkEmailStep.Nickname;
                    UpdateUI();
                    break;

                default:
                    Hide();
                    onComplete?.Invoke(false);
                    break;
            }
        }

        /// <summary>
        /// [다음] / [완료] 버튼 클릭
        /// Unity Editor에서 이벤트 연결
        /// </summary>
        public void OnNextClicked()
        {
            switch (currentStep)
            {
                case LinkEmailStep.EmailAndPassword:
                    OnStep1Next();
                    break;

                case LinkEmailStep.Nickname:
                    OnStep2Next();
                    break;

                case LinkEmailStep.EmailVerification:
                    OnStep3Complete();
                    break;
            }
        }

        #endregion

        #region Step 1: Email & Password

        /// <summary>
        /// Step 1: 다음 버튼 처리
        /// </summary>
        private async void OnStep1Next()
        {
            string email = emailInputField.text.Trim();
            string password = passwordInputField.text;
            string confirmPassword = confirmPasswordInputField.text;

            // 1. 이메일 형식 검증
            if (string.IsNullOrEmpty(email))
            {
                messageText.text = "이메일을 입력해주세요.";
                messageText.color = Color.red;
                return;
            }

            if (!IsValidEmail(email))
            {
                messageText.text = "올바른 이메일 형식이 아닙니다.";
                messageText.color = Color.red;
                return;
            }

            // 2. 이메일 중복 확인
            SystemMessageManager.Instance?.ShowMessage("CheckingEmail");
            bool isRegistered = await DatabaseManager.Instance.IsEmailRegistered(email);

            if (isRegistered)
            {
                messageText.text = "이미 등록된 이메일입니다.";
                messageText.color = Color.red;
                return;
            }

            // 3. 비밀번호 검증
            if (string.IsNullOrEmpty(password))
            {
                messageText.text = "비밀번호를 입력해주세요.";
                messageText.color = Color.red;
                return;
            }

            if (password.Length < 6)
            {
                messageText.text = "비밀번호는 6자 이상이어야 합니다.";
                messageText.color = Color.red;
                return;
            }

            if (password != confirmPassword)
            {
                messageText.text = "비밀번호가 일치하지 않습니다.";
                messageText.color = Color.red;
                return;
            }

            // 4. 입력값 저장 (메모리에만)
            enteredEmail = email;
            enteredPassword = password;

            // 5. Step 2로 이동
            currentStep = LinkEmailStep.Nickname;
            UpdateUI();
        }

        /// <summary>
        /// 이메일 형식 검증
        /// </summary>
        private bool IsValidEmail(string email)
        {
            return email.Contains("@") && email.Contains(".");
        }

        /// <summary>
        /// 실시간 비밀번호 일치 검증
        /// </summary>
        private void OnPasswordChanged(string value)
        {
            if (!string.IsNullOrEmpty(passwordInputField.text) &&
                !string.IsNullOrEmpty(confirmPasswordInputField.text))
            {
                if (passwordInputField.text == confirmPasswordInputField.text)
                {
                    messageText.text = "비밀번호가 일치합니다.";
                    messageText.color = Color.green;
                }
                else
                {
                    messageText.text = "비밀번호가 일치하지 않습니다.";
                    messageText.color = Color.red;
                }
            }
            else
            {
                messageText.text = "";
            }
        }

        #endregion

        #region Step 2: Nickname

        /// <summary>
        /// Step 2: 다음 버튼 처리
        /// </summary>
        private async void OnStep2Next()
        {
            string newNickname = newNicknameInputField.text.Trim();

            // 1. 닉네임 검증
            if (!ValidateNickname(newNickname, out string errorMessage))
            {
                messageText.text = errorMessage;
                messageText.color = Color.red;
                return;
            }

            // 2. 닉네임 중복 확인
            SystemMessageManager.Instance?.ShowMessage("CheckingNickname");
            bool isAvailable = await DatabaseManager.Instance.IsNicknameAvailable(newNickname);

            if (!isAvailable)
            {
                messageText.text = "이미 사용 중인 닉네임입니다.";
                messageText.color = Color.red;
                return;
            }

            // 3. 입력값 저장 (메모리에만)
            enteredNickname = newNickname;

            // 4. Step 3로 이동
            currentStep = LinkEmailStep.EmailVerification;
            UpdateUI();
        }

        /// <summary>
        /// 닉네임 검증
        /// </summary>
        private bool ValidateNickname(string nickname, out string errorMessage)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                errorMessage = "닉네임을 입력해주세요.";
                return false;
            }

            int pixelLength = CalculateNicknamePixelLength(nickname);
            if (pixelLength > maxNicknamePixelLength)
            {
                errorMessage = $"닉네임이 너무 깁니다 ({pixelLength}/{maxNicknamePixelLength})";
                return false;
            }

            errorMessage = "";
            return true;
        }

        /// <summary>
        /// 닉네임 픽셀 길이 계산
        /// </summary>
        private int CalculateNicknamePixelLength(string text)
        {
            int length = 0;
            foreach (char c in text)
            {
                // 한글: 2px, 영문/숫자: 1px
                if (c >= 0xAC00 && c <= 0xD7A3)
                    length += 2;
                else
                    length += 1;
            }
            return length;
        }

        /// <summary>
        /// 실시간 닉네임 픽셀 길이 표시
        /// </summary>
        private void OnNicknameInputChanged(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                messageText.text = "";
                return;
            }

            int pixelLength = CalculateNicknamePixelLength(input);

            if (pixelLength > maxNicknamePixelLength)
            {
                messageText.text = $"닉네임이 너무 깁니다 ({pixelLength}/{maxNicknamePixelLength})";
                messageText.color = Color.red;
            }
            else
            {
                messageText.text = $"{pixelLength}/{maxNicknamePixelLength}";
                messageText.color = new Color(0.3f, 0.8f, 1f); // 파란색
            }
        }

        #endregion

        #region Step 3: Email Verification

        /// <summary>
        /// [인증하기] 버튼 클릭
        /// Unity Editor에서 이벤트 연결
        /// </summary>
        public void OnVerifyEmailButtonClicked()
        {
            // 확인 팝업 표시
            ConfirmationPopup.Show(
                "입력된 정보는 수정할 수 없습니다.\n인증을 진행하시겠습니까?",
                onConfirm: async () =>
                {
                    await ProcessEmailLinking();
                },
                onCancel: () =>
                {
                    // 취소 - 아무것도 안 함
                },
                confirmText: "확인",
                cancelText: "취소"
            );
        }

        /// <summary>
        /// 계정 전환 처리 (확인 팝업에서 "확인" 클릭 시)
        /// </summary>
        private async System.Threading.Tasks.Task ProcessEmailLinking()
        {
            try
            {
                // 뒤로가기 버튼 숨김
                backButton.gameObject.SetActive(false);
                isAccountConverted = true;

                string userId = AuthManager.Instance.CurrentUserUID;

                // 1. 계정 전환
                SystemMessageManager.Instance?.ShowMessage("ConvertingAccount");

                var credential = Firebase.Auth.EmailAuthProvider.GetCredential(enteredEmail, enteredPassword);
                await AuthManager.Instance.CurrentUser.LinkWithCredentialAsync(credential);

                // 2. Firestore 업데이트
                await DatabaseManager.Instance.UpdateUserEmail(userId, enteredEmail);
                await DatabaseManager.Instance.UpdateUserAuthProvider(userId, "email");
                await DatabaseManager.Instance.UpdateUserNickname(userId, enteredNickname);

                // 3. 인증 메일 발송
                await AuthManager.Instance.SendEmailVerification();

                // 4. 자동 로그아웃
                AuthManager.Instance.SignOutWithoutSessionClear();

                SystemMessageManager.Instance?.ShowMessage("EmailLinkSuccess");

                // 5. 이메일 발송 완료 팝업 표시
                ConfirmationPopup.Show(
                    $"인증 메일이 '{enteredEmail}'로 발송되었습니다.\n\n" +
                    "이메일의 인증 링크를 클릭한 후\n" +
                    "\"완료\" 버튼을 눌러주세요.",
                    onConfirm: () => { },
                    onCancel: null,
                    confirmText: "확인"
                );

                // 6. UI 업데이트
                verifyEmailButton.gameObject.SetActive(false); // 인증하기 버튼 숨김
                resendEmailButton.gameObject.SetActive(true); // 재발송 버튼 표시

                messageText.text = "인증 메일이 발송되었습니다.\n메일함을 확인하고 인증 링크를 클릭한 후\n[완료] 버튼을 눌러주세요.";
                }
            catch (Exception ex)
            {
                Debug.LogError($"[LinkEmailPopup] 계정 전환 실패: {ex.Message}");

                // 실패 시 뒤로가기 버튼 다시 표시
                backButton.gameObject.SetActive(true);
                isAccountConverted = false;

                messageText.text = "계정 전환에 실패했습니다.";
                messageText.color = Color.red;
            }
        }

        /// <summary>
        /// [재발송] 버튼 클릭
        /// Unity Editor에서 이벤트 연결
        /// </summary>
        public async void OnResendEmailButtonClicked()
        {
            // 쿨다운 체크
            if ((System.DateTime.Now - lastResendTime).TotalSeconds < RESEND_COOLDOWN)
            {
                int remaining = RESEND_COOLDOWN - (int)(System.DateTime.Now - lastResendTime).TotalSeconds;
                messageText.text = $"{remaining}초 후에 재발송할 수 있습니다.";
                messageText.color = new Color(1f, 0.92f, 0.016f); // 노란색
                return;
            }

            try
            {
                // 재로그인 필요 (로그아웃 상태이므로)
                var loginResult = await AuthManager.Instance.LoginWithEmail(enteredEmail, enteredPassword);

                if (!loginResult.success)
                {
                    messageText.text = "재발송 실패: 로그인할 수 없습니다.";
                    messageText.color = Color.red;
                    return;
                }

                // 인증 메일 재발송
                await AuthManager.Instance.SendEmailVerification();

                // 다시 로그아웃
                AuthManager.Instance.SignOutWithoutSessionClear();

                lastResendTime = System.DateTime.Now;

                // 재발송 완료 팝업
                ConfirmationPopup.Show(
                    "인증 메일이 재발송되었습니다.\n\n" +
                    "이메일의 인증 링크를 클릭한 후\n" +
                    "\"완료\" 버튼을 눌러주세요.",
                    onConfirm: () => { },
                    onCancel: null,
                    confirmText: "확인"
                );

                messageText.text = "인증 메일이 재발송되었습니다.";
                messageText.color = Color.green;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LinkEmailPopup] 재발송 실패: {ex.Message}");
                messageText.text = "재발송에 실패했습니다.";
                messageText.color = Color.red;
            }
        }

        /// <summary>
        /// [완료] 버튼 처리 (Step 3)
        /// 이메일 인증 확인
        /// </summary>
        private async void OnStep3Complete()
        {
            try
            {
                // 재로그인하여 인증 상태 확인
                var loginResult = await AuthManager.Instance.LoginWithEmail(enteredEmail, enteredPassword);

                if (!loginResult.success)
                {
                    messageText.text = "로그인할 수 없습니다.";
                    messageText.color = Color.red;
                    return;
                }

                // 사용자 정보 새로고침
                await AuthManager.Instance.ReloadUserInfo();

                // 인증 여부 체크
                if (AuthManager.Instance.IsEmailVerified)
                {
                    // 인증 완료 - 팝업 닫기
                    SystemMessageManager.Instance?.ShowMessage("EmailVerificationComplete");

                    // 이메일 연동 완료 이벤트 발생
                    AuthManager.Instance.NotifyEmailLinked();

                    Hide();
                    onComplete?.Invoke(true);
                }
                else
                {
                    // 인증 미완료 - 경고 팝업
                    AuthManager.Instance.SignOutWithoutSessionClear();

                    ConfirmationPopup.Show(
                        "이메일 인증을 완료해주세요.\n\n" +
                        "메일함을 확인하고 인증 링크를 클릭하신 후\n" +
                        "다시 시도해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LinkEmailPopup] 인증 확인 실패: {ex.Message}");
                messageText.text = "인증 확인에 실패했습니다.";
                messageText.color = Color.red;
            }
        }

        #endregion

        #region Keyboard Manager Integration
        /// <summary>
        /// KeyboardManager에 InputField 등록
        /// 팝업이 생성될 때 호출되어 모바일 키보드 대응 활성화
        /// </summary>
        private void RegisterInputFieldsToKeyboardManager()
        {
#if UNITY_ANDROID || UNITY_IOS
            // 모바일 플랫폼에서만 KeyboardManager에 등록
            var keyboardManager = Manager.KeyboardManager.Instance;
            if (keyboardManager != null)
            {
                // Step 1: 이메일 + 비밀번호 입력 필드
                if (emailInputField != null)
                    keyboardManager.RegisterInputFieldRuntime(emailInputField);

                if (passwordInputField != null)
                    keyboardManager.RegisterInputFieldRuntime(passwordInputField);

                if (confirmPasswordInputField != null)
                    keyboardManager.RegisterInputFieldRuntime(confirmPasswordInputField);

                // Step 2: 닉네임 입력 필드
                if (newNicknameInputField != null)
                    keyboardManager.RegisterInputFieldRuntime(newNicknameInputField);

                // Step 3: 이메일 인증 필드 (읽기 전용이지만 선택 가능)
                if (emailVerificationField != null)
                    keyboardManager.RegisterInputFieldRuntime(emailVerificationField);
            }
#endif
        }
        #endregion
    }
}

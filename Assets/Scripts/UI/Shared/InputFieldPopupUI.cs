using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utills;

namespace UI.Shared
{
    /// <summary>
    /// 입력 타입 enum
    /// </summary>
    public enum InputPopupType
    {
        Nickname,   // 닉네임 입력 (한글/영문, 픽셀 길이 검증)
        Password,   // 비밀번호 입력 (최소 6자)
        Custom      // 커스텀 검증 (validator 직접 지정)
    }

    /// <summary>
    /// 범용 입력 팝업 UI
    /// 닉네임, 비밀번호 등 다양한 입력 상황에 재사용 가능
    /// ValidationHelper를 활용하여 입력 검증
    /// </summary>
    public class InputFieldPopupUI : PopupBase<InputFieldPopupUI>
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TextMeshProUGUI placeholderText;
        [SerializeField] private TextMeshProUGUI validationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        #endregion

        #region Private Fields

        private Action<string> onConfirmed;
        private Action onCanceled;
        private Func<string, Task<(bool valid, string message)>> customValidator;
        private InputPopupType currentInputType;
        private bool isProcessing;
        private bool isValidationPassed;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            confirmButton?.onClick.AddListener(OnConfirmClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);

            // KeyboardManager에 InputField 등록 (모바일 키보드 대응)
            RegisterInputFieldToKeyboard(inputField);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            confirmButton?.onClick.RemoveListener(OnConfirmClicked);
            cancelButton?.onClick.RemoveListener(OnCancelClicked);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// 일반화된 입력 팝업 표시
        /// </summary>
        /// <param name="type">입력 타입 (Nickname, Password, Custom)</param>
        /// <param name="onConfirm">확인 버튼 콜백</param>
        /// <param name="onCancel">취소 버튼 콜백 (null이면 취소 버튼 숨김)</param>
        /// <param name="title">팝업 제목 (null이면 타입별 기본값 사용)</param>
        /// <param name="placeholder">입력 필드 placeholder (null이면 타입별 기본값 사용)</param>
        /// <param name="validator">서버 검증 함수 (Nickname: 중복 체크, Custom: 직접 지정)</param>
        public static void Show(
            InputPopupType type,
            Action<string> onConfirm,
            Action onCancel = null,
            string title = null,
            string placeholder = null,
            Func<string, Task<(bool valid, string message)>> validator = null)
        {
            if (instance == null && !LoadPopup())
                return;

            // 타입별 기본값 설정
            var (defaultTitle, defaultPlaceholder, contentType, defaultValidator) = GetDefaultsForType(type);

            instance.ShowInternal(
                title ?? defaultTitle,
                placeholder ?? defaultPlaceholder,
                contentType,
                type,
                onConfirm,
                onCancel,
                validator ?? defaultValidator
            );
        }

        /// <summary>
        /// 타입별 기본값 반환
        /// </summary>
        private static (string title, string placeholder, TMP_InputField.ContentType contentType, Func<string, Task<(bool valid, string message)>> validator) GetDefaultsForType(InputPopupType type)
        {
            return type switch
            {
                InputPopupType.Nickname => ("닉네임 입력", "닉네임을 입력하세요", TMP_InputField.ContentType.Standard, ValidateNicknameAsync),
                InputPopupType.Password => ("비밀번호 입력", "비밀번호를 입력하세요", TMP_InputField.ContentType.Password, null),
                InputPopupType.Custom => ("입력", "입력하세요", TMP_InputField.ContentType.Standard, null),
                _ => ("입력", "입력하세요", TMP_InputField.ContentType.Standard, null)
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 팝업 표시 (Instance)
        /// </summary>
        private void ShowInternal(
            string title,
            string placeholder,
            TMP_InputField.ContentType contentType,
            InputPopupType inputType,
            Action<string> onConfirm,
            Action onCancel,
            Func<string, Task<(bool valid, string message)>> validator)
        {
            onConfirmed = onConfirm;
            onCanceled = onCancel;
            customValidator = validator;
            currentInputType = inputType;

            SetupUI(title, placeholder, contentType);
            InitializeValidation();

            gameObject.SetActive(true);
            RegisterToUIStack();
            PlayShowAnimation();
            PlayClickSound();
        }

        private void SetupUI(string title, string placeholder, TMP_InputField.ContentType contentType)
        {
            if (titleText != null)
                titleText.text = title;

            if (placeholderText != null)
                placeholderText.text = placeholder;

            if (inputField != null)
            {
                inputField.contentType = contentType;
                inputField.text = string.Empty;
                inputField.onValueChanged.RemoveAllListeners();
                inputField.onValueChanged.AddListener(OnInputValueChanged);
                inputField.ActivateInputField();
            }

            if (cancelButton != null)
                cancelButton.gameObject.SetActive(onCanceled != null);
        }

        private void InitializeValidation()
        {
            isValidationPassed = false;

            if (validationText != null)
            {
                validationText.text = "ㅤ";
                validationText.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        #endregion

        #region Input Validation

        /// <summary>
        /// 입력값 변경 시 호출 - InputPopupType에 따라 검증 분기
        /// </summary>
        private void OnInputValueChanged(string input)
        {
            if (validationText == null)
                return;

            string trimmedInput = input.Trim();

            if (string.IsNullOrWhiteSpace(trimmedInput))
            {
                UpdateValidationText("ㅤ", new Color(1f, 1f, 1f, 0f));
                isValidationPassed = false;
                return;
            }

            // InputPopupType에 따라 검증 분기
            switch (currentInputType)
            {
                case InputPopupType.Nickname:
                    ValidateNicknameInput(trimmedInput);
                    break;
                case InputPopupType.Password:
                    ValidatePasswordInput(trimmedInput);
                    break;
                case InputPopupType.Custom:
                    // Custom 타입은 실시간 검증 없이 확인 버튼 클릭 시 서버 검증
                    isValidationPassed = true;
                    break;
            }
        }

        /// <summary>
        /// 닉네임 입력 검증 (ValidationHelper 사용)
        /// </summary>
        private void ValidateNicknameInput(string input)
        {
            var (isValid, errorMessage) = ValidationHelper.ValidateNickname(input);

            if (!isValid)
            {
                UpdateValidationText(errorMessage, Global.GlowRed);
                isValidationPassed = false;
                return;
            }

            // 검증 통과 - 중복 확인 안내
            int totalPixels = ValidationHelper.CalculatePixelLength(input);
            UpdateValidationText($"중복 확인을 진행해주세요 ({totalPixels}/{ValidationHelper.MAX_NICKNAME_PIXEL_LENGTH})", Global.Purple);
            isValidationPassed = true;
        }

        /// <summary>
        /// 비밀번호 입력 검증 (ValidationHelper 사용)
        /// </summary>
        private void ValidatePasswordInput(string input)
        {
            var (isValid, errorMessage) = ValidationHelper.ValidatePassword(input);

            if (!isValid)
            {
                UpdateValidationText(errorMessage, Global.GlowRed);
                isValidationPassed = false;
                return;
            }

            // 검증 통과
            UpdateValidationText("ㅤ", new Color(1f, 1f, 1f, 0f));
            isValidationPassed = true;
        }

        /// <summary>
        /// 닉네임 서버 검증 (중복 확인)
        /// </summary>
        private static async Task<(bool valid, string message)> ValidateNicknameAsync(string nickname)
        {
            // 서버에서 닉네임 중복 확인
            bool isAvailable = await Manager.DatabaseManager.Instance.IsNicknameAvailable(nickname);

            if (!isAvailable)
            {
                return (false, "이미 사용 중인 닉네임입니다.");
            }

            return (true, "사용 가능한 닉네임입니다.");
        }

        #endregion

        #region Button Handlers

        private void OnCancelClicked()
        {
            if (isProcessing)
                return;

            onCanceled?.Invoke();
            // Hide()를 여기서 호출하지 않음 - 콜백에서 필요 시 Hide() 호출
        }

        private async void OnConfirmClicked()
        {
            if (isProcessing)
                return;

            string input = inputField?.text.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                UpdateValidationText("입력값을 입력해주세요.", Global.GlowRed);
                return;
            }

            if (!isValidationPassed)
            {
                // 기존 검증 메시지 유지 (메시지를 덮어쓰지 않음)
                return;
            }

            // 서버 검증 (닉네임 중복 체크 등)
            if (customValidator != null)
            {
                bool validationResult = await ValidateWithServer(input);
                if (!validationResult)
                {
                    // 검증 실패 (중복 닉네임 등) - 팝업 유지하고 재입력 유도
                    return;
                }
            }

            // 검증 성공 - 콜백 실행 및 팝업 닫기
            onConfirmed?.Invoke(input);
            await Task.Delay(300);
            HideInternal();
        }

        private async Task<bool> ValidateWithServer(string input)
        {
            isProcessing = true;
            SetButtonsInteractable(false);

            // 타입별 검증 중 메시지
            string validatingMessage = currentInputType == InputPopupType.Nickname ? "중복 확인 중..." : "확인 중...";
            UpdateValidationText(validatingMessage, Global.Yellow);

            var (valid, message) = await customValidator(input);

            if (!valid)
            {
                UpdateValidationText(message, Global.GlowRed);
                isProcessing = false;
                SetButtonsInteractable(true);

                // 커서를 맨 끝으로 이동 (재입력 시 텍스트 삭제 방지)
                inputField?.ActivateInputField();
                if (inputField != null)
                {
                    inputField.caretPosition = inputField.text.Length;
                    inputField.selectionAnchorPosition = inputField.text.Length;
                    inputField.selectionFocusPosition = inputField.text.Length;
                }
                return false;
            }

            if (!string.IsNullOrEmpty(message))
                UpdateValidationText(message, Global.GlowGreen);

            isProcessing = false;
            SetButtonsInteractable(true);
            return true;
        }

        #endregion

        #region UI Helpers

        private void UpdateValidationText(string message, Color color)
        {
            if (validationText != null)
            {
                validationText.text = message;
                validationText.color = color;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (confirmButton != null)
                confirmButton.interactable = interactable;

            if (cancelButton != null)
                cancelButton.interactable = interactable;

            if (inputField != null)
                inputField.interactable = interactable;
        }

        #endregion
    }
}

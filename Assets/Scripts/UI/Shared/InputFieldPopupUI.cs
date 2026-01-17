using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared
{
    /// <summary>
    /// 범용 입력 팝업 UI
    /// 닉네임, 비밀번호 등 다양한 입력 상황에 재사용 가능
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
        /// 닉네임 입력 팝업 표시
        /// </summary>
        public static void ShowNicknameInput(Action<string> onConfirm, Action onCancel = null)
        {
            Show(
                "닉네임 입력",
                "닉네임을 입력하세요",
                TMP_InputField.ContentType.Standard,
                onConfirm,
                onCancel,
                ValidateNickname
            );
        }

        /// <summary>
        /// 비밀번호 입력 팝업 표시
        /// </summary>
        public static void ShowPasswordInput(string title, Action<string> onConfirm, Action onCancel = null)
        {
            Show(
                title,
                "비밀번호를 입력하세요",
                TMP_InputField.ContentType.Password,
                onConfirm,
                onCancel,
                null
            );
        }

        /// <summary>
        /// 커스텀 입력 팝업 표시
        /// </summary>
        public static void ShowCustomInput(
            string title,
            string placeholder,
            TMP_InputField.ContentType contentType,
            Action<string> onConfirm,
            Action onCancel = null,
            Func<string, Task<(bool valid, string message)>> validator = null)
        {
            Show(title, placeholder, contentType, onConfirm, onCancel, validator);
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        public static void Show(
            string title,
            string placeholder,
            TMP_InputField.ContentType contentType,
            Action<string> onConfirm,
            Action onCancel = null,
            Func<string, Task<(bool valid, string message)>> validator = null)
        {
            if (instance == null && !LoadPopup())
                return;

            instance.ShowInternal(title, placeholder, contentType, onConfirm, onCancel, validator);
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
            Action<string> onConfirm,
            Action onCancel,
            Func<string, Task<(bool valid, string message)>> validator)
        {
            onConfirmed = onConfirm;
            onCanceled = onCancel;
            customValidator = validator;

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

        private void OnInputValueChanged(string input)
        {
            if (customValidator == null || validationText == null)
                return;

            string trimmedInput = input.Trim();

            if (string.IsNullOrWhiteSpace(trimmedInput))
            {
                UpdateValidationText("ㅤ", new Color(1f, 1f, 1f, 0f));
                isValidationPassed = false;
                return;
            }

            ValidateInputRules(trimmedInput);
        }

        private void ValidateInputRules(string input)
        {
            if (input.Length < 1)
            {
                UpdateValidationText("최소 1자 이상 입력해주세요.", Global.GlowRed);
                isValidationPassed = false;
                return;
            }

            int totalPixels = CalculatePixelLength(input);

            if (totalPixels > 24)
            {
                UpdateValidationText($"닉네임이 너무 깁니다. ({totalPixels}/24)", Global.GlowRed);
                isValidationPassed = false;
                return;
            }

            UpdateValidationText($"중복 확인을 진행해주세요 ({totalPixels}/24)", Global.Purple);
            isValidationPassed = true;
        }

        private int CalculatePixelLength(string input)
        {
            int totalPixels = 0;

            foreach (char c in input)
            {
                bool isKorean = (c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㅎ') || (c >= 'ㅏ' && c <= 'ㅣ');
                bool isEnglish = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

                if (!isKorean && !isEnglish)
                {
                    UpdateValidationText("한글, 영문만 사용 가능합니다.", Global.GlowRed);
                    isValidationPassed = false;
                    return -1;
                }

                totalPixels += isKorean ? 2 : 1;
            }

            return totalPixels;
        }

        /// <summary>
        /// 닉네임 검증 (서버 중복 확인 포함)
        /// </summary>
        private static async Task<(bool valid, string message)> ValidateNickname(string nickname)
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
                UpdateValidationText("입력 규칙을 확인해주세요.", Global.GlowRed);
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

            UpdateValidationText("중복 확인 중...", Global.Yellow);

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

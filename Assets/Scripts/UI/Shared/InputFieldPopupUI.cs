using System;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared
{
    /// <summary>
    /// 범용 입력 팝업 UI
    /// 닉네임, 비밀번호 등 다양한 입력 상황에 재사용 가능
    /// </summary>
    public class InputFieldPopupUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TextMeshProUGUI placeholderText;
        [SerializeField] private TextMeshProUGUI validationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;
        #endregion

        #region Private Fields
        private Action<string> onConfirmed;
        private Action onCanceled;
        private Func<string, Task<(bool valid, string message)>> customValidator;
        private Tween showTween;
        private Tween hideTween;
        private bool isProcessing;
        private bool isValidationPassed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            confirmButton?.onClick.AddListener(OnConfirmClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // KeyboardManager에 InputField 등록 (모바일 키보드 대응)
            RegisterInputFieldToKeyboardManager();

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            showTween?.Kill();
            hideTween?.Kill();
            confirmButton?.onClick.RemoveListener(OnConfirmClicked);
            cancelButton?.onClick.RemoveListener(OnCancelClicked);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 팝업 표시
        /// </summary>
        public void Show(
            string title,
            string placeholder,
            TMP_InputField.ContentType contentType,
            Action<string> onConfirm,
            Action onCancel = null,
            Func<string, Task<(bool valid, string message)>> validator = null)
        {
            hideTween?.Kill();

            onConfirmed = onConfirm;
            onCanceled = onCancel;
            customValidator = validator;

            SetupUI(title, placeholder, contentType);
            InitializeValidation();

            gameObject.SetActive(true);
            PlayShowAnimation();
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 팝업 숨기기
        /// </summary>
        public void Hide()
        {
            showTween?.Kill();
            PlayHideAnimation();
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);
        }
        #endregion

        #region UI Setup
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
                UpdateValidationText("최소 1자 이상 입력해주세요.", new Color(1f, 0.3f, 0.3f));
                isValidationPassed = false;
                return;
            }

            int totalPixels = CalculatePixelLength(input);

            if (totalPixels > 24)
            {
                UpdateValidationText($"닉네임이 너무 깁니다. ({totalPixels}/24)", new Color(1f, 0.3f, 0.3f));
                isValidationPassed = false;
                return;
            }

            UpdateValidationText($"중복 확인을 진행해주세요 ({totalPixels}/24)", new Color(0.5f, 0.8f, 1f));
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
                    UpdateValidationText("한글, 영문만 사용 가능합니다.", new Color(1f, 0.3f, 0.3f));
                    isValidationPassed = false;
                    return -1;
                }

                totalPixels += isKorean ? 2 : 1;
            }

            return totalPixels;
        }
        #endregion

        #region Button Handlers
        private void OnCancelClicked()
        {
            if (isProcessing)
                return;

            onCanceled?.Invoke();
            Hide();
        }

        private async void OnConfirmClicked()
        {
            if (isProcessing)
                return;

            string input = inputField?.text.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                UpdateValidationText("입력값을 입력해주세요.", new Color(1f, 0.3f, 0.3f));
                return;
            }

            if (!isValidationPassed)
            {
                UpdateValidationText("입력 규칙을 확인해주세요.", new Color(1f, 0.3f, 0.3f));
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
            Hide();
        }

        private async Task<bool> ValidateWithServer(string input)
        {
            isProcessing = true;
            SetButtonsInteractable(false);

            UpdateValidationText("중복 확인 중...", new Color(1f, 1f, 0.5f));

            var (valid, message) = await customValidator(input);

            if (!valid)
            {
                UpdateValidationText(message, new Color(1f, 0.3f, 0.3f));
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
                return false; // 검증 실패
            }

            if (!string.IsNullOrEmpty(message))
                UpdateValidationText(message, new Color(0.3f, 1f, 0.3f));

            isProcessing = false;
            SetButtonsInteractable(true);
            return true; // 검증 성공
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

        #region Keyboard Manager Integration
        /// <summary>
        /// KeyboardManager에 InputField 등록
        /// 팝업이 생성될 때 호출되어 모바일 키보드 대응 활성화
        /// </summary>
        private void RegisterInputFieldToKeyboardManager()
        {
            if (inputField == null)
                return;

#if UNITY_ANDROID || UNITY_IOS
            // 모바일 플랫폼에서만 KeyboardManager에 등록
            var keyboardManager = Manager.KeyboardManager.Instance;
            if (keyboardManager != null)
            {
                keyboardManager.RegisterInputFieldRuntime(inputField);
            }
#endif
        }
        #endregion

        #region Animations
        private void PlayShowAnimation()
        {
            if (canvasGroup == null || popupRect == null)
                return;

            canvasGroup.alpha = 0f;
            popupRect.localScale = Vector3.one * 0.9f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Sequence showSequence = DOTween.Sequence();
            showSequence.Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));
            showSequence.Join(popupRect.DOScale(1f, showDuration).SetEase(Ease.OutBack));
            showSequence.OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });

            showTween = showSequence;
        }

        private void PlayHideAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Sequence hideSequence = DOTween.Sequence();
            hideSequence.Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));
            hideSequence.Join(popupRect.DOScale(0.9f, hideDuration).SetEase(Ease.InBack));
            hideSequence.OnComplete(() => gameObject.SetActive(false));

            hideTween = hideSequence;
        }
        #endregion
    }
}

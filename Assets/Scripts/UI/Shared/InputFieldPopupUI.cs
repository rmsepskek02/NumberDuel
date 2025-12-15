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
        #region Fields and Properties
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TextMeshProUGUI placeholderText; // InputField Placeholder
        [SerializeField] private TextMeshProUGUI validationText; // 유효성 검사 메시지
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;

        private Action<string> onConfirmed; // 확인 콜백
        private Action onCanceled; // 취소 콜백
        private Func<string, Task<(bool valid, string message)>> customValidator; // 커스텀 검증 함수
        private Tween showTween;
        private Tween hideTween;
        private bool isProcessing = false;
        private TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 버튼 이벤트 등록
            confirmButton?.onClick.AddListener(OnConfirmClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);

            // 초기 상태: 숨김
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Tween 정리
            showTween?.Kill();
            hideTween?.Kill();

            // 버튼 이벤트 해제
            confirmButton?.onClick.RemoveListener(OnConfirmClicked);
            cancelButton?.onClick.RemoveListener(OnCancelClicked);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 팝업 표시 (범용)
        /// </summary>
        /// <param name="title">팝업 제목</param>
        /// <param name="placeholder">입력 필드 플레이스홀더</param>
        /// <param name="contentType">입력 타입 (Standard, Password 등)</param>
        /// <param name="onConfirm">확인 콜백</param>
        /// <param name="onCancel">취소 콜백 (null이면 취소 버튼 숨김)</param>
        /// <param name="validator">커스텀 검증 함수 (선택)</param>
        public void Show(
            string title,
            string placeholder,
            TMP_InputField.ContentType contentType,
            Action<string> onConfirm,
            Action onCancel = null,
            Func<string, Task<(bool valid, string message)>> validator = null)
        {
            // 이전 Tween 정리
            hideTween?.Kill();

            // 콜백 저장
            onConfirmed = onConfirm;
            onCanceled = onCancel;
            customValidator = validator;
            this.contentType = contentType;

            // UI 설정
            if (titleText != null)
                titleText.text = title;

            if (placeholderText != null)
                placeholderText.text = placeholder;

            if (inputField != null)
            {
                inputField.contentType = contentType;
                inputField.text = string.Empty;
                inputField.Select();
                inputField.ActivateInputField();
            }

            // 취소 버튼 표시/숨김
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(onCancel != null);
            }

            // 유효성 메시지 초기화
            UpdateValidationText(string.Empty, Color.white);

            // 활성화
            gameObject.SetActive(true);

            // 애니메이션
            PlayShowAnimation();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 팝업 숨기기
        /// </summary>
        public void Hide()
        {
            // 이전 Tween 정리
            showTween?.Kill();

            // 애니메이션
            PlayHideAnimation();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);
        }
        #endregion

        #region Button Handlers
        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void OnCancelClicked()
        {
            if (isProcessing)
                return;

            onCanceled?.Invoke();
            Hide();
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private async void OnConfirmClicked()
        {
            if (isProcessing)
                return;

            string input = inputField?.text.Trim() ?? string.Empty;

            // 빈 입력 체크
            if (string.IsNullOrWhiteSpace(input))
            {
                UpdateValidationText("입력값을 입력해주세요.", new Color(1f, 0.3f, 0.3f));
                return;
            }

            // 커스텀 검증
            if (customValidator != null)
            {
                isProcessing = true;
                SetButtonsInteractable(false);

                var (valid, message) = await customValidator(input);

                if (!valid)
                {
                    UpdateValidationText(message, new Color(1f, 0.3f, 0.3f));
                    isProcessing = false;
                    SetButtonsInteractable(true);
                    inputField?.Select();
                    inputField?.ActivateInputField();
                    return;
                }

                // 성공 메시지
                if (!string.IsNullOrEmpty(message))
                {
                    UpdateValidationText(message, new Color(0.3f, 1f, 0.3f));
                }

                isProcessing = false;
                SetButtonsInteractable(true);
            }

            // 확인 콜백 호출
            onConfirmed?.Invoke(input);

            // 잠시 후 팝업 닫기
            await Task.Delay(300);
            Hide();
        }
        #endregion

        #region UI Helpers
        /// <summary>
        /// 유효성 검사 텍스트 업데이트
        /// </summary>
        private void UpdateValidationText(string message, Color color)
        {
            if (validationText != null)
            {
                validationText.text = message;
                validationText.color = color;
            }
        }

        /// <summary>
        /// 버튼 상호작용 설정
        /// </summary>
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

        #region Animations
        /// <summary>
        /// 표시 애니메이션
        /// </summary>
        private void PlayShowAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                return;
            }

            // 초기 상태
            canvasGroup.alpha = 0f;
            popupRect.localScale = Vector3.one * 0.9f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence showSequence = DOTween.Sequence();

            // Fade In
            showSequence.Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));

            // Scale Up
            showSequence.Join(popupRect.DOScale(1f, showDuration).SetEase(Ease.OutBack));

            // 완료 시 상호작용 활성화
            showSequence.OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });

            showTween = showSequence;
        }

        /// <summary>
        /// 숨김 애니메이션
        /// </summary>
        private void PlayHideAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // 상호작용 비활성화
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence hideSequence = DOTween.Sequence();

            // Fade Out
            hideSequence.Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));

            // Scale Down
            hideSequence.Join(popupRect.DOScale(0.9f, hideDuration).SetEase(Ease.InBack));

            // 완료 시 비활성화
            hideSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

            hideTween = hideSequence;
        }
        #endregion
    }
}

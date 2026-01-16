using System;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utills;

namespace UI.Shared
{
    /// <summary>
    /// 비밀번호 찾기 팝업 UI
    /// 이메일 입력 → 비밀번호 재설정 이메일 발송
    /// </summary>
    public class FindPasswordPopupUI : MonoBehaviour, ICloseable
    {
        #region Fields
        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInputField;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI sendButtonText;
        [SerializeField] private TextMeshProUGUI cancelButtonText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;

        // 상태 관리
        private bool isEmailSent = false;
        private CooldownTimer sendCooldown = new CooldownTimer(60f);

        private bool isProcessing = false;
        private bool isAnimating = false;
        private Tween showTween;
        private Tween hideTween;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 버튼 이벤트 등록
            sendButton?.onClick.AddListener(OnSendButtonClicked);
            cancelButton?.onClick.AddListener(OnCancelButtonClicked);

            // 초기 상태: 숨김
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // KeyboardManager에 InputField 등록
            RegisterInputFieldsToKeyboardManager();

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Tween 정리
            showTween?.Kill();
            hideTween?.Kill();

            sendButton?.onClick.RemoveListener(OnSendButtonClicked);
            cancelButton?.onClick.RemoveListener(OnCancelButtonClicked);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 팝업 표시
        /// </summary>
        public void Show()
        {
            // 상태 초기화
            ResetState();

            // 활성화
            gameObject.SetActive(true);

            // UIStackManager에 등록
            UIStackManager.Instance?.Push(this);

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

            // UIStackManager에서 제거
            UIStackManager.Instance?.Pop();

            // 애니메이션
            PlayHideAnimation();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);
        }
        #endregion

        #region Button Events
        /// <summary>
        /// 발송/재발송 버튼 클릭
        /// </summary>
        private async void OnSendButtonClicked()
        {
            // Step 1: 이메일 입력 확인
            string email = emailInputField?.text.Trim() ?? "";

            // Step 2: 이메일 형식 검증
            var (isValidEmail, emailError) = Global.ValidateEmail(email);
            if (!isValidEmail)
            {
                ShowError(emailError);
                return;
            }

            // Step 3: 쿨다운 체크 (재발송인 경우)
            if (isEmailSent && sendCooldown.IsActive)
            {
                int remainingSeconds = sendCooldown.GetRemainingSeconds();

                // ConfirmationPopup으로 쿨다운 안내
                ConfirmationPopup.Show(
                    $"재발송은 {remainingSeconds}초 후에\n다시 시도할 수 있습니다.",
                    onConfirm: null,
                    onCancel: null,
                    confirmText: "확인",
                    cancelText: null
                );
                return;
            }

            // Step 4: 이메일 발송
            await SendPasswordResetEmail(email);
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void OnCancelButtonClicked()
        {
            Hide();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 상태 초기화
        /// </summary>
        private void ResetState()
        {
            // 입력 필드 비우기
            if (emailInputField != null)
            {
                emailInputField.text = "";
                emailInputField.ActivateInputField();
            }

            // 에러 텍스트 비우기
            if (errorText != null)
            {
                errorText.text = "";
                errorText.color = Global.GlowRed; // 기본 색상으로 복원
            }

            // 버튼 텍스트 초기화
            if (sendButtonText != null)
            {
                sendButtonText.text = "발송";
            }

            if (cancelButtonText != null)
            {
                cancelButtonText.text = "취소";
            }

            // 발송 상태 초기화
            isEmailSent = false;
            sendCooldown.Reset();
        }

        /// <summary>
        /// 비밀번호 재설정 이메일 발송
        /// </summary>
        private async Task SendPasswordResetEmail(string email)
        {
            if (isProcessing) return;

            isProcessing = true;

            // 버튼 비활성화 (중복 클릭 방지)
            if (sendButton != null) sendButton.interactable = false;
            if (cancelButton != null) cancelButton.interactable = false;

            // 처리 중 표시
            if (errorText != null)
            {
                errorText.text = "처리 중...";
                errorText.color = Global.Processing;
            }

            try
            {
                // Firebase 초기화 확인
                if (!Manager.AuthManager.Instance.IsInitialized)
                {
                    bool authReady = await Manager.AuthManager.Instance.WaitForInitialization(10f);
                    if (!authReady)
                    {
                        ShowError("Firebase 초기화 실패");
                        return;
                    }
                }

                // Firestore 이메일 등록 확인
                bool isRegistered = await Manager.DatabaseManager.Instance.IsEmailRegistered(email);
                if (!isRegistered)
                {
                    ShowError("가입되지 않은 이메일입니다");
                    return;
                }

                // AuthManager를 통해 이메일 발송
                var result = await Manager.AuthManager.Instance.SendPasswordResetEmail(email);

                if (result.success)
                {
                    // 성공 처리
                    isEmailSent = true;
                    sendCooldown.Start();

                    if (sendButtonText != null)
                    {
                        sendButtonText.text = "재발송";
                    }

                    // CancelButton 텍스트를 "닫기"로 변경
                    if (cancelButtonText != null)
                    {
                        cancelButtonText.text = "닫기";
                    }

                    // errorText를 안내 메시지로 변경
                    if (errorText != null)
                    {
                        errorText.text = "비밀번호를 재설정하면 창을 닫고\n다시 로그인을 진행해주세요";
                        errorText.color = Global.Purple;
                    }

                    // ConfirmationPopup으로 성공 안내
                    ConfirmationPopup.Show(
                        $"비밀번호 재설정 이메일이\n'{email}'\n(으)로 발송되었습니다.\n\n" +
                        "이메일을 확인하여\n비밀번호를 재설정해주세요.",
                        onConfirm: () => { },
                        onCancel: null,
                        confirmText: "확인",
                        cancelText: null
                    );
                }
                else
                {
                    // 실패 처리
                    ShowError(result.message);
                }
            }
            finally
            {
                isProcessing = false;

                // 버튼 재활성화
                if (sendButton != null) sendButton.interactable = true;
                if (cancelButton != null) cancelButton.interactable = true;
            }
        }


        /// <summary>
        /// 에러 메시지 표시
        /// </summary>
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.color = Global.GlowRed;
            }

            Debug.LogWarning($"[FindPasswordPopupUI] {message}");
        }
        #endregion

        #region ICloseable Implementation
        /// <summary>
        /// UIStackManager에서 호출 (ESC 키 등)
        /// </summary>
        public void Close()
        {
            OnCancelButtonClicked();
        }

        /// <summary>
        /// 닫기 가능 여부 확인
        /// </summary>
        public bool CanClose()
        {
            return !isAnimating;
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
            if (keyboardManager != null && emailInputField != null)
            {
                keyboardManager.RegisterInputFieldRuntime(emailInputField);
            }
#endif
        }
        #endregion

        #region Animations
        /// <summary>
        /// 표시 애니메이션 (Scale 0.9 → 1.0 + Fade In)
        /// </summary>
        private void PlayShowAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                return;
            }

            // 애니메이션 시작
            isAnimating = true;

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
                isAnimating = false;
            });

            showTween = showSequence;
        }

        /// <summary>
        /// 숨김 애니메이션 (Scale 1.0 → 0.9 + Fade Out)
        /// </summary>
        private void PlayHideAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // 애니메이션 시작
            isAnimating = true;

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
                isAnimating = false;
            });

            hideTween = hideSequence;
        }
        #endregion
    }
}

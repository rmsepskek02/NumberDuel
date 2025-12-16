using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared
{
    /// <summary>
    /// Google 계정에 비밀번호 연동 팝업 UI
    /// (Google으로 로그인한 사용자가 PC에서도 플레이할 수 있도록)
    /// </summary>
    public class LinkPasswordPopupUI : MonoBehaviour, ICloseable
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInputField;           // 읽기 전용 (Google 이메일 고정)
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField confirmPasswordInputField;
        [SerializeField] private Button linkButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;

        private Action<bool> onCompleteCallback;
        private bool isProcessing = false;
        private bool isAnimating = false;
        private Tween showTween;
        private Tween hideTween;

        private void Awake()
        {
            // 버튼 이벤트 등록
            linkButton?.onClick.AddListener(OnLinkButtonClicked);
            cancelButton?.onClick.AddListener(OnCancelButtonClicked);

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

            linkButton?.onClick.RemoveListener(OnLinkButtonClicked);
            cancelButton?.onClick.RemoveListener(OnCancelButtonClicked);
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        /// <param name="googleEmail">Google 계정 이메일 (수정 불가)</param>
        /// <param name="onComplete">완료 콜백</param>
        public void Show(string googleEmail, Action<bool> onComplete)
        {
            onCompleteCallback = onComplete;

            // Google 이메일 설정 (읽기 전용)
            if (emailInputField != null)
            {
                emailInputField.text = googleEmail;
                emailInputField.interactable = false; // 수정 불가

                // 시각적으로 비활성화 표시 (회색)
                var colors = emailInputField.colors;
                colors.normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                emailInputField.colors = colors;
            }

            // 비밀번호 입력 필드 초기화
            if (passwordInputField != null)
                passwordInputField.text = string.Empty;

            if (confirmPasswordInputField != null)
                confirmPasswordInputField.text = string.Empty;

            if (errorText != null)
                errorText.text = string.Empty;

            isProcessing = false;

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

        /// <summary>
        /// 연동 버튼 클릭
        /// </summary>
        private async void OnLinkButtonClicked()
        {
            if (isProcessing)
                return;

            isProcessing = true;

            // 비밀번호 입력값 가져오기
            string password = passwordInputField?.text ?? string.Empty;
            string confirmPassword = confirmPasswordInputField?.text ?? string.Empty;

            // 유효성 검증
            if (!ValidatePassword(password, confirmPassword))
            {
                isProcessing = false;
                return;
            }

            // 계정 연동 시도
            var result = await Manager.AuthManager.Instance.LinkPasswordToGoogle(password);

            if (result.success)
            {
                // 성공: 완료 팝업
                ConfirmationPopup.Show(
                    "✅ PC 로그인 연동 완료!\n\n" +
                    "이제 PC에서도\n" +
                    $"{emailInputField.text}으로\n" +
                    "로그인할 수 있습니다.",
                    onConfirm: () =>
                    {
                        Hide();
                        onCompleteCallback?.Invoke(true);
                    }
                );
            }
            else
            {
                // 실패: 에러 메시지 표시
                ShowError(result.message);
            }

            isProcessing = false;
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void OnCancelButtonClicked()
        {
            Hide();
            onCompleteCallback?.Invoke(false);
        }

        /// <summary>
        /// 비밀번호 유효성 검증
        /// </summary>
        private bool ValidatePassword(string password, string confirmPassword)
        {
            // 비밀번호 확인
            if (string.IsNullOrEmpty(password))
            {
                ShowError("비밀번호를 입력해주세요.");
                return false;
            }

            if (password.Length < 6)
            {
                ShowError("비밀번호는 최소 6자 이상이어야 합니다.");
                return false;
            }

            // 비밀번호 확인 일치
            if (password != confirmPassword)
            {
                ShowError("비밀번호가 일치하지 않습니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 에러 메시지 표시
        /// </summary>
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.color = Color.red;
            }

            Debug.LogWarning($"[LinkPasswordPopupUI] {message}");
        }

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

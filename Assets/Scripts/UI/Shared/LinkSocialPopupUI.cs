using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared
{
    /// <summary>
    /// SNS 계정 연동 팝업 UI
    /// 이메일 계정 사용자가 SNS 계정을 추가로 연동
    /// </summary>
    public class LinkSocialPopupUI : MonoBehaviour, ICloseable
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button googleButton;           // Google 연동 버튼
        [SerializeField] private Button kakaoButton;            // Kakao 연동 버튼 (향후 구현)
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;

        private string currentEmail;
        private Action<bool> onCompleteCallback;
        private bool isProcessing = false;
        private bool isAnimating = false;
        private Tween showTween;
        private Tween hideTween;

        private void Awake()
        {
            // 버튼 이벤트 등록
            googleButton?.onClick.AddListener(OnGoogleButtonClicked);
            kakaoButton?.onClick.AddListener(OnKakaoButtonClicked);
            closeButton?.onClick.AddListener(OnCloseButtonClicked);

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

            googleButton?.onClick.RemoveListener(OnGoogleButtonClicked);
            kakaoButton?.onClick.RemoveListener(OnKakaoButtonClicked);
            closeButton?.onClick.RemoveListener(OnCloseButtonClicked);
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        /// <param name="email">현재 계정 이메일</param>
        /// <param name="onComplete">완료 콜백</param>
        public void Show(string email, Action<bool> onComplete)
        {
            currentEmail = email;
            onCompleteCallback = onComplete;

            // 타이틀 및 설명 표시
            if (titleText != null)
                titleText.text = "SNS 계정 연동";

            if (descriptionText != null)
                descriptionText.text = $"현재 이메일: {email}\n\nSNS 계정의 이메일이 일치해야 연동됩니다.";

            if (errorText != null)
                errorText.text = string.Empty;

            // Kakao 버튼 비활성화 (향후 구현)
            if (kakaoButton != null)
            {
                kakaoButton.interactable = false;

                // "준비 중" 표시
                var kakaoText = kakaoButton.GetComponentInChildren<TextMeshProUGUI>();
                if (kakaoText != null)
                    kakaoText.text = "Kakao (준비 중)";
            }

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
        /// Google 버튼 클릭
        /// </summary>
        private async void OnGoogleButtonClicked()
        {
            if (isProcessing)
                return;

            isProcessing = true;

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);

            // Google 연동 확인 팝업
            ConfirmationPopup.Show(
                $"Google 계정을 연동하시겠습니까?\n\n" +
                $"현재 이메일: {currentEmail}\n\n" +
                $"Google 계정의 이메일이\n일치해야 연동됩니다.",
                onConfirm: async () =>
                {
                    // Google 연동 시도
                    var result = await Manager.AuthManager.Instance.LinkGoogleToEmail();

                    if (result.success)
                    {
                        // 성공
                        ConfirmationPopup.Show(
                            "✅ Google 계정 연동 완료!\n\n" +
                            "이제 모바일에서도\n" +
                            "Google 간편 로그인을 사용할 수 있습니다.",
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
                        isProcessing = false;
                    }
                },
                onCancel: () =>
                {
                    isProcessing = false;
                }
            );
        }

        /// <summary>
        /// Kakao 버튼 클릭 (향후 구현)
        /// </summary>
        private void OnKakaoButtonClicked()
        {
            ShowError("Kakao 연동은 준비 중입니다.");

            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void OnCloseButtonClicked()
        {
            Hide();
            onCompleteCallback?.Invoke(false);
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

            Debug.LogWarning($"[LinkSocialPopupUI] {message}");
        }

        #region ICloseable Implementation
        /// <summary>
        /// UIStackManager에서 호출 (ESC 키 등)
        /// </summary>
        public void Close()
        {
            OnCloseButtonClicked();
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

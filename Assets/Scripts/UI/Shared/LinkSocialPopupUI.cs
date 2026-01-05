using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared
{
    /// <summary>
    /// SNS 계정 연동 팝업 UI
    /// - 게스트: SNS 또는 이메일로 계정 전환
    /// - 이메일 계정 사용자: SNS 계정을 추가로 연동
    /// </summary>
    public class LinkSocialPopupUI : MonoBehaviour, ICloseable
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button emailButton;            // 이메일 연동 버튼 (게스트 전용)
        [SerializeField] private Button googleButton;           // Google 연동 버튼
        [SerializeField] private Button kakaoButton;            // Kakao 연동 버튼 (향후 구현)
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;

        private bool isGuestMode = false;                       // 게스트 모드 플래그
        private string displayText;                             // 표시 텍스트 ("손님 계정" 또는 실제 이메일)
        private Action<bool> onCompleteCallback;
        private bool isProcessing = false;
        private bool isAnimating = false;
        private Tween showTween;
        private Tween hideTween;

        private void Awake()
        {
            // 버튼 이벤트는 Unity 에디터에서 수동으로 연결

            // 초기 상태: 숨김
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            // 계정 연동 이벤트 구독
            if (Manager.AuthManager.Instance != null)
            {
                Manager.AuthManager.Instance.OnAccountLinked += OnAccountLinked;
            }
        }

        private void OnDisable()
        {
            // 계정 연동 이벤트 구독 해제
            if (Manager.AuthManager.Instance != null)
            {
                Manager.AuthManager.Instance.OnAccountLinked -= OnAccountLinked;
            }
        }

        private void OnDestroy()
        {
            // Tween 정리
            showTween?.Kill();
            hideTween?.Kill();

            // 이벤트 구독 해제 (안전장치)
            if (Manager.AuthManager.Instance != null)
            {
                Manager.AuthManager.Instance.OnAccountLinked -= OnAccountLinked;
            }
        }

        /// <summary>
        /// 계정 연동 완료 이벤트 핸들러
        /// AuthManager의 OnAccountLinked 이벤트 발생 시 호출되어 UI를 갱신
        /// </summary>
        private void OnAccountLinked()
        {
            // 팝업이 현재 표시 중일 때만 UI 갱신
            if (!gameObject.activeSelf)
                return;

            RefreshAccountInfo();
        }

        /// <summary>
        /// 계정 정보를 갱신하여 UI 업데이트
        /// </summary>
        private void RefreshAccountInfo()
        {
            if (Manager.AuthManager.Instance == null)
                return;

            // 현재 계정 정보 가져오기
            displayText = Manager.AuthManager.Instance.CurrentUserEmail ?? "손님 계정";
            isGuestMode = Manager.AuthManager.Instance.IsAnonymous;
            bool hasEmailProvider = Manager.AuthManager.Instance.IsEmailOnlyAccount();

            // UI 텍스트 업데이트
            if (isGuestMode)
            {
                // 게스트 모드
                if (titleText != null)
                    titleText.text = "계정 연동하기";

                if (descriptionText != null)
                    descriptionText.text = $"현재: {displayText}\n\nSNS 또는 이메일로 계정을 연동하면\n게임 데이터를 안전하게 보관할 수 있습니다.";

                // 이메일 버튼 표시
                if (emailButton != null)
                    emailButton.gameObject.SetActive(true);
            }
            else
            {
                // 이메일 계정 또는 SNS 계정 모드
                if (titleText != null)
                    titleText.text = hasEmailProvider ? "SNS 계정 연동" : "계정 연동하기";

                if (descriptionText != null)
                {
                    if (hasEmailProvider)
                    {
                        descriptionText.text = $"현재 이메일: {displayText}\n\nSNS 계정을 추가로 연동할 수 있습니다.";
                    }
                    else
                    {
                        descriptionText.text = $"현재: {displayText}\n\n이메일 또는 다른 SNS로 계정을 연동하면\n게임 데이터를 안전하게 보관할 수 있습니다.";
                    }
                }

                // 이메일 provider가 없는 경우(SNS 전용 계정) 이메일 버튼 표시
                if (emailButton != null)
                    emailButton.gameObject.SetActive(!hasEmailProvider);
            }

            // 에러 텍스트 초기화
            if (errorText != null)
                errorText.text = string.Empty;

            Debug.Log($"[LinkSocialPopupUI] RefreshAccountInfo - displayText: {displayText}, isGuestMode: {isGuestMode}, hasEmailProvider: {hasEmailProvider}");
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        /// <param name="displayText">표시할 텍스트 (이메일 계정: 실제 이메일, 게스트: "손님 계정")</param>
        /// <param name="isGuestMode">게스트 모드 여부</param>
        /// <param name="onComplete">완료 콜백</param>
        public void Show(string displayText, bool isGuestMode, Action<bool> onComplete)
        {
            this.displayText = displayText;
            this.isGuestMode = isGuestMode;
            onCompleteCallback = onComplete;

            // 이메일 provider 여부 확인
            bool hasEmailProvider = Manager.AuthManager.Instance.IsEmailOnlyAccount();

            // UI 텍스트 업데이트
            if (isGuestMode)
            {
                // 게스트 모드
                if (titleText != null)
                    titleText.text = "계정 연동하기";

                if (descriptionText != null)
                    descriptionText.text = $"현재: {displayText}\n\nSNS 또는 이메일로 계정을 연동하면\n게임 데이터를 안전하게 보관할 수 있습니다.";

                // 이메일 버튼 표시
                if (emailButton != null)
                    emailButton.gameObject.SetActive(true);
            }
            else
            {
                // 이메일 계정 또는 SNS 계정 모드
                if (titleText != null)
                    titleText.text = hasEmailProvider ? "SNS 계정 연동" : "계정 연동하기";

                if (descriptionText != null)
                {
                    if (hasEmailProvider)
                    {
                        descriptionText.text = $"현재 이메일: {displayText}\n\nSNS 계정을 추가로 연동할 수 있습니다.";
                    }
                    else
                    {
                        descriptionText.text = $"현재: {displayText}\n\n이메일 또는 다른 SNS로 계정을 연동하면\n게임 데이터를 안전하게 보관할 수 있습니다.";
                    }
                }

                // 이메일 provider가 없는 경우(SNS 전용 계정) 이메일 버튼 표시
                if (emailButton != null)
                    emailButton.gameObject.SetActive(!hasEmailProvider);
            }

            // 공통: Kakao 버튼 비활성화
            if (kakaoButton != null)
            {
                kakaoButton.interactable = false;

                var kakaoText = kakaoButton.GetComponentInChildren<TextMeshProUGUI>();
                if (kakaoText != null)
                    kakaoText.text = "Kakao (준비 중)";
            }

            // 공통: PC/Editor에서는 Google 버튼 비활성화
            #if !UNITY_ANDROID
            if (googleButton != null)
            {
                googleButton.interactable = false;

                var googleText = googleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (googleText != null)
                    googleText.text = "Google (모바일 전용)";
            }
            #endif

            // 에러 텍스트 초기화
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
        /// 이메일 버튼 클릭 (게스트/SNS 계정 → 이메일 계정 연동)
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public void OnEmailButtonClicked()
        {
            if (isProcessing)
                return;

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);

            // 이미 이메일 provider가 있는지 확인
            if (Manager.AuthManager.Instance.IsEmailOnlyAccount())
            {
                ShowError("이미 이메일 계정이 연동되어 있습니다.");
                return;
            }

            // LinkEmailPopupManager를 통해 이메일 연동 팝업 표시
            LinkEmailPopupManager.Show((success) =>
            {
                if (success)
                {
                    // 이메일 연동 성공
                    Hide();
                    onCompleteCallback?.Invoke(true);
                }
                else
                {
                    // 이메일 연동 취소 또는 실패
                    isProcessing = false;
                }
            });
        }

        /// <summary>
        /// Google 버튼 클릭 (Android만 지원)
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public async void OnGoogleButtonClicked()
        {
            #if !UNITY_ANDROID
            // PC/Editor에서는 동작하지 않음 (버튼이 비활성화되어 있음)
            ShowError("Google 연동은 모바일 앱에서만 가능합니다.");
            return;
            #else

            if (isProcessing)
                return;

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);

            isProcessing = true;

            if (isGuestMode)
            {
                // ===== 게스트 → Google 전환 (Android만) =====
                ConfirmationPopup.Show(
                    $"Google 계정으로 전환하시겠습니까?\n\n" +
                    $"게스트 계정의 데이터가\n" +
                    $"Google 계정으로 이전됩니다.",
                    onConfirm: async () =>
                    {
                        // AuthManager의 게스트 → Google 전환 메서드 호출
                        var result = await Manager.AuthManager.Instance.ConvertGuestToGoogle();

                        if (result.success)
                        {
                            // 성공: 닉네임 입력 팝업 표시
                            InputFieldPopup.ShowNicknameInput(
                                onConfirm: async (nickname) =>
                                {
                                    // 닉네임을 Firebase에 저장
                                    string uid = Manager.AuthManager.Instance.CurrentUserUID;
                                    bool nicknameUpdated = await Manager.DatabaseManager.Instance.UpdateNickname(uid, nickname);

                                    if (nicknameUpdated)
                                    {
                                        // Firebase User의 DisplayName도 업데이트
                                        var user = Manager.AuthManager.Instance.CurrentUser;
                                        if (user != null)
                                        {
                                            var profile = new Firebase.Auth.UserProfile { DisplayName = nickname };
                                            await user.UpdateUserProfileAsync(profile);
                                        }

                                        // 성공 팝업
                                        ConfirmationPopup.Show(
                                            "Google 계정 전환 완료!\n\n" +
                                            $"닉네임: {nickname}\n\n" +
                                            "이제 Google 계정으로\n" +
                                            "안전하게 로그인할 수 있습니다.",
                                            onConfirm: () =>
                                            {
                                                Hide();
                                                onCompleteCallback?.Invoke(true);
                                            }
                                        );
                                    }
                                    else
                                    {
                                        ShowError("닉네임 저장에 실패했습니다.");
                                        isProcessing = false;
                                    }
                                },
                                onCancel: () =>
                                {
                                    // 닉네임 입력 취소 - 전환은 완료되었으므로 기본 닉네임으로 진행
                                    ConfirmationPopup.Show(
                                        "✅ Google 계정 전환 완료!\n\n" +
                                        "이제 Google 계정으로\n" +
                                        "안전하게 로그인할 수 있습니다.",
                                        onConfirm: () =>
                                        {
                                            Hide();
                                            onCompleteCallback?.Invoke(true);
                                        }
                                    );
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
            else
            {
                // ===== 이메일 계정 → Google 연동 (Android만) =====
                ConfirmationPopup.Show(
                    $"Google 계정을 연동하시겠습니까?\n\n" +
                    $"현재 이메일: {displayText}\n\n" +
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
            #endif
        }

        /// <summary>
        /// Kakao 버튼 클릭 (향후 구현)
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public void OnKakaoButtonClicked()
        {
            ShowError("Kakao 연동은 준비 중입니다.");

            Manager.SoundManager.Instance?.PlaySFX(Objects.SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public void OnCloseButtonClicked()
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

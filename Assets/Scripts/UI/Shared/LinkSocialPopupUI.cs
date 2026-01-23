using System;
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
    public class LinkSocialPopupUI : PopupBase<LinkSocialPopupUI>
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button emailButton;            // 이메일 연동 버튼 (게스트 전용)
        [SerializeField] private Button googleButton;           // Google 연동 버튼
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI errorText;

        #endregion

        #region Private Fields

        private bool isGuestMode = false;                       // 게스트 모드 플래그
        private string displayText;                             // 표시 텍스트 ("손님 계정" 또는 실제 이메일)
        private Action<bool> onCompleteCallback;
        private bool isProcessing = false;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            // 버튼 이벤트는 Unity 에디터에서 수동으로 연결
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 이벤트 구독 해제 (안전장치)
            if (Manager.AuthManager.Instance != null)
            {
                Manager.AuthManager.Instance.OnAccountLinked -= OnAccountLinked;
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// 팝업 표시
        /// </summary>
        /// <param name="displayText">표시할 텍스트 (이메일 계정: 실제 이메일, 게스트: "손님 계정")</param>
        /// <param name="isGuestMode">게스트 모드 여부</param>
        /// <param name="onComplete">완료 콜백</param>
        public static void Show(string displayText, bool isGuestMode, Action<bool> onComplete)
        {
            if (instance == null && !LoadPopup())
                return;

            instance.ShowInternal(displayText, isGuestMode, onComplete);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 팝업 표시 (Instance)
        /// </summary>
        private void ShowInternal(string displayText, bool isGuestMode, Action<bool> onComplete)
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
            RegisterToUIStack();

            // 애니메이션
            PlayShowAnimation();

            // 사운드 재생
            PlayClickSound();
        }

        /// <summary>
        /// 팝업 숨기기 내부 구현
        /// </summary>
        protected override void HideInternal()
        {
            base.HideInternal();
        }

        /// <summary>
        /// 계정 연동 완료 이벤트 핸들러
        /// AuthManager의 OnAccountLinked 이벤트 발생 시 호출되어 UI를 갱신
        /// </summary>
        private void OnAccountLinked()
        {
            // 팝업이 현재 표시 중일 때만 처리
            if (!gameObject.activeSelf)
                return;

            // 이메일 연동이 완료된 경우 팝업 닫기
            // (Google 연동은 기존 콜백 방식으로 처리되므로 여기서는 이메일만 처리)
            if (!isProcessing)
            {
                // 이메일 연동 완료로 인한 이벤트 → 팝업 닫기
                HideInternal();
                onCompleteCallback?.Invoke(true);
            }
            else
            {
                // 처리 중이면 UI만 갱신 (Google 연동 중)
                RefreshAccountInfo();
            }
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
        /// 에러 메시지 표시
        /// </summary>
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.color = Global.GlowRed;
            }

            Debug.LogWarning($"[LinkSocialPopupUI] {message}");
        }

        #endregion

        #region Button Events

        /// <summary>
        /// 이메일 버튼 클릭 (게스트/SNS 계정 → 이메일 계정 연동)
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public void OnEmailButtonClicked()
        {
            if (isProcessing)
                return;

            // 사운드 재생
            PlayClickSound();

            // 이미 이메일 provider가 있는지 확인
            if (Manager.AuthManager.Instance.IsEmailOnlyAccount())
            {
                ShowError("이미 이메일 계정이 연동되어 있습니다.");
                return;
            }

            // LinkEmailPopupUI를 통해 이메일 연동 팝업 표시
            LinkEmailPopupUI.Show((success) =>
            {
                if (success)
                {
                    // 이메일 연동 성공
                    HideInternal();
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
            PlayClickSound();

            isProcessing = true;

            if (isGuestMode)
            {
                // ===== 게스트 → Google 전환 (Android만) =====
                ConfirmationPopupUI.Show(
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
                            InputFieldPopupUI.Show(
                                InputPopupType.Nickname,
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
                                        ConfirmationPopupUI.Show(
                                            "Google 계정 전환 완료!\n\n" +
                                            $"닉네임: {nickname}\n\n" +
                                            "이제 Google 계정으로\n" +
                                            "안전하게 로그인할 수 있습니다.",
                                            onConfirm: () =>
                                            {
                                                HideInternal();
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
                                    ConfirmationPopupUI.Show(
                                        "Google 계정 전환 완료!\n\n" +
                                        "이제 Google 계정으로\n" +
                                        "안전하게 로그인할 수 있습니다.",
                                        onConfirm: () =>
                                        {
                                            HideInternal();
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
                ConfirmationPopupUI.Show(
                    $"Google 계정을 연동하시겠습니까?\n\n" +
                    $"현재 이메일: <color=#{ColorUtility.ToHtmlStringRGB(Global.Purple)}>{displayText}</color>",
                    onConfirm: async () =>
                    {
                        // Google 연동 시도
                        var result = await Manager.AuthManager.Instance.LinkGoogleToEmail();

                        if (result.success)
                        {
                            // 성공
                            ConfirmationPopupUI.Show(
                                "Google 계정 연동 완료!\n\n" +
                                "이제 모바일에서도\n" +
                                "Google 간편 로그인을 사용할 수 있습니다.",
                                onConfirm: () =>
                                {
                                    HideInternal();
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
        /// 닫기 버튼 클릭
        /// Unity 에디터에서 이벤트 연결
        /// </summary>
        public void OnCloseButtonClicked()
        {
            HideInternal();
            onCompleteCallback?.Invoke(false);
        }

        #endregion

        #region ICloseable Override

        /// <summary>
        /// UIStackManager에서 호출 (ESC 키 등)
        /// </summary>
        public override void Close()
        {
            OnCloseButtonClicked();
        }

        #endregion
    }
}

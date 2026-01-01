using System;
using System.Threading.Tasks;
using DG.Tweening;
using Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    /// <summary>
    /// 닉네임 변경 입력 팝업
    /// 유효성 검사 및 Firebase 업데이트 포함
    /// </summary>
    public class NicknameChangePopupUI : MonoBehaviour
    {
        #region Fields and Properties
        [Header("UI References")]
        [SerializeField] private TMP_InputField nicknameInputField;
        [SerializeField] private TextMeshProUGUI validationText; // 유효성 검사 메시지
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;

        [Header("Validation Settings")]
        [SerializeField] private int minNicknameLength = 1;
        [SerializeField] private int maxNicknameLength = 12;

        private Action<string> onNicknameChanged; // 닉네임 변경 완료 콜백
        private Tween showTween;
        private Tween hideTween;
        private bool isProcessing = false; // 처리 중 플래그 (중복 클릭 방지)
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 버튼 이벤트 등록
            cancelButton?.onClick.AddListener(OnCancelClicked);
            confirmButton?.onClick.AddListener(OnConfirmClicked);

            // 입력 필드 이벤트 등록
            nicknameInputField?.onValueChanged.AddListener(OnInputValueChanged);

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
            cancelButton?.onClick.RemoveListener(OnCancelClicked);
            confirmButton?.onClick.RemoveListener(OnConfirmClicked);
            nicknameInputField?.onValueChanged.RemoveListener(OnInputValueChanged);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 닉네임 변경 팝업 표시
        /// </summary>
        /// <param name="onChanged">닉네임 변경 완료 시 호출할 콜백</param>
        public void Show(Action<string> onChanged)
        {
            // 이전 Tween 정리
            hideTween?.Kill();

            // 콜백 저장
            onNicknameChanged = onChanged;

            // 입력 필드 초기화
            if (nicknameInputField != null)
            {
                nicknameInputField.text = string.Empty;
                nicknameInputField.Select();
                nicknameInputField.ActivateInputField();
            }

            // 유효성 메시지 초기화
            UpdateValidationText(string.Empty, Color.white);

            // 활성화
            gameObject.SetActive(true);

            // 애니메이션
            PlayShowAnimation();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 닉네임 변경 팝업 숨기기
        /// </summary>
        public void Hide()
        {
            // 이전 Tween 정리
            showTween?.Kill();

            // 애니메이션
            PlayHideAnimation();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
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

            Hide();
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private async void OnConfirmClicked()
        {
            if (isProcessing)
                return;

            string newNickname = nicknameInputField?.text.Trim() ?? string.Empty;

            // 유효성 검사
            if (!ValidateNickname(newNickname, out string errorMessage))
            {
                UpdateValidationText(errorMessage, Color.red);
                return;
            }

            // 처리 중 플래그 설정
            isProcessing = true;
            SetButtonsInteractable(false);

            // Firebase에 닉네임 업데이트
            bool success = await UpdateNicknameToFirebase(newNickname);

            if (success)
            {
                // 성공 메시지
                UpdateValidationText("닉네임이 변경되었습니다!", Color.green);

                // 콜백 호출
                onNicknameChanged?.Invoke(newNickname);

                // 잠시 후 팝업 닫기
                await Task.Delay(500);
                Hide();
            }
            else
            {
                // 실패 메시지
                UpdateValidationText("닉네임 변경에 실패했습니다. 다시 시도해주세요.", Color.red);
            }

            // 처리 중 플래그 해제
            isProcessing = false;
            SetButtonsInteractable(true);
        }

        /// <summary>
        /// 입력 필드 값 변경 시
        /// </summary>
        private void OnInputValueChanged(string value)
        {
            // 실시간 유효성 검사 (선택적)
            // 필요하면 여기서 ValidateNickname 호출
        }
        #endregion

        #region Validation
        /// <summary>
        /// 닉네임 유효성 검사
        /// </summary>
        private bool ValidateNickname(string nickname, out string errorMessage)
        {
            // 빈 문자열 확인
            if (string.IsNullOrWhiteSpace(nickname))
            {
                errorMessage = "닉네임을 입력해주세요.";
                return false;
            }

            // 길이 확인
            if (nickname.Length < minNicknameLength)
            {
                errorMessage = $"닉네임은 최소 {minNicknameLength}자 이상이어야 합니다.";
                return false;
            }

            if (nickname.Length > maxNicknameLength)
            {
                errorMessage = $"닉네임은 최대 {maxNicknameLength}자까지 가능합니다.";
                return false;
            }

            // 특수문자 확인 (한글, 영문, 숫자, 공백만 허용)
            foreach (char c in nickname)
            {
                bool isKorean = (c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㅎ') || (c >= 'ㅏ' && c <= 'ㅣ');
                bool isEnglish = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                bool isNumber = (c >= '0' && c <= '9');
                bool isSpace = c == ' ';

                if (!isKorean && !isEnglish && !isNumber && !isSpace)
                {
                    errorMessage = "닉네임은 한글, 영문, 숫자, 공백만 사용할 수 있습니다.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }
        #endregion

        #region Firebase Update
        /// <summary>
        /// Firebase에 닉네임 업데이트
        /// </summary>
        private async Task<bool> UpdateNicknameToFirebase(string newNickname)
        {
            try
            {
                // AuthManager 확인
                if (Manager.AuthManager.Instance == null || !Manager.AuthManager.Instance.IsLoggedIn)
                {
                    Debug.LogError("[NicknameChangePopupUI] 로그인 정보를 찾을 수 없습니다.");
                    return false;
                }

                // DatabaseManager 확인
                if (Manager.DatabaseManager.Instance == null)
                {
                    Debug.LogError("[NicknameChangePopupUI] DatabaseManager가 없습니다.");
                    return false;
                }

                string uid = Manager.AuthManager.Instance.CurrentUserUID;

                // Firebase에 닉네임 업데이트
                bool success = await Manager.DatabaseManager.Instance.UpdateNickname(uid, newNickname);

                if (success)
                {
                    return true;
                }
                else
                {
                    Debug.LogError("[NicknameChangePopupUI] 닉네임 업데이트 실패.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NicknameChangePopupUI] 닉네임 업데이트 중 오류: {ex.Message}");
                return false;
            }
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
            if (cancelButton != null)
                cancelButton.interactable = interactable;

            if (confirmButton != null)
                confirmButton.interactable = interactable;

            if (nicknameInputField != null)
                nicknameInputField.interactable = interactable;
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
        /// 숨김 애니메이션 (Scale 1.0 → 0.9 + Fade Out)
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

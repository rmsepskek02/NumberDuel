using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Objects;
using Manager;
using System.Collections;

namespace Objects
{
    /// <summary>
    /// 체력 UI 표시 및 애니메이션을 담당하는 컴포넌트
    /// Image의 fillAmount를 사용하여 체력바를 표현
    /// HealthManager의 이벤트를 구독하여 UI를 업데이트
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("플레이어 HP UI")]
        [SerializeField] private Image playerHealthFill;
        [SerializeField] private Image playerHealthBorder;
        [SerializeField] private TextMeshProUGUI playerHealthText;
        [SerializeField] private RectTransform playerHPBar;

        [Header("상대 HP UI")]
        [SerializeField] private Image opponentHealthFill;
        [SerializeField] private Image opponentHealthBorder;
        [SerializeField] private TextMeshProUGUI opponentHealthText;
        [SerializeField] private RectTransform opponentHPBar;

        [Header("애니메이션 설정")]
        [SerializeField] private float hpBarAnimDuration = 1.2f;
        [SerializeField] private float damageShakeDuration = 0.3f;
        [SerializeField] private float damageShakeStrength = 10f;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private Ease hpBarEaseType = Ease.OutCubic;

        [Header("글로우 효과 설정")]
        [SerializeField] private Color borderGlowColor = Color.yellow;
        [SerializeField] private float glowDuration = 0.8f;
        [SerializeField] private float glowIntensity = 1.5f;
        [SerializeField] private int glowPulseCount = 2;

        [Header("디버그 설정")]
        [SerializeField] private bool enableDebugLog = false;

        // 원본 색상 저장용
        private Color originalPlayerFillColor;
        private Color originalOpponentFillColor;
        private Color originalPlayerBorderColor;
        private Color originalOpponentBorderColor;

        #region Unity Lifecycle
        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // Fill Amount 초기 설정 (1.0 = 100% 체력)
            if (playerHealthFill != null)
                playerHealthFill.fillAmount = 1.0f;

            if (opponentHealthFill != null)
                opponentHealthFill.fillAmount = 1.0f;

            // 원본 색상 저장
            StoreOriginalColors();

            // 초기 텍스트 설정
            UpdateHealthText(CardZone.OwnerType.Player);
            UpdateHealthText(CardZone.OwnerType.Opponent);

            if (enableDebugLog)
                Debug.Log("[HealthUI] UI 초기화 완료");
        }

        /// <summary>
        /// 원본 색상 저장
        /// </summary>
        private void StoreOriginalColors()
        {
            if (playerHealthFill != null)
                originalPlayerFillColor = playerHealthFill.color;

            if (opponentHealthFill != null)
                originalOpponentFillColor = opponentHealthFill.color;

            if (playerHealthBorder != null)
                originalPlayerBorderColor = playerHealthBorder.color;

            if (opponentHealthBorder != null)
                originalOpponentBorderColor = opponentHealthBorder.color;
        }
        #endregion

        #region Event Subscription
        /// <summary>
        /// HealthManager 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            HealthManager.OnHealthChanged += OnHealthChanged;
            HealthManager.OnPlayerDefeated += OnPlayerDefeated;
        }

        /// <summary>
        /// HealthManager 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            HealthManager.OnHealthChanged -= OnHealthChanged;
            HealthManager.OnPlayerDefeated -= OnPlayerDefeated;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 체력 변경 이벤트 처리
        /// </summary>
        /// <param name="player">체력이 변경된 플레이어</param>
        /// <param name="oldHP">이전 체력</param>
        /// <param name="newHP">새로운 체력</param>
        private void OnHealthChanged(CardZone.OwnerType player, int oldHP, int newHP)
        {
            if (enableDebugLog)
                Debug.Log($"[HealthUI] {player} 체력 변경: {oldHP} → {newHP}");

            // HP 바 애니메이션
            AnimateHealthBar(player, newHP);

            // 텍스트 업데이트
            UpdateHealthText(player);

            // 데미지를 받았다면 데미지 이펙트 재생
            if (newHP < oldHP)
            {
                PlayDamageEffect(player);
            }
        }

        /// <summary>
        /// 플레이어 패배 이벤트 처리
        /// </summary>
        /// <param name="defeatedPlayer">패배한 플레이어</param>
        private void OnPlayerDefeated(CardZone.OwnerType defeatedPlayer)
        {
            if (enableDebugLog)
                Debug.Log($"[HealthUI] {defeatedPlayer} 패배 처리");

            // 패배 이펙트 재생
            PlayDefeatEffect(defeatedPlayer);

            // 핵심: HP 바 애니메이션이 완료될 때까지 대기 후 게임 종료
            StartCoroutine(WaitForAnimationThenEndGame(defeatedPlayer));
        }

        /// <summary>
        /// HP 바 애니메이션 완료 대기 후 게임 종료
        /// </summary>
        private IEnumerator WaitForAnimationThenEndGame(CardZone.OwnerType defeatedPlayer)
        {
            // HP 바 애니메이션 시간만큼 대기
            yield return new WaitForSeconds(hpBarAnimDuration);

            // 추가로 패배 이펙트 시간만큼 대기
            yield return new WaitForSeconds(0.5f);

            // 이제 게임 종료 처리
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameEnd(defeatedPlayer);
            }
        }
        #endregion

        #region UI Updates
        /// <summary>
        /// HP 바 애니메이션 (Image.fillAmount 사용)
        /// </summary>
        /// <param name="player">업데이트할 플레이어</param>
        /// <param name="newHP">새로운 체력</param>
        private void AnimateHealthBar(CardZone.OwnerType player, int newHP)
        {
            Image targetFillImage = GetFillImageByPlayer(player);
            if (targetFillImage == null) return;

            // 체력 비율 계산 (0.0 ~ 1.0)
            float targetFillAmount = (float)newHP / HealthManager.Instance.GetMaxHP();
            float currentFillAmount = targetFillImage.fillAmount;

            // 이미 같은 값이면 애니메이션 스킵
            if (Mathf.Approximately(currentFillAmount, targetFillAmount))
                return;

            // 체력이 감소하는 경우 더 부드러운 애니메이션
            bool isDecreasing = targetFillAmount < currentFillAmount;

            // 감소 시 더 긴 시간과 부드러운 이징 적용
            float animDuration = isDecreasing ? hpBarAnimDuration : hpBarAnimDuration * 0.7f;
            Ease easeType = isDecreasing ? hpBarEaseType : Ease.OutQuart;

            // DOTween으로 부드러운 fillAmount 애니메이션
            targetFillImage.DOFillAmount(targetFillAmount, animDuration)
                .SetEase(easeType)
                .OnStart(() => {
                    if (enableDebugLog)
                        Debug.Log($"[HealthUI] {player} HP 바 애니메이션 시작: {currentFillAmount:F2} → {targetFillAmount:F2}");
                })
                .OnComplete(() => {
                    if (enableDebugLog)
                        Debug.Log($"[HealthUI] {player} HP 바 애니메이션 완료");
                });
        }

        /// <summary>
        /// 체력 텍스트 업데이트
        /// </summary>
        /// <param name="player">업데이트할 플레이어</param>
        private void UpdateHealthText(CardZone.OwnerType player)
        {
            TextMeshProUGUI targetText = GetTextByPlayer(player);
            if (targetText == null) return;

            int currentHP = HealthManager.Instance.GetCurrentHP(player);
            int maxHP = HealthManager.Instance.GetMaxHP();

            targetText.text = $"{currentHP}/{maxHP}";
        }
        #endregion

        #region Damage Effects
        /// <summary>
        /// 데미지 이펙트 재생
        /// </summary>
        /// <param name="player">데미지를 받은 플레이어</param>
        private void PlayDamageEffect(CardZone.OwnerType player)
        {
            RectTransform targetBar = GetBarByPlayer(player);
            if (targetBar == null) return;

            // 흔들림 효과
            targetBar.DOShakePosition(damageShakeDuration, damageShakeStrength, 10, 90f, false, true);

            // 색상 플래시 효과
            PlayColorFlash(player);

            // 테두리 글로우 효과 (새로 추가)
            PlayBorderGlow(player);
        }

        /// <summary>
        /// 색상 플래시 효과
        /// </summary>
        /// <param name="player">이펙트를 적용할 플레이어</param>
        private void PlayColorFlash(CardZone.OwnerType player)
        {
            Image targetFillImage = GetFillImageByPlayer(player);
            if (targetFillImage == null) return;

            Color originalColor = player == CardZone.OwnerType.Player
                ? originalPlayerFillColor
                : originalOpponentFillColor;

            // HP 바 애니메이션과 동시에 실행되도록 타이밍 조정
            Sequence flashSequence = DOTween.Sequence();

            // 즉시 데미지 색상으로 변경
            flashSequence.Append(targetFillImage.DOColor(damageFlashColor, 0.15f));

            // HP 바 애니메이션과 함께 서서히 원래 색상으로 복귀
            flashSequence.Append(targetFillImage.DOColor(originalColor, hpBarAnimDuration * 0.8f)
                .SetEase(Ease.OutCubic));
        }

        /// <summary>
        /// 테두리 글로우 효과
        /// </summary>
        /// <param name="player">글로우 효과를 적용할 플레이어</param>
        private void PlayBorderGlow(CardZone.OwnerType player)
        {
            Image targetBorderImage = GetBorderImageByPlayer(player);
            if (targetBorderImage == null) return;

            Color originalBorderColor = player == CardZone.OwnerType.Player
                ? originalPlayerBorderColor
                : originalOpponentBorderColor;

            // 글로우 색상 준비 (원본 색상 + 글로우 색상 혼합)
            Color glowColor = Color.Lerp(originalBorderColor, borderGlowColor, glowIntensity);
            glowColor.a = Mathf.Clamp01(originalBorderColor.a + 0.8f); // 투명도 증가

            // 글로우 시퀀스 생성
            Sequence glowSequence = DOTween.Sequence();

            // 펄스 효과 (여러 번 깜빡이기)
            for (int i = 0; i < glowPulseCount; i++)
            {
                // 글로우 색상으로 빠르게 변경
                glowSequence.Append(targetBorderImage.DOColor(glowColor, glowDuration / (glowPulseCount * 4))
                    .SetEase(Ease.OutQuad));

                // 원본 색상으로 빠르게 복귀
                glowSequence.Append(targetBorderImage.DOColor(originalBorderColor, glowDuration / (glowPulseCount * 4))
                    .SetEase(Ease.InQuad));
            }

            // 최종적으로 원본 색상으로 완전히 복귀
            glowSequence.Append(targetBorderImage.DOColor(originalBorderColor, glowDuration / 4)
                .SetEase(Ease.OutCubic));

            if (enableDebugLog)
                Debug.Log($"[HealthUI] {player} 테두리 글로우 효과 재생 ({glowPulseCount}회 펄스)");
        }

        /// <summary>
        /// 패배 이펙트 재생
        /// </summary>
        /// <param name="player">패배한 플레이어</param>
        private void PlayDefeatEffect(CardZone.OwnerType player)
        {
            Image targetFillImage = GetFillImageByPlayer(player);
            RectTransform targetBar = GetBarByPlayer(player);

            if (targetFillImage != null)
            {
                // 패배 시 어두운 색상으로 변경
                targetFillImage.DOColor(Color.black, 0.5f);
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 플레이어별 Fill Image 반환
        /// </summary>
        private Image GetFillImageByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHealthFill : opponentHealthFill;
        }

        /// <summary>
        /// 플레이어별 Border Image 반환
        /// </summary>
        private Image GetBorderImageByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHealthBorder : opponentHealthBorder;
        }

        /// <summary>
        /// 플레이어별 텍스트 반환
        /// </summary>
        private TextMeshProUGUI GetTextByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHealthText : opponentHealthText;
        }

        /// <summary>
        /// 플레이어별 HP 바 RectTransform 반환
        /// </summary>
        private RectTransform GetBarByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHPBar : opponentHPBar;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 체력바를 수동으로 업데이트 (외부에서 호출 가능)
        /// </summary>
        /// <param name="player">업데이트할 플레이어</param>
        public void RefreshHealthUI(CardZone.OwnerType player)
        {
            if (HealthManager.Instance == null) return;

            int currentHP = HealthManager.Instance.GetCurrentHP(player);
            AnimateHealthBar(player, currentHP);
            UpdateHealthText(player);
        }

        /// <summary>
        /// 모든 체력 UI 새로고침
        /// </summary>
        public void RefreshAllHealthUI()
        {
            RefreshHealthUI(CardZone.OwnerType.Player);
            RefreshHealthUI(CardZone.OwnerType.Opponent);
        }
        #endregion

        #region Debug Methods
        /// <summary>
        /// 디버그용 체력 변경 테스트
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTestDamage(CardZone.OwnerType player, int damage)
        {
            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.ApplyDamage(damage, player);
            }
        }

        /// <summary>
        /// 디버그용 Fill Amount 직접 설정
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugSetFillAmount(CardZone.OwnerType player, float fillAmount)
        {
            Image targetFillImage = GetFillImageByPlayer(player);
            if (targetFillImage != null)
            {
                targetFillImage.fillAmount = Mathf.Clamp01(fillAmount);
                Debug.Log($"[HealthUI] DEBUG: {player} Fill Amount 설정 → {fillAmount}");
            }
        }

        /// <summary>
        /// 디버그용 애니메이션 테스트 (점진적 데미지)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTestGradualDamage(CardZone.OwnerType player, int totalDamage, int steps)
        {
            if (HealthManager.Instance == null) return;

            StartCoroutine(DebugGradualDamageCoroutine(player, totalDamage, steps));
        }

        /// <summary>
        /// 점진적 데미지 테스트 코루틴
        /// </summary>
        private System.Collections.IEnumerator DebugGradualDamageCoroutine(CardZone.OwnerType player, int totalDamage, int steps)
        {
            int damagePerStep = Mathf.Max(1, totalDamage / steps);

            for (int i = 0; i < steps; i++)
            {
                HealthManager.Instance.ApplyDamage(damagePerStep, player);
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log($"[HealthUI] DEBUG: {player}에게 {steps}단계로 총 {totalDamage} 데미지 테스트 완료");
        }

        /// <summary>
        /// 애니메이션 설정 런타임 변경 (테스트용)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugSetAnimationSettings(float duration, Ease easeType)
        {
            hpBarAnimDuration = duration;
            hpBarEaseType = easeType;
            Debug.Log($"[HealthUI] DEBUG: 애니메이션 설정 변경 - 시간: {duration}초, 이징: {easeType}");
        }

        /// <summary>
        /// 디버그용 글로우 효과 테스트
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTestGlowEffect(CardZone.OwnerType player)
        {
            PlayBorderGlow(player);
            Debug.Log($"[HealthUI] DEBUG: {player} 글로우 효과 테스트");
        }

        /// <summary>
        /// 디버그용 전체 데미지 이펙트 테스트
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTestFullDamageEffect(CardZone.OwnerType player)
        {
            PlayDamageEffect(player);
            Debug.Log($"[HealthUI] DEBUG: {player} 전체 데미지 이펙트 테스트");
        }

        /// <summary>
        /// 글로우 설정 런타임 변경 (테스트용)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugSetGlowSettings(Color glowColor, float duration, float intensity, int pulseCount)
        {
            borderGlowColor = glowColor;
            glowDuration = duration;
            glowIntensity = intensity;
            glowPulseCount = pulseCount;
            Debug.Log($"[HealthUI] DEBUG: 글로우 설정 변경 - 색상: {glowColor}, 시간: {duration}, 강도: {intensity}, 펄스: {pulseCount}");
        }

        /// <summary>
        /// 디버그 로그 활성화/비활성화
        /// </summary>
        public void SetDebugMode(bool enable) => enableDebugLog = enable;
        #endregion

        #region Public Methods
        /// <summary>
        /// UI 완전 초기화 (게임 재시작 시 사용)
        /// </summary>
        public void ResetUI()
        {
            // 진행 중인 모든 DOTween 애니메이션 중지
            if (playerHealthFill != null)
                playerHealthFill.DOKill();
            if (opponentHealthFill != null)
                opponentHealthFill.DOKill();
            if (playerHealthBorder != null)
                playerHealthBorder.DOKill();
            if (opponentHealthBorder != null)
                opponentHealthBorder.DOKill();
            if (playerHPBar != null)
                playerHPBar.DOKill();
            if (opponentHPBar != null)
                opponentHPBar.DOKill();

            // Fill Amount 초기화
            if (playerHealthFill != null)
                playerHealthFill.fillAmount = 1.0f;
            if (opponentHealthFill != null)
                opponentHealthFill.fillAmount = 1.0f;

            // 색상 초기화
            if (playerHealthFill != null)
                playerHealthFill.color = originalPlayerFillColor;
            if (opponentHealthFill != null)
                opponentHealthFill.color = originalOpponentFillColor;
            if (playerHealthBorder != null)
                playerHealthBorder.color = originalPlayerBorderColor;
            if (opponentHealthBorder != null)
                opponentHealthBorder.color = originalOpponentBorderColor;

            // 스케일 초기화
            if (playerHPBar != null)
            {
                playerHPBar.localScale = Vector3.one;
                playerHPBar.anchoredPosition = Vector2.zero;
            }
            if (opponentHPBar != null)
            {
                opponentHPBar.localScale = Vector3.one;
                opponentHPBar.anchoredPosition = Vector2.zero;
            }

            // 텍스트 업데이트
            UpdateHealthText(CardZone.OwnerType.Player);
            UpdateHealthText(CardZone.OwnerType.Opponent);

            if (enableDebugLog)
                Debug.Log("[HealthUI] UI 완전 초기화 완료");
        }
        #endregion
    }
}
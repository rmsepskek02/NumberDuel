using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Objects;
using Manager;

namespace Objects
{
    /// <summary>
    /// 체력 UI 표시 및 애니메이션을 담당하는 컴포넌트
    /// Image의 fillAmount를 사용하여 체력바를 표시
    /// HealthManager의 이벤트를 구독하여 UI를 업데이트
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        #region Fields and Properties
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

        // 원본 색상 저장용
        private Color originalPlayerFillColor;
        private Color originalOpponentFillColor;
        private Color originalPlayerBorderColor;
        private Color originalOpponentBorderColor;
        #endregion

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
        private void OnHealthChanged(CardZone.OwnerType player, int oldHP, int newHP)
        {
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
        private void OnPlayerDefeated(CardZone.OwnerType defeatedPlayer)
        {
            // 패배 이펙트 재생
            PlayDefeatEffect(defeatedPlayer);

            // HP 바 애니메이션이 완료될 때까지 대기 후 게임 종료
            StartCoroutine(WaitForAnimationThenEndGame(defeatedPlayer));
        }

        /// <summary>
        /// HP 바 애니메이션 완료 대기 후 게임 종료
        /// </summary>
        private IEnumerator WaitForAnimationThenEndGame(CardZone.OwnerType defeatedPlayer)
        {
            // HP 바 애니메이션 시간만큼 대기
            yield return new WaitForSeconds(hpBarAnimDuration);

            // 추가적인 패배 이펙트 시간만큼 대기
            yield return new WaitForSeconds(0.5f);

            // 게임 종료 로직 처리
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameEnd(defeatedPlayer);
            }
        }
        #endregion

        #region UI Updates
        /// <summary>
        /// HP 바 애니메이션 (Image.fillAmount 보간)
        /// </summary>
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

            // 체력이 감소하는 경우 더 느리고 부드러운 애니메이션
            bool isDecreasing = targetFillAmount < currentFillAmount;

            // 감소 할 때 더 긴 시간과 느리고 부드러운 곡선 사용
            float animDuration = isDecreasing ? hpBarAnimDuration : hpBarAnimDuration * 0.7f;
            Ease easeType = isDecreasing ? hpBarEaseType : Ease.OutQuart;

            // DOTween으로 부드러운 fillAmount 애니메이션
            targetFillImage.DOFillAmount(targetFillAmount, animDuration)
                .SetEase(easeType);
        }

        /// <summary>
        /// 체력 텍스트 업데이트
        /// </summary>
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
        private void PlayDamageEffect(CardZone.OwnerType player)
        {
            RectTransform targetBar = GetBarByPlayer(player);
            if (targetBar == null) return;

            // 흔들림 효과
            targetBar.DOShakePosition(damageShakeDuration, damageShakeStrength, 10, 90f, false, true);

            // 색상 플래시 효과
            PlayColorFlash(player);

            // 테두리 글로우 효과
            PlayBorderGlow(player);
        }

        /// <summary>
        /// 색상 플래시 효과
        /// </summary>
        private void PlayColorFlash(CardZone.OwnerType player)
        {
            Image targetFillImage = GetFillImageByPlayer(player);
            if (targetFillImage == null) return;

            Color originalColor = player == CardZone.OwnerType.Player
                ? originalPlayerFillColor
                : originalOpponentFillColor;

            // HP 바 애니메이션과 동시에 진행되도록 타이밍 조정
            Sequence flashSequence = DOTween.Sequence();

            // 빠른 속도로 데미지 색상으로 변경
            flashSequence.Append(targetFillImage.DOColor(damageFlashColor, 0.15f));

            // HP 바 애니메이션과 함께 서서히 원래 색상으로 복귀
            flashSequence.Append(targetFillImage.DOColor(originalColor, hpBarAnimDuration * 0.8f)
                .SetEase(Ease.OutCubic));
        }

        /// <summary>
        /// 테두리 글로우 효과
        /// </summary>
        private void PlayBorderGlow(CardZone.OwnerType player)
        {
            Image targetBorderImage = GetBorderImageByPlayer(player);
            if (targetBorderImage == null) return;

            Color originalBorderColor = player == CardZone.OwnerType.Player
                ? originalPlayerBorderColor
                : originalOpponentBorderColor;

            // 글로우 색상 준비 (원본 색상 + 글로우 색상 혼합)
            Color glowColor = Color.Lerp(originalBorderColor, borderGlowColor, glowIntensity);
            glowColor.a = Mathf.Clamp01(originalBorderColor.a + 0.8f);

            // 글로우 펄스 시퀀스 생성
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

            // 최종적으로 원본 색상으로 복귀 확정
            glowSequence.Append(targetBorderImage.DOColor(originalBorderColor, glowDuration / 4)
                .SetEase(Ease.OutCubic));
        }

        /// <summary>
        /// 패배 이펙트 재생
        /// </summary>
        private void PlayDefeatEffect(CardZone.OwnerType player)
        {
            Image targetFillImage = GetFillImageByPlayer(player);
            if (targetFillImage != null)
            {
                targetFillImage.DOColor(Color.black, 0.5f);
            }
        }
        #endregion

        #region Utility Methods
        private Image GetFillImageByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHealthFill : opponentHealthFill;
        }

        private Image GetBorderImageByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHealthBorder : opponentHealthBorder;
        }

        private TextMeshProUGUI GetTextByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHealthText : opponentHealthText;
        }

        private RectTransform GetBarByPlayer(CardZone.OwnerType player)
        {
            return player == CardZone.OwnerType.Player ? playerHPBar : opponentHPBar;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 체력바를 강제로 업데이트 (외부에서 호출 가능)
        /// </summary>
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

        /// <summary>
        /// UI 완전 초기화 (게임 재시작 시 사용)
        /// </summary>
        public void ResetUI()
        {
            // 진행 중인 모든 DOTween 애니메이션 정지
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

            // 텍스트 업데이트
            UpdateHealthText(CardZone.OwnerType.Player);
            UpdateHealthText(CardZone.OwnerType.Opponent);
        }
        #endregion
    }
}

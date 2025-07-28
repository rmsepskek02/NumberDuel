using UnityEngine;
using Objects;
using Manager;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

namespace Objects
{
    /// <summary>
    /// 조커 카드 클릭 시 효과를 선택하는 UI를 제어한다.
    /// - 통합된 GLOW 상태 관리 시스템
    /// - Draw/Delete/Swap 조커 효과 처리
    /// - 사용 조건 검증 및 UI 활성화 관리
    /// - ResourcesManager 안전장치 추가
    /// </summary>
    public class JokerModeSelector : MonoBehaviour
    {
        #region Singleton
        private static JokerModeSelector instance;
        public static JokerModeSelector Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<JokerModeSelector>();
                return instance;
            }
        }
        #endregion

        #region Inspector Fields
        [Header("연결할 오브젝트")]
        [SerializeField] private GameObject dimBackground;
        [SerializeField] private GameObject cancelButton;
        [SerializeField] private JokerEffectOption drawOption;
        [SerializeField] private JokerEffectOption deleteOption;
        [SerializeField] private JokerEffectOption swapOption;

        [Header("효과 설명 텍스트")]
        [SerializeField] private TextMeshPro drawText;
        [SerializeField] private TextMeshPro deleteText;
        [SerializeField] private TextMeshPro swapText;

        [Header("애니메이션 설정")]
        [SerializeField] private float maxScale = 30f;
        [SerializeField] private float animDurationUI = 0.2f;
        #endregion

        #region Private Fields
        private Card selectedJokerCard;
        private ObjectMouseEvent bgClick;
        private JokerEffectType selectedEffect;
        private string spriteColorName;

        /// <summary>초기화 완료 여부</summary>
        private bool isInitialized = false;

        // GLOW 상태 저장용 (통합 관리)
        private Dictionary<Card, bool> savedGlowStates = new Dictionary<Card, bool>();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 안전한 초기화 시작
            StartCoroutine(SafeInitialization());
        }

        private void OnDisable()
        {
            if (bgClick != null)
                bgClick.OnClickReleased -= OnCancelPressed;
        }
        #endregion

        #region Safe Initialization
        /// <summary>
        /// 안전한 초기화 시퀀스
        /// ResourcesManager 준비 완료까지 대기 후 초기화
        /// </summary>
        private IEnumerator SafeInitialization()
        {
            Debug.Log("[JokerModeSelector] 안전한 초기화 시작");

            // ResourcesManager 기본 초기화 완료 대기
            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (ResourcesManager.Instance != null && ResourcesManager.Instance.IsBasicInitialized)
                {
                    Debug.Log("[JokerModeSelector] ResourcesManager 준비 완료");
                    break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout)
            {
                Debug.LogError("[JokerModeSelector] ResourcesManager 대기 시간 초과");
                yield break;
            }

            // 안전한 초기화 실행
            try
            {
                InitializeOptions();
                InitializeBackground();
                InitializeTexts();
                SafeInitializeSprites();
                SetUIActive(false);

                isInitialized = true;
                Debug.Log("[JokerModeSelector] 초기화 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JokerModeSelector] 초기화 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 안전한 스프라이트 초기화
        /// null 체크 및 예외 처리 포함
        /// </summary>
        private void SafeInitializeSprites()
        {
            try
            {
                // ResourcesManager 안전성 체크
                if (ResourcesManager.Instance == null)
                {
                    Debug.LogError("[JokerModeSelector] ResourcesManager 인스턴스가 없습니다.");
                    return;
                }

                // Player 스프라이트 안전하게 가져오기
                var playerSprite = ResourcesManager.Instance.GetPlayerSprite();
                if (playerSprite == null)
                {
                    Debug.LogError("[JokerModeSelector] Player 스프라이트를 가져올 수 없습니다.");
                    return;
                }

                spriteColorName = playerSprite.name;
                Debug.Log($"[JokerModeSelector] 스프라이트 색상 이름 설정: {spriteColorName}");

                UpdateOptionSprites();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JokerModeSelector] 스프라이트 초기화 중 오류: {ex.Message}");
            }
        }
        #endregion

        #region Basic Initialization
        private void InitializeOptions()
        {
            if (drawOption != null) drawOption.SetSelector(this);
            if (deleteOption != null) deleteOption.SetSelector(this);
            if (swapOption != null) swapOption.SetSelector(this);
        }

        private void InitializeBackground()
        {
            if (dimBackground != null)
            {
                bgClick = dimBackground.GetComponent<ObjectMouseEvent>();
                if (bgClick != null)
                    bgClick.OnClickReleased += OnCancelPressed;
            }
        }

        private void InitializeTexts()
        {
            if (drawText != null) drawText.text = "Draw\n2 Card";
            if (deleteText != null) deleteText.text = "Delete\nCard";
            if (swapText != null) swapText.text = "Swap\nCards";
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 조커 카드 효과 선택 UI를 표시
        /// </summary>
        public void Show(Card jokerCard)
        {
            // 초기화 완료 체크
            if (!isInitialized)
            {
                Debug.LogWarning("[JokerModeSelector] 아직 초기화가 완료되지 않았습니다.");
                return;
            }

            if (!ValidateJokerCard(jokerCard)) return;
            if (InGameManager.Instance.IsProcessing)
            {
                Debug.LogWarning($"[JokerModeSelector] {InGameManager.Instance.CurrentProcess} 진행 중이므로 조커 UI를 표시할 수 없습니다.");
                return;
            }

            selectedJokerCard = jokerCard;
            UpdateAvailableEffects();
            SetUIActive(true);
            PlayShowAnimation();
        }

        /// <summary>
        /// UI를 숨기고 내부 상태를 초기화
        /// </summary>
        public void Hide()
        {
            selectedJokerCard = null;
            SetUIActive(false);
        }

        /// <summary>
        /// 조커 효과가 선택되었을 때 호출됨
        /// </summary>
        public void OnJokerEffectSelected(JokerEffectType effectType)
        {
            if (selectedJokerCard == null) return;

            selectedEffect = effectType;
            SetUIActive(false); // UI만 숨기기 (상태는 유지)
            ExecuteJokerEffect();
        }

        /// <summary>
        /// 취소 버튼 클릭 시 호출
        /// </summary>
        public void OnCancelPressed()
        {
            Hide();
        }
        #endregion

        #region Effect Execution
        /// <summary>
        /// 선택된 조커 효과 실행
        /// </summary>
        private void ExecuteJokerEffect()
        {
            switch (selectedEffect)
            {
                case JokerEffectType.Draw:
                    ExecuteDrawEffect();
                    break;
                case JokerEffectType.Delete:
                    StartDeleteTargetSelection();
                    break;
                case JokerEffectType.Swap:
                    StartSwapTargetSelection();
                    break;
            }
        }

        /// <summary>
        /// 카드 드로우 효과 실행
        /// </summary>
        private void ExecuteDrawEffect()
        {
            InGameManager.Instance.StartProcess(GameProcessState.JokerDrawProcess);

            CardZone.OwnerType cardOwner = selectedJokerCard.CurrentOwnerType;
            InGameManager.Instance.DrawCardsToHand(2, cardOwner);

            InGameManager.Instance.EndProcess();
            EndJokerProcess();
            RemoveUsedJokerCard();

            // 네트워크 동기화 - Draw 조커 결과 전송
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Draw);
            }
            Hide();
        }

        /// <summary>
        /// 카드 삭제 대상 선택 시작
        /// </summary>
        private void StartDeleteTargetSelection()
        {
            InGameManager.Instance.StartProcess(GameProcessState.JokerDeleteProcess);
            StartJokerProcess(JokerEffectType.Delete);
            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.Delete, OnDeleteTargetSelected);
        }

        /// <summary>
        /// 카드 교환 대상 선택 시작
        /// </summary>
        private void StartSwapTargetSelection()
        {
            InGameManager.Instance.StartProcess(GameProcessState.JokerSwapProcess);
            StartJokerProcess(JokerEffectType.Swap);
            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapFirst, OnSwapFirstTargetSelected);
        }
        #endregion

        #region Effect Callbacks
        /// <summary>
        /// 삭제 대상이 선택되었을 때
        /// </summary>
        private void OnDeleteTargetSelected(Card target)
        {
            if (target == null) return;
            StartCoroutine(DeleteCardSequence(target));
        }

        /// <summary>
        /// 교환 첫 번째 대상이 선택되었을 때
        /// </summary>
        private void OnSwapFirstTargetSelected(Card firstTarget)
        {
            if (firstTarget == null) return;

            ClearAllGlow();
            ApplyGlowToOpponentCards();
            firstTarget.SetCardState(true, Color.cyan); // 선택됨 표시

            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapSecond,
                (secondTarget) => OnSwapSecondTargetSelected(firstTarget, secondTarget));
        }

        /// <summary>
        /// 교환 두 번째 대상이 선택되었을 때
        /// </summary>
        private void OnSwapSecondTargetSelected(Card firstTarget, Card secondTarget)
        {
            if (firstTarget == null || secondTarget == null) return;

            SwapCardValues(firstTarget, secondTarget);
            RemoveUsedJokerCard();

            InGameManager.Instance.EndProcess();
            EndJokerProcess();
            Hide();
        }
        #endregion

        #region Card Sequences
        /// <summary>
        /// 카드 삭제 시퀀스 (대상 카드 → 조커 카드)
        /// </summary>
        private IEnumerator DeleteCardSequence(Card targetCard)
        {
            // 대상 카드 삭제 애니메이션
            CardZone targetZone = FindZoneOfCard(targetCard.transform);
            yield return StartCoroutine(targetCard.AnimateRemoval(() =>
            {
                if (targetZone != null)
                    targetZone.RemoveCard(targetCard.transform);
                Destroy(targetCard.gameObject);
            }));

            yield return new WaitForSeconds(0.2f);

            // 네트워크 동기화 - Delete 조커 결과 전송 (조커 카드 삭제 전에!)
            if (NetworkGameManager.Instance != null)
            {
                var targetCards = new System.Collections.Generic.List<Card> { targetCard };
                NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Delete, targetCards);
            }

            // 조커 카드 삭제
            RemoveUsedJokerCard();

            InGameManager.Instance.EndProcess();
            EndJokerProcess();
            Hide();
        }

        /// <summary>
        /// 두 카드의 텍스트 값 교환
        /// </summary>
        private void SwapCardValues(Card firstTarget, Card secondTarget)
        {
            var firstCardText = firstTarget.GetComponentInChildren<CardText>();
            var secondCardText = secondTarget.GetComponentInChildren<CardText>();

            if (firstCardText != null && secondCardText != null)
            {
                float firstValue = firstCardText.RawValue;
                float secondValue = secondCardText.RawValue;

                firstCardText.SetRawValue(secondValue);
                secondCardText.SetRawValue(firstValue);

                // 네트워크 동기화 - Swap 조커 결과 전송
                if (NetworkGameManager.Instance != null)
                {
                    var targetCards = new System.Collections.Generic.List<Card> { firstTarget, secondTarget };
                    NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Swap, targetCards);
                }
            }
        }
        #endregion

        #region GLOW Management System
        /// <summary>
        /// 조커 프로세스 시작 - 현재 GLOW 상태 저장 후 초기화
        /// </summary>
        private void StartJokerProcess(JokerEffectType effectType)
        {
            SaveCurrentGlowStates();
            ClearAllGlow();
            ApplyJokerSpecificGlow(effectType);
        }

        /// <summary>
        /// 조커 프로세스 종료 - 원래 GLOW 상태 복원
        /// </summary>
        private void EndJokerProcess()
        {
            ClearAllGlow();
            RestoreGlowStates();
            savedGlowStates.Clear();
        }

        /// <summary>
        /// 현재 모든 카드의 GLOW 상태 저장
        /// </summary>
        private void SaveCurrentGlowStates()
        {
            savedGlowStates.Clear();
            var allCards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in allCards)
            {
                var effect = card.GetComponentInChildren<CardEffect>();
                if (effect != null)
                    savedGlowStates[card] = effect.IsGlowing();
            }
        }

        /// <summary>
        /// 저장된 GLOW 상태 복원
        /// </summary>
        private void RestoreGlowStates()
        {
            foreach (var kvp in savedGlowStates)
            {
                if (kvp.Value) // 원래 GLOW가 있었다면
                    kvp.Key.SetCardState(true, Global.GlowGreen);
            }
        }

        /// <summary>
        /// 모든 카드의 GLOW 제거
        /// </summary>
        private void ClearAllGlow()
        {
            var allCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in allCards)
                card.SetCardState(false);
        }

        /// <summary>
        /// 조커 효과별 특정 GLOW 적용
        /// </summary>
        private void ApplyJokerSpecificGlow(JokerEffectType effectType)
        {
            switch (effectType)
            {
                case JokerEffectType.Delete:
                    ApplyGlowToOpponentCards();
                    break;
                case JokerEffectType.Swap:
                    ApplyGlowToPlayerCards();
                    break;
            }
        }

        private void ApplyGlowToOpponentCards()
        {
            var fieldCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Opponent)
                    card.SetCardState(true, Global.GlowGreen);
            }
        }

        private void ApplyGlowToPlayerCards()
        {
            var fieldCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                    card.SetCardState(true, Global.GlowGreen);
            }
        }
        #endregion

        #region Joker Card Management
        /// <summary>
        /// 사용한 조커 카드 제거
        /// </summary>
        private void RemoveUsedJokerCard()
        {
            if (selectedJokerCard != null)
                StartCoroutine(RemoveJokerCardWithAnimation(selectedJokerCard));
        }

        /// <summary>
        /// 조커 카드를 애니메이션과 함께 제거
        /// </summary>
        private IEnumerator RemoveJokerCardWithAnimation(Card jokerCard)
        {
            CardZone zone = FindZoneOfCard(jokerCard.transform);
            yield return StartCoroutine(jokerCard.AnimateRemoval(() =>
            {
                if (zone != null)
                    zone.RemoveCard(jokerCard.transform);
                Destroy(jokerCard.gameObject);
            }));
        }
        #endregion

        #region Validation & Utility
        /// <summary>
        /// 조커 카드 유효성 검증
        /// </summary>
        private bool ValidateJokerCard(Card jokerCard)
        {
            if (jokerCard == null || jokerCard.CardType != CardType.Joker)
            {
                Debug.LogError("[JokerModeSelector] 유효하지 않은 조커 카드입니다.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 사용 가능한 효과 확인 및 UI 업데이트
        /// </summary>
        private void UpdateAvailableEffects()
        {
            var fieldCards = InGameManager.Instance.GetAllFieldCards();

            bool hasMyFieldCards = false;
            bool hasOpponentFieldCards = false;

            foreach (var card in fieldCards)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                    hasMyFieldCards = true;
                else if (card.CurrentOwnerType == CardZone.OwnerType.Opponent)
                    hasOpponentFieldCards = true;
            }

            SetEffectAvailability(drawOption, true); // Draw는 항상 사용 가능
            SetEffectAvailability(deleteOption, hasOpponentFieldCards);
            SetEffectAvailability(swapOption, hasMyFieldCards && hasOpponentFieldCards);
        }

        /// <summary>
        /// 효과 사용 가능 여부에 따라 UI 업데이트
        /// </summary>
        private void SetEffectAvailability(JokerEffectOption option, bool available)
        {
            if (option == null) return;

            var sr = option.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Color color = sr.color;
                color.a = available ? 1f : 0.5f;
                sr.color = color;
            }

            var mouseEvent = option.GetComponent<ObjectMouseEvent>();
            if (mouseEvent != null)
                mouseEvent.isClickable = available;
        }

        /// <summary>
        /// 카드가 속한 Zone 찾기
        /// </summary>
        private CardZone FindZoneOfCard(Transform card)
        {
            if (CardZone.AllZonesRoot == null || card == null) return null;

            foreach (var zone in CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>())
            {
                if (zone.Contains(card))
                    return zone;
            }
            return null;
        }
        #endregion

        #region UI Management
        /// <summary>
        /// 하위 UI 오브젝트들을 일괄로 켜거나 끈다
        /// </summary>
        private void SetUIActive(bool active)
        {
            if (dimBackground != null) dimBackground.SetActive(active);
            if (cancelButton != null) cancelButton.SetActive(active);
            if (drawOption != null) drawOption.gameObject.SetActive(active);
            if (deleteOption != null) deleteOption.gameObject.SetActive(active);
            if (swapOption != null) swapOption.gameObject.SetActive(active);
        }

        /// <summary>
        /// UI 표시 애니메이션 실행
        /// </summary>
        private void PlayShowAnimation()
        {
            if (drawOption != null)
            {
                drawOption.transform.localScale = Vector3.zero;
                drawOption.transform.DOScale(Vector3.one * maxScale, animDurationUI).SetEase(Ease.OutBack);
            }

            if (deleteOption != null)
            {
                deleteOption.transform.localScale = Vector3.zero;
                deleteOption.transform.DOScale(Vector3.one * maxScale, animDurationUI).SetEase(Ease.OutBack).SetDelay(0.05f);
            }

            if (swapOption != null)
            {
                swapOption.transform.localScale = Vector3.zero;
                swapOption.transform.DOScale(Vector3.one * maxScale, animDurationUI).SetEase(Ease.OutBack).SetDelay(0.1f);
            }
        }

        /// <summary>
        /// 각 옵션의 스프라이트 업데이트 (안전장치 추가)
        /// </summary>
        private void UpdateOptionSprites()
        {
            if (string.IsNullOrEmpty(spriteColorName))
            {
                Debug.LogWarning("[JokerModeSelector] spriteColorName이 설정되지 않았습니다.");
                return;
            }

            SetOptionSprite(drawOption, GetJokerSpriteName(spriteColorName, JokerEffectType.Draw));
            SetOptionSprite(deleteOption, GetJokerSpriteName(spriteColorName, JokerEffectType.Delete));
            SetOptionSprite(swapOption, GetJokerSpriteName(spriteColorName, JokerEffectType.Swap));
        }

        /// <summary>
        /// 색상과 효과에 맞는 스프라이트 이름 생성
        /// </summary>
        private string GetJokerSpriteName(string color, JokerEffectType effect)
        {
            if (string.IsNullOrEmpty(color))
            {
                Debug.LogWarning("[JokerModeSelector] color가 비어있어 기본 색상을 사용합니다.");
                return $"color_green_{effect.ToString().ToLower()}";
            }

            string[] colorStrArr = color.Split('_');
            if (colorStrArr.Length < 2)
            {
                Debug.LogWarning($"[JokerModeSelector] 색상 파싱 실패, 기본 색상 사용: {color}");
                return $"color_green_{effect.ToString().ToLower()}";
            }

            string colorStr = colorStrArr[1];
            string effectStr = effect.ToString().ToLower();
            return $"color_{colorStr}_{effectStr}";
        }

        /// <summary>
        /// 개별 옵션의 스프라이트 설정 (안전장치 추가)
        /// </summary>
        private void SetOptionSprite(JokerEffectOption option, string spriteName)
        {
            if (option == null)
            {
                Debug.LogWarning($"[JokerModeSelector] option이 null입니다: {spriteName}");
                return;
            }

            var sr = option.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                try
                {
                    Sprite sprite = ResourcesManager.Instance?.GetSprite(Global.Joker, spriteName);
                    if (sprite != null)
                    {
                        sr.sprite = sprite;
                        Debug.Log($"[JokerModeSelector] 스프라이트 설정 성공: {spriteName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[JokerModeSelector] 스프라이트를 찾을 수 없습니다: {spriteName}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[JokerModeSelector] 스프라이트 설정 중 오류: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[JokerModeSelector] SpriteRenderer를 찾을 수 없습니다: {option.name}");
            }
        }
        #endregion
    }
}
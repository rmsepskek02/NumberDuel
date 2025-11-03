using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects;
using Manager;
using TMPro;
using DG.Tweening;

namespace Objects
{
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
        [Header("UI 컴포넌트")]
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
        private bool isInitialized = false;
        private bool isSpritesSet = false;
        private Dictionary<Card, bool> savedGlowStates = new Dictionary<Card, bool>();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            StartCoroutine(SafeInitialization());
        }

        private void OnDisable()
        {
            if (bgClick != null)
                bgClick.OnClickReleased -= OnCancelPressed;
        }
        #endregion

        #region Safe Initialization
        private IEnumerator SafeInitialization()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (ResourcesManager.Instance != null && ResourcesManager.Instance.IsBasicInitialized)
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout)
            {
                yield break;
            }

            try
            {
                InitializeOptions();
                InitializeBackground();
                InitializeTexts();
                SetUIActive(false);

                isInitialized = true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[JokerModeSelector] 초기화 중 오류: {ex.Message}");
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
        public void Show(Card jokerCard)
        {
            if (!isInitialized)
            {
                return;
            }

            if (!ValidateJokerCard(jokerCard)) return;
            if (InGameManager.Instance.IsProcessing)
            {
                return;
            }

            selectedJokerCard = jokerCard;

            if (!isSpritesSet)
            {
                SetSpritesForCurrentColor();
                isSpritesSet = true;
            }

            UpdateAvailableEffects();
            SetUIActive(true);
            PlayShowAnimation();
        }

        public void Hide()
        {
            selectedJokerCard = null;
            SetUIActive(false);
        }

        public void OnJokerEffectSelected(JokerEffectType effectType)
        {
            if (selectedJokerCard == null)
            {
                return;
            }

            selectedEffect = effectType;
            SetUIActive(false);
            ExecuteJokerEffect();
        }

        public void OnCancelPressed()
        {
            Hide();
        }
        #endregion

        #region Sprite Management
        private void SetSpritesForCurrentColor()
        {
            try
            {
                if (ResourcesManager.Instance == null)
                {
                    return;
                }

                var playerSprite = ResourcesManager.Instance.GetPlayerSprite();
                if (playerSprite == null)
                {
                    return;
                }

                string currentColor = ExtractColorFromSpriteName(playerSprite.name);

                string drawSpriteName = $"color_{currentColor}_draw";
                string deleteSpriteName = $"color_{currentColor}_delete";
                string swapSpriteName = $"color_{currentColor}_swap";

                SetOptionSprite(drawOption, drawSpriteName);
                SetOptionSprite(deleteOption, deleteSpriteName);
                SetOptionSprite(swapOption, swapSpriteName);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[JokerModeSelector] 스프라이트 설정 중 오류: {ex.Message}");
            }
        }

        private string ExtractColorFromSpriteName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return "green";

            if (spriteName.StartsWith("color_"))
            {
                string remaining = spriteName.Substring(6);
                int underscoreIndex = remaining.IndexOf('_');
                if (underscoreIndex > 0)
                {
                    return remaining.Substring(0, underscoreIndex);
                }
                else
                {
                    return remaining;
                }
            }

            return "green";
        }

        private void SetOptionSprite(JokerEffectOption option, string spriteName)
        {
            if (option == null) return;

            var sr = option.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;

            Sprite sprite = ResourcesManager.Instance.GetSprite(Global.Joker, spriteName);

            if (sprite != null)
            {
                sr.sprite = sprite;
            }
        }
        #endregion

        #region Effect Execution
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

        private void ExecuteDrawEffect()
        {
            InGameManager.Instance.StartProcess(GameProcessState.JokerDrawProcess);

            CardZone.OwnerType cardOwner = selectedJokerCard.CurrentOwnerType;

            // 1. 손패에서 2장 드로우
            InGameManager.Instance.DrawCardsToHand(2, cardOwner);

            // 2. 네트워크 동기화 전송
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Draw);
            }

            // 3. 조커 카드 즉시 삭제
            RemoveJokerCardImmediately();

            InGameManager.Instance.EndProcess();
            Hide();
        }

        private void StartDeleteTargetSelection()
        {
            InGameManager.Instance.StartProcess(GameProcessState.JokerDeleteProcess);
            StartJokerProcess(JokerEffectType.Delete);

            // ExpressionZone에 Delete 효과 초기화
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.InitForJokerDelete();
            }

            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.Delete, OnDeleteTargetSelected);
        }

        private void StartSwapTargetSelection()
        {
            InGameManager.Instance.StartProcess(GameProcessState.JokerSwapProcess);
            StartJokerProcess(JokerEffectType.Swap);

            // ExpressionZone에 Swap 효과 초기화
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.InitForJokerSwap();
            }

            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapFirst, OnSwapFirstTargetSelected);
        }
        #endregion

        #region Effect Callbacks
        private void OnDeleteTargetSelected(Card target)
        {
            if (target == null) return;

            // ExpressionZone에 삭제 대상 카드 표시
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.SetJokerDeleteTarget(target);
            }

            // 삭제 대상 카드에 아이콘 표시
            target.ShowSelectionIcon(CardIconType.Delete);

            // 네트워크 동기화
            if (Manager.NetworkGameManager.Instance != null)
            {
                Manager.NetworkGameManager.Instance.SyncCardIcon(target, CardIconType.Delete);
            }

            StartCoroutine(DeleteCardSequence(target));
        }

        private void OnSwapFirstTargetSelected(Card firstTarget)
        {
            if (firstTarget == null) return;

            // ExpressionZone에 첫 번째 스왑 카드 표시
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.SetJokerSwapFirst(firstTarget);
            }

            // 첫 번째 스왑 대상 카드에 아이콘 표시
            firstTarget.ShowSelectionIcon(CardIconType.Swap);

            // 네트워크 동기화
            if (Manager.NetworkGameManager.Instance != null)
            {
                Manager.NetworkGameManager.Instance.SyncCardIcon(firstTarget, CardIconType.Swap);
            }

            ClearAllGlow();
            ApplyGlowToOpponentCards();
            firstTarget.SetCardState(true, Color.cyan);

            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapSecond,
                (secondTarget) => OnSwapSecondTargetSelected(firstTarget, secondTarget));
        }

        private void OnSwapSecondTargetSelected(Card firstTarget, Card secondTarget)
        {
            if (firstTarget == null || secondTarget == null) return;

            // ExpressionZone에 두 번째 스왑 카드 표시
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.SetJokerSwapSecond(secondTarget);
            }

            // 두 번째 스왑 대상 카드에 아이콘 표시
            secondTarget.ShowSelectionIcon(CardIconType.Swap);

            // 네트워크 동기화 (아이콘 표시)
            if (Manager.NetworkGameManager.Instance != null)
            {
                Manager.NetworkGameManager.Instance.SyncCardIcon(secondTarget, CardIconType.Swap);
            }

            // 코루틴으로 Swap 시퀀스 실행
            StartCoroutine(SwapCardSequence(firstTarget, secondTarget));
        }
        #endregion

        #region Card Sequences
        private IEnumerator DeleteCardSequence(Card targetCard)
        {
            // ExpressionZone에 모든 내용 표시 후 2초 대기 (플레이어가 볼 시간)
            yield return new WaitForSeconds(2.0f);

            // 다시: NetworkCard ID를 통한 정확 이름 전달
            var targetNetworkCard = targetCard.GetComponent<NetworkCard>();
            string targetCardId = targetNetworkCard != null ? targetNetworkCard.UniqueId : "";

            // 1. 네트워크 동기화 전송 먼저 (카드 삭제 전!)
            if (NetworkGameManager.Instance != null && !string.IsNullOrEmpty(targetCardId))
            {
                // targetCard 들을 ID로 명확 전달하도록 개선된 메서드 필요
                var targetCards = new List<Card> { targetCard };
                NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Delete, targetCards);
            }

            yield return new WaitForSeconds(0.1f);

            // 2. 카드 제거 애니메이션 및 파괴
            CardZone targetZone = FindZoneOfCard(targetCard.transform);
            yield return StartCoroutine(targetCard.AnimateRemoval(() =>
            {
                if (targetZone != null)
                {
                    targetZone.RemoveCard(targetCard.transform);
                }
                Destroy(targetCard.gameObject);
            }));

            yield return new WaitForSeconds(0.2f);

            // 3. 조커 카드 삭제
            RemoveJokerCardImmediately();

            InGameManager.Instance.EndProcess();
            EndJokerProcess();
            Hide();
        }

        private IEnumerator SwapCardSequence(Card firstTarget, Card secondTarget)
        {
            // ExpressionZone에 모든 내용 표시 후 2초 대기 (플레이어가 볼 시간)
            yield return new WaitForSeconds(2.0f);

            // 값 교환
            var firstCardText = firstTarget.GetComponentInChildren<CardText>();
            var secondCardText = secondTarget.GetComponentInChildren<CardText>();

            if (firstCardText != null && secondCardText != null)
            {
                float firstValue = firstCardText.RawValue;
                float secondValue = secondCardText.RawValue;

                // 1. 값 교환
                firstCardText.SetRawValue(secondValue);
                secondCardText.SetRawValue(firstValue);

                // 2. 네트워크 동기화 전송
                if (NetworkGameManager.Instance != null)
                {
                    var targetCards = new List<Card> { firstTarget, secondTarget };
                    NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Swap, targetCards);
                }
            }

            yield return new WaitForSeconds(0.3f);

            // 스왑 완료 후 두 카드 모두 아이콘 숨김
            firstTarget.HideSelectionIcon();
            secondTarget.HideSelectionIcon();

            // 네트워크 동기화 (아이콘 숨김)
            if (Manager.NetworkGameManager.Instance != null)
            {
                Manager.NetworkGameManager.Instance.HideCardIcon(firstTarget);
                Manager.NetworkGameManager.Instance.HideCardIcon(secondTarget);
            }

            // 3. 조커 카드 삭제
            RemoveJokerCardImmediately();

            InGameManager.Instance.EndProcess();
            EndJokerProcess();
            Hide();
        }
        #endregion

        #region GLOW Management System
        private void StartJokerProcess(JokerEffectType effectType)
        {
            SaveCurrentGlowStates();
            ClearAllGlow();
            ApplyJokerSpecificGlow(effectType);
        }

        private void EndJokerProcess()
        {
            // 모든 필드 카드의 선택 아이콘 숨김 (안전장치)
            var allCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in allCards)
            {
                card.HideSelectionIcon();
            }

            // ExpressionZone 초기화
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.ResetAllSlots();
            }

            ClearAllGlow();
            RestoreGlowStates();
            savedGlowStates.Clear();
        }

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

        private void RestoreGlowStates()
        {
            foreach (var kvp in savedGlowStates)
            {
                if (kvp.Value)
                    kvp.Key.SetCardState(true, Global.GlowGreen);
            }
        }

        private void ClearAllGlow()
        {
            var allCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in allCards)
                card.SetCardState(false);
        }

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
        /// 조커 카드를 즉시 삭제 (코루틴 없음)
        /// </summary>
        private void RemoveJokerCardImmediately()
        {
            if (selectedJokerCard == null)
            {
                return;
            }

            GameObject jokerObj = selectedJokerCard.gameObject;

            // 1. 즉시 비활성화하여 화면에서 제거
            jokerObj.SetActive(false);

            // 2. Zone에서 제거
            CardZone zone = FindZoneOfCard(selectedJokerCard.transform);
            if (zone != null)
            {
                zone.RemoveCard(selectedJokerCard.transform);
            }

            // 3. 부모에서 분리
            selectedJokerCard.transform.SetParent(null);

            // 4. 오브젝트 파괴
            Destroy(jokerObj);

            selectedJokerCard = null;
        }
        #endregion

        #region Validation & Utility
        private bool ValidateJokerCard(Card jokerCard)
        {
            if (jokerCard == null || jokerCard.CardType != CardType.Joker)
            {
                return false;
            }
            return true;
        }

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

            SetEffectAvailability(drawOption, true);
            SetEffectAvailability(deleteOption, hasOpponentFieldCards);
            SetEffectAvailability(swapOption, hasMyFieldCards && hasOpponentFieldCards);
        }

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

        private CardZone FindZoneOfCard(Transform card)
        {
            if (CardZone.AllZonesRoot == null || card == null)
            {
                return null;
            }

            foreach (var zone in CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>())
            {
                if (zone.Contains(card))
                {
                    return zone;
                }
            }

            return null;
        }
        #endregion

        #region UI Management
        private void SetUIActive(bool active)
        {
            if (dimBackground != null) dimBackground.SetActive(active);
            if (cancelButton != null) cancelButton.SetActive(active);
            if (drawOption != null) drawOption.gameObject.SetActive(active);
            if (deleteOption != null) deleteOption.gameObject.SetActive(active);
            if (swapOption != null) swapOption.gameObject.SetActive(active);
        }

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
        #endregion
    }
}

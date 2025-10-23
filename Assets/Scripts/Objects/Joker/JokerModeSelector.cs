using UnityEngine;
using Objects;
using Manager;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

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
                Debug.LogError("[JokerModeSelector] ResourcesManager 대기 시간 초과");
                yield break;
            }

            try
            {
                InitializeOptions();
                InitializeBackground();
                InitializeTexts();
                SetUIActive(false);

                isInitialized = true;
                Debug.Log("[JokerModeSelector] 초기화 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JokerModeSelector] 초기화 중 오류: {ex.Message}");
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
            Debug.Log($"[JokerModeSelector] 조커 UI 표시 - 카드: {jokerCard.name}");

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
            Debug.Log("[JokerModeSelector] UI 숨김");
            selectedJokerCard = null;
            SetUIActive(false);
        }

        public void OnJokerEffectSelected(JokerEffectType effectType)
        {
            if (selectedJokerCard == null)
            {
                Debug.LogError("[JokerModeSelector] selectedJokerCard가 null입니다.");
                return;
            }

            Debug.Log($"[JokerModeSelector] 조커 효과 선택: {effectType}");
            selectedEffect = effectType;
            SetUIActive(false);
            ExecuteJokerEffect();
        }

        public void OnCancelPressed()
        {
            Debug.Log("[JokerModeSelector] 취소 버튼 클릭");
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
                    Debug.LogError("[JokerModeSelector] ResourcesManager 인스턴스가 없습니다.");
                    return;
                }

                var playerSprite = ResourcesManager.Instance.GetPlayerSprite();
                if (playerSprite == null)
                {
                    Debug.LogError("[JokerModeSelector] Player 스프라이트를 가져올 수 없습니다.");
                    return;
                }

                string currentColor = ExtractColorFromSpriteName(playerSprite.name);

                string drawSpriteName = $"color_{currentColor}_draw";
                string deleteSpriteName = $"color_{currentColor}_delete";
                string swapSpriteName = $"color_{currentColor}_swap";

                SetOptionSprite(drawOption, drawSpriteName);
                SetOptionSprite(deleteOption, deleteSpriteName);
                SetOptionSprite(swapOption, swapSpriteName);

                Debug.Log($"[JokerModeSelector] 스프라이트 설정 완료 - 색상: {currentColor}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JokerModeSelector] 스프라이트 설정 중 오류: {ex.Message}");
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
            else
            {
                Debug.LogWarning($"[JokerModeSelector] 스프라이트를 찾을 수 없습니다: {spriteName}");
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
            Debug.Log("[JokerModeSelector] Draw 효과 실행 시작");
            InGameManager.Instance.StartProcess(GameProcessState.JokerDrawProcess);

            CardZone.OwnerType cardOwner = selectedJokerCard.CurrentOwnerType;

            // 1. 로컬에서 2장 드로우
            InGameManager.Instance.DrawCardsToHand(2, cardOwner);

            // 2. 네트워크 동기화 전송
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Draw);
            }

            // 3. 조커 카드 즉시 제거
            RemoveJokerCardImmediately();

            InGameManager.Instance.EndProcess();
            Hide();

            Debug.Log("[JokerModeSelector] Draw 효과 실행 완료");
        }

        private void StartDeleteTargetSelection()
        {
            Debug.Log("[JokerModeSelector] Delete 대상 선택 시작");
            InGameManager.Instance.StartProcess(GameProcessState.JokerDeleteProcess);
            StartJokerProcess(JokerEffectType.Delete);
            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.Delete, OnDeleteTargetSelected);
        }

        private void StartSwapTargetSelection()
        {
            Debug.Log("[JokerModeSelector] Swap 대상 선택 시작");
            InGameManager.Instance.StartProcess(GameProcessState.JokerSwapProcess);
            StartJokerProcess(JokerEffectType.Swap);
            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapFirst, OnSwapFirstTargetSelected);
        }
        #endregion

        #region Effect Callbacks
        private void OnDeleteTargetSelected(Card target)
        {
            if (target == null) return;
            Debug.Log($"[JokerModeSelector] Delete 대상 선택됨: {target.name}");
            StartCoroutine(DeleteCardSequence(target));
        }

        private void OnSwapFirstTargetSelected(Card firstTarget)
        {
            if (firstTarget == null) return;
            Debug.Log($"[JokerModeSelector] Swap 첫 번째 대상 선택됨: {firstTarget.name}");

            ClearAllGlow();
            ApplyGlowToOpponentCards();
            firstTarget.SetCardState(true, Color.cyan);

            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapSecond,
                (secondTarget) => OnSwapSecondTargetSelected(firstTarget, secondTarget));
        }

        private void OnSwapSecondTargetSelected(Card firstTarget, Card secondTarget)
        {
            if (firstTarget == null || secondTarget == null) return;
            Debug.Log($"[JokerModeSelector] Swap 두 번째 대상 선택됨: {secondTarget.name}");

            SwapCardValues(firstTarget, secondTarget);
            RemoveJokerCardImmediately();

            InGameManager.Instance.EndProcess();
            EndJokerProcess();
            Hide();
        }
        #endregion

        #region Card Sequences
        private IEnumerator DeleteCardSequence(Card targetCard)
        {
            Debug.Log($"[JokerModeSelector] 카드 삭제 시퀀스 시작: {targetCard.name}");

            // 핵심: NetworkCard ID를 삭제 전에 미리 추출
            var targetNetworkCard = targetCard.GetComponent<NetworkCard>();
            string targetCardId = targetNetworkCard != null ? targetNetworkCard.UniqueId : "";

            if (string.IsNullOrEmpty(targetCardId))
            {
                Debug.LogError($"[JokerModeSelector] 대상 카드의 ID를 찾을 수 없습니다: {targetCard.name}");
            }

            // 1. 네트워크 동기화 먼저 전송 (카드 삭제 전!)
            if (NetworkGameManager.Instance != null && !string.IsNullOrEmpty(targetCardId))
            {
                // targetCard 대신 ID만 직접 전달하도록 수정된 메서드 필요
                var targetCards = new System.Collections.Generic.List<Card> { targetCard };
                NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Delete, targetCards);
                Debug.Log($"[JokerModeSelector] Delete 동기화 전송 완료: {targetCardId}");
            }

            yield return new WaitForSeconds(0.3f);

            // 2. 카드 삭제 애니메이션 및 제거
            CardZone targetZone = FindZoneOfCard(targetCard.transform);
            yield return StartCoroutine(targetCard.AnimateRemoval(() =>
            {
                if (targetZone != null)
                {
                    targetZone.RemoveCard(targetCard.transform);
                    Debug.Log($"[JokerModeSelector] Zone에서 카드 제거: {targetCard.name}");
                }
                Destroy(targetCard.gameObject);
                Debug.Log($"[JokerModeSelector] 카드 오브젝트 파괴: {targetCard.name}");
            }));

            yield return new WaitForSeconds(0.2f);

            // 3. 조커 카드 제거
            RemoveJokerCardImmediately();

            InGameManager.Instance.EndProcess();
            EndJokerProcess();
            Hide();

            Debug.Log("[JokerModeSelector] 카드 삭제 시퀀스 완료");
        }

        private void SwapCardValues(Card firstTarget, Card secondTarget)
        {
            var firstCardText = firstTarget.GetComponentInChildren<CardText>();
            var secondCardText = secondTarget.GetComponentInChildren<CardText>();

            if (firstCardText != null && secondCardText != null)
            {
                float firstValue = firstCardText.RawValue;
                float secondValue = secondCardText.RawValue;

                // 1. 값 교환
                firstCardText.SetRawValue(secondValue);
                secondCardText.SetRawValue(firstValue);

                Debug.Log($"[JokerModeSelector] 카드 값 교환 완료: {firstValue} <-> {secondValue}");

                // 2. 네트워크 동기화 전송
                if (NetworkGameManager.Instance != null)
                {
                    var targetCards = new System.Collections.Generic.List<Card> { firstTarget, secondTarget };
                    NetworkGameManager.Instance.SyncJokerResult(selectedJokerCard, JokerEffectType.Swap, targetCards);
                    Debug.Log("[JokerModeSelector] Swap 동기화 전송 완료");
                }
            }

            // 3. 조커 카드 제거
            RemoveJokerCardImmediately();
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
        /// 조커 카드를 즉시 제거 (코루틴 없이)
        /// </summary>
        private void RemoveJokerCardImmediately()
        {
            if (selectedJokerCard == null)
            {
                Debug.LogWarning("[JokerModeSelector] 제거할 조커 카드가 없습니다 (null)");
                return;
            }

            Debug.Log($"[JokerModeSelector] 조커 카드 즉시 제거 시작: {selectedJokerCard.name}");

            GameObject jokerObj = selectedJokerCard.gameObject;

            // 1. 즉시 비활성화하여 화면에서 숨김
            jokerObj.SetActive(false);
            Debug.Log($"[JokerModeSelector] 조커 카드 비활성화 완료");

            // 2. Zone에서 제거
            CardZone zone = FindZoneOfCard(selectedJokerCard.transform);
            if (zone != null)
            {
                Debug.Log($"[JokerModeSelector] Zone 발견: {zone.name}");
                zone.RemoveCard(selectedJokerCard.transform);
                Debug.Log($"[JokerModeSelector] Zone에서 조커 카드 제거 완료");
            }
            else
            {
                Debug.LogWarning($"[JokerModeSelector] 조커 카드의 Zone을 찾을 수 없습니다: {selectedJokerCard.name}");
                LogAllZones();
            }

            // 3. 부모에서 분리
            selectedJokerCard.transform.SetParent(null);
            Debug.Log($"[JokerModeSelector] 조커 카드 부모에서 분리 완료");

            // 4. 오브젝트 파괴
            Destroy(jokerObj);
            Debug.Log($"[JokerModeSelector] 조커 오브젝트 파괴 요청 완료: {jokerObj.name}");

            selectedJokerCard = null;
        }

        /// <summary>
        /// 디버깅용: 모든 Zone 정보 출력
        /// </summary>
        private void LogAllZones()
        {
            if (CardZone.AllZonesRoot == null)
            {
                Debug.LogError("[JokerModeSelector] AllZonesRoot가 null입니다");
                return;
            }

            var allZones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            Debug.Log($"[JokerModeSelector] 전체 Zone 개수: {allZones.Length}");

            foreach (var zone in allZones)
            {
                Debug.Log($"[JokerModeSelector] Zone: {zone.name}, Cards: {zone.transform.childCount}");
            }
        }
        #endregion

        #region Validation & Utility
        private bool ValidateJokerCard(Card jokerCard)
        {
            if (jokerCard == null || jokerCard.CardType != CardType.Joker)
            {
                Debug.LogError("[JokerModeSelector] 유효하지 않은 조커 카드입니다.");
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
                Debug.LogWarning("[JokerModeSelector] AllZonesRoot 또는 card가 null입니다");
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
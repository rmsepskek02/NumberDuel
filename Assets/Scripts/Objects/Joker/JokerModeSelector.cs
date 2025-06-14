using UnityEngine;
using Objects;
using Manager;
using TMPro;
using DG.Tweening;
using System.Collections;
namespace Objects
{
    /// <summary>
    /// 조커 카드 클릭 시 효과를 선택하는 UI를 제어한다.
    /// CardModeSelector와 유사한 구조로 구현
    /// </summary>
    public class JokerModeSelector : MonoBehaviour
    {
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
        [SerializeField] private float animDuration = 1.0f;

        private Card selectedJokerCard;
        private ObjectMouseEvent bgClick;

        // 선택된 효과와 색상을 저장
        private JokerEffectType selectedEffect;
        private Color selectedColor;
        private string spriteColorName;

        private void Start()
        {
            // 각 옵션에 Selector 연결
            drawOption.SetSelector(this);
            deleteOption.SetSelector(this);
            swapOption.SetSelector(this);

            // 배경 클릭 시 Cancel 처리
            bgClick = dimBackground.GetComponent<ObjectMouseEvent>();
            if (bgClick != null)
                bgClick.OnClickReleased += OnCancelPressed;

            // 효과 설명 텍스트 설정
            if (drawText != null) drawText.text = "Draw\n2 Card";
            if (deleteText != null) deleteText.text = "Delete\nCard";
            if (swapText != null) swapText.text = "Swap\nCards";

            // 각 옵션의 스프라이트 설정 (초기값)
            spriteColorName = ResourcesManager.Instance.GetPlayerSprite().name;
            UpdateOptionSprites();

            SetUIActive(false);
        }

        private void OnDisable()
        {
            if (bgClick != null)
                bgClick.OnClickReleased -= OnCancelPressed;
        }

        /// <summary>
        /// 조커 카드 효과 선택 UI를 표시
        /// </summary>
        public void Show(Card jokerCard)
        {
            if (jokerCard == null || jokerCard.CardType != CardType.Joker)
            {
                Debug.LogError("[JokerModeSelector] 유효하지 않은 조커 카드입니다.");
                return;
            }

            // 다른 프로세스가 진행 중이면 표시하지 않음
            if (InGameManager.Instance.IsProcessing)
            {
                Debug.LogWarning($"[JokerModeSelector] {InGameManager.Instance.CurrentProcess} 진행 중이므로 조커 UI를 표시할 수 없습니다.");
                return;
            }

            selectedJokerCard = jokerCard;
            SetUIActive(true);

            // 초기 스케일 설정
            drawOption.transform.localScale = Vector3.zero;
            deleteOption.transform.localScale = Vector3.zero;
            swapOption.transform.localScale = Vector3.zero;

            // DOTween 애니메이션 실행
            Ease easeType = Ease.OutBack;

            drawOption.transform
                .DOScale(Vector3.one * maxScale, animDurationUI)
                .SetEase(easeType);

            deleteOption.transform
                .DOScale(Vector3.one * maxScale, animDurationUI)
                .SetEase(easeType)
                .SetDelay(0.05f);

            swapOption.transform
                .DOScale(Vector3.one * maxScale, animDurationUI)
                .SetEase(easeType)
                .SetDelay(0.1f);
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
        /// 색상과 효과에 맞는 스프라이트 이름 생성
        /// </summary>
        private string GetJokerSpriteName(string color, JokerEffectType effect)
        {
            string[] colorStrArr = color.ToString().Split("_");
            if (colorStrArr.Length < 2)
            {
                return "green";
            }
            string colorStr = colorStrArr[1];
            string effectStr = effect.ToString().ToLower();
            return $"color_{colorStr}_{effectStr}";
        }

        /// <summary>
        /// 각 옵션의 스프라이트 업데이트
        /// </summary>
        private void UpdateOptionSprites()
        {
            // 각 효과별로 기본 스프라이트 설정
            SetOptionSprite(drawOption, GetJokerSpriteName(spriteColorName, drawOption.effectType));
            SetOptionSprite(deleteOption, GetJokerSpriteName(spriteColorName, deleteOption.effectType));
            SetOptionSprite(swapOption, GetJokerSpriteName(spriteColorName, swapOption.effectType));
        }

        /// <summary>
        /// 개별 옵션의 스프라이트 설정
        /// </summary>
        private void SetOptionSprite(JokerEffectOption option, string spriteName)
        {
            if (option == null) return;

            var sr = option.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Sprite sprite = ResourcesManager.Instance.GetSprite(Global.Joker, spriteName);
                if (sprite != null)
                {
                    sr.sprite = sprite;
                }
            }
        }

        /// <summary>
        /// 조커 효과가 선택되었을 때 호출됨
        /// </summary>
        public void OnJokerEffectSelected(JokerEffectType effectType)
        {
            if (selectedJokerCard == null)
                return;

            selectedEffect = effectType;

            // 효과 실행
            ExecuteJokerEffect();
            Hide();
        }

        /// <summary>
        /// 선택된 조커 효과 실행
        /// </summary>
        private void ExecuteJokerEffect()
        {
            switch (selectedEffect)
            {
                case JokerEffectType.Draw:
                    ExecuteDrawEffect();
                    // Draw는 즉시 조커 제거
                    RemoveUsedJokerCard();
                    break;
                case JokerEffectType.Delete:
                    // Delete는 대상 삭제 후 조커 제거
                    StartDeleteTargetSelection();
                    break;
                case JokerEffectType.Swap:
                    // Swap은 교환 완료 후 조커 제거
                    StartSwapTargetSelection();
                    break;
            }
        }

        /// <summary>
        /// 카드 드로우 효과 실행
        /// </summary>
        private void ExecuteDrawEffect()
        {
            Debug.Log("[JokerModeSelector] Draw 효과 실행 - 구현 예정");

            // 프로세스 시작
            InGameManager.Instance.StartProcess(GameProcessState.JokerDrawProcess);

            // TODO: CardManager에서 드로우 기능 구현 후 연결

            // 임시로 프로세스 종료
            InGameManager.Instance.EndProcess();
        }

        /// <summary>
        /// 카드 삭제 대상 선택 시작
        /// </summary>
        private void StartDeleteTargetSelection()
        {
            Debug.Log("[JokerModeSelector] Delete 대상 선택 시작");

            InGameManager.Instance.StartProcess(GameProcessState.JokerDeleteProcess);

            // 1. 모든 카드의 GLOW 먼저 제거 (추가)
            ResetAllGlowEffects();

            // 2. 상대 필드 카드만 초록 GLOW 효과 부여
            var fieldCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Opponent)
                {
                    card.SetCardState(true, Global.GlowGreen); // 초록색으로!
                }
            }

            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.Delete, OnDeleteTargetSelected);
        }

        /// <summary>
        /// 카드 교환 대상 선택 시작
        /// </summary>
        private void StartSwapTargetSelection()
        {
            Debug.Log("[JokerModeSelector] Swap 대상 선택 시작");

            // 프로세스 시작
            InGameManager.Instance.StartProcess(GameProcessState.JokerSwapProcess);

            // 내 필드 카드만 Glow 효과 부여
            var fieldCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    card.SetCardState(true, Global.GlowGreen);
                }
            }

            // 대상 선택 대기 상태로 전환
            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapFirst, OnSwapFirstTargetSelected);
        }

        /// <summary>
        /// 삭제 대상이 선택되었을 때
        /// </summary>
        private void OnDeleteTargetSelected(Card target)
        {
            if (target == null) return;

            // Glow 효과 제거
            ResetAllGlowEffects();

            // 순차적 삭제 시작: 대상 카드 → 조커 카드
            StartCoroutine(DeleteCardSequence(target));
        }

        /// <summary>
        /// 카드 삭제 시퀀스 (대상 카드 → 조커 카드)
        /// </summary>
        private IEnumerator DeleteCardSequence(Card targetCard)
        {
            // 1. 대상 카드 삭제 애니메이션
            CardZone targetZone = FindZoneOfCard(targetCard.transform);

            // 애니메이션 시작
            yield return StartCoroutine(targetCard.AnimateRemoval(() =>
            {
                // 애니메이션 완료 후 Zone에서 제거
                if (targetZone != null)
                {
                    targetZone.RemoveCard(targetCard.transform);
                }
                Destroy(targetCard.gameObject);
            }));

            // 2. 약간의 대기 시간 (선택사항)
            yield return new WaitForSeconds(0.2f);

            // 3. 조커 카드 삭제 (Zone 정렬 포함)
            if (selectedJokerCard != null)
            {
                RemoveUsedJokerCard();
            }

            // 4. 프로세스 종료
            InGameManager.Instance.EndProcess();

            // 공격 가능한 카드들에게 다시 GLOW 적용
            RestoreAttackableCardGlow();
        }

        /// <summary>
        /// 공격 가능한 내 카드들에게 다시 GLOW 적용
        /// </summary>
        private void RestoreAttackableCardGlow()
        {
            var fieldCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                // 내 필드의 공격 가능한 카드만 GLOW 복원
                if (card.CurrentOwnerType == CardZone.OwnerType.Player &&
                    card.CurrentZoneType == CardZone.ZoneType.Field &&
                    card.IsAttackableThisTurn())
                {
                    card.SetCardState(true, Global.GlowGreen);
                }
            }
        }

        /// <summary>
        /// 교환 첫 번째 대상이 선택되었을 때
        /// </summary>
        private void OnSwapFirstTargetSelected(Card firstTarget)
        {
            if (firstTarget == null) return;

            // 모든 Glow 제거 후 상대 필드만 Glow
            ResetAllGlowEffects();

            var fieldCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Opponent)
                {
                    card.SetCardState(true, Global.GlowRed);
                }
            }

            // 두 번째 대상 선택 대기
            JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.SwapSecond,
                (secondTarget) => OnSwapSecondTargetSelected(firstTarget, secondTarget));
        }

        /// <summary>
        /// 교환 두 번째 대상이 선택되었을 때
        /// </summary>
        private void OnSwapSecondTargetSelected(Card firstTarget, Card secondTarget)
        {
            if (firstTarget == null || secondTarget == null) return;

            // 두 카드의 위치 교환
            Transform firstParent = firstTarget.transform.parent;
            Transform secondParent = secondTarget.transform.parent;

            firstTarget.transform.SetParent(secondParent);
            secondTarget.transform.SetParent(firstParent);

            // Zone 정보 업데이트
            var firstZone = firstParent.GetComponent<CardZone>();
            var secondZone = secondParent.GetComponent<CardZone>();

            if (firstZone != null && secondZone != null)
            {
                firstZone.UpdateLayout();
                secondZone.UpdateLayout();
            }

            // Glow 효과 제거
            ResetAllGlowEffects();

            // 조커 카드 제거
            RemoveUsedJokerCard();

            // 프로세스 종료
            InGameManager.Instance.EndProcess();
        }

        /// <summary>
        /// 모든 카드의 Glow 효과 제거
        /// </summary>
        private void ResetAllGlowEffects()
        {
            var allCards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in allCards)
            {
                card.SetCardState(false);
            }
        }

        /// <summary>
        /// 사용한 조커 카드 제거
        /// </summary>
        private void RemoveUsedJokerCard()
        {
            if (selectedJokerCard == null) return;

            // 1. 조커 카드를 시각적으로 먼저 페이드 아웃
            StartCoroutine(RemoveJokerCardWithAnimation(selectedJokerCard));
        }

        /// <summary>
        /// 조커 카드를 애니메이션과 함께 제거
        /// </summary>
        private IEnumerator RemoveJokerCardWithAnimation(Card jokerCard)
        {
            // 카드가 속한 Zone 미리 찾아두기
            CardZone zone = FindZoneOfCard(jokerCard.transform);

            // Card의 공통 애니메이션 사용
            yield return StartCoroutine(jokerCard.AnimateRemoval(() =>
            {
                // 애니메이션 완료 후 Zone에서 제거
                if (zone != null)
                {
                    zone.RemoveCard(jokerCard.transform);
                }

                // 오브젝트 파괴
                Destroy(jokerCard.gameObject);
            }));
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

        /// <summary>
        /// 취소 버튼 클릭 시 호출
        /// </summary>
        public void OnCancelPressed()
        {
            Hide();
        }

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
    }
}
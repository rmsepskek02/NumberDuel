using UnityEngine;
using Objects;
using Manager;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

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
                instance = FindObjectOfType<JokerModeSelector>();
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
        // 각 효과별로 기본 스프라이트 설정 (예: 초록색)
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
                break;
            case JokerEffectType.Delete:
                StartDeleteTargetSelection();
                break;
            case JokerEffectType.Swap:
                StartSwapTargetSelection();
                break;
        }

        // 사용한 조커 카드 제거
        RemoveUsedJokerCard();
    }

    /// <summary>
    /// 카드 드로우 효과 실행
    /// </summary>
    private void ExecuteDrawEffect()
    {
        Debug.Log("[JokerModeSelector] Draw 효과 실행 - 구현 예정");
        // TODO: CardManager에서 드로우 기능 구현 후 연결
    }

    /// <summary>
    /// 카드 삭제 대상 선택 시작
    /// </summary>
    private void StartDeleteTargetSelection()
    {
        Debug.Log("[JokerModeSelector] Delete 대상 선택 시작");

        // 모든 필드 카드에 Glow 효과 부여
        var fieldCards = InGameManager.Instance.GetAllFieldCards();
        foreach (var card in fieldCards)
        {
            card.SetCardState(true, Global.GlowGreen);
        }

        // 대상 선택 대기 상태로 전환
        JokerTargetSelector.Instance.StartTargetSelection(JokerTargetMode.Delete, OnDeleteTargetSelected);
    }

    /// <summary>
    /// 카드 교환 대상 선택 시작
    /// </summary>
    private void StartSwapTargetSelection()
    {
        Debug.Log("[JokerModeSelector] Swap 대상 선택 시작");

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

        // 카드 삭제 처리
        CardZone targetZone = FindZoneOfCard(target.transform);
        if (targetZone != null)
        {
            targetZone.RemoveCard(target.transform);
            Destroy(target.gameObject, 0.5f);
        }

        // Glow 효과 제거
        ResetAllGlowEffects();
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
    /// 사용한 조커 카드 제거 (개선된 버전)
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

        // 1. 카드 상호작용 비활성화 (클릭/드래그 방지)
        ObjectMouseEvent mouseEvent = jokerCard.GetComponentInChildren<ObjectMouseEvent>();
        if (mouseEvent != null)
        {
            mouseEvent.isClickable = false;
            mouseEvent.isDraggable = false;
        }

        // 모든 시각적 요소 찾기
        List<SpriteRenderer> spritesToFade = new List<SpriteRenderer>();
        List<TextMeshPro> textsToFade = new List<TextMeshPro>();

        // SpriteRenderer 찾기
        foreach (Transform child in jokerCard.transform.GetComponentsInChildren<Transform>())
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                spritesToFade.Add(sr);
            }
        }

        // 페이드 애니메이션
        foreach (var sr in spritesToFade)
        {
            sr.DOFade(0f, animDuration);
        }

        foreach (var text in textsToFade)
        {
            text.DOFade(0f, animDuration);
        }

        // 스케일 + 이동 애니메이션
        jokerCard.transform.DOScale(Vector3.one * 0.8f, animDuration).SetEase(Ease.InQuad);
        jokerCard.transform.DOLocalMoveY(jokerCard.transform.localPosition.y + 30f, animDuration).SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(animDuration / 3);

        if (zone != null)
        {
            zone.RemoveCard(jokerCard.transform);
        }

        jokerCard.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        Destroy(jokerCard.gameObject);
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

/// <summary>
/// 조커 효과 타입
/// </summary>
public enum JokerEffectType
{
    Draw,
    Delete,
    Swap
}
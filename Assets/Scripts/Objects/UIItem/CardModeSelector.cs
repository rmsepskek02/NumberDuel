using UnityEngine;
using Objects;
using Manager;
using TMPro;
using DG.Tweening;

/// <summary>
/// 사용자가 카드를 드래그해 필드로 가져오면,
/// 어떤 방식(Open/Secret)으로 낼지를 선택하는 UI를 제어한다.
/// </summary>
public class CardModeSelector : MonoBehaviour
{
    [Header("연결할 오브젝트")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private CardModeOption openOption;
    [SerializeField] private CardModeOption secretOption;
    [SerializeField] private TextMeshPro openValueText;

    //[SerializeField] private ObjectMouseEvent cancelButtonEvent;

    private Transform pendingCard;
    private CardZone targetZone;
    private ObjectMouseEvent bgClick;
    [SerializeField] private float maxScale = 30f;

    private void Start()
    {
        openOption.SetSelector(this);
        secretOption.SetSelector(this);

        // 배경 클릭 시 Cancel 처리
        bgClick = dimBackground.GetComponent<ObjectMouseEvent>();
        if (bgClick != null)
            bgClick.OnClickReleased += OnCancelPressed;

        // Open 카드 Sprite 설정
        SpriteRenderer sr = openOption.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.sprite = ResourcesManager.Instance.GetPlayerSprite();

        //if (cancelButtonEvent != null)
        //    cancelButtonEvent.OnClickReleased += OnCancelPressed;

        SetUIActive(false);
    }

    private void OnEnable()
    {
        CardPlayDetector.OnCardPlayRequested += HandleCardPlayRequested;
    }

    private void OnDisable()
    {
        CardPlayDetector.OnCardPlayRequested -= HandleCardPlayRequested;
        
        if (bgClick != null)
            bgClick.OnClickReleased -= OnCancelPressed;
    }

    private void HandleCardPlayRequested(Transform card, CardZone zone)
    {
        Show(card, zone);
    }

    /// <summary>
    /// 카드가 제출될 준비가 되었을 때 UI를 표시하고
    /// 선택할 수 있도록 구성한다.
    /// </summary>
    public void Show(Transform card, CardZone zone)
    {
        pendingCard = card;
        targetZone = zone;

        SetUIActive(true);

        // 초기 스케일 설정
        openOption.transform.localScale = Vector3.zero;
        secretOption.transform.localScale = Vector3.zero;

        // 카드 텍스트 설정
        var cardText = card.GetComponentInChildren<CardText>();
        if (cardText != null && openValueText != null)
        {
            openValueText.text = "Open\n" + cardText.TextValue;
        }

        // DOTween 애니메이션 실행
        float duration = 0.2f;
        Ease easeType = Ease.OutBack;

        openOption.transform
            .DOScale(Vector3.one * maxScale, duration)
            .SetEase(easeType);

        secretOption.transform
            .DOScale(Vector3.one * maxScale, duration)
            .SetEase(easeType);
    }

    /// <summary>
    /// 모드 선택 UI를 숨기고 내부 상태를 초기화한다.
    /// </summary>
    public void Hide()
    {
        pendingCard = null;
        targetZone = null;

        if (openValueText != null)
            openValueText.text = "";

        SetUIActive(false);
    }

    /// <summary>
    /// 사용자가 Open 또는 Secret 중 하나를 선택했을 때 호출됨
    /// 선택된 카드가 지정된 Zone에 추가된다.
    /// </summary>
    public void OnCardModeSelected(CardModeType mode)
    {
        if (pendingCard == null || targetZone == null)
            return;

        // 기존 Zone에서 제거 (Card.CurrentZoneType 활용)
        Card cardComponent = pendingCard.GetComponentInChildren<Card>();
        if (cardComponent != null)
        {
            foreach (var zone in CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>())
            {
                if (zone.Zone == cardComponent.CurrentZoneType 
                    && zone.Owner == cardComponent.CurrentOwnerType)
                {
                    zone.RemoveCard(pendingCard);
                    break;
                }
            }

            // Open으로 낸 카드만, 필드에 있을 때, 이번 턴에 수정된 적이 없어야 공격 가능
            bool isField = targetZone.Zone == CardZone.ZoneType.Field;
            bool isPlayerCard = targetZone.Owner == CardZone.OwnerType.Player;
            bool isOpen = mode == CardModeType.Open;

            if (isField && isPlayerCard)
            {
                cardComponent.SetCanAttack(isOpen);
            }
        }

        // Secret 모드일 경우 시각 효과 적용
        if (mode == CardModeType.Secret)
        {
            cardComponent.SetSecret(true);
        }
        else if(mode == CardModeType.Open)
        {
            cardComponent.SetSecret(false);
        }

        // 이 시점에서 CardMotion/DragHandler 제거
        DragHandler drag = pendingCard.GetComponent<DragHandler>();
        if (drag != null) Destroy(drag);

        CardMotion motion = pendingCard.GetComponentInChildren<CardMotion>();
        if (motion != null) Destroy(motion);

        // Zone에 카드 추가
        targetZone.AddCard(pendingCard);

        Hide();
    }

    /// <summary>
    /// 카드가 현재 속해있는 CardZone 부모 오브젝트 찾는 함수
    /// </summary>
    //private CardZone FindZoneOfCard(Transform card)
    //{
    //    if (AllZonesRoot == null || card == null) return null;

    //    foreach (var zone in AllZonesRoot.GetComponentsInChildren<CardZone>())
    //    {
    //        if (zone.Contains(card))
    //            return zone;
    //    }

    //    return null;
    //}

    /// <summary>
    /// 취소 버튼 클릭 시 호출됨.
    /// 현재는 UI만 닫고 카드 복귀는 미구현 상태.
    /// </summary>
    public void OnCancelPressed()
    {
        Hide();
    }

    /// <summary>
    /// 하위 UI 오브젝트들을 일괄로 켜거나 끈다.
    /// 루트 오브젝트는 항상 활성 상태로 유지됨.
    /// </summary>
    private void SetUIActive(bool active)
    {
        if (dimBackground != null) dimBackground.SetActive(active);
        if (cancelButton != null) cancelButton.SetActive(active);
        if (openOption != null) openOption.gameObject.SetActive(active);
        if (secretOption != null) secretOption.gameObject.SetActive(active);
    }
}

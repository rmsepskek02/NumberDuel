using UnityEngine;
using Objects;
using Manager;
using TMPro;
using DG.Tweening;

/// <summary>
/// 사용자가 카드를 드래그해 필드에 배치하려할때,
/// 어떤 모드(Open/Secret)로 놓을지 선택하는 UI를 관리한다.
/// </summary>
public class CardModeSelector : MonoBehaviour
{
    #region Fields and Properties
    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private CardModeOption openOption;
    [SerializeField] private CardModeOption secretOption;
    [SerializeField] private TextMeshPro openValueText;

    private Transform pendingCard;
    private CardZone targetZone;
    private ObjectMouseEvent bgClick;

    [Header("애니메이션 설정")]
    [SerializeField] private float maxScale = 30f;
    [SerializeField] private float animDuration = 0.2f;

    private bool isSpritesSet = false;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        openOption.SetSelector(this);
        secretOption.SetSelector(this);

        // 배경 클릭 시 Cancel 처리
        bgClick = dimBackground.GetComponent<ObjectMouseEvent>();
        if (bgClick != null)
            bgClick.OnClickReleased += OnCancelPressed;

        // 스프라이트 설정은 Show() 호출시까지 대기 (색상 동기화 완료 후)
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
    #endregion

    #region Event Handlers
    private void HandleCardPlayRequested(Transform card, CardZone zone)
    {
        Show(card, zone);
    }
    #endregion

    #region UI Display
    /// <summary>
    /// 카드가 배치될 준비 되었을 때 UI를 표시하고
    /// 사용자가 선택할 수 있도록 대기한다.
    /// </summary>
    public void Show(Transform card, CardZone zone)
    {
        pendingCard = card;
        targetZone = zone;

        // 첫 Show() 호출 상황에서 스프라이트 설정
        if (!isSpritesSet)
        {
            SetOpenOptionSprite();
            isSpritesSet = true;
        }

        SetUIActive(true);

        // 초기 스케일 리셋
        openOption.transform.localScale = Vector3.zero;
        secretOption.transform.localScale = Vector3.zero;

        // 카드 텍스트 표시
        var cardText = card.GetComponentInChildren<CardText>();
        if (cardText != null && openValueText != null)
        {
            openValueText.text = "Open\n" + cardText.TextValue;
        }

        // DOTween 애니메이션 재생
        Ease easeType = Ease.OutBack;

        openOption.transform
            .DOScale(Vector3.one * maxScale, animDuration)
            .SetEase(easeType);

        secretOption.transform
            .DOScale(Vector3.one * maxScale, animDuration)
            .SetEase(easeType);
    }

    /// <summary>
    /// 선택 창을 UI를 숨기고 대기 상태를 초기화한다.
    /// </summary>
    public void Hide()
    {
        pendingCard = null;
        targetZone = null;

        if (openValueText != null)
            openValueText.text = "";

        SetUIActive(false);
    }
    #endregion

    #region Sprite Setup
    /// <summary>
    /// Open 옵션에 현재 플레이어 색상의 empty 스프라이트 설정
    /// 첫 Show() 호출 상황에서 실행되며 이후 재사용
    /// </summary>
    private void SetOpenOptionSprite()
    {
        SpriteRenderer openSr = openOption.GetComponentInChildren<SpriteRenderer>();
        if (openSr == null)
        {
            return;
        }

        // ResourcesManager에서 현재 플레이어 색상 스프라이트 가져오기
        if (ResourcesManager.Instance != null)
        {
            Sprite playerSprite = ResourcesManager.Instance.GetPlayerSprite();
            if (playerSprite != null)
            {
                openSr.sprite = playerSprite;
            }
        }
    }
    #endregion

    #region Mode Selection
    /// <summary>
    /// 사용자가 Open 또는 Secret 중 하나를 선택했을 때 호출됨 (옵션 버튼에서)
    /// 선택된 카드가 대상으로 Zone에 추가된다.
    /// </summary>
    public void OnCardModeSelected(CardModeType mode)
    {
        if (pendingCard == null || targetZone == null)
            return;

        Card cardComponent = pendingCard.GetComponentInChildren<Card>();
        if (cardComponent == null)
        {
            return;
        }

        // 현재 Zone에서 제거
        RemoveCardFromCurrentZone(cardComponent);

        // Open/Secret 상태 설정 (GLOW 상태 자동 제거!)
        if (mode == CardModeType.Secret)
        {
            cardComponent.SetSecret(true);
        }
        else if (mode == CardModeType.Open)
        {
            cardComponent.SetSecret(false);
        }

        // 드래그 관련 컴포넌트 제거
        RemoveDragComponents();

        // Zone에 카드 추가 (Card.cs가 알아서 GLOW 제거)
        targetZone.AddCard(pendingCard);

        Hide();
    }

    /// <summary>
    /// 카드를 현재 Zone에서 제거 (손패에서)
    /// </summary>
    private void RemoveCardFromCurrentZone(Card cardComponent)
    {
        if (CardZone.AllZonesRoot == null) return;

        foreach (var zone in CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>())
        {
            if (zone.Zone == cardComponent.CurrentZoneType
                && zone.Owner == cardComponent.CurrentOwnerType)
            {
                zone.RemoveCard(pendingCard);
                break;
            }
        }
    }

    /// <summary>
    /// 드래그 관련 컴포넌트 제거 (분리됨)
    /// </summary>
    private void RemoveDragComponents()
    {
        DragHandler drag = pendingCard.GetComponent<DragHandler>();
        if (drag != null) Destroy(drag);

        CardMotion motion = pendingCard.GetComponentInChildren<CardMotion>();
        if (motion != null) Destroy(motion);
    }

    /// <summary>
    /// 취소 버튼 클릭 시 호출됨.
    /// </summary>
    public void OnCancelPressed()
    {
        Hide();
    }
    #endregion

    #region UI Management
    /// <summary>
    /// 전체 UI 오브젝트들을 켜거나 끈다.
    /// </summary>
    private void SetUIActive(bool active)
    {
        if (dimBackground != null) dimBackground.SetActive(active);
        if (cancelButton != null) cancelButton.SetActive(active);
        if (openOption != null) openOption.gameObject.SetActive(active);
        if (secretOption != null) secretOption.gameObject.SetActive(active);
    }
    #endregion
}

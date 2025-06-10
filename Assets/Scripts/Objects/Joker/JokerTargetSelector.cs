using UnityEngine;
using Objects;
using System;

/// <summary>
/// 조커 효과의 대상 카드를 선택하는 기능을 관리
/// Delete와 Swap 효과에서 사용됨
/// </summary>
public class JokerTargetSelector : MonoBehaviour
{
    private static JokerTargetSelector instance;
    public static JokerTargetSelector Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<JokerTargetSelector>();
            return instance;
        }
    }

    private JokerTargetMode currentMode = JokerTargetMode.None;
    private Action<Card> currentCallback;
    private Card firstSelectedCard;

    private void OnEnable()
    {
        // 카드 클릭 이벤트 구독
        Card.onClicked += HandleCardClicked;
    }

    private void OnDisable()
    {
        // 카드 클릭 이벤트 구독 해제
        Card.onClicked -= HandleCardClicked;
    }

    /// <summary>
    /// 대상 선택 모드 시작
    /// </summary>
    public void StartTargetSelection(JokerTargetMode mode, Action<Card> onTargetSelected)
    {
        currentMode = mode;
        currentCallback = onTargetSelected;
        firstSelectedCard = null;

        Debug.Log($"[JokerTargetSelector] {mode} 모드로 대상 선택 시작");
    }

    /// <summary>
    /// 대상 선택 모드 종료
    /// </summary>
    public void EndTargetSelection()
    {
        currentMode = JokerTargetMode.None;
        currentCallback = null;
        firstSelectedCard = null;

        Debug.Log("[JokerTargetSelector] 대상 선택 모드 종료");
    }

    /// <summary>
    /// 카드가 클릭되었을 때 처리
    /// </summary>
    private void HandleCardClicked(Card clickedCard)
    {
        if (currentMode == JokerTargetMode.None || clickedCard == null)
            return;

        // 조커 카드는 대상으로 선택 불가
        if (clickedCard.CardType == CardType.Joker)
        {
            Debug.Log("[JokerTargetSelector] 조커 카드는 대상으로 선택할 수 없습니다.");
            return;
        }

        switch (currentMode)
        {
            case JokerTargetMode.Delete:
                HandleDeleteTarget(clickedCard);
                break;

            case JokerTargetMode.SwapFirst:
                HandleSwapFirstTarget(clickedCard);
                break;

            case JokerTargetMode.SwapSecond:
                HandleSwapSecondTarget(clickedCard);
                break;
        }
    }

    /// <summary>
    /// 삭제 대상 처리
    /// </summary>
    private void HandleDeleteTarget(Card target)
    {
        // 필드에 있는 카드만 삭제 가능
        if (target.CurrentZoneType != CardZone.ZoneType.Field)
        {
            Debug.Log("[JokerTargetSelector] 필드에 있는 카드만 삭제할 수 있습니다.");
            return;
        }

        // Glow 효과가 있는 카드만 선택 가능
        var effect = target.GetComponentInChildren<CardEffect>();
        if (effect == null || !effect.IsGlowing())
        {
            Debug.Log("[JokerTargetSelector] 선택할 수 없는 카드입니다.");
            return;
        }

        Debug.Log($"[JokerTargetSelector] 삭제 대상 선택됨: {target.name}");

        // 콜백 실행 후 모드 종료
        currentCallback?.Invoke(target);
        EndTargetSelection();
    }

    /// <summary>
    /// 교환 첫 번째 대상 처리
    /// </summary>
    private void HandleSwapFirstTarget(Card target)
    {
        // 내 필드 카드만 선택 가능
        if (target.CurrentZoneType != CardZone.ZoneType.Field ||
            target.CurrentOwnerType != CardZone.OwnerType.Player)
        {
            Debug.Log("[JokerTargetSelector] 내 필드의 카드만 선택할 수 있습니다.");
            return;
        }

        // Glow 효과가 있는 카드만 선택 가능
        var effect = target.GetComponentInChildren<CardEffect>();
        if (effect == null || !effect.IsGlowing())
        {
            Debug.Log("[JokerTargetSelector] 선택할 수 없는 카드입니다.");
            return;
        }

        firstSelectedCard = target;
        Debug.Log($"[JokerTargetSelector] 교환 첫 번째 대상 선택됨: {target.name}");

        // 첫 번째 카드 시각적 표시 (선택됨 표시)
        target.SetCardState(true, Color.cyan);

        // 콜백 실행 (다음 선택을 위해)
        currentCallback?.Invoke(target);

        // 모드 변경하지 않음 (JokerModeSelector에서 변경할 것)
    }

    /// <summary>
    /// 교환 두 번째 대상 처리
    /// </summary>
    private void HandleSwapSecondTarget(Card target)
    {
        // 상대 필드 카드만 선택 가능
        if (target.CurrentZoneType != CardZone.ZoneType.Field ||
            target.CurrentOwnerType != CardZone.OwnerType.Opponent)
        {
            Debug.Log("[JokerTargetSelector] 상대 필드의 카드만 선택할 수 있습니다.");
            return;
        }

        // Glow 효과가 있는 카드만 선택 가능
        var effect = target.GetComponentInChildren<CardEffect>();
        if (effect == null || !effect.IsGlowing())
        {
            Debug.Log("[JokerTargetSelector] 선택할 수 없는 카드입니다.");
            return;
        }

        Debug.Log($"[JokerTargetSelector] 교환 두 번째 대상 선택됨: {target.name}");

        // 콜백 실행 후 모드 종료
        currentCallback?.Invoke(target);
        EndTargetSelection();
    }

    /// <summary>
    /// 현재 선택 모드 확인
    /// </summary>
    public bool IsSelecting()
    {
        return currentMode != JokerTargetMode.None;
    }

    /// <summary>
    /// 현재 모드 가져오기
    /// </summary>
    public JokerTargetMode GetCurrentMode()
    {
        return currentMode;
    }
}

/// <summary>
/// 조커 대상 선택 모드
/// </summary>
public enum JokerTargetMode
{
    None,           // 선택 모드 아님
    Delete,         // 삭제할 카드 선택
    SwapFirst,      // 교환할 첫 번째 카드 선택
    SwapSecond      // 교환할 두 번째 카드 선택
}

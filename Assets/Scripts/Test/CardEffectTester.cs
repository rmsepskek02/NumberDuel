using Objects;
using UnityEngine;

public class CardEffectTester : MonoBehaviour
{
    private bool glowing = false;
    private bool colorAtoB = true;

    // Global에서 정의한 Glow 색상 사용
    private readonly Color colorA = Global.GlowGreen;
    private readonly Color colorB = Global.GlowRed;

    public void ToggleGlowAll()
    {
        glowing = !glowing;

        CardEffect[] cards = FindObjectsByType<CardEffect>(FindObjectsSortMode.None);

        foreach (var card in cards)
        {
            if (card != null && card.gameObject != null && card.enabled)
            {
                card.SetGlow(glowing);
            }
        }
    }

    public void ToggleGlowColorAll()
    {
        CardEffect[] cards = FindObjectsByType<CardEffect>(FindObjectsSortMode.None);

        colorAtoB = !colorAtoB;
        Color target = colorAtoB ? colorA : colorB;

        foreach (var card in cards)
        {
            if (card != null)
                card.LerpGlowColor(target, 0.4f);
        }
    }
}

using Objects;
using UnityEngine;

public class CardTestSpawner : MonoBehaviour
{
    public GameObject cardPrefab;
    public CardZone playerHandZone;

    public int spawnCount = 10;

    void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject card = Instantiate(cardPrefab);
            card.name = $"Card_{i + 1}";
            playerHandZone.AddCard(card.transform);
        }
    }
}

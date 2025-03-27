using UnityEngine;
using Objects;

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

            // 초기 위치 리셋
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            // 카드존에 등록
            playerHandZone.AddCard(card.transform);
        }
    }
}

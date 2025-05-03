using UnityEngine;
using Objects;

public class CardTestSpawner : MonoBehaviour
{
    public CardZone playerHandZone;
    public int spawnCount = 10;

    void Start()
    {
        GameObject template = playerHandZone.Owner == CardZone.OwnerType.Player
            ? Manager.ResourcesManager.Instance.GetPlayerCardTemplate()
            : Manager.ResourcesManager.Instance.GetOpponentCardTemplate();

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject card = Instantiate(template);
            card.name = $"Card_{i + 1}";

            card.SetActive(true);

            // 초기 위치 리셋
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            // 카드존에 등록
            playerHandZone.AddCard(card.transform);
        }
    }
}

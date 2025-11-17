using Objects;
using UnityEngine;

public class CardTestSpawner : MonoBehaviour
{
    public CardZone playerHandZone;
    public int spawnCount = 10; // 4 숫자 + 4 연산자 + 2 조커

    private void Start()
    {
        GameObject template = playerHandZone.Owner == CardZone.OwnerType.Player
            ? Manager.ResourcesManager.Instance.GetPlayerCardTemplate()
            : Manager.ResourcesManager.Instance.GetOpponentCardTemplate();

        // 1. 숫자 카드 4장
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject card = Instantiate(template);
            card.name = $"NumCard_{i + 1}";
            card.SetActive(true);
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            var cardComponent = card.GetComponent<Card>();
            cardComponent.InitializeAsNumber(Random.Range(1, 6));

            playerHandZone.AddCard(card.transform);
        }

        // 2. 연산자 카드 4장
        //OperatorType[] ops = { OperatorType.Plus, OperatorType.Minus, OperatorType.Multiply, OperatorType.Divide };
        //for (int i = 0; i < ops.Length; i++)
        //{
        //    GameObject card = Instantiate(template);
        //    card.name = $"OpCard_{ops[i]}";
        //    card.SetActive(true);
        //    card.transform.localPosition = Vector3.zero;
        //    card.transform.localRotation = Quaternion.identity;

        //    var cardComponent = card.GetComponent<Card>();
        //    cardComponent.InitializeAsOperator(ops[i]);

        //    playerHandZone.AddCard(card.transform);
        //}

        //// 3. 조커 카드 2장
        //for (int i = 0; i < 2; i++)
        //{
        //    GameObject card = Instantiate(template);
        //    card.name = $"JokerCard_{i + 1}";
        //    card.SetActive(true);
        //    card.transform.localPosition = Vector3.zero;
        //    card.transform.localRotation = Quaternion.identity;

        //    var cardComponent = card.GetComponent<Card>();
        //    cardComponent.InitializeAsJoker();

        //    playerHandZone.AddCard(card.transform);
        //}
    }
}
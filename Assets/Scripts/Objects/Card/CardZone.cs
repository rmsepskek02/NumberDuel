using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    public class CardZone : MonoBehaviour
    {
        public enum ZoneType { Hand, Field }
        public enum OwnerType { Player, Opponent }

        [Header("Zone Settings")]
        public ZoneType zoneType;
        public OwnerType ownerType;

        [Header("Layout Settings")]
        public float spacing = 2f;
        public float fanRadius = 5f;
        public float fanAngle = 30f;
        public int maxFieldCards = 5;

        private readonly List<Transform> cards = new();

        public void AddCard(Transform card)
        {
            if (!cards.Contains(card))
            {
                cards.Add(card);
                card.SetParent(transform);
                UpdateLayout();

                if (zoneType == ZoneType.Hand)
                    AddHover(card);
            }
        }

        public void RemoveCard(Transform card)
        {
            if (cards.Contains(card))
            {
                cards.Remove(card);
                UpdateLayout();

                if (zoneType == ZoneType.Hand)
                    RemoveHover(card);
            }
        }

        private void AddHover(Transform card)
        {
            if (!card.TryGetComponent(out HoverCardMotion hover))
                hover = card.gameObject.AddComponent<HoverCardMotion>();

            StartCoroutine(DelaySetInitialState(hover));
        }

        private System.Collections.IEnumerator DelaySetInitialState(HoverCardMotion hover)
        {
            yield return null; // 1 프레임 대기 후 위치가 안정된 뒤에
            hover.SetInitialState();
        }


        private void RemoveHover(Transform card)
        {
            if (card.TryGetComponent(out HoverCardMotion hover))
                Destroy(hover);
        }

        public void UpdateLayout()
        {
            if (zoneType == ZoneType.Hand)
                ArrangeFanLayout();
            else
                ArrangeFieldLayout();
        }

        private void ArrangeFanLayout()
        {
            int count = cards.Count;
            float angleStep = fanAngle / Mathf.Max(count - 1, 1);
            float startAngle = -fanAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 localPos = new Vector3(Mathf.Sin(rad), i * 0.01f + 0.01f, Mathf.Cos(rad)) * fanRadius;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);

                Transform card = cards[i];
                card.localPosition = localPos;
                card.localRotation = rotation;
            }
        }

        private void ArrangeFieldLayout()
        {
            int count = cards.Count;
            float totalWidth = (maxFieldCards - 1) * spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                Vector3 localPos = new Vector3(startX + i * spacing, 0, 0);
                Transform card = cards[i];
                card.localPosition = localPos;
                card.localRotation = Quaternion.identity;
            }
        }
    }
}

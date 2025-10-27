using Objects;
using System;
using UnityEngine;

namespace Manager
{
    [RequireComponent(typeof(Card))]
    public class NetworkCard : MonoBehaviour
    {
        #region Fields and Properties
        [Header("네트워크 고유 식별 (디버그용 - 읽기 전용)")]
        [SerializeField] private string debugUniqueId = "미생성";

        [Header("위치 정보")]
        [SerializeField] private CardZone.OwnerType currentOwner;
        [SerializeField] private CardZone.ZoneType currentZone;
        [SerializeField] private int currentIndex;

        private string uniqueId;

        private Card cardComponent;
        private bool isInitialized = false;
        public string UniqueId => uniqueId;
        public CardZone.OwnerType CurrentOwner => currentOwner;
        public CardZone.ZoneType CurrentZone => currentZone;
        public int CurrentIndex => currentIndex;
        public string NetworkReference => $"{uniqueId}_{currentOwner}_{currentZone}_{currentIndex}";
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            cardComponent = GetComponent<Card>();
            GenerateUniqueId();
        }

        private void Start()
        {
            RegisterToNetworkGameManager();
            UpdateLocationInfo();
            isInitialized = true;
        }
        #endregion

        #region Initialization
        private void GenerateUniqueId()
        {
            uniqueId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            debugUniqueId = uniqueId;
        }

        public void SetUniqueId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            if (!string.IsNullOrEmpty(uniqueId) && uniqueId != id)
            {
                UnregisterFromNetworkGameManager();
            }

            uniqueId = id;
            debugUniqueId = id;
            RegisterToNetworkGameManager();
        }

        private void RegisterToNetworkGameManager()
        {
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RegisterNetworkCard(this);
            }
        }

        private void UnregisterFromNetworkGameManager()
        {
            if (NetworkGameManager.Instance != null && !string.IsNullOrEmpty(uniqueId))
            {
                NetworkGameManager.Instance.UnregisterNetworkCard(uniqueId);
            }
        }
        #endregion

        #region Location Management
        public void UpdateLocationInfo()
        {
            CardZone parentZone = GetComponentInParent<CardZone>();
            if (parentZone == null)
                return;

            currentOwner = parentZone.Owner;
            currentZone = parentZone.Zone;
            currentIndex = GetIndexInZone(parentZone);

            debugUniqueId = uniqueId;
        }

        private int GetIndexInZone(CardZone zone)
        {
            for (int i = 0; i < zone.transform.childCount; i++)
            {
                if (zone.transform.GetChild(i) == transform)
                {
                    return i;
                }
            }
            return -1;
        }
        #endregion

        #region Validation System
        public bool ValidateCurrentState()
        {
            if (cardComponent == null)
                return false;

            CardZone parentZone = GetComponentInParent<CardZone>();
            if (parentZone == null)
                return false;

            if (parentZone.Owner != currentOwner || parentZone.Zone != currentZone)
            {
                UpdateLocationInfo();
                return true;
            }

            int actualIndex = GetIndexInZone(parentZone);
            if (actualIndex != currentIndex)
            {
                currentIndex = actualIndex;
            }

            return true;
        }

        public bool CanPerformAction(NetworkActionType actionType)
        {
            if (!ValidateCurrentState()) return false;

            switch (actionType)
            {
                case NetworkActionType.Attack:
                    return CanAttack();
                case NetworkActionType.UseOperator:
                    return CanUseOperator();
                case NetworkActionType.UseJoker:
                    return CanUseJoker();
                case NetworkActionType.PlaceToField:
                    return CanPlaceToField();
                default:
                    return false;
            }
        }

        private bool CanAttack()
        {
            return currentOwner == CardZone.OwnerType.Player &&
                   currentZone == CardZone.ZoneType.Field &&
                   cardComponent.CanAttack &&
                   cardComponent.IsAttackableThisTurn();
        }

        private bool CanUseOperator()
        {
            return currentOwner == CardZone.OwnerType.Player &&
                   currentZone == CardZone.ZoneType.Hand &&
                   cardComponent.CardType == CardType.Operator;
        }

        private bool CanUseJoker()
        {
            return currentOwner == CardZone.OwnerType.Player &&
                   currentZone == CardZone.ZoneType.Hand &&
                   cardComponent.CardType == CardType.Joker;
        }

        private bool CanPlaceToField()
        {
            return currentOwner == CardZone.OwnerType.Player &&
                   currentZone == CardZone.ZoneType.Hand &&
                   cardComponent.CardType == CardType.Number;
        }
        #endregion

        #region Network Reference Parsing
        public static bool TryParseNetworkReference(string networkRef, out NetworkCardInfo info)
        {
            info = default;

            if (string.IsNullOrEmpty(networkRef))
                return false;

            string[] parts = networkRef.Split('_');
            if (parts.Length < 4)
                return false;

            try
            {
                info.uniqueId = parts[0];
                info.owner = Enum.Parse<CardZone.OwnerType>(parts[1]);
                info.zone = Enum.Parse<CardZone.ZoneType>(parts[2]);
                info.index = int.Parse(parts[3]);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        #region Lifecycle
        private void OnDestroy()
        {
            UnregisterFromNetworkGameManager();
        }
        #endregion

        #region Debug & Utility
        private void OnValidate()
        {
            if (Application.isPlaying && isInitialized)
            {
                UpdateLocationInfo();
                debugUniqueId = uniqueId;
            }
        }
        #endregion
    }

    #region Supporting Types
    public enum NetworkActionType
    {
        Attack,
        UseOperator,
        UseJoker,
        PlaceToField
    }

    public struct NetworkCardInfo
    {
        public string uniqueId;
        public CardZone.OwnerType owner;
        public CardZone.ZoneType zone;
        public int index;

        public string ToNetworkReference()
        {
            return $"{uniqueId}_{owner}_{zone}_{index}";
        }
    }
    #endregion
}
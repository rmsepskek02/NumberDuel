using UnityEngine;
using Objects;

namespace Manager
{
    [RequireComponent(typeof(Card))]
    public class NetworkCard : MonoBehaviour
    {
        [Header("네트워크 식별 정보 (디버그용 - 수정 금지)")]
        [SerializeField] private string debugUniqueId = "미생성";

        [Header("위치 정보")]
        [SerializeField] private CardZone.OwnerType currentOwner;
        [SerializeField] private CardZone.ZoneType currentZone;
        [SerializeField] private int currentIndex;

        private string uniqueId;

        private Card cardComponent;
        private bool isInitialized = false;

        #region Properties
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
            uniqueId = System.Guid.NewGuid().ToString("N")[..8].ToUpper();
            debugUniqueId = uniqueId;
            Debug.Log($"[NetworkCard] ID 생성: {uniqueId} for {gameObject.name}");
        }

        public void SetUniqueId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[NetworkCard] 빈 ID는 설정할 수 없습니다.");
                return;
            }

            if (!string.IsNullOrEmpty(uniqueId) && uniqueId != id)
            {
                UnregisterFromNetworkGameManager();
                Debug.Log($"[NetworkCard] ID 변경: {uniqueId} → {id}");
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
            {
                Debug.LogWarning($"[NetworkCard] {uniqueId}: Zone을 찾을 수 없습니다.");
                return;
            }

            currentOwner = parentZone.Owner;
            currentZone = parentZone.Zone;
            currentIndex = GetIndexInZone(parentZone);

            if (isInitialized)
            {
                Debug.Log($"[NetworkCard] {uniqueId} 위치 업데이트: {NetworkReference}");
            }

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
            {
                Debug.LogError($"[NetworkCard] {uniqueId}: Card 컴포넌트가 없습니다.");
                return false;
            }

            CardZone parentZone = GetComponentInParent<CardZone>();
            if (parentZone == null)
            {
                Debug.LogError($"[NetworkCard] {uniqueId}: 부모 Zone을 찾을 수 없습니다.");
                return false;
            }

            if (parentZone.Owner != currentOwner || parentZone.Zone != currentZone)
            {
                Debug.LogWarning($"[NetworkCard] {uniqueId}: 위치 정보 불일치 감지");
                UpdateLocationInfo();
                return true;
            }

            int actualIndex = GetIndexInZone(parentZone);
            if (actualIndex != currentIndex)
            {
                Debug.LogWarning($"[NetworkCard] {uniqueId}: 인덱스 불일치 {currentIndex} → {actualIndex}");
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
                info.owner = System.Enum.Parse<CardZone.OwnerType>(parts[1]);
                info.zone = System.Enum.Parse<CardZone.ZoneType>(parts[2]);
                info.index = int.Parse(parts[3]);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkCard] 참조 파싱 실패: {networkRef}, 오류: {ex.Message}");
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
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintInfo()
        {
            Debug.Log($"[NetworkCard] {uniqueId}: {NetworkReference}, 유효성: {ValidateCurrentState()}");
        }

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
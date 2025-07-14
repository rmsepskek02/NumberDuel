using UnityEngine;
using Objects;

namespace Manager
{
    /// <summary>
    /// 네트워크 동기화를 위한 카드 식별 및 검증 시스템
    /// 버그 방지를 위한 다중 검증 및 자동 복구 메커니즘 포함
    /// PhotonNetwork.RPC 방식을 위한 간소화된 버전
    /// </summary>
    [RequireComponent(typeof(Card))]
    public class NetworkCard : MonoBehaviour
    {
        [Header("네트워크 식별 정보")]
        [SerializeField] private string uniqueId;
        [SerializeField] private CardZone.OwnerType currentOwner;
        [SerializeField] private CardZone.ZoneType currentZone;
        [SerializeField] private int currentIndex;

        private Card cardComponent;
        private bool isInitialized = false;

        #region Properties
        /// <summary>
        /// 고유 ID (읽기 전용)
        /// </summary>
        public string UniqueId => uniqueId;

        /// <summary>
        /// 현재 소유자
        /// </summary>
        public CardZone.OwnerType CurrentOwner => currentOwner;

        /// <summary>
        /// 현재 Zone
        /// </summary>
        public CardZone.ZoneType CurrentZone => currentZone;

        /// <summary>
        /// 현재 인덱스
        /// </summary>
        public int CurrentIndex => currentIndex;

        /// <summary>
        /// 네트워크 참조 문자열 생성
        /// </summary>
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
            // 카드가 Zone에 배치된 후 초기화
            UpdateLocationInfo();
            isInitialized = true;
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 고유 ID 생성 (8자리 알파뉴메릭)
        /// </summary>
        private void GenerateUniqueId()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                // 더 안전한 ID 생성 (알파뉴메릭만 사용)
                uniqueId = System.Guid.NewGuid().ToString("N")[..8].ToUpper();
            }
        }

        /// <summary>
        /// 외부에서 ID 강제 설정 (네트워크 동기화시 사용)
        /// </summary>
        public void SetUniqueId(string id)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = id;
            }
            else
            {
                Debug.LogWarning($"[NetworkCard] ID가 이미 설정되어 있습니다: {uniqueId}");
            }
        }
        #endregion

        #region Location Management
        /// <summary>
        /// 위치 정보 업데이트 (Zone 변경시 호출)
        /// </summary>
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
        }

        /// <summary>
        /// Zone 내에서의 인덱스 계산
        /// </summary>
        private int GetIndexInZone(CardZone zone)
        {
            for (int i = 0; i < zone.transform.childCount; i++)
            {
                if (zone.transform.GetChild(i) == transform)
                {
                    return i;
                }
            }
            return -1; // 찾을 수 없음
        }
        #endregion

        #region Validation System
        /// <summary>
        /// 현재 카드 상태 검증
        /// </summary>
        public bool ValidateCurrentState()
        {
            // 1단계: 기본 컴포넌트 확인
            if (cardComponent == null)
            {
                Debug.LogError($"[NetworkCard] {uniqueId}: Card 컴포넌트가 없습니다.");
                return false;
            }

            // 2단계: Zone 정보 확인
            CardZone parentZone = GetComponentInParent<CardZone>();
            if (parentZone == null)
            {
                Debug.LogError($"[NetworkCard] {uniqueId}: 부모 Zone을 찾을 수 없습니다.");
                return false;
            }

            // 3단계: 위치 정보 일치 확인
            if (parentZone.Owner != currentOwner || parentZone.Zone != currentZone)
            {
                Debug.LogWarning($"[NetworkCard] {uniqueId}: 위치 정보 불일치 감지");
                UpdateLocationInfo(); // 자동 복구
                return true; // 복구 후 계속 진행
            }

            // 4단계: 인덱스 유효성 확인
            int actualIndex = GetIndexInZone(parentZone);
            if (actualIndex != currentIndex)
            {
                Debug.LogWarning($"[NetworkCard] {uniqueId}: 인덱스 불일치 {currentIndex} → {actualIndex}");
                currentIndex = actualIndex; // 자동 복구
            }

            return true;
        }

        /// <summary>
        /// 특정 액션이 가능한지 검증
        /// </summary>
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
        /// <summary>
        /// 네트워크 참조 문자열 파싱
        /// </summary>
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

        #region Debug & Utility
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintInfo()
        {
            Debug.Log($"[NetworkCard] {uniqueId}: {NetworkReference}, 유효성: {ValidateCurrentState()}");
        }

        /// <summary>
        /// 에디터에서 Inspector 업데이트
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying && isInitialized)
            {
                UpdateLocationInfo();
            }
        }
        #endregion
    }

    #region Supporting Types
    /// <summary>
    /// 네트워크 액션 타입
    /// </summary>
    public enum NetworkActionType
    {
        Attack,
        UseOperator,
        UseJoker,
        PlaceToField
    }

    /// <summary>
    /// 네트워크 카드 정보 구조체
    /// </summary>
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
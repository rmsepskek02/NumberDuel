namespace Objects
{
    /// <summary>
    /// 카드 컴포넌트가 반드시 구현해야 하는 인터페이스
    /// - Zone에서 상호작용 권한 설정 시 사용
    /// </summary>
    public interface ICard
    {
        /// <summary>
        /// 카드의 상호작용 권한을 설정한다
        /// </summary>
        void SetInteraction(CardZone.ZoneType zoneType, CardZone.OwnerType ownerType);
    }
}

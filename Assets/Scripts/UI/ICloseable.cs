namespace UI
{
    /// <summary>
    /// ESC 키로 닫을 수 있는 UI 요소가 구현하는 인터페이스
    /// UIStackManager에서 스택 관리를 위해 사용
    /// </summary>
    public interface ICloseable
    {
        /// <summary>
        /// UI 요소를 닫습니다
        /// </summary>
        void Close();

        /// <summary>
        /// 현재 UI 요소가 닫힐 수 있는 상태인지 확인
        /// (예: 애니메이션 진행 중이면 false)
        /// </summary>
        /// <returns>닫을 수 있으면 true</returns>
        bool CanClose();
    }
}

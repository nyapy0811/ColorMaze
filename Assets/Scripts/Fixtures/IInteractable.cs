namespace ColorMaze
{
    /// <summary>
    /// 조준해 좌클릭으로 상호작용하는 기물(색 원천, 문, 목적지).
    /// 상호작용할 수 있는 동안 기물 기반이 강조를 켠다. 값 제공(조준 값 표시)은 <see cref="IValueProvider"/>로 나눈다.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>지금 상호작용할 수 있는가(흡수한 색 원천, 열린 문, 클리어한 목적지는 아님).</summary>
        bool CanInteract { get; }

        void Interact(PlayerContext player);
    }
}

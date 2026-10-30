namespace ColorMaze
{
    /// <summary>
    /// 조준해 좌클릭으로 상호작용하는 기물(추출 오브젝트, 오브 편집기 계열, 작동기 계열).
    /// 상호작용할 수 있는 동안 기물 기반이 강조를 켠다. 값 제공(조준 값 표시)은 따로 나눈다(코어 루프 8단계).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>지금 상호작용할 수 있는가(추출한 오브젝트, 다 쓴 일회용 작동기는 아님).</summary>
        bool CanInteract { get; }

        void Interact(PlayerContext player);
    }
}

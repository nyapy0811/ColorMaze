namespace ColorMaze
{
    /// <summary>
    /// 합성기(6.3). 조준해 상호작용하면 합성 세션을 만들어 팝업으로 연다. 오브를 보관하지 않으므로 늘 상호작용할 수 있고,
    /// 닫으면 선택을 버린다. 오브 편집기 계열 기반은 두 번째 편집기(필터 등)를 만들 때 공통 부분을 보고 더하고,
    /// '합성함'은 받는 곳(소리·연출)을 만드는 8단계에서 더한다.
    /// </summary>
    public class ColorMixer : FixtureBase, IInteractable
    {
        public bool CanInteract => true;

        public void Interact(PlayerContext player)
        {
            var session = new MixSession(player.Inventory.Orbs);
            Popups.TryOpen(session, session.Close);
        }
    }
}

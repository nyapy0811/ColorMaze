using Framework.Core;

namespace ColorMaze
{
    /// <summary>
    /// 합성기(6.3). 조준해 상호작용하면 합성 세션을 만들어 팝업으로 연다. 오브를 보관하지 않으므로 늘 상호작용할 수 있고,
    /// 닫으면 선택을 버린다. 합성할 때마다 '합성함'을 보낸다. 오브 편집기 계열 기반은 두 번째 편집기(필터 등)를 만들 때
    /// 공통 부분을 보고 더한다.
    /// </summary>
    public class ColorMixer : FixtureBase, IInteractable
    {
        public bool CanInteract => true;

        public void Interact(PlayerContext player)
        {
            var session = new MixSession(player.Inventory.Orbs);
            session.Mixed += result => EventBus.Publish(new OrbsMixed(result));
            Popups.TryOpen(session, session.Close);
        }
    }
}

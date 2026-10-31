using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 플레이어의 소지 오브. 코어 규칙의 소지 오브(<see cref="OrbInventory"/>)를 들고, 용량과 기본 채널 한도는
    /// 맵 매니저의 게임 규칙에서 읽는다(맵 매니저가 먼저 깨어남). 바뀌면 '소지 오브 바뀜'을 보낸다.
    /// Tab으로 인벤토리 창을 열고 닫는다(토글, 7.7). 팝업이라 ESC로도 닫히고, 다른 팝업이 열려 있거나
    /// 일시정지·클리어 화면(<c>Time.timeScale</c> 0)에서는 열리지 않는다. 지금은 소지 오브를 그대로 넘겨 보기만 한다.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public OrbInventory Orbs { get; private set; }

        bool windowOpen;

        void Awake()
        {
            var rules = MapManager.Current.Rules;
            Orbs = new OrbInventory(rules.slotCount, rules.baseChannelLimit);
            Orbs.Changed += () => EventBus.Publish(new InventoryChanged(this));
        }

        void Update()
        {
            if (Time.timeScale == 0f || !InputManager.Instance.ReadInventory()) return;
            if (windowOpen) Popups.Close();
            else windowOpen = Popups.TryOpen(Orbs, () => windowOpen = false);
        }
    }
}

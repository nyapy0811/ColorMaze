using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 플레이어의 소지 오브. 코어 규칙의 소지 오브(<see cref="OrbInventory"/>)를 들고, 용량과 기본 채널 한도는
    /// 맵 매니저의 게임 규칙에서 읽는다(맵 매니저가 먼저 깨어남). 바뀌면 '소지 오브 바뀜'을 보낸다.
    /// 인벤토리 창(Tab)은 코어 루프 8단계에서 더한다.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public OrbInventory Orbs { get; private set; }

        void Awake()
        {
            var rules = MapManager.Current.Rules;
            Orbs = new OrbInventory(rules.slotCount, rules.baseChannelLimit);
            Orbs.Changed += () => EventBus.Publish(new InventoryChanged(this));
        }
    }
}

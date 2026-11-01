using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 플레이어의 지금 색. 코어 규칙의 지금 색(<see cref="CurrentColor"/>)을 들고, 채널 한도는 맵 매니저의 게임 규칙에서 읽는다
    /// (맵 매니저가 먼저 깨어남). 바뀌면 '지금 색 바뀜'을 보내고, 시작할 때도 한 번 보낸다(처음부터 다시 해도 UIScene은 남아 있음).
    /// </summary>
    public class PlayerColor : MonoBehaviour
    {
        public CurrentColor Current { get; private set; }

        void Awake()
        {
            Current = new CurrentColor(MapManager.Current.Rules.channelLimit);
            Current.Changed += Publish;
        }

        void Start() => Publish();

        void Publish() => EventBus.Publish(new CurrentColorChanged(Current.Value));
    }
}

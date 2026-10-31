using Framework.Core;

namespace ColorMaze
{
    /// <summary>작동기가 켜지거나 꺼짐. 보내는 곳: 작동기(목적지는 '맵 클리어'로 알 수 있어 보내지 않음). 받는 곳: 소리 연결.</summary>
    public readonly struct ActuatorStateChanged : IEvent
    {
        public readonly bool IsOn;

        public ActuatorStateChanged(bool isOn) => IsOn = isOn;
    }
}

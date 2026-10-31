using Framework.Core;

namespace ColorMaze
{
    /// <summary>문 계열이 열리거나 닫힘. 보내는 곳: 문 계열. 받는 곳: 소리 연결.</summary>
    public readonly struct GateStateChanged : IEvent
    {
        public readonly bool IsOpen;

        public GateStateChanged(bool isOpen) => IsOpen = isOpen;
    }
}

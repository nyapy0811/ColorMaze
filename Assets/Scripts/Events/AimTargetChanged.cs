using Framework.Core;

namespace ColorMaze
{
    /// <summary>조준해서 보일 값이 바뀜(생김, 없어짐, 다른 값). 보내는 곳: 조준 컨트롤러. 받는 곳: 조준 값 표시.</summary>
    public readonly struct AimTargetChanged : IEvent
    {
        public readonly bool HasValue;
        public readonly OrbValue Value;

        public AimTargetChanged(bool hasValue, OrbValue value)
        {
            HasValue = hasValue;
            Value = value;
        }
    }
}

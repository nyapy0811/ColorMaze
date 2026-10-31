using Framework.Core;

namespace ColorMaze
{
    /// <summary>합성함. 보내는 곳: 합성기. 받는 곳: 소리 연결.</summary>
    public readonly struct OrbsMixed : IEvent
    {
        /// <summary>결과 값. 재료가 모두 (0,0,0)이면 (0,0,0)이다(결과 오브는 생기지 않음).</summary>
        public readonly OrbValue Result;

        public OrbsMixed(OrbValue result) => Result = result;
    }
}

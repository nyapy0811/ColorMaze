using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>소지 오브가 가득 차 추출할 수 없음. 보내는 곳: 추출 오브젝트. 받는 곳: 추출 불가 효과(뷰모델), 소리 연결(8단계).</summary>
    public readonly struct ExtractRejected : IEvent
    {
        public readonly Vector3 Position;

        public ExtractRejected(Vector3 position) => Position = position;
    }
}

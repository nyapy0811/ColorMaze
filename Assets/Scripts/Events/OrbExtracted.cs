using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>추출 오브젝트에서 오브를 추출함. 보내는 곳: 추출 오브젝트. 받는 곳: 뷰모델(손 뻗기), 소리 연결.</summary>
    public readonly struct OrbExtracted : IEvent
    {
        /// <summary>추출한 오브젝트의 위치(연출이 날아오는 곳).</summary>
        public readonly Vector3 Position;
        public readonly OrbValue Value;

        public OrbExtracted(Vector3 position, OrbValue value)
        {
            Position = position;
            Value = value;
        }
    }
}

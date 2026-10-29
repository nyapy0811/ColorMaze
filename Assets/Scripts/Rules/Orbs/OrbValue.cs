using System;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 오브의 R·G·B 값. 인스펙터와 JSON에 보이도록 직렬화하지만 코드에서는 값으로만 다룬다
    /// (바꿀 때는 새 값을 만든다). 채널 한도도 채널마다 따로인 값이라 이 타입으로 나타낸다.
    /// </summary>
    [Serializable]
    public struct OrbValue : IEquatable<OrbValue>
    {
        [SerializeField] int r;
        [SerializeField] int g;
        [SerializeField] int b;

        public OrbValue(int r, int g, int b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static OrbValue Zero => new(0, 0, 0);

        public int R => r;
        public int G => g;
        public int B => b;

        /// <summary>가장 큰 채널 값.</summary>
        public int Max => Math.Max(r, Math.Max(g, b));

        public static OrbValue operator +(OrbValue a, OrbValue b) => new(a.r + b.r, a.g + b.g, a.b + b.b);

        public bool Equals(OrbValue other) => r == other.r && g == other.g && b == other.b;
        public override bool Equals(object obj) => obj is OrbValue other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(r, g, b);
        public static bool operator ==(OrbValue a, OrbValue b) => a.Equals(b);
        public static bool operator !=(OrbValue a, OrbValue b) => !a.Equals(b);

        /// <summary>'R G B' 숫자 세 개(예: <c>2 0 0</c>). 화면의 값 표시가 이 형식이다.</summary>
        public override string ToString() => $"{r} {g} {b}";
    }
}

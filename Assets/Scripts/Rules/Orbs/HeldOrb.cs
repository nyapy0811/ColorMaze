namespace ColorMaze
{
    /// <summary>
    /// 소지 오브 하나. 같은 값이어도 런타임 번호로 구분한다(팝업에서 고르기, 흔들 대상).
    /// 번호는 <see cref="OrbInventory"/>가 붙이고 저장하지 않는다.
    /// </summary>
    public sealed class HeldOrb
    {
        public int Id { get; }
        public OrbValue Value { get; }

        internal HeldOrb(int id, OrbValue value)
        {
            Id = id;
            Value = value;
        }

        public override string ToString() => $"#{Id} ({Value})";
    }
}

namespace ColorMaze
{
    /// <summary>제출하는 곳의 종류.</summary>
    public enum SubmitKind
    {
        /// <summary>일회용 작동기: 켜지면 다 쓰고 다시 열 수 없다.</summary>
        SingleUse,
        /// <summary>반복 작동기: 넣은 오브를 언제든 회수할 수 있다.</summary>
        Reusable,
        /// <summary>목적지: 맞는 오브를 넣고 닫으면 클리어한다.</summary>
        Goal,
    }
}

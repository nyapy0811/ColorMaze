namespace ColorMaze
{
    /// <summary>제출 팝업을 닫을 때의 결과: 제출 슬롯에 든 오브(없을 수 있음). 작동기에 이것을 적용한다.</summary>
    public readonly struct SubmitOutcome
    {
        public SubmitOutcome(OrbValue? storedOrb)
        {
            StoredOrb = storedOrb;
        }

        public OrbValue? StoredOrb { get; }
    }
}

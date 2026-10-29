namespace ColorMaze
{
    /// <summary>
    /// 작동기·목적지의 상태: 든 오브(없을 수 있음). 든 오브가 있으면 켜짐이다.
    /// 일회용 작동기는 켜지면 다 쓰고, 목적지는 켜지면 클리어한 것이다.
    /// </summary>
    public readonly struct ActuatorState
    {
        public ActuatorState(SubmitKind kind, OrbValue? storedOrb = null)
        {
            Kind = kind;
            StoredOrb = storedOrb;
        }

        public SubmitKind Kind { get; }
        public OrbValue? StoredOrb { get; }
        public bool IsOn => StoredOrb.HasValue;

        /// <summary>일회용 작동기가 켜진 뒤.</summary>
        public bool IsUsedUp => Kind == SubmitKind.SingleUse && IsOn;

        /// <summary>팝업을 열 수 있는가: 다 쓴 일회용 작동기와 클리어한 뒤의 목적지는 열 수 없다.</summary>
        public bool CanOpenPopup => Kind == SubmitKind.Reusable || !IsOn;

        /// <summary>제출 세션의 결과를 적용한 새 상태. 켜짐이 바뀌었는지도 알려 준다.</summary>
        public ActuatorState Apply(SubmitOutcome outcome, out bool onChanged)
        {
            var next = new ActuatorState(Kind, outcome.StoredOrb);
            onChanged = next.IsOn != IsOn;
            return next;
        }
    }
}

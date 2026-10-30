namespace ColorMaze
{
    /// <summary>
    /// 일회용 작동기: 맞는 오브를 넣고 팝업을 닫으면 켜지고 다 쓴다. 다 쓰면 팝업이 다시 열리지 않고 강조가 꺼진다(3차 Q478).
    /// 외형은 반복 작동기와 다르게 한다(임시: 네모 받침).
    /// </summary>
    public class SingleUseActuator : ActuatorBase
    {
        protected override SubmitKind Kind => SubmitKind.SingleUse;
    }
}

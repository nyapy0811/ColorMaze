namespace ColorMaze
{
    /// <summary>
    /// 반복 작동기: 넣은 오브를 언제든 회수할 수 있고, 회수하고 팝업을 닫으면 꺼진다(2차 Q469).
    /// 외형은 일회용 작동기와 다르게 한다(임시: 원기둥 받침).
    /// </summary>
    public class ReusableActuator : ActuatorBase
    {
        protected override SubmitKind Kind => SubmitKind.Reusable;
    }
}

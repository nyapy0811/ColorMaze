namespace ColorMaze
{
    /// <summary>
    /// 목적지(맵당 1개): 맞는 오브를 넣고 팝업을 닫으면 켜지고 맵 매니저에 클리어를 요청한다. 클리어한 뒤에는 팝업이 다시 열리지 않는다.
    /// 연결된 기물이 없어 요구 색만 보인다. 외형은 작동기와 다르게 한다(임시: 넓고 낮은 원판).
    /// </summary>
    public class Goal : SubmittableObjectBase
    {
        protected override SubmitKind Kind => SubmitKind.Goal;

        protected override void OnSwitched()
        {
            if (IsOn) MapManager.Current.Clear();
        }
    }
}

namespace ColorMaze
{
    /// <summary>
    /// 작동기가 켜고 끄는 대상(지금은 문 계열, 나중에 리프트 같은 기물도). 연결색을 정하고, 작동기의 켜짐이 바뀔 때 알림을 받는다.
    /// </summary>
    public interface IActuatorTarget
    {
        /// <summary>연결색 목록의 번호. 연결된 작동기가 같은 색을 보인다.</summary>
        int LinkColorIndex { get; }

        /// <summary>연결된 작동기를 꺼도 되는가. 문 계열은 꺼서 닫히는데 막는 자리에 플레이어가 있으면 안 된다.</summary>
        bool CanActuatorTurnOff { get; }

        /// <summary>연결된 작동기의 켜짐이 바뀌었다.</summary>
        void OnActuatorChanged();
    }
}

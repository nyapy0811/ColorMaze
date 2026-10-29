using Framework.Core;

namespace ColorMaze
{
    /// <summary>
    /// Framework.Core의 범용 GameManagerBase를 이 게임의 GameState로 구체화한 전역 상태 관리자.
    /// 부팅 직후 상태는 Boot이고, 맵이 시작되면 맵 매니저가 Playing으로 바꾼다.
    /// </summary>
    public class GameManager : GameManagerBase<GameManager, GameState>
    {
        public void StartGame() => ChangeState(GameState.Playing);
    }
}

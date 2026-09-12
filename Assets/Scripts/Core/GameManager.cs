using Framework.Core;
using UnityEngine;

/// <summary>프레임워크의 범용 GameManagerBase&lt;TSelf,TState&gt;를 이 프로젝트의 GameState로
/// 구체화한 매니저. 상태 이름을 아는 이름 있는 편의 메서드도 전부 여기 있다(프레임워크는 모름).</summary>
public class GameManager : GameManagerBase<GameManager, GameState>
{
    protected override void OnAwake()
    {
        // 부팅 시점 초기화 자리. 지금은 바로 메뉴로 전환.
        ChangeState(GameState.MainMenu);
    }

    GameState stateBeforePause;

    public void StartGame() => ChangeState(GameState.Playing);

    /// <summary>Playing과 MapEditor 둘 다에서 일시정지할 수 있다. Resume()이 Playing으로 고정되어
    /// 있지 않고 정확히 원래 있던 상태로 돌아가도록, 일시정지 직전 상태를 기억해둔다.</summary>
    public void Pause()
    {
        if (State != GameState.Playing && State != GameState.MapEditor) return;
        stateBeforePause = State;
        Time.timeScale = 0f;
        ChangeState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        ChangeState(stateBeforePause);
    }

    /// <summary>스테이지 클리어. 진행 중일 때만 유효.</summary>
    public void StageClear()
    {
        if (State != GameState.Playing) return;
        ChangeState(GameState.Cleared);
    }

    /// <summary>
    /// 로딩 상태로 전환. 스테이지 이동이나 메뉴 복귀 전 딜레이 부여용.
    /// 일시정지로 멈춰 있던 시간을 원복하고 넘어간다.
    /// </summary>
    public void BeginLoading()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Loading);
    }

    /// <summary>
    /// 종료 절차. Quitting 상태로 바꾼 뒤 앱을 끈다.
    /// 저장 등 마무리 작업은 OnStateChanged(Quitting)를 구독한 쪽에서 처리한다.
    /// </summary>
    public void Quit()
    {
        ChangeState(GameState.Quitting);
        Application.Quit();
    }
}

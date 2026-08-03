using Framework.Core;
using UnityEngine;

/// <summary>
/// GameManager.OnStateChanged 구독/해제 보일러플레이트를 한 곳에 모은 공용 베이스.
/// Start()에서 구독하고 현재 상태로 한 번 초기 갱신을 호출하며, OnDestroy에서 안전하게 해제한다.
/// 하위 클래스가 Start()/OnDestroy()를 오버라이드할 때는 반드시 base를 호출해야 한다.
/// </summary>
public abstract class GameStateListener : MonoBehaviour
{
    protected virtual void Start()
    {
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        var state = GameManager.Instance.State;
        OnGameStateChanged(state, state); // 현재 상태로 1회 초기 갱신
    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    /// <summary>GameState가 바뀔 때(및 Start 시 현재 상태로 1회) 호출된다.</summary>
    protected abstract void OnGameStateChanged(GameState previous, GameState next);
}

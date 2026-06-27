using UnityEngine;
using Framework.Core;

/// <summary>
/// 미로 레벨과 현재 층을 관리하는 게임 매니저.
/// MazeGenerator에게 빌드를 지시하고, 플레이어가 있는 층을 추적한다.
/// 다른 시스템은 LevelManager.Instance.CurrentFloor / OnFloorChanged 로 반응한다.
/// </summary>
public class LevelManager : MonoSingleton<LevelManager>
{
    [SerializeField] private MazeGenerator maze;

    /// <summary>플레이어가 현재 있는 층(0 = 1층).</summary>
    public int CurrentFloor { get; private set; }

    /// <summary>층이 바뀔 때 호출된다.</summary>
    public event System.Action<int> OnFloorChanged;

    protected override void OnAwake()
    {
        if (maze == null) maze = FindAnyObjectByType<MazeGenerator>();
    }

    /// <summary>미로를 생성하고 시작 층(0)으로 초기화한다.</summary>
    public void BuildLevel()
    {
        if (maze == null)
        {
            Debug.LogWarning("[LevelManager] MazeGenerator를 찾을 수 없어 빌드를 건너뜀.");
            return;
        }
        maze.Build();
        CurrentFloor = 0;
        OnFloorChanged?.Invoke(CurrentFloor);
    }

    /// <summary>현재 층을 지정 층으로 바꾼다.</summary>
    public void SetFloor(int floor)
    {
        if (floor == CurrentFloor) return;
        CurrentFloor = floor;
        Debug.Log($"[LevelManager] 층 변경 -> {floor + 1}F");
        OnFloorChanged?.Invoke(floor);
    }
}

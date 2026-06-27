using UnityEngine;
using Framework.Core;

/// <summary>
/// 미로 레벨을 관리하는 게임 매니저.
/// MazeGenerator에게 빌드를 지시한다.
/// </summary>
public class LevelManager : MonoSingleton<LevelManager>
{
    [SerializeField] private MazeGenerator maze;

    protected override void OnAwake()
    {
        if (maze == null) maze = FindAnyObjectByType<MazeGenerator>();
    }

    /// <summary>미로를 생성한다.</summary>
    public void BuildLevel()
    {
        if (maze == null)
        {
            Debug.LogWarning("[LevelManager] MazeGenerator를 찾을 수 없어 빌드를 건너뜀.");
            return;
        }
        // 씬에 미리 구워둔 경우 런타임 재생성을 건너뛴다.
        if (!maze.buildAtRuntime) return;
        maze.Build();
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [개발자 전용] 플레이 중 MazeEditor가 저장(F5)한 편집 데이터를,
/// 플레이를 멈추는 순간 씬에 자동으로 구워 반영한다.
/// </summary>
[InitializeOnLoad]
static class MazeAutoBake
{
    static MazeAutoBake()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 플레이를 종료하고 에디트 모드로 막 돌아온 시점.
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (!MazeFile.TryLoad(out var model)) return;

        var maze = Object.FindAnyObjectByType<MazeGenerator>();
        if (maze == null)
        {
            MazeFile.Delete();
            return;
        }

        // 저장된 맵을 floors에 적용하고 씬에 생성.
        var floors = new MazeGenerator.Floor[model.floors.Length];
        for (int i = 0; i < floors.Length; i++)
            floors[i] = new MazeGenerator.Floor { rows = model.floors[i].rows };

        maze.floors = floors;
        maze.buildAtRuntime = false; // 씬에 구웠으니 런타임 재생성 끔
        maze.Build();

        EditorUtility.SetDirty(maze);
        EditorSceneManager.MarkSceneDirty(maze.gameObject.scene);
        MazeFile.Delete();

        Debug.Log("[MazeAutoBake] 플레이 중 편집을 씬에 반영했습니다. (씬 저장 Ctrl+S 잊지 마세요)");
    }
}
#endif

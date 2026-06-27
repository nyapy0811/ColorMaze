#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [개발자 전용] MazeGenerator 인스펙터에 "씬에 생성 / 지우기" 버튼을 추가한다.
/// 에디터에서 생성하면 미로가 실제 씬 오브젝트로 구워져 씬과 함께 저장된다.
/// </summary>
[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var maze = (MazeGenerator)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("씬에 생성 (Build In Scene)"))
        {
            maze.Build();              // 내부에서 기존 미로를 먼저 Clear
            MarkDirty(maze);
        }

        if (GUILayout.Button("지우기 (Clear)"))
        {
            maze.Clear();
            MarkDirty(maze);
        }

        EditorGUILayout.HelpBox(
            "씬에 구운 뒤에는 'Build At Runtime'을 꺼야 플레이 시 중복 생성되지 않습니다.",
            MessageType.Info);
    }

    static void MarkDirty(MazeGenerator maze)
    {
        EditorUtility.SetDirty(maze);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(maze.gameObject.scene);
    }
}
#endif

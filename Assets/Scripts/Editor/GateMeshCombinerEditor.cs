#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [개발자 전용] GateMeshCombiner 인스펙터에 에디터 미리보기 버튼을 추가한다.
/// 플레이하지 않아도 같은 색 게이트 병합 결과를 Scene 뷰에서 볼 수 있다.
/// </summary>
[CustomEditor(typeof(GateMeshCombiner))]
public class GateMeshCombinerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var combiner = (GateMeshCombiner)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("병합 미리보기 (Rebuild)"))
        {
            combiner.Rebuild();
            MarkDirty(combiner);
        }
        if (GUILayout.Button("미리보기 해제 (개별 블록 보기)"))
        {
            combiner.ClearPreview();
            MarkDirty(combiner);
        }

        EditorGUILayout.HelpBox(
            "블록을 추가/삭제한 뒤에는 'Rebuild'를 다시 눌러 갱신하세요.\n" +
            "개별 블록을 편집하려면 '미리보기 해제'로 되돌리면 됩니다.",
            MessageType.Info);
    }

    static void MarkDirty(GateMeshCombiner combiner)
    {
        EditorUtility.SetDirty(combiner);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(combiner.gameObject.scene);
    }
}
#endif

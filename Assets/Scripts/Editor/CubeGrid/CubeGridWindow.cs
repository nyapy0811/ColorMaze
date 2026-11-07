using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 큐브 맵 편집 창(메뉴 ColorMaze/큐브 맵 편집). 지금 맵의 큐브 격자를 찾아 붓(블록, 시작 지점, 색 원천, 목적지)을 고르고,
    /// 씬 뷰 도구를 켜고, 블록을 다시 만든다. 기물 값(색 원천 값, 요구 색 등)은 놓은 뒤 인스펙터에서 고친다.
    /// </summary>
    public class CubeGridWindow : EditorWindow
    {
        [MenuItem("ColorMaze/큐브 맵 편집")]
        static void Open() => GetWindow<CubeGridWindow>("큐브 맵 편집");

        void OnEnable()
        {
            CubeGridEditing.BrushChanged += Repaint;
            Undo.undoRedoPerformed += Repaint;
        }

        void OnDisable()
        {
            CubeGridEditing.BrushChanged -= Repaint;
            Undo.undoRedoPerformed -= Repaint;
        }

        void OnHierarchyChange() => Repaint();

        void OnGUI()
        {
            var grid = FindAnyObjectByType<CubeGrid>();
            if (grid == null)
            {
                EditorGUILayout.HelpBox("지금 씬에 큐브 격자가 없다.", MessageType.Info);
                if (GUILayout.Button("큐브 격자 만들기")) CubeGridEditing.CreateGrid();
                return;
            }
            if (CubeGridEditing.FindConfig() == null)
            {
                EditorGUILayout.HelpBox("큐브 맵 설정 애셋(CubeGridConfig)이 없다. Assets/Data에 만든다.", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("씬 뷰 도구 켜기"))
                {
                    Selection.activeGameObject = grid.gameObject;
                    ToolManager.SetActiveTool<CubeGridSceneTool>();
                }
                if (GUILayout.Button("다시 만들기")) CubeGridEditing.Rebuild(grid);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("붓", EditorStyles.boldLabel);
            int brush = GUILayout.Toolbar((int)CubeGridEditing.SelectedBrush, CubeGridEditing.BrushNames);
            if (brush != (int)CubeGridEditing.SelectedBrush) CubeGridEditing.SelectedBrush = (CubeGridEditing.Brush)brush;

            EditorGUILayout.HelpBox(
                "클릭: 가리킨 면 쪽 칸에 놓기 / Ctrl+클릭: 지우기 / Shift+드래그: 범위에 블록 놓기 / Shift+Ctrl+드래그: 범위 지우기\n" +
                "아무것도 가리키지 않으면 높이 0의 바닥면에 놓는다(블록은 바닥면 아래 칸). 한 칸에는 하나만 둔다.\n" +
                "기물 값과 방향은 놓은 뒤 인스펙터에서 고친다.",
                MessageType.None);

            EditorGUILayout.LabelField($"블록 {grid.Data.blocks.Count}개");
        }
    }
}

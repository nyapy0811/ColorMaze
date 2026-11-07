using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColorMaze
{
    /// <summary>
    /// 씬 뷰의 큐브 맵 편집 도구(v1 MazeGeneratorEditor 조작). 큐브 격자를 고르면 씬 뷰 툴바에 나타난다(큐브 맵 편집 창으로도 켬).
    /// 클릭: 가리킨 면 쪽 칸에 고른 붓으로 놓기 / Ctrl+클릭: 가리킨 칸 지우기 /
    /// Shift+드래그: 직사각형 범위에 블록 놓기 / Shift+Ctrl+드래그: 범위 지우기.
    /// 가리킨 칸은 콜라이더가 아니라 격자로 찾는다(블록과 기물이 든 칸). 아무것도 가리키지 않으면 높이 0의 바닥면을 쓴다
    /// (블록은 그 아래 y = −1 칸, 기물은 그 위 y = 0 칸). 범위는 시작한 칸을 지나고 가리킨 면에 수직인 평면에서 정한다(v1과 같음).
    /// </summary>
    [EditorTool("큐브 맵 편집", typeof(CubeGrid))]
    public class CubeGridSceneTool : EditorTool
    {
        const float MaxDistance = 500f;

        static readonly Color PlaceColor = new(0.3f, 1f, 0.4f, 1f);
        static readonly Color RemoveColor = new(1f, 0.25f, 0.25f, 1f);

        bool dragging;
        bool dragRemove;
        int dragAxis;
        Vector3Int dragStart;
        Vector3Int dragEnd;

        public override GUIContent toolbarIcon => new("큐브", "큐브 맵 편집 (ColorMaze)");

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView) return;
            var grid = (CubeGrid)target;
            var e = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.GetTypeForControl(control))
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(control); // 클릭이 다른 오브젝트를 고르지 않게 한다.
                    break;
                case EventType.MouseMove:
                    window.Repaint();
                    break;
                case EventType.MouseDown when e.button == 0 && !e.alt:
                    OnMouseDown(grid, e, control);
                    break;
                case EventType.MouseDrag when dragging && GUIUtility.hotControl == control:
                    UpdateDrag(e.mousePosition);
                    window.Repaint();
                    e.Use();
                    break;
                case EventType.MouseUp when dragging && GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    dragging = false;
                    var cells = CubeGridRules.Box(dragStart, dragEnd);
                    if (dragRemove) CubeGridEditing.Remove(grid, cells);
                    else CubeGridEditing.PlaceBlocks(grid, cells);
                    e.Use();
                    break;
                case EventType.Repaint:
                    Draw(grid, e);
                    break;
            }
        }

        void OnMouseDown(CubeGrid grid, Event e, int control)
        {
            bool hit = Pick(grid, e.mousePosition, out var cell, out var normal);
            if (e.shift)
            {
                dragging = true;
                dragRemove = e.control;
                dragAxis = hit ? DominantAxis(normal) : 1;
                dragStart = hit ? cell : GroundCell(e.mousePosition, CubeGridEditing.Brush.Block) ?? Vector3Int.zero;
                dragEnd = dragStart;
                GUIUtility.hotControl = control;
            }
            else if (e.control)
            {
                if (hit) CubeGridEditing.Remove(grid, new[] { cell });
            }
            else
            {
                var placeAt = hit ? cell + normal : GroundCell(e.mousePosition, CubeGridEditing.SelectedBrush);
                if (placeAt.HasValue) CubeGridEditing.Place(grid, placeAt.Value);
            }
            e.Use();
        }

        // 가리킨 칸(블록이나 기물이 든 칸)과 들어간 면.
        static bool Pick(CubeGrid grid, Vector2 mouse, out Vector3Int cell, out Vector3Int normal)
        {
            var fixtures = CubeGridEditing.FixtureCells(grid);
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            return CubeGridRules.Raycast(ray.origin, ray.direction, MaxDistance, c => CubeGridEditing.IsFilled(grid, fixtures, c), out cell, out normal);
        }

        // 높이 0의 바닥면 위 칸: 블록은 바닥면 아래 칸(윗면이 0), 기물은 바닥면 위 칸.
        static Vector3Int? GroundCell(Vector2 mouse, CubeGridEditing.Brush brush)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance) || distance > MaxDistance) return null;
            var point = ray.GetPoint(distance);
            return new Vector3Int(Mathf.RoundToInt(point.x), brush == CubeGridEditing.Brush.Block ? -1 : 0, Mathf.RoundToInt(point.z));
        }

        static int DominantAxis(Vector3Int normal) => normal.x != 0 ? 0 : normal.y != 0 ? 1 : 2;

        // 시작한 칸의 중심을 지나고 고정축에 수직인 평면과 마우스 광선이 만나는 칸.
        void UpdateDrag(Vector2 mouse)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            var axis = Vector3.zero;
            axis[dragAxis] = 1f;
            if (!new Plane(axis, CubeGridRules.CellCenter(dragStart)).Raycast(ray, out float distance)) return;
            var cell = CubeGridRules.CellAt(ray.GetPoint(distance));
            cell[dragAxis] = dragStart[dragAxis];
            dragEnd = cell;
        }

        void Draw(CubeGrid grid, Event e)
        {
            var cells = new List<Vector3Int>();
            bool remove;
            if (dragging)
            {
                cells.AddRange(CubeGridRules.Box(dragStart, dragEnd));
                remove = dragRemove;
            }
            else
            {
                remove = e.control;
                bool hit = Pick(grid, e.mousePosition, out var cell, out var normal);
                if (hit) cells.Add(remove ? cell : cell + normal);
                else if (!remove && GroundCell(e.mousePosition, CubeGridEditing.SelectedBrush) is { } ground) cells.Add(ground);
            }

            var zTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = remove ? RemoveColor : PlaceColor;
            foreach (var cell in cells) Handles.DrawWireCube(CubeGridRules.CellCenter(cell), Vector3.one * 1.001f);
            Handles.zTest = zTest;
        }
    }
}

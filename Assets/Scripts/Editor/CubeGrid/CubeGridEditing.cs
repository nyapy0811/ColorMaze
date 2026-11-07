using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ColorMaze
{
    /// <summary>
    /// 큐브 맵 편집 창과 씬 뷰 도구가 함께 쓰는 편집 동작(v1 MazeGeneratorEditor 방식을 v2 구조로).
    /// 고른 붓으로 칸에 블록이나 기물을 놓고 지운다. 한 칸에는 하나만 두며, 블록은 큐브 격자 데이터에, 기물은 씬의 프리팹 인스턴스로
    /// 칸 바닥 가운데에 놓는다(맵 안 id는 놓을 때 고침). 편집 한 번이 Undo 하나다.
    /// </summary>
    public static class CubeGridEditing
    {
        public enum Brush { Block, StartPoint, ColorSource, Goal }

        public static readonly string[] BrushNames = { "블록", "시작 지점", "색 원천", "목적지" };

        public static Brush SelectedBrush
        {
            get => (Brush)SessionState.GetInt("ColorMaze.CubeGrid.Brush", 0);
            set
            {
                SessionState.SetInt("ColorMaze.CubeGrid.Brush", (int)value);
                BrushChanged?.Invoke();
            }
        }

        public static event Action BrushChanged;

        public static CubeGridConfig FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:CubeGridConfig");
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<CubeGridConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static CubeGrid CreateGrid()
        {
            var go = new GameObject("CubeGrid");
            Undo.RegisterCreatedObjectUndo(go, "큐브 격자 만들기");
            Selection.activeGameObject = go;
            return go.AddComponent<CubeGrid>();
        }

        /// <summary>씬의 기물이 든 칸(칸 바닥 가운데에 놓인 기물 기준).</summary>
        public static Dictionary<Vector3Int, FixtureBase> FixtureCells(CubeGrid grid)
        {
            var cells = new Dictionary<Vector3Int, FixtureBase>();
            foreach (var fixture in FixtureIdAssigner.FixturesIn(grid.gameObject.scene))
                cells.TryAdd(CubeGridRules.CellAt(fixture.transform.position), fixture);
            return cells;
        }

        public static bool IsFilled(CubeGrid grid, Dictionary<Vector3Int, FixtureBase> fixtures, Vector3Int cell) =>
            CubeGridRules.HasBlock(grid.Data, cell) || fixtures.ContainsKey(cell);

        /// <summary>빈 칸이면 고른 붓으로 놓는다.</summary>
        public static void Place(CubeGrid grid, Vector3Int cell) => PlaceAll(grid, new[] { cell }, "칸에 놓기");

        /// <summary>범위의 빈 칸에 블록을 놓는다(범위 놓기는 블록만).</summary>
        public static void PlaceBlocks(CubeGrid grid, IEnumerable<Vector3Int> cells) => PlaceAll(grid, cells, "블록 범위 놓기", blocksOnly: true);

        /// <summary>칸의 블록이나 기물을 지운다.</summary>
        public static void Remove(CubeGrid grid, IEnumerable<Vector3Int> cells)
        {
            var config = RequireConfig();
            if (config == null) return;
            var fixtures = FixtureCells(grid);
            Edit(grid, "칸 지우기", () =>
            {
                foreach (var cell in cells)
                {
                    CubeGridRules.RemoveBlock(grid.Data, cell);
                    if (fixtures.Remove(cell, out var fixture)) Undo.DestroyObjectImmediate(fixture.gameObject);
                }
            }, config);
        }

        public static void Rebuild(CubeGrid grid)
        {
            var config = RequireConfig();
            if (config == null) return;
            Edit(grid, "블록 다시 만들기", () => { }, config, fullRebuild: true);
        }

        static void PlaceAll(CubeGrid grid, IEnumerable<Vector3Int> cells, string undoName, bool blocksOnly = false)
        {
            var config = RequireConfig();
            if (config == null) return;
            var brush = blocksOnly ? Brush.Block : SelectedBrush;
            var prefab = PrefabFor(config, brush);
            if (brush != Brush.Block && prefab == null)
            {
                Debug.LogError($"[큐브 맵 편집] 큐브 맵 설정에 '{BrushNames[(int)brush]}' 프리팹이 없다.", config);
                return;
            }
            var fixtures = FixtureCells(grid);
            Edit(grid, undoName, () =>
            {
                foreach (var cell in cells)
                {
                    if (IsFilled(grid, fixtures, cell)) continue;
                    if (brush == Brush.Block) CubeGridRules.AddBlock(grid.Data, cell);
                    else fixtures[cell] = PlaceFixture(grid, prefab, brush, cell);
                }
                if (brush != Brush.Block) FixtureIdAssigner.FixIds(grid.gameObject.scene);
            }, config);
        }

        static FixtureBase PlaceFixture(CubeGrid grid, GameObject prefab, Brush brush, Vector3Int cell)
        {
            var parent = FolderFor(grid.gameObject.scene, brush);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grid.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(go, "기물 놓기");
            if (parent != null) Undo.SetTransformParent(go.transform, parent, "기물 놓기");
            go.transform.SetPositionAndRotation(CubeGridRules.CellBottom(cell), Quaternion.identity);
            Selection.activeGameObject = go;
            return go.GetComponent<FixtureBase>();
        }

        // 기물은 종류별 묶음 오브젝트 아래에 둔다(시험 맵의 구성과 같음). 시작 지점은 씬 바로 아래.
        static Transform FolderFor(UnityEngine.SceneManagement.Scene scene, Brush brush)
        {
            string name = brush switch { Brush.ColorSource => "ColorSources", Brush.Goal => "Activators", _ => null };
            if (name == null) return null;
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
            if (existing != null) return existing.transform;
            var folder = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(folder, scene);
            Undo.RegisterCreatedObjectUndo(folder, "기물 묶음 만들기");
            return folder.transform;
        }

        static GameObject PrefabFor(CubeGridConfig config, Brush brush) => brush switch
        {
            Brush.StartPoint => config.startPointPrefab,
            Brush.ColorSource => config.colorSourcePrefab,
            Brush.Goal => config.goalPrefab,
            _ => config.blockPrefab,
        };

        static CubeGridConfig RequireConfig()
        {
            var config = FindConfig();
            if (config == null) Debug.LogError("[큐브 맵 편집] 큐브 맵 설정 애셋(CubeGridConfig)이 없다. Assets/Data에 만든다.");
            return config;
        }

        static void Edit(CubeGrid grid, string undoName, Action change, CubeGridConfig config, bool fullRebuild = false)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RecordObject(grid, undoName);
            change();
            if (fullRebuild) CubeGridBuilder.Rebuild(grid, config);
            else CubeGridBuilder.Build(grid, config);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        }
    }
}

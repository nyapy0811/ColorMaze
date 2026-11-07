using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColorMaze
{
    /// <summary>
    /// 큐브 격자 빌더: 데이터의 블록 칸마다 블록 프리팹을 큐브 격자 아래 '만든 부분'에 놓는다. 에디터(Play 밖)에서는 프리팹 연결을
    /// 지키고 Undo에 기록하고, 런타임(나중의 UGC 맵)에는 보통의 Instantiate·Destroy다.
    /// 지금 있는 블록은 자리로 데이터와 비교해 남은 칸은 그대로 두고, 없어진 칸은 지우고, 새 칸만 만든다.
    /// </summary>
    public static class CubeGridBuilder
    {
        public const string GeneratedPartsName = "만든 부분";
        const string UndoName = "블록 만들기";

        public static void Build(CubeGrid grid, CubeGridConfig config)
        {
            var parts = grid.transform.Find(GeneratedPartsName);
            if (parts == null)
            {
                parts = new GameObject(GeneratedPartsName).transform;
                parts.SetParent(grid.transform, false);
                RegisterCreated(parts.gameObject);
            }

            var wanted = new HashSet<Vector3Int>(grid.Data.blocks);
            var built = new HashSet<Vector3Int>();
            var stale = new List<GameObject>();
            foreach (Transform block in parts)
            {
                var cell = CubeGridRules.CellAt(block.position);
                if (wanted.Contains(cell) && built.Add(cell)) continue;
                stale.Add(block.gameObject);
            }
            foreach (var block in stale) Destroy(block);

            foreach (var cell in grid.Data.blocks)
            {
                if (!built.Add(cell)) continue;
                var block = Spawn(config.blockPrefab, parts);
                block.name = $"Block {cell.x},{cell.y},{cell.z}";
                block.transform.position = CubeGridRules.CellCenter(cell);
                RegisterCreated(block);
            }
        }

        /// <summary>블록을 모두 지우고 다시 만든다(블록 프리팹을 바꾼 뒤).</summary>
        public static void Rebuild(CubeGrid grid, CubeGridConfig config)
        {
            var parts = grid.transform.Find(GeneratedPartsName);
            if (parts != null) Destroy(parts.gameObject);
            Build(grid, config);
        }

        static bool Recording
        {
#if UNITY_EDITOR
            get => !Application.isPlaying;
#else
            get => false;
#endif
        }

        static GameObject Spawn(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (Recording) return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
            return Object.Instantiate(prefab, parent);
        }

        static void RegisterCreated(GameObject created)
        {
#if UNITY_EDITOR
            if (Recording) Undo.RegisterCreatedObjectUndo(created, UndoName);
#endif
        }

        static void Destroy(GameObject target)
        {
#if UNITY_EDITOR
            if (Recording)
            {
                Undo.DestroyObjectImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }
    }
}

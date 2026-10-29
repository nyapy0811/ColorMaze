using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 노드 데이터의 순수 계산: id 발급, 같은 자리 노드 합치기, 구간의 모듈 수, 문을 끼울 수 있는지.
    /// 문 폭은 아직 임시값이라 인자로 받는다.
    /// </summary>
    public static class NodeGraphRules
    {
        /// <summary>벽 모듈 하나의 길이(유닛).</summary>
        public const float ModuleLength = 0.25f;

        const float Epsilon = 1e-4f;

        /// <summary>다음 id를 주고 하나 늘린다.</summary>
        public static int IssueId(NodeGraphData data) => data.nextId++;

        /// <summary>불러온 뒤: 다음 id를 가장 큰 번호 + 1로 맞춘다(노드·벽·끼운 문 전체).</summary>
        public static void RefreshNextId(NodeGraphData data)
        {
            int max = 0;
            foreach (var node in data.nodes) max = Math.Max(max, node.id);
            foreach (var wall in data.walls)
            {
                max = Math.Max(max, wall.id);
                if (wall.hasDoor) max = Math.Max(max, wall.door.id);
            }
            data.nextId = max + 1;
        }

        /// <summary>
        /// 같은 자리에 있는 노드를 하나로 합친다. 목록에서 먼저 나온 노드가 남고, 없어지는 노드를 가리키던 벽은
        /// 남는 노드를 가리키게 고친다. 그래서 양 끝이 같아진 벽(합친 두 노드 사이의 벽)은 지운다. 합쳐 없앤 노드 수를 돌려준다.
        /// </summary>
        public static int MergeNodesAtSamePosition(NodeGraphData data)
        {
            var keptAt = new Dictionary<Vector3Int, int>();
            var replaced = new Dictionary<int, int>();
            foreach (var node in data.nodes)
            {
                if (keptAt.TryGetValue(node.position, out int keptId)) replaced[node.id] = keptId;
                else keptAt[node.position] = node.id;
            }
            if (replaced.Count == 0) return 0;

            data.nodes.RemoveAll(node => replaced.ContainsKey(node.id));
            foreach (var wall in data.walls)
            {
                if (replaced.TryGetValue(wall.startNodeId, out int start)) wall.startNodeId = start;
                if (replaced.TryGetValue(wall.endNodeId, out int end)) wall.endNodeId = end;
            }
            data.walls.RemoveAll(wall => wall.startNodeId == wall.endNodeId);
            return replaced.Count;
        }

        /// <summary>두 노드 사이 구간의 길이(유닛). 대각선이면 직선 거리다.</summary>
        public static float SegmentLength(Vector3Int start, Vector3Int end) => Vector3Int.Distance(start, end);

        /// <summary>구간을 채우는 벽 모듈 수. 남는 0.25 미만의 길이는 노드 기둥이 가린다.</summary>
        public static int ModuleCount(float segmentLength) => (int)Math.Floor(segmentLength / ModuleLength + Epsilon);

        /// <summary>모듈로 채우고 남는 길이(0.25 미만).</summary>
        public static float Remainder(float segmentLength) => Math.Max(0f, segmentLength - ModuleCount(segmentLength) * ModuleLength);

        /// <summary>문 폭이 차지하는 모듈 수.</summary>
        public static int DoorModules(float doorWidth) => (int)Math.Round(doorWidth / ModuleLength);

        /// <summary>
        /// 문을 이 위치에 끼울 수 있는가: 문 양옆에 문 폭 절반 이상의 벽이 남아야 한다.
        /// <paramref name="offsetModules"/>는 첫 모듈부터 센 문의 시작 쪽 가장자리다.
        /// </summary>
        public static bool CanPlaceDoor(float segmentLength, float doorWidth, int offsetModules)
        {
            int modules = ModuleCount(segmentLength);
            int door = DoorModules(doorWidth);
            int side = (door + 1) / 2;
            return offsetModules >= side && offsetModules + door <= modules - side;
        }
    }
}

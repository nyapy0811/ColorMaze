using System;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>노드: id와 정수 좌표(월드 1유닛). y는 그 노드가 선 층의 바닥 높이다.</summary>
    [Serializable]
    public sealed class NodeData
    {
        public int id;
        public Vector3Int position;
    }
}

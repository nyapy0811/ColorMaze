using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 큐브 맵의 블록 데이터: 블록이 있는 칸 목록(v1 칸 규칙, <see cref="CubeGridRules"/>). 블록은 아직 한 종류다.
    /// 씬의 큐브 격자와 나중의 UGC 맵 파일이 같은 클래스를 쓴다(JsonUtility).
    /// </summary>
    [Serializable]
    public sealed class CubeGridData
    {
        public List<Vector3Int> blocks = new();
    }
}

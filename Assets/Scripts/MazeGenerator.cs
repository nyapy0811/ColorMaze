using System;
using UnityEngine;

/// <summary>
/// 문자맵을 읽어 수직 다층 3D 미로를 기본 큐브로 생성한다.
///   '#' = 벽,  '.' = 길,  'P' = 플레이어 시작 칸
/// floors[0]이 1층(바닥), 인덱스가 올라갈수록 위층.
/// 각 층에서 rows[0]이 가장 안쪽(+Z), 각 문자의 인덱스가 X.
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Serializable]
    public class Floor
    {
        [TextArea(5, 20)]
        public string[] rows;
    }

    [Tooltip("한 칸의 크기(월드 유닛). GridMovement.cellSize와 같게 맞출 것")]
    public float cellSize = 1f;

    [Tooltip("층 간 수직 간격")]
    public float wallHeight = 1f;

    [Tooltip("시작 시 'P' 칸으로 옮길 플레이어")]
    public Transform player;

    [Tooltip("아래층부터 위로 쌓이는 각 층의 문자맵")]
    public Floor[] floors =
    {
        new Floor { rows = new[]
        {
            "#########",
            "#P..#...#",
            "#.#.#.#.#",
            "#.#...#.#",
            "#.#####.#",
            "#.......#",
            "#########",
        }},
        new Floor { rows = new[]
        {
            "#########",
            "#.......#",
            "#.#####.#",
            "#.#...#.#",
            "#...#...#",
            "#.#####.#",
            "#########",
        }},
    };

    public void Build()
    {
        var root = new GameObject("Maze").transform;
        root.SetParent(transform, false);

        BuildGround(root);

        for (int f = 0; f < floors.Length; f++)
        {
            float baseY = f * wallHeight;
            string[] map = floors[f].rows;

            for (int row = 0; row < map.Length; row++)
            {
                string line = map[row];
                for (int col = 0; col < line.Length; col++)
                {
                    char c = line[col];
                    Vector3 cell = new Vector3(col * cellSize, baseY, row * cellSize);

                    // 프로토타입: 벽 칸마다 1×1×1 큐브 하나만 놓는다.
                    // 바닥 윗면(cell.y) 위에 온전히 서도록 중심을 0.5 올린다.
                    if (c == '#')
                    {
                        CreateCube(root, cell + Vector3.up * 0.5f, Vector3.one, "Wall");
                    }
                    else if (c == 'P' && player != null)
                    {
                        // 바닥 윗면(cell.y)에 플레이어의 발이 닿도록 높이를 보정한다.
                        // 피벗이 중앙인 모델(캡슐 등)이 바닥에 파묻히는 문제 방지.
                        float bottomOffset = 0f;
                        if (player.TryGetComponent<Collider>(out var playerCol))
                            bottomOffset = player.position.y - playerCol.bounds.min.y;
                        else
                        {
                            var rend = player.GetComponentInChildren<Renderer>();
                            if (rend != null) bottomOffset = player.position.y - rend.bounds.min.y;
                        }
                        player.position = cell + Vector3.up * bottomOffset;
                    }
                }
            }
        }
    }

    // 가상의 0층: 1층 발판 전체를 덮는 바닥. 윗면이 y=0(플레이어가 서는 높이)에 맞도록 깐다.
    void BuildGround(Transform root)
    {
        if (floors.Length == 0) return;

        string[] map = floors[0].rows;
        int cols = 0;
        foreach (var line in map) cols = Mathf.Max(cols, line.Length);

        for (int row = 0; row < map.Length; row++)
            for (int col = 0; col < cols; col++)
                CreateCube(root, new Vector3(col * cellSize, -0.5f, row * cellSize),
                    Vector3.one, "Ground");
    }

    static void CreateCube(Transform parent, Vector3 pos, Vector3 scale, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
    }
}

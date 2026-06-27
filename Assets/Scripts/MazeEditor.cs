// 개발자 전용 기능: 에디터와 개발 빌드(Development Build)에서만 컴파일된다.
// 정식 릴리스 빌드에는 포함되지 않는다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [개발자 전용] 플레이 중 1인칭 시점에서 맵을 직접 편집하는 런타임 에디터.
///   좌클릭 = 바라보는 면에 벽 블록 설치
///   우클릭 = 바라보는 벽 블록 제거
///   M 키   = 현재 층 맵을 텍스트로 출력 + 클립보드 복사
/// 화면 중앙(카메라 정면)에서 레이캐스트하므로 별도 조준점만 있으면 편하다.
/// </summary>
public class MazeEditor : MonoBehaviour
{
    [Tooltip("편집 사정거리")]
    public float reach = 6f;

    [Tooltip("편집 중인 층(0 = 1층). 벽 중심 y = editFloor + 0.5")]
    public int editFloor = 0;

    Camera cam;
    Transform mazeRoot;
    Transform player;

    void Start()
    {
        cam = Camera.main;
        var root = GameObject.Find("Maze");
        mazeRoot = root != null ? root.transform : transform;
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc != null) player = fpc.transform;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || cam == null) return;

        if (mouse.leftButton.wasPressedThisFrame) Place();
        if (mouse.rightButton.wasPressedThisFrame) Remove();

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.mKey.wasPressedThisFrame) Export();
        if (kb.f5Key.wasPressedThisFrame) SaveToScene();
    }

    bool RaycastCenter(out RaycastHit hit)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out hit, reach, ~0, QueryTriggerInteraction.Ignore);
    }

    void Place()
    {
        if (!RaycastCenter(out var hit)) return;

        // 맞은 면의 법선 쪽 인접 칸(수평)에 설치. 바닥을 보면 그 칸 위에 설치.
        int col = Mathf.RoundToInt(hit.point.x + hit.normal.x * 0.5f);
        int row = Mathf.RoundToInt(hit.point.z + hit.normal.z * 0.5f);
        Vector3 center = new Vector3(col, editFloor + 0.5f, row);

        // 이미 그 칸에 벽이 있으면 중복 설치 방지.
        var hits = Physics.OverlapBox(center, Vector3.one * 0.4f, Quaternion.identity);
        foreach (var h in hits)
            if (h.gameObject.name == "Wall") return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Wall";
        go.transform.SetParent(mazeRoot, true);
        go.transform.position = center;
        go.transform.localScale = Vector3.one;
    }

    void Remove()
    {
        if (!RaycastCenter(out var hit)) return;
        if (hit.collider.gameObject.name == "Wall")
            Destroy(hit.collider.gameObject);
    }

    // 모든 층의 벽 + 플레이어를 맵 데이터로 만들어 파일에 저장한다.
    // 플레이를 멈추면 MazeAutoBake가 이 파일을 읽어 씬에 반영한다.
    void SaveToScene()
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue, maxF = 0;
        var wallSet = new HashSet<Vector3Int>();

        foreach (var t in mazeRoot.GetComponentsInChildren<Transform>())
        {
            if (t.name != "Wall") continue;
            int f = Mathf.RoundToInt(t.position.y - 0.5f);
            int x = Mathf.RoundToInt(t.position.x);
            int z = Mathf.RoundToInt(t.position.z);
            wallSet.Add(new Vector3Int(x, f, z));
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
            maxF = Mathf.Max(maxF, f);
        }

        if (wallSet.Count == 0)
        {
            Debug.Log("[MazeEditor] 저장할 벽이 없습니다.");
            return;
        }

        // 플레이어 시작 칸(P)
        int pf = -1, px = 0, pz = 0;
        if (player != null)
        {
            pf = Mathf.RoundToInt(player.position.y);
            px = Mathf.RoundToInt(player.position.x);
            pz = Mathf.RoundToInt(player.position.z);
            minX = Mathf.Min(minX, px); maxX = Mathf.Max(maxX, px);
            minZ = Mathf.Min(minZ, pz); maxZ = Mathf.Max(maxZ, pz);
            maxF = Mathf.Max(maxF, Mathf.Max(pf, 0));
        }

        // 모든 층을 같은 사각형 범위로 그려 층 간 정렬을 유지한다.
        var model = new MazeFile.Model { floors = new MazeFile.FloorDTO[maxF + 1] };
        for (int f = 0; f <= maxF; f++)
        {
            var rows = new string[maxZ - minZ + 1];
            for (int z = minZ; z <= maxZ; z++)
            {
                var sb = new StringBuilder();
                for (int x = minX; x <= maxX; x++)
                {
                    char c = '.';
                    if (wallSet.Contains(new Vector3Int(x, f, z))) c = '#';
                    else if (f == pf && x == px && z == pz) c = 'P';
                    sb.Append(c);
                }
                rows[z - minZ] = sb.ToString();
            }
            model.floors[f] = new MazeFile.FloorDTO { rows = rows };
        }

        MazeFile.Save(model);
        Debug.Log($"[MazeEditor] 편집 저장됨. 플레이를 멈추면 씬에 반영됩니다.\n{MazeFile.Path}");
    }

    void Export()
    {
        // 현재 층의 벽 칸을 수집.
        var walls = new HashSet<Vector2Int>();
        int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var t in mazeRoot.GetComponentsInChildren<Transform>())
        {
            if (t.name != "Wall") continue;
            if (Mathf.Abs(t.position.y - (editFloor + 0.5f)) > 0.1f) continue;

            int x = Mathf.RoundToInt(t.position.x);
            int z = Mathf.RoundToInt(t.position.z);
            walls.Add(new Vector2Int(x, z));
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
        }

        if (walls.Count == 0)
        {
            Debug.Log("[MazeEditor] 이 층에 벽이 없습니다.");
            return;
        }

        // rows[0]이 z 최소(가장 앞)인 MazeGenerator 규칙에 맞춰 출력.
        var sb = new StringBuilder();
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
                sb.Append(walls.Contains(new Vector2Int(x, z)) ? '#' : '.');
            sb.AppendLine();
        }

        string map = sb.ToString();
        GUIUtility.systemCopyBuffer = map;
        Debug.Log($"[MazeEditor] {editFloor + 1}층 맵 (클립보드 복사됨):\n{map}");
    }
}
#endif

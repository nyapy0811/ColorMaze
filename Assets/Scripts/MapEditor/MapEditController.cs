using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 인게임 맵 에디터의 배치/제거 로직. 개발자용 MazeGeneratorEditor(Scene 뷰)의 클릭=설치/
/// 우클릭=제거, 면 판정+그리드 스냅 방식을 런타임 카메라 레이캐스트로 그대로 옮겼다.
/// 화면 하단 기물 팔레트·파라미터 입력 UI는 아래 public 메서드를 버튼/입력창 OnClick·OnValueChanged에
/// 연결해서 쓴다(실제 UI 배치는 MapEditor.unity 씬에서 직접 구성).
/// </summary>
public class MapEditController : MonoBehaviour
{
    [SerializeField] CustomStagePrefabs prefabs;
    [SerializeField] float maxPlaceDistance = 1000f;
    [SerializeField] LayerMask placementMask = ~0;

    readonly CustomStageData data = new();
    int nextFixtureId = 1;

    FixtureType? currentFixtureType; // null = 기본 블록
    int presetR, presetG, presetB;
    LightColor presetColorA, presetColorB;

    class PlacedCell
    {
        public GameObject GameObject;
        public BlockEntry Block;
        public FixtureEntry Fixture;
    }

    readonly Dictionary<Vector3Int, PlacedCell> cells = new();

    Transform mazeRoot;
    Transform mapObjectsRoot;

    /// <summary>지금까지 배치한 내용을 담은 데이터. 저장/불러오기·플레이 테스트가 이걸 그대로 쓴다.</summary>
    public CustomStageData Data => data;

    void Awake()
    {
        data.id = System.Guid.NewGuid().ToString();

        mazeRoot = new GameObject("Maze").transform;
        mazeRoot.SetParent(transform, false);

        mapObjectsRoot = new GameObject("MapObjects").transform;
        mapObjectsRoot.SetParent(transform, false);
    }

    void Update()
    {
        // 팔레트/입력창 등 UI를 클릭한 것까지 월드 배치로 새지 않게 막는다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (InputManager.Instance.ReadInteract()) TryPlace();
        if (InputManager.Instance.ReadRemove()) TryRemove();
    }

    // --- 배치/제거 ---

    void TryPlace()
    {
        if (!TryGetTargetCell(out Vector3 center)) return;

        Vector3Int cell = ToCell(center);
        if (cells.ContainsKey(cell)) return; // 이미 그 칸에 뭔가 있으면 무시(기존 에디터와 동일)

        if (currentFixtureType == null) PlaceBlockAt(cell);
        else PlaceFixtureAt(cell, currentFixtureType.Value);
    }

    void TryRemove()
    {
        var cam = Camera.main;
        if (cam == null || Mouse.current == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementMask, QueryTriggerInteraction.Ignore))
            return;

        var tag = hit.collider.GetComponentInParent<PlacedTag>();
        if (tag != null) RemoveCell(tag.Cell);
    }

    void PlaceBlockAt(Vector3Int cell)
    {
        var entry = new BlockEntry { x = cell.x, y = cell.y, z = cell.z };
        var go = CustomStageLoader.PlaceBlock(entry, prefabs, mazeRoot);
        Register(cell, go, entry, null);
        data.blocks.Add(entry);
    }

    void PlaceFixtureAt(Vector3Int cell, FixtureType type)
    {
        var entry = new FixtureEntry
        {
            id = nextFixtureId++,
            type = type,
            x = cell.x, y = cell.y, z = cell.z,
            paramR = presetR, paramG = presetG, paramB = presetB,
            paramColorA = presetColorA, paramColorB = presetColorB,
        };

        var instance = CustomStageLoader.PlaceFixture(entry, prefabs, mapObjectsRoot);
        if (instance == null) return; // 팔레트 프리팹이 연결 안 돼 있으면 조용히 무시

        Register(cell, instance.gameObject, null, entry);
        data.fixtures.Add(entry);

        if (IsFilter(type)) FilterBlockBase.RebuildAll();
    }

    void Register(Vector3Int cell, GameObject go, BlockEntry block, FixtureEntry fixture)
    {
        var tag = go.AddComponent<PlacedTag>();
        tag.Cell = cell;
        cells[cell] = new PlacedCell { GameObject = go, Block = block, Fixture = fixture };
    }

    void RemoveCell(Vector3Int cell)
    {
        if (!cells.TryGetValue(cell, out var placed)) return;
        cells.Remove(cell);

        if (placed.Block != null) data.blocks.Remove(placed.Block);
        if (placed.Fixture != null)
        {
            data.fixtures.Remove(placed.Fixture);
            data.correctOrder1FixtureIds.Remove(placed.Fixture.id);
            data.correctOrder2FixtureIds.Remove(placed.Fixture.id);
        }

        bool wasFilter = placed.Fixture != null && IsFilter(placed.Fixture.type);

        Object.Destroy(placed.GameObject);

        if (wasFilter) FilterBlockBase.RebuildAll();
    }

    static bool IsFilter(FixtureType type) => type == FixtureType.ColorFilter || type == FixtureType.RgbFilter;

    // 마우스 위치 → 설치될 칸 중심. 블록에 맞은 면 쪽 이웃 칸(위/아래/옆 모두)에 스냅한다.
    // 기존 개발자용 에디터(MazeGeneratorEditor.TryGetTargetCell)와 동일한 규칙: x,z=정수, y=정수+0.5.
    bool TryGetTargetCell(out Vector3 center)
    {
        var cam = Camera.main;
        if (cam == null || Mouse.current == null) { center = default; return false; }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementMask, QueryTriggerInteraction.Ignore))
        {
            float px = hit.point.x + hit.normal.x * 0.5f;
            float py = hit.point.y + hit.normal.y * 0.5f;
            float pz = hit.point.z + hit.normal.z * 0.5f;
            center = new Vector3(
                Mathf.RoundToInt(px),
                Mathf.Round(py - 0.5f) + 0.5f,
                Mathf.RoundToInt(pz));
            return true;
        }

        center = default;
        return false;
    }

    static Vector3Int ToCell(Vector3 center) => new Vector3Int(
        Mathf.RoundToInt(center.x),
        Mathf.RoundToInt(center.y - 0.5f),
        Mathf.RoundToInt(center.z));

    // --- 기물 팔레트 UI 연결용 ---

    /// <summary>다음 클릭부터 기본 블록을 설치하도록 전환한다.</summary>
    public void SelectBlockTool() => currentFixtureType = null;

    /// <summary>다음 클릭부터 지정한 기물을 설치하도록 전환한다. FixtureType의 int 값(순서)을 받는다
    /// (버튼 OnClick에서 정수 파라미터로 바로 연결 가능).</summary>
    public void SelectFixtureTool(int type) => currentFixtureType = (FixtureType)type;

    // --- 파라미터 입력 UI 연결용 ---

    /// <summary>맵 제목을 지정한다.</summary>
    public void SetTitle(string title) => data.title = title;

    public void SetPresetR(string value) => int.TryParse(value, out presetR);
    public void SetPresetG(string value) => int.TryParse(value, out presetG);
    public void SetPresetB(string value) => int.TryParse(value, out presetB);

    /// <summary>LightColor 드롭다운(Red=0, Green=1, Blue=2)의 OnValueChanged에 바로 연결.</summary>
    public void SetPresetColorA(int index) => presetColorA = (LightColor)index;
    public void SetPresetColorB(int index) => presetColorB = (LightColor)index;
}

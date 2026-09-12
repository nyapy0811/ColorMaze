using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField] GameObject rgbInputPanel;
    [SerializeField] GameObject rgbSelectPanel;
    [SerializeField] Toggle[] rgbSelectToggles; // Red, Green, Blue 순서로 연결(LightColor enum과 동일 순서)
    [SerializeField] TMP_InputField rgbInputField; // 기물 재선택 시 텍스트 초기화용
    [SerializeField] Image[] hotBarSlotImages; // 슬롯 1~8 순서로 연결
    [SerializeField] Color hotBarNormalColor = Color.white;
    [SerializeField] Color hotBarSelectedColor = Color.yellow;

    readonly CustomStageData data = new();
    int nextFixtureId = 1;

    FixtureType? currentFixtureType; // null = 기본 블록
    int presetR, presetG, presetB;
    LightColor presetColorA, presetColorB;

    // RGBSelect 패널은 단일 색상(RgbFilter/Bucket)과 두 색상(StackChanger, 3개 중 2개 선택) 선택에
    // 재사용된다. pendingColor/SetPendingColor는 기존 토글 onValueChanged 바인딩 때문에 남겨뒀지만
    // 확정 판정(ConfirmColorSelect)에서는 더 이상 참조하지 않는다(토글의 실제 isOn 상태를 직접 읽음).
    LightColor pendingColor;

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
        GameManager.Instance.ChangeState(GameState.MapEditor);

        data.id = System.Guid.NewGuid().ToString();

        mazeRoot = new GameObject("Maze").transform;
        mazeRoot.SetParent(transform, false);

        mapObjectsRoot = new GameObject("MapObjects").transform;
        mapObjectsRoot.SetParent(transform, false);

        RegisterPreplacedBlocks();

        UpdateHotBarHighlight(1); // 기본 선택(블록) 표시
    }

    // 씬에 미리 배치해 둔 시작용 블록(예: 발판용 "Block")을 Maze 밑으로 옮기고 정식 배치 데이터로
    // 등록한다 — 그 결과 다른 블록과 완전히 동일하게 Ctrl+좌클릭으로 제거할 수 있고 data.blocks에도
    // 포함된다. 대상은 Awake 시점에 이미 컨트롤러의 자식으로 있던 오브젝트 전부(Maze/MapObjects 본인 제외).
    void RegisterPreplacedBlocks()
    {
        var preplaced = new List<Transform>();
        foreach (Transform child in transform)
            if (child != mazeRoot && child != mapObjectsRoot)
                preplaced.Add(child);

        foreach (var child in preplaced)
        {
            Vector3Int cell = ToCell(child.position);
            if (cells.ContainsKey(cell)) continue; // 이미 등록된 칸이면 건드리지 않음

            child.SetParent(mazeRoot, true);
            var entry = new BlockEntry { x = cell.x, y = cell.y, z = cell.z };
            Register(cell, child.gameObject, entry, null);
            data.blocks.Add(entry);
        }
    }

    void Update()
    {
        // 일시정지 중에는 Time.timeScale이 0이어도 Update()는 계속 돌기 때문에, 기물 선택·배치·제거
        // 등 편집 입력을 전부 막고 커서 관리도 PauseMenuController에 맡긴다.
        if (GameManager.Instance.State == GameState.Paused) { HideHoverPreview(); return; }

        UpdateCursorLock(); // 우클릭(시점 회전) 상태가 매 프레임 바뀌므로 매 프레임 갱신

        // 파라미터 패널이 떠 있는 동안(RGB 값 입력 중)은 숫자키가 핫바 전환과 겹치면 안 되므로 무시.
        if (!IsParamPanelOpen && InputManager.Instance.ReadHotBarSlot(out int slot))
        {
            if (slot == 1) SelectBlockTool();
            else SelectFixtureTool(slot - 2);
        }

        // 스페이스바로도 Confirm 버튼과 동일하게 확정할 수 있게 한다(마우스 포인터 위치와 무관).
        if (IsParamPanelOpen && InputManager.Instance.ReadConfirm())
        {
            if (rgbInputPanel != null && rgbInputPanel.activeSelf) ConfirmRGBInput();
            else if (rgbSelectPanel != null && rgbSelectPanel.activeSelf) ConfirmColorSelect();
        }

        // 팔레트/입력창 등 UI를 클릭한 것까지 월드 배치로 새지 않게 막는다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HideHoverPreview();
            return;
        }

        HandleDragRect();
        if (dragging) { HideHoverPreview(); return; }

        UpdateHoverPreview();

        if (InputManager.Instance.ReadInteract()) TryPlace();
        if (InputManager.Instance.ReadRemove()) TryRemove();
    }

    // --- 배치/제거 ---

    void TryPlace()
    {
        if (!TryGetTargetCell(out Vector3 center, out _)) return;

        Vector3Int cell = ToCell(center);
        if (cells.ContainsKey(cell)) return; // 이미 그 칸에 뭔가 있으면 무시(기존 에디터와 동일)

        if (currentFixtureType == null) PlaceBlockAt(cell);
        else PlaceFixtureAt(cell, currentFixtureType.Value);
    }

    void TryRemove()
    {
        if (TryGetRemoveTargetCell(out Vector3Int cell)) RemoveCell(cell);
    }

    // 마우스가 가리키는 기존 배치물의 칸. 실제 모양과 무관하게 항상 블록 크기로 판정한다
    // (TryRaycastCells). TryRemove와 제거 미리보기(UpdateHoverPreview) 양쪽에서 쓴다.
    bool TryGetRemoveTargetCell(out Vector3Int cell)
    {
        var cam = Camera.main;
        if (cam == null || Mouse.current == null) { cell = default; return false; }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        return TryRaycastCells(ray, out cell, out _, out _);
    }

    // 배치된 모든 칸에 대해 가상의 1x1x1 박스로 레이 교차를 검사해서, 기물의 실제 모양(얇거나 작을
    // 수 있음)과 무관하게 항상 블록 하나 크기로 클릭 판정을 준다. 가장 가까운 칸과 진입면을 반환한다.
    bool TryRaycastCells(Ray ray, out Vector3Int hitCell, out Vector3Int normal, out float distance)
    {
        hitCell = default;
        normal = default;
        distance = float.PositiveInfinity;
        bool found = false;

        foreach (var cell in cells.Keys)
        {
            Vector3 center = CellCenter(cell);

            // 카메라가 이 칸 안이나 바로 근처에 있으면 통째로 건너뛴다 — EditorFlyCamera는 충돌 없이
            // 자유 비행이라(특히 뒤로 Dolly할 때) 블록 안/바로 옆으로 들어갈 수 있는데, 그 상태에서
            // Bounds.IntersectRay를 그대로 쓰면 카메라 위치 기준의 임의 방향이 진입면으로 잡혀
            // 플레이어 근처 허공에 미리보기가 뜬다. 실제 1칸 크기보다 약간 넉넉한 범위로 제외한다.
            var selfExclusionBounds = new Bounds(center, Vector3.one * 1.2f);
            if (selfExclusionBounds.Contains(ray.origin)) continue;

            var bounds = new Bounds(center, Vector3.one);
            if (!bounds.IntersectRay(ray, out float dist) || dist >= distance) continue;

            found = true;
            distance = dist;
            hitCell = cell;
        }

        if (found) normal = FaceNormalFromPoint(ray.GetPoint(distance) - CellCenter(hitCell));
        return found;
    }

    // 박스 중심 기준 로컬 좌표에서 가장 많이 벗어난 축의 방향을 그 면의 바깥 법선으로 삼는다.
    static Vector3Int FaceNormalFromPoint(Vector3 local)
    {
        float ax = Mathf.Abs(local.x), ay = Mathf.Abs(local.y), az = Mathf.Abs(local.z);
        if (ax >= ay && ax >= az) return new Vector3Int(local.x >= 0 ? 1 : -1, 0, 0);
        if (ay >= ax && ay >= az) return new Vector3Int(0, local.y >= 0 ? 1 : -1, 0);
        return new Vector3Int(0, 0, local.z >= 0 ? 1 : -1);
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
    // 바닥/벽처럼 아직 배치되지 않은 표면은 실제 물리 레이캐스트로, 이미 배치된 칸(기물 포함)은
    // 실제 모양과 무관하게 항상 블록 크기 가상 박스(TryRaycastCells)로 판정해서 더 가까운 쪽을 쓴다.
    bool TryGetTargetCell(out Vector3 center, out Vector3 normal)
    {
        var cam = Camera.main;
        if (cam == null || Mouse.current == null) { center = default; normal = default; return false; }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // 필터류는 플레이어 스택과 맞으면 콜라이더가 트리거로 바뀌므로(FilterBlockBase.Refresh), 편집
        // 중에는 트리거도 맞아야 클릭이 씹히지 않는다 — Ignore가 아니라 Collide로 레이캐스트한다.
        bool havePhysicsHit = Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementMask, QueryTriggerInteraction.Collide);
        bool haveCellHit = TryRaycastCells(ray, out Vector3Int cellHit, out Vector3Int cellNormal, out float cellDist);

        if (haveCellHit && (!havePhysicsHit || cellDist <= hit.distance))
        {
            center = CellCenter(cellHit + cellNormal);
            normal = cellNormal;
            return true;
        }

        if (havePhysicsHit)
        {
            float px = hit.point.x + hit.normal.x * 0.5f;
            float py = hit.point.y + hit.normal.y * 0.5f;
            float pz = hit.point.z + hit.normal.z * 0.5f;
            center = new Vector3(
                Mathf.RoundToInt(px),
                Mathf.Round(py - 0.5f) + 0.5f,
                Mathf.RoundToInt(pz));
            normal = hit.normal;
            return true;
        }

        center = default;
        normal = default;
        return false;
    }

    // --- 단일 클릭 설치/제거 미리보기 (드래그 미리보기와 같은 고스트 큐브, 재질만 공유) ---

    GameObject hoverPreview;

    // Ctrl이 눌려있으면 제거 대상(TryRemove가 실제로 지울 칸), 아니면 설치 대상(TryPlace가 실제로
    // 채울 칸)을 미리 보여준다 — 클릭해도 아무 일 안 일어나는 상황(빈 곳 제거, 이미 막힌 칸 설치)
    // 에서는 미리보기도 뜨지 않는다.
    void UpdateHoverPreview()
    {
        if (InputManager.Instance.ReadRemoveModifierHeld())
        {
            if (TryGetRemoveTargetCell(out Vector3Int cell))
                ShowHoverPreview(CellCenter(cell), RedGhostMaterial());
            else
                HideHoverPreview();
            return;
        }

        if (TryGetTargetCell(out Vector3 center, out _) && !cells.ContainsKey(ToCell(center)))
            ShowHoverPreview(center, GreenGhostMaterial());
        else
            HideHoverPreview();
    }

    void ShowHoverPreview(Vector3 center, Material mat)
    {
        if (hoverPreview == null)
        {
            hoverPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hoverPreview.name = "HoverPreview";
            Object.Destroy(hoverPreview.GetComponent<Collider>()); // 미리보기가 배치 레이캐스트에 걸리면 안 됨
            hoverPreview.transform.localScale = Vector3.one * 1.01f;
        }
        hoverPreview.SetActive(true);
        hoverPreview.transform.position = center;
        hoverPreview.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    void HideHoverPreview()
    {
        if (hoverPreview != null) hoverPreview.SetActive(false);
    }

    // --- Shift+드래그 직사각형 범위 설치/제거 (MazeGeneratorEditor의 Scene 뷰 기능을 런타임으로 이식) ---

    const int MaxDragCells = 1000; // 너무 큰 범위로 프레임 드랍/대량 생성되는 것 방지

    bool dragging;
    bool dragRemove;
    int dragAxis; // 드래그 평면의 고정축 (0=x, 1=y, 2=z)
    Vector3 dragStartCenter, dragEndCenter;
    readonly List<GameObject> dragPreviewPool = new();
    static Material greenGhost, redGhost;

    void HandleDragRect()
    {
        if (dragging)
        {
            UpdateDragRect();
            ShowDragPreview();

            if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
            {
                CommitDragRect();
                dragging = false;
                ClearDragPreview();
            }
            return;
        }

        if (!InputManager.Instance.ReadRangeModifierHeld()) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (!TryGetTargetCell(out Vector3 center, out Vector3 normal)) return;

        dragging = true;
        dragRemove = InputManager.Instance.ReadRemoveModifierHeld();
        dragAxis = DominantAxis(normal);

        // 고정축은 클릭한 면 쪽 이웃 칸이 아니라, 실제로 클릭한 블록 자신의 칸으로 맞춘다
        // (Y면을 클릭해 드래그하면 한 층 위/아래가 아니라 같은 층에서 옆으로 채워지도록).
        Vector3 start = center;
        if (dragAxis == 0) start.x -= normal.x;
        else if (dragAxis == 1) start.y -= normal.y;
        else start.z -= normal.z;
        dragStartCenter = start;
        dragEndCenter = start;
    }

    static int DominantAxis(Vector3 n)
    {
        float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
        return ax >= ay && ax >= az ? 0 : (ay >= ax && ay >= az ? 1 : 2);
    }

    static Vector3 AxisVector(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

    // 드래그 시작 칸을 지나는 고정축 평면과 마우스 레이의 교점으로 반대쪽 끝 칸을 갱신.
    void UpdateDragRect()
    {
        var cam = Camera.main;
        if (cam == null || Mouse.current == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!new Plane(AxisVector(dragAxis), dragStartCenter).Raycast(ray, out float dist)) return;

        Vector3 p = ray.GetPoint(dist);
        float x = dragAxis == 0 ? dragStartCenter.x : Mathf.RoundToInt(p.x);
        float y = dragAxis == 1 ? dragStartCenter.y : Mathf.Round(p.y - 0.5f) + 0.5f;
        float z = dragAxis == 2 ? dragStartCenter.z : Mathf.RoundToInt(p.z);
        dragEndCenter = new Vector3(x, y, z);
    }

    // 시작/끝 칸 사이의 직사각형 범위에 속한 모든 칸 중심 목록.
    List<Vector3> GetDragCenters()
    {
        var result = new List<Vector3>();
        float minX = Mathf.Min(dragStartCenter.x, dragEndCenter.x), maxX = Mathf.Max(dragStartCenter.x, dragEndCenter.x);
        float minY = Mathf.Min(dragStartCenter.y, dragEndCenter.y), maxY = Mathf.Max(dragStartCenter.y, dragEndCenter.y);
        float minZ = Mathf.Min(dragStartCenter.z, dragEndCenter.z), maxZ = Mathf.Max(dragStartCenter.z, dragEndCenter.z);

        for (float x = minX; x <= maxX + 0.01f; x += 1f)
            for (float y = minY; y <= maxY + 0.01f; y += 1f)
                for (float z = minZ; z <= maxZ + 0.01f; z += 1f)
                {
                    result.Add(new Vector3(x, y, z));
                    if (result.Count >= MaxDragCells) return result;
                }
        return result;
    }

    void CommitDragRect()
    {
        foreach (var center in GetDragCenters())
        {
            Vector3Int cell = ToCell(center);
            if (dragRemove)
            {
                if (cells.ContainsKey(cell)) RemoveCell(cell);
            }
            else if (!cells.ContainsKey(cell))
            {
                if (currentFixtureType == null) PlaceBlockAt(cell);
                else PlaceFixtureAt(cell, currentFixtureType.Value);
            }
        }
    }

    void ShowDragPreview()
    {
        var centers = GetDragCenters();
        while (dragPreviewPool.Count < centers.Count)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DragPreview";
            Object.Destroy(go.GetComponent<Collider>()); // 미리보기가 배치 레이캐스트에 걸리면 안 됨
            go.transform.localScale = Vector3.one * 1.01f;
            dragPreviewPool.Add(go);
        }

        var mat = dragRemove ? RedGhostMaterial() : GreenGhostMaterial();
        for (int i = 0; i < dragPreviewPool.Count; i++)
        {
            bool active = i < centers.Count;
            dragPreviewPool[i].SetActive(active);
            if (!active) continue;
            dragPreviewPool[i].transform.position = centers[i];
            dragPreviewPool[i].GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }

    void ClearDragPreview()
    {
        foreach (var go in dragPreviewPool) go.SetActive(false);
    }

    static Material GreenGhostMaterial() => greenGhost ??= new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.3f, 1f, 0.4f) };
    static Material RedGhostMaterial() => redGhost ??= new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.25f, 0.25f) };

    static Vector3Int ToCell(Vector3 center) => new Vector3Int(
        Mathf.RoundToInt(center.x),
        Mathf.RoundToInt(center.y - 0.5f),
        Mathf.RoundToInt(center.z));

    static Vector3 CellCenter(Vector3Int cell) => new Vector3(cell.x, cell.y + 0.5f, cell.z);

    // --- 기물 팔레트 UI 연결용 ---

    /// <summary>다음 클릭부터 기본 블록을 설치하도록 전환한다.</summary>
    public void SelectBlockTool()
    {
        currentFixtureType = null;
        HideParamPanels();
        UpdateHotBarHighlight(1);
    }

    /// <summary>다음 클릭부터 지정한 기물을 설치하도록 전환한다. FixtureType의 int 값(순서)을 받는다
    /// (버튼 OnClick에서 정수 파라미터로 바로 연결 가능). 기물 종류에 맞는 파라미터 패널도 같이 연다.
    /// 이미 선택돼 있던 기물을 다시 선택하면(재선택) 패널 값을 초기화한다 — 다른 기물을 거쳐 돌아온
    /// 최초 복귀는 재선택이 아니므로 기존 값이 유지된다.</summary>
    public void SelectFixtureTool(int type)
    {
        var fixtureType = (FixtureType)type;
        bool reselecting = currentFixtureType.HasValue && currentFixtureType.Value == fixtureType;
        currentFixtureType = fixtureType;

        if (reselecting) ResetPanelValues(fixtureType);

        switch (fixtureType)
        {
            case FixtureType.ColorFilter:
            case FixtureType.Canvas:
            case FixtureType.Palette:
                SetPanelActive(rgbInputPanel, true);
                SetPanelActive(rgbSelectPanel, false);
                break;
            case FixtureType.RgbFilter:
            case FixtureType.Bucket:
            case FixtureType.StackChanger:
                SetPanelActive(rgbInputPanel, false);
                SetPanelActive(rgbSelectPanel, true);
                break;
            default: // ColorChanger 등 파라미터가 필요 없는 기물
                HideParamPanels();
                break;
        }

        UpdateHotBarHighlight(type + 2); // 0=ColorFilter→슬롯2 ... 6=StackChanger→슬롯8
    }

    // 같은 기물을 다시 선택했을 때(재선택)만 패널을 빈 상태로 되돌린다.
    void ResetPanelValues(FixtureType type)
    {
        switch (type)
        {
            case FixtureType.ColorFilter:
            case FixtureType.Canvas:
            case FixtureType.Palette:
                presetR = presetG = presetB = 0;
                if (rgbInputField != null) rgbInputField.text = "";
                break;
            case FixtureType.RgbFilter:
            case FixtureType.Bucket:
            case FixtureType.StackChanger:
                foreach (var t in rgbSelectToggles)
                    if (t != null) t.isOn = false;
                break;
        }
    }

    void HideParamPanels()
    {
        SetPanelActive(rgbInputPanel, false);
        SetPanelActive(rgbSelectPanel, false);
    }

    static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    bool IsParamPanelOpen =>
        (rgbInputPanel != null && rgbInputPanel.activeSelf) || (rgbSelectPanel != null && rgbSelectPanel.activeSelf);

    // 우클릭(시점 회전, EditorFlyCamera) 중일 때만 커서를 잠근다 — 유니티 씬 뷰와 동일하게 그 외에는
    // 항상 커서가 보여야 Pan/Dolly나 RGBInput/RGBSelect 패널 조작이 가능하다.
    void UpdateCursorLock()
    {
        bool shouldLock = !IsParamPanelOpen && InputManager.Instance.ReadRotateHeld();
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    void UpdateHotBarHighlight(int slot)
    {
        for (int i = 0; i < hotBarSlotImages.Length; i++)
            if (hotBarSlotImages[i] != null)
                hotBarSlotImages[i].color = (i + 1 == slot) ? hotBarSelectedColor : hotBarNormalColor;
    }

    // --- 파라미터 입력 UI 연결용 ---

    /// <summary>맵 제목을 지정한다.</summary>
    public void SetTitle(string title) => data.title = title;

    public void SetPresetR(string value) => int.TryParse(value, out presetR);
    public void SetPresetG(string value) => int.TryParse(value, out presetG);
    public void SetPresetB(string value) => int.TryParse(value, out presetB);

    /// <summary>RGBInput 입력창("RRGGBB" 6자리, 2자리씩 R/G/B)의 OnValueChanged에 연결.</summary>
    public void SetPresetRGB(string value)
    {
        if (value.Length < 6) return;
        SetPresetR(value.Substring(0, 2));
        SetPresetG(value.Substring(2, 2));
        SetPresetB(value.Substring(4, 2));
    }

    /// <summary>LightColor 드롭다운(Red=0, Green=1, Blue=2)의 OnValueChanged에 바로 연결.</summary>
    public void SetPresetColorA(int index) => presetColorA = (LightColor)index;
    public void SetPresetColorB(int index) => presetColorB = (LightColor)index;

    /// <summary>RGBSelect의 Red/Green/Blue 토글 OnValueChanged에 정적 인자(0/1/2)로 연결.
    /// 실제 A/B 반영은 Confirm 클릭 시(ConfirmColorSelect)에 이뤄진다.</summary>
    public void SetPendingColor(int index) => pendingColor = (LightColor)index;

    /// <summary>RGBInput의 Confirm 버튼 OnClick에 연결 — 입력은 이미 실시간 반영되므로 패널만 닫는다.</summary>
    public void ConfirmRGBInput() => HideParamPanels();

    /// <summary>RGBSelect의 Confirm 버튼 OnClick에 연결. StackChanger는 3개 중 정확히 2개가 선택돼
    /// 있어야(배열 순서상 앞쪽이 A, 뒤쪽이 B) 한 번에 확정되고, 그 외(RgbFilter/Bucket)는 정확히
    /// 1개가 선택돼 있어야 확정된다. 조건이 안 맞으면 패널이 닫히지 않는다.</summary>
    public void ConfirmColorSelect()
    {
        var on = new List<int>();
        for (int i = 0; i < rgbSelectToggles.Length; i++)
            if (rgbSelectToggles[i] != null && rgbSelectToggles[i].isOn)
                on.Add(i);

        if (currentFixtureType == FixtureType.StackChanger)
        {
            if (on.Count != 2) return; // 정확히 2개 선택돼야 확정 — 아니면 패널 유지
            SetPresetColorA(on[0]);
            SetPresetColorB(on[1]);
        }
        else
        {
            if (on.Count != 1) return; // 정확히 1개 선택돼야 확정 — 아니면 패널 유지
            SetPresetColorA(on[0]);
        }

        HideParamPanels();
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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

    [System.Serializable]
    class FixtureValuePanel
    {
        public RectTransform buttonRect;   // 이 기물 버튼 자신의 RectTransform — 비활성 80 / 활성 160 높이 전환용
        public GameObject panel;           // 버튼 밑에 인라인으로 달린 값 입력 컨테이너(Valueinput) — RGBInput/RGBSelect를 둘 다 자식으로 가짐
        public GameObject rgbInputPanel;   // panel/RGBInput
        public GameObject rgbSelectPanel;  // panel/RGBSelect
        public TMP_InputField rgbField;    // panel/RGBInput 안의 TMP_InputField
        public Image[] colorButtonImages;  // panel/RGBSelect 안의 Red/Green/Blue Image (RGBSelect류일 때만 연결)
    }

    // RGBInput(텍스트 입력)을 쓰는 기물 종류. 나머지 파라미터 필요 기물(RgbFilter/Bucket/StackChanger)은
    // RGBSelect(색 버튼 클릭)를 쓴다. ColorChanger/Block은 애초에 파라미터가 없어 해당 없음.
    static bool UsesRgbInput(FixtureType type) =>
        type == FixtureType.ColorFilter || type == FixtureType.Canvas || type == FixtureType.Palette;

    // RGBSelect 버튼(Red, Green, Blue 순서)의 원래(선택됨) 색. 선택 안 된 버튼은 흰색과 50% 섞어 연하게 표시한다.
    static readonly Color[] ColorButtonBaseColors = { Color.red, Color.green, Color.blue };

    static void UpdateColorButtonHighlight(Image[] images, IReadOnlyList<int> selected)
    {
        if (images == null) return;
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            bool isSelected = selected != null && selected.Contains(i);
            images[i].color = isSelected ? ColorButtonBaseColors[i] : Color.Lerp(ColorButtonBaseColors[i], Color.white, 0.5f);
        }
    }

    const float CollapsedButtonHeight = 80f;
    const float ExpandedButtonHeight = 160f;

    // ObjectButton(0=Block)~(7=StackChanger)과 동일한 인덱스. 파라미터가 필요 없는 기물(Block,
    // ColorChanger)은 배열 항목의 panel을 비워둔다. 평소엔 버튼 높이 80에 패널 비활성 상태이다가, 그
    // 버튼이 선택된 동안에만 높이 160으로 커지면서 패널이 활성화된다.
    [SerializeField] FixtureValuePanel[] fixtureValuePanels;
    [SerializeField] RectTransform objectListContent; // mode1Panel/ObjectList/Viewport/Content — 높이 변경 후 레이아웃 강제 재계산용

    [SerializeField] Transform orderListContent;       // CorrectOrder/OrderList/Viewport/Content
    [SerializeField] OrderListItemUI orderItemTemplate; // 위 Content 밑에 비활성 상태로 두는 항목 템플릿
    [SerializeField] Transform canvasListContent;       // CorrectOrder/CanvasList/Viewport/Content
    [SerializeField] CanvasCardUI canvasCardTemplate;   // 위 Content 밑에 비활성 상태로 두는 카드 템플릿
    [SerializeField] Button addToOrderButton;           // 항상 고정 표시되는 "기물 추가" 버튼
    [SerializeField] Color tabNormalColor = Color.white;
    [SerializeField] Color tabSelectedColor = Color.yellow;

    [SerializeField] GameObject mode1Panel, mode2Panel, mode3Panel, mode4Panel, mode5Panel;
    [SerializeField] Image[] modeTabImages;      // Mode1, Mode2, Mode3 순서
    [SerializeField] Image[] objectButtonImages; // ObjectButton, (1)..(7) 순서 — 기물 팔레트 하이라이트용

    // mode2Panel 전용 값 수정 파라미터 패널 — mode1Panel의 fixtureValuePanels와는 별개 오브젝트
    [SerializeField] GameObject editRgbInputPanel;
    [SerializeField] GameObject editRgbSelectPanel;
    [SerializeField] TMP_InputField editRgbInputField;
    [SerializeField] Image[] editColorButtonImages; // editRgbSelectPanel 안의 Red/Green/Blue Image

    // 값 수정 모드에서 기물마다 뜨는 클릭 가능한 화면 마크
    [SerializeField] Button editMarkTemplate;    // UICanvas 밑에 항상 비활성 상태로 두는 템플릿
    [SerializeField] RectTransform editMarkRoot; // 화면 전체 크기 컨테이너

    int selectedCanvasFixtureId = -1; // 지금 OrderList에 보여주는 캔버스의 기물 id. 없으면 -1

    readonly CustomStageData data = new();
    int nextFixtureId = 1;

    FixtureType? currentFixtureType; // null = 기본 블록

    // 배치 모드의 파라미터 preset. 기물 종류별로 완전히 독립적으로 저장한다(예: RgbFilter에서 고른 색이
    // Bucket이나 StackChanger에 새지 않도록) — 예전엔 전역 변수 하나를 모든 종류가 공유해서, 색을 안
    // 골라도 이전에 다른 기물에 골랐던 색이 그대로 적용되는 버그가 있었다.
    class PresetValues
    {
        public int r, g, b;
        // RGBSelect류 최근 클릭 기록(중복 색 제거됨) — 단일 선택(RgbFilter/Bucket)은 1개,
        // StackChanger는 2개가 모여야 유효한 선택으로 친다(colorClicks[0]=A, [1]=B).
        public readonly List<int> colorClicks = new();
    }

    readonly Dictionary<FixtureType, PresetValues> presets = new();

    PresetValues GetPreset(FixtureType type)
    {
        if (!presets.TryGetValue(type, out var p)) presets[type] = p = new PresetValues();
        return p;
    }

    static int RequiredColorClicks(FixtureType type) => type == FixtureType.StackChanger ? 2 : 1;

    // colorIndex를 clicks에 추가한다. 이미 들어있으면(같은 색 재클릭) 무시하고 false를 반환 — StackChanger에서
    // 같은 색이 A/B에 중복 선택되는 걸 막는다. 꽉 차면(StackChanger 2개, 그 외 1개) 가장 오래된 것부터 밀어낸다.
    static bool RegisterColorClick(List<int> clicks, int colorIndex, FixtureType type)
    {
        if (clicks.Contains(colorIndex)) return false;
        clicks.Add(colorIndex);
        int maxClicks = RequiredColorClicks(type);
        while (clicks.Count > maxClicks) clicks.RemoveAt(0);
        return true;
    }

    // 배치 가능 여부: 파라미터가 없는 기물(Block/ColorChanger)이거나 RGBInput류(항상 유효, 기본 0/0/0
    // 허용)면 항상 true. RGBSelect류는 필요한 개수만큼 색을 실제로 골랐을 때만 true — 색을 안 고르고
    // 재선택으로 초기화된 채로 설치하면 엉뚱한(예전엔 빨간색 기본값) 기물이 깔리던 버그를 막는다.
    bool IsPlaceReady(FixtureType? type)
    {
        if (!type.HasValue) return true;
        if (UsesRgbInput(type.Value) || type.Value == FixtureType.ColorChanger) return true;
        return GetPreset(type.Value).colorClicks.Count == RequiredColorClicks(type.Value);
    }

    // 배치/제거(Place)·값 수정(ValueEdit)·정답 순서 추가(AddToOrder)·순서 목록 열람(OrderView)은 절대
    // 중첩되지 않고 항상 정확히 1개만 활성화된다 — 이후 모드가 늘어나도 이 enum에 추가하는 방식으로
    // 원칙을 유지한다. OrderView는 Mode3 탭 진입 시의 상태로, 카메라 이동만 가능하고 안쪽 "기물 추가"
    // 버튼을 눌러야 AddToOrder로 전환된다.
    enum EditorMode { Place, ValueEdit, AddToOrder, OrderView, SaveLoad, PlayView }
    EditorMode mode = EditorMode.Place;

    // 값 수정 모드에서 지금 선택된(편집 대상) 기물들 — 비어있으면 편집 대상 없음. 보통 1개지만, Shift+
    // 클릭으로 종류·현재 값이 완전히 같은 기물을 추가하면 여러 개를 한 번에 수정할 수 있다.
    // editingFixtures[i]와 editingFixtureInstances[i]는 인덱스로 1:1 대응.
    readonly List<FixtureEntry> editingFixtures = new();
    readonly List<MapObjectBase> editingFixtureInstances = new();

    // 값 수정 모드 전용 RGBSelect 최근 클릭 기록(Toggle이 아닌 일반 Button, Confirm 없이 클릭 즉시
    // 반영). 선택된 기물들이 전부 같은 값을 공유하는 상태에서 시작하므로 공유 필드 하나로 충분하다 —
    // 배치 모드는 기물 종류별로 완전히 분리된 PresetValues.colorClicks를 쓴다(값이 서로 새지 않도록).
    readonly List<int> editColorClicks = new();

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

        if (addToOrderButton != null) addToOrderButton.onClick.AddListener(EnterAddToOrderMode);

        // 편집 화면 대신 맵 선택 화면(LoadListPopup)을 맨 처음에 띄운다 — 기존 맵을 고르거나
        // New Map으로 새로 만들어야 비로소 편집 화면(ShowEditorUI)으로 넘어간다.
        if (selectionPanel != null) selectionPanel.SetActive(false);
        OpenLoadPopup();

        RefreshCanvasList();
        RefreshOrderListUI();
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
        // 플레이 테스트 중엔 에디터의 모든 프레임 로직을 완전히 건너뛴다 — 플레이어 조작은
        // FirstPersonController가 알아서 처리한다. ESC는 일시정지 메뉴를 거치지 않고 곧장
        // 에디터로 돌아간다(테스트 중엔 마우스 커서가 잠겨 있어 PlayTestOverlay의 버튼을 직접
        // 클릭할 수 없으므로 — "Back to Editor 버튼이 무반응이다" 버그로 발견됨).
        if (isPlayTesting)
        {
            if (InputManager.Instance.ReadPause()) StopPlayTest();
            return;
        }

        // 일시정지 중에는 Time.timeScale이 0이어도 Update()는 계속 돌기 때문에, 기물 선택·배치·제거
        // 등 편집 입력을 전부 막고 커서 관리도 PauseMenuController에 맡긴다.
        if (GameManager.Instance.State == GameState.Paused) { HideHoverPreview(); return; }

        UpdateCursorLock(); // 우클릭(시점 회전) 상태가 매 프레임 바뀌므로 매 프레임 갱신

        // 스페이스바 = 지금 편집 중인 기물의 값 수정을 마친다. 패널엔 Confirm 버튼이 없고(클릭/입력
        // 즉시 반영) 값은 이미 다 반영된 상태이므로, 그냥 편집 모드만 종료하면 된다.
        if (editingFixtures.Count > 0 && InputManager.Instance.ReadConfirm()) EndEdit();

        // 팔레트/입력창 등 UI를 클릭한 것까지 월드 배치로 새지 않게 막는다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HideHoverPreview();
            return;
        }

        switch (mode)
        {
            case EditorMode.ValueEdit:
            case EditorMode.OrderView:
            case EditorMode.AddToOrder:
            case EditorMode.SaveLoad:
            case EditorMode.PlayView:
                HideHoverPreview(); // 카메라 이동만, 월드 좌클릭으로 하는 일 없음(대상은 전부 화면 마크로 지정)
                return; // 설치·제거·드래그 범위는 전부 비활성
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
        if (!IsPlaceReady(currentFixtureType)) return; // RGBSelect류인데 색 선택이 안 끝났으면 설치 안 함

        Vector3Int cell = ToCell(center);
        if (cells.ContainsKey(cell)) return; // 이미 그 칸에 뭔가 있으면 무시(기존 에디터와 동일)

        if (currentFixtureType == null) PlaceBlockAt(cell);
        else PlaceFixtureAt(cell, currentFixtureType.Value);
    }

    void TryRemove()
    {
        if (TryGetRemoveTargetCell(out Vector3Int cell)) RemoveCell(cell);
    }


    // ColorChanger 등 파라미터 없는 기물은 값 수정 모드의 마크 대상에서 애초에 제외되므로(RefreshMarks의
    // 필터 조건) 여기 fixture.type은 항상 UsesRgbInput 여부로 RGBInput/RGBSelect 중 하나가 정해진다.
    // 기존 선택을 전부 지우고 이 기물 하나만으로 새로 시작한다(Shift 없이 클릭했을 때의 동작).
    void BeginEditFixture(FixtureEntry fixture, GameObject go)
    {
        ResetEditingSelection();
        editingFixtures.Add(fixture);
        editingFixtureInstances.Add(go.GetComponent<MapObjectBase>());

        bool usesInput = UsesRgbInput(fixture.type);
        SetPanelActive(editRgbInputPanel, usesInput);
        SetPanelActive(editRgbSelectPanel, !usesInput);

        if (usesInput)
        {
            if (editRgbInputField != null)
                editRgbInputField.text = $"{fixture.paramR:D2}{fixture.paramG:D2}{fixture.paramB:D2}";
        }
        else
        {
            editColorClicks.Clear();
            UpdateColorButtonHighlight(editColorButtonImages, editColorClicks); // 클릭 전엔 셋 다 연하게(미선택)
        }

        UpdateMarkHighlight();
    }

    // Shift+클릭으로 다중 선택에 추가 — 지금 선택된 기물들과 종류·현재 파라미터 값이 완전히 같을 때만
    // 허용한다(다르면 조용히 무시). 이미 같은 값이므로 패널 표시 상태는 새로 세팅할 필요 없다.
    void AddToEditSelection(FixtureEntry fixture, GameObject go)
    {
        if (editingFixtures.Any(f => f.id == fixture.id)) return; // 이미 선택돼 있으면 무시
        if (!MatchesEditingSelection(fixture)) return;

        editingFixtures.Add(fixture);
        editingFixtureInstances.Add(go.GetComponent<MapObjectBase>());
        UpdateMarkHighlight();
    }

    // fixture가 지금 편집 중인 그룹(editingFixtures[0] 기준)과 종류·현재 파라미터 값이 완전히 같은지.
    bool MatchesEditingSelection(FixtureEntry fixture)
    {
        if (editingFixtures.Count == 0) return true;
        var reference = editingFixtures[0];
        if (fixture.type != reference.type) return false;

        if (UsesRgbInput(fixture.type))
            return fixture.paramR == reference.paramR && fixture.paramG == reference.paramG && fixture.paramB == reference.paramB;

        if (fixture.type == FixtureType.StackChanger)
            return fixture.paramColorA == reference.paramColorA && fixture.paramColorB == reference.paramColorB;

        return fixture.paramColorA == reference.paramColorA; // RgbFilter, Bucket
    }

    void RefreshEditingVisual()
    {
        MarkEdited();

        for (int i = 0; i < editingFixtureInstances.Count; i++)
            if (editingFixtureInstances[i] != null)
                CustomStageLoader.ApplyParams(editingFixtureInstances[i], editingFixtures[i]);

        // 필터(ColorFilter/RgbFilter)는 같은 색끼리 묶인 병합 메시로 그려지고, 그 외형은 병합 그룹이
        // 다시 만들어질 때만 반영된다(FilterBlockBase.Configure 주석 참고) — 배치/제거 때와 동일하게 호출.
        if (editingFixtures.Count > 0 && IsFilter(editingFixtures[0].type)) FilterBlockBase.RebuildAll();
    }

    void EndEdit()
    {
        ResetEditingSelection();
        HideParamPanels();
        UpdateMarkHighlight();
    }

    // 값 수정 대상 선택을 비운다. editingFixtures와 editingFixtureInstances는 인덱스로 1:1 대응하므로
    // 항상 같이 지워야 한다 — 모드 전환·선택 시작 등 여러 곳에서 반복되던 2줄짜리 패턴을 모았다.
    void ResetEditingSelection()
    {
        editingFixtures.Clear();
        editingFixtureInstances.Clear();
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
        MarkEdited();
    }

    // 캔버스(FixtureType.Canvas)는 배치될 때마다 정답 순서가 하나씩 자동으로 생기므로, 무지개 7색
    // 마커 한도에 맞춰 최대 7개까지만 배치할 수 있다.
    int CanvasCount => data.fixtures.Count(f => f.type == FixtureType.Canvas);

    void PlaceFixtureAt(Vector3Int cell, FixtureType type, bool suppressFilterRebuild = false)
    {
        if (type == FixtureType.Canvas && CanvasCount >= 7) return;

        var preset = GetPreset(type);
        var entry = new FixtureEntry
        {
            id = nextFixtureId++,
            type = type,
            x = cell.x, y = cell.y, z = cell.z,
            paramR = preset.r, paramG = preset.g, paramB = preset.b,
            paramColorA = preset.colorClicks.Count > 0 ? (LightColor)preset.colorClicks[0] : default,
            paramColorB = preset.colorClicks.Count > 1 ? (LightColor)preset.colorClicks[1] : default,
        };

        var instance = CustomStageLoader.PlaceFixture(entry, prefabs, mapObjectsRoot);
        if (instance == null) return; // 팔레트 프리팹이 연결 안 돼 있으면 조용히 무시

        Register(cell, instance.gameObject, null, entry);
        data.fixtures.Add(entry);
        MarkEdited();

        if (type == FixtureType.Canvas)
        {
            data.canvasOrders.Add(new CanvasOrderEntry { canvasFixtureId = entry.id });
            RefreshCanvasList();
        }

        if (IsFilter(type) && !suppressFilterRebuild) FilterBlockBase.RebuildAll();
    }

    void Register(Vector3Int cell, GameObject go, BlockEntry block, FixtureEntry fixture)
    {
        cells[cell] = new PlacedCell { GameObject = go, Block = block, Fixture = fixture };
    }

    void RemoveCell(Vector3Int cell, bool suppressFilterRebuild = false)
    {
        if (!cells.TryGetValue(cell, out var placed)) return;
        cells.Remove(cell);

        if (placed.Block != null) data.blocks.Remove(placed.Block);
        if (placed.Fixture != null)
        {
            data.fixtures.Remove(placed.Fixture);
            // 필터는 순서에 여러 번 들어갈 수 있으므로(AddFixtureToOrder) 하나만 지우지 않고 전부 지운다.
            foreach (var order in data.canvasOrders) order.orderFixtureIds.RemoveAll(id => id == placed.Fixture.id);

            if (placed.Fixture.type == FixtureType.Canvas)
            {
                data.canvasOrders.RemoveAll(c => c.canvasFixtureId == placed.Fixture.id);
                if (selectedCanvasFixtureId == placed.Fixture.id) selectedCanvasFixtureId = -1;
                RefreshCanvasList();
            }
        }

        bool wasFilter = placed.Fixture != null && IsFilter(placed.Fixture.type);

        Object.Destroy(placed.GameObject);

        if (wasFilter && !suppressFilterRebuild) FilterBlockBase.RebuildAll();
        if (placed.Fixture != null) RefreshOrderListUI(); // 지운 기물이 순서 목록에 있었을 수도 있음
        MarkEdited();
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
        bool placeReady = IsPlaceReady(currentFixtureType); // RGBSelect류인데 색 선택이 안 끝났으면 드래그 설치도 안 함

        // 드래그 범위가 필터일 때 칸마다 FilterBlockBase.RebuildAll()(씬 전체 필터 메시 재생성)을
        // 부르면 칸 수만큼 반복 비용이 든다 — 개별 호출은 억제하고 드래그가 끝난 뒤 한 번만 부른다.
        bool touchedFilter = false;
        foreach (var center in GetDragCenters())
        {
            Vector3Int cell = ToCell(center);
            if (dragRemove)
            {
                if (cells.TryGetValue(cell, out var placed))
                {
                    if (placed.Fixture != null && IsFilter(placed.Fixture.type)) touchedFilter = true;
                    RemoveCell(cell, suppressFilterRebuild: true);
                }
            }
            else if (placeReady && !cells.ContainsKey(cell))
            {
                if (currentFixtureType == null) PlaceBlockAt(cell);
                else
                {
                    if (IsFilter(currentFixtureType.Value)) touchedFilter = true;
                    PlaceFixtureAt(cell, currentFixtureType.Value, suppressFilterRebuild: true);
                }
            }
        }

        if (touchedFilter) FilterBlockBase.RebuildAll();
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
        mode = EditorMode.Place;
        ResetEditingSelection();
        currentFixtureType = null;
        HideParamPanels();
        ClearMarks();
        UpdateObjectButtonHighlight(0);
    }

    /// <summary>값 수정 모드로 전환한다. 이 모드에서는 설치·제거·드래그 범위 설치가 전부 비활성화되고
    /// (카메라 이동은 영향 없음), 대신 값 수정 가능한 기물마다 뜨는 화면 마크를 눌러 파라미터를
    /// 편집한다.</summary>
    public void SelectEditTool()
    {
        mode = EditorMode.ValueEdit;
        currentFixtureType = null;
        HideParamPanels();
        RefreshMarks(type => type != FixtureType.ColorChanger, SelectFixtureForEdit);
    }

    /// <summary>CorrectOrder 패널의 "기물 추가" 버튼 OnClick에 연결 — 기물 리스트 추가 모드로 전환한다.
    /// 이 모드에서도 값 수정 모드와 마찬가지로 설치·제거가 비활성화되고 카메라 이동만 가능하며, 배치된
    /// 기물마다 뜨는 화면 마크를 눌러 지금 선택된 캔버스 카드의 정답 순서에 파라미터 패널 없이 바로
    /// 추가한다.</summary>
    public void EnterAddToOrderMode()
    {
        mode = EditorMode.AddToOrder;
        currentFixtureType = null;
        ResetEditingSelection();
        HideParamPanels();
        RefreshMarks(type => true, AddFixtureToOrder);
    }

    /// <summary>Mode1 버튼 OnClick — 배치 모드 탭을 보여준다.</summary>
    public void ShowPlaceTab()
    {
        SelectBlockTool();
        SetActivePanel(mode1Panel);
        UpdateModeTabHighlight(0);
    }

    /// <summary>Mode2 버튼 OnClick — 값 수정 모드 탭을 보여준다.</summary>
    public void ShowValueEditTab()
    {
        SelectEditTool();
        SetActivePanel(mode2Panel);
        UpdateModeTabHighlight(1);
    }

    /// <summary>Mode3 버튼 OnClick — 카메라 이동만 가능한 순서 열람 탭을 보여준다. 안쪽 "기물 추가"
    /// 버튼(EnterAddToOrderMode)을 눌러야 비로소 좌클릭으로 순서에 기물을 추가할 수 있다.</summary>
    public void ShowOrderTab()
    {
        mode = EditorMode.OrderView;
        ResetEditingSelection();
        HideParamPanels();
        ClearMarks();
        SetActivePanel(mode3Panel);
        UpdateModeTabHighlight(2);
    }

    /// <summary>Mode4 버튼 OnClick — 카메라 이동만 가능한 저장/불러오기 탭을 보여준다.</summary>
    public void ShowSaveLoadTab()
    {
        mode = EditorMode.SaveLoad;
        ResetEditingSelection();
        HideParamPanels();
        ClearMarks();
        SetActivePanel(mode4Panel);
        UpdateModeTabHighlight(3);
    }

    /// <summary>Mode5 버튼 OnClick — 카메라 이동만 가능한 플레이 테스트 탭을 보여준다. 안쪽 시작
    /// 버튼(StartPlayTest)을 눌러야 비로소 실제 플레이 테스트가 시작된다.</summary>
    public void ShowPlayTab()
    {
        mode = EditorMode.PlayView;
        ResetEditingSelection();
        HideParamPanels();
        ClearMarks();
        SetActivePanel(mode5Panel);
        UpdateModeTabHighlight(4);
    }

    void SetActivePanel(GameObject panel)
    {
        if (mode1Panel != null) mode1Panel.SetActive(panel == mode1Panel);
        if (mode2Panel != null) mode2Panel.SetActive(panel == mode2Panel);
        if (mode3Panel != null) mode3Panel.SetActive(panel == mode3Panel);
        if (mode4Panel != null) mode4Panel.SetActive(panel == mode4Panel);
        if (mode5Panel != null) mode5Panel.SetActive(panel == mode5Panel);
    }

    void UpdateModeTabHighlight(int index)
    {
        if (modeTabImages == null) return;
        for (int i = 0; i < modeTabImages.Length; i++)
            if (modeTabImages[i] != null)
                modeTabImages[i].color = (i == index) ? tabSelectedColor : tabNormalColor;
    }

    /// <summary>다음 클릭부터 지정한 기물을 설치하도록 전환한다. FixtureType의 int 값(순서)을 받는다
    /// (버튼 OnClick에서 정수 파라미터로 바로 연결 가능). 기물 종류에 맞는 파라미터 패널도 같이 연다.
    /// 이미 선택돼 있던 기물을 다시 선택하면(재선택) 패널 값을 초기화한다 — 다른 기물을 거쳐 돌아온
    /// 최초 복귀는 재선택이 아니므로 기존 값이 유지된다.</summary>
    public void SelectFixtureTool(int type)
    {
        mode = EditorMode.Place;
        ResetEditingSelection();
        var fixtureType = (FixtureType)type;
        bool reselecting = currentFixtureType.HasValue && currentFixtureType.Value == fixtureType;
        currentFixtureType = fixtureType;

        if (reselecting) ResetPanelValues(fixtureType);

        ApplyPlacePanelStates(fixtureType);
        UpdateObjectButtonHighlight(type + 1); // 0=ColorFilter→버튼(1) ... 6=StackChanger→버튼(7)
    }

    // ObjectButton(0=Block)~(7=StackChanger)과 동일한 인덱스로 그 기물의 인라인 값 입력 패널을 찾는다.
    // 파라미터가 필요 없는 기물(Block, ColorChanger)은 null.
    FixtureValuePanel PlacePanelFor(FixtureType? type)
    {
        if (!type.HasValue || fixtureValuePanels == null) return null;
        int index = (int)type.Value + 1;
        return index >= 0 && index < fixtureValuePanels.Length ? fixtureValuePanels[index] : null;
    }

    // active로 지정한 기물의 버튼만 높이 160 + 패널 활성으로 펼치고, 나머지는 전부 높이 80 + 패널
    // 비활성으로 접는다. VerticalLayoutGroup이 각 버튼의 현재 높이로 다른 버튼들의 위치를 자동 재배치
    // 하므로(겹침 없이), 높이를 바꾼 뒤 레이아웃을 강제로 즉시 재계산한다.
    void ApplyPlacePanelStates(FixtureType? active)
    {
        if (fixtureValuePanels != null)
            for (int i = 0; i < fixtureValuePanels.Length; i++)
            {
                var p = fixtureValuePanels[i];
                if (p == null) continue;
                bool expanded = active.HasValue && (int)active.Value + 1 == i;
                SetButtonExpanded(p, expanded, (FixtureType)(i - 1));
            }

        if (objectListContent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(objectListContent);
    }

    // 패널을 펼칠 때는 Valueinput 안의 RGBInput/RGBSelect 중 이 기물 종류에 맞는 쪽만 켠다.
    void SetButtonExpanded(FixtureValuePanel p, bool expanded, FixtureType type)
    {
        if (p.buttonRect != null)
        {
            var size = p.buttonRect.sizeDelta;
            size.y = expanded ? ExpandedButtonHeight : CollapsedButtonHeight;
            p.buttonRect.sizeDelta = size;
        }
        SetPanelActive(p.panel, expanded);
        if (expanded)
        {
            bool usesInput = UsesRgbInput(type);
            SetPanelActive(p.rgbInputPanel, usesInput);
            SetPanelActive(p.rgbSelectPanel, !usesInput);
            if (!usesInput)
            {
                // 이 기물 종류 전용 preset을 그대로 반영한다(지우지 않음) — 한 번도 안 건드렸으면
                // 원래 비어있어서 셋 다 연하게 보이고, 전에 이 종류에서 골라둔 색이 있으면 그대로 유지된다.
                UpdateColorButtonHighlight(p.colorButtonImages, GetPreset(type).colorClicks);
            }
        }
    }

    // 같은 기물을 다시 선택했을 때(재선택)만 패널을 빈 상태로 되돌린다.
    void ResetPanelValues(FixtureType type)
    {
        var panel = PlacePanelFor(type);
        if (panel == null) return;

        var preset = GetPreset(type);
        if (panel.rgbField != null)
        {
            preset.r = preset.g = preset.b = 0;
            panel.rgbField.text = "";
        }
        else
        {
            preset.colorClicks.Clear();
            UpdateColorButtonHighlight(panel.colorButtonImages, preset.colorClicks);
        }
    }

    void HideParamPanels()
    {
        ApplyPlacePanelStates(null);
        SetPanelActive(editRgbInputPanel, false);
        SetPanelActive(editRgbSelectPanel, false);
    }

    static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    bool IsParamPanelOpen =>
        (fixtureValuePanels != null && fixtureValuePanels.Any(p => p != null && p.panel != null && p.panel.activeSelf)) ||
        (editRgbInputPanel != null && editRgbInputPanel.activeSelf) || (editRgbSelectPanel != null && editRgbSelectPanel.activeSelf);

    // 우클릭(시점 회전, EditorFlyCamera) 중일 때만 커서를 잠근다 — 유니티 씬 뷰와 동일하게 그 외에는
    // 항상 커서가 보여야 Pan/Dolly나 RGBInput/RGBSelect 패널 조작이 가능하다.
    void UpdateCursorLock()
    {
        bool shouldLock = !IsParamPanelOpen && InputManager.Instance.ReadRotateHeld();
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    void UpdateObjectButtonHighlight(int index)
    {
        if (objectButtonImages == null) return;
        for (int i = 0; i < objectButtonImages.Length; i++)
            if (objectButtonImages[i] != null)
                objectButtonImages[i].color = (i == index) ? tabSelectedColor : tabNormalColor;
    }

    // --- 값 수정 모드 / 정답 순서 추가 모드 공용 화면 마크 ---
    // 두 모드 다 카메라 이동만 가능하고 월드 클릭 대신 화면 마크를 눌러 대상을 지정한다 — 마크가 켜져
    // 있는 동안 배치된 기물 목록은 안 바뀌므로(두 모드 다 설치·제거가 막혀 있음) 모드 진입 시 한 번만
    // 만들면 된다.

    readonly List<(Transform worldTransform, RectTransform marker, int fixtureId)> marks = new();

    [SerializeField] Color markNormalColor = new(1f, 0.85f, 0.1f, 0.9f);  // 기존 마크 기본색
    [SerializeField] Color markSelectedColor = new(0.2f, 1f, 0.4f, 1f);   // 값 수정 대상으로 선택된 마크

    // 모드 진입 시(SelectEditTool/EnterAddToOrderMode) 호출 — include(기물 타입)가 true인 기물마다
    // 클릭 가능한 화면 마크를 하나씩 만들고, 클릭하면 onClick(기물 id)을 호출한다.
    void RefreshMarks(System.Func<FixtureType, bool> include, UnityEngine.Events.UnityAction<int> onClick)
    {
        ClearMarks();
        if (editMarkTemplate == null || editMarkRoot == null) return;

        foreach (var placed in cells.Values)
        {
            if (placed.Fixture == null || !include(placed.Fixture.type)) continue;

            var marker = Object.Instantiate(editMarkTemplate, editMarkRoot);
            marker.gameObject.SetActive(true);
            int id = placed.Fixture.id;
            marker.onClick.AddListener(() => onClick(id));
            marks.Add((placed.GameObject.transform, marker.GetComponent<RectTransform>(), id));
        }

        UpdateMarkHighlight();
    }

    void ClearMarks()
    {
        foreach (var (_, marker, _) in marks)
            if (marker != null) Object.Destroy(marker.gameObject);
        marks.Clear();
    }

    // 값 수정 모드에서 지금 편집 중인(editingFixtures) 기물들의 마크만 다른 색으로 구분해서 보여준다.
    void UpdateMarkHighlight()
    {
        foreach (var (_, marker, fixtureId) in marks)
        {
            if (marker == null) continue;
            var img = marker.GetComponent<Image>();
            if (img == null) continue;
            img.color = editingFixtures.Any(f => f.id == fixtureId) ? markSelectedColor : markNormalColor;
        }
    }

    // 상하좌우전후 6방향(대각선 제외) 이웃 칸을 도는 오프셋 — Ctrl+클릭의 연쇄 선택에 사용.
    static readonly Vector3Int[] SixDirections =
    {
        Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
    };

    /// <summary>값 수정 모드의 화면 마크 클릭 시 호출.
    /// - 아무 키 없이 클릭: 그 기물 하나로 선택을 새로 시작.
    /// - Shift+클릭(이미 선택된 기물이 있을 때): 종류·현재 값이 완전히 같은 경우에만 그 기물 하나만
    ///   다중 선택에 추가(다르면 무시).
    /// - Ctrl+클릭: Shift와 같은 종류·값 일치 조건으로, 클릭한 기물과 상하좌우전후로 맞닿은 채 그
    ///   조건을 만족하는 기물들을 이웃의 이웃까지 연쇄적으로 전부 선택에 추가(BFS). 선택된 기물이
    ///   없는 상태에서 Ctrl+클릭하면 그 기물을 기준으로 새로 선택을 시작한 뒤 연쇄 확장한다.</summary>
    public void SelectFixtureForEdit(int fixtureId)
    {
        foreach (var kv in cells)
        {
            if (kv.Value.Fixture == null || kv.Value.Fixture.id != fixtureId) continue;

            bool ctrlHeld = InputManager.Instance.ReadRemoveModifierHeld();
            bool shiftHeld = InputManager.Instance.ReadRangeModifierHeld();
            bool hasSelection = editingFixtures.Count > 0;

            if (ctrlHeld)
            {
                if (!hasSelection) BeginEditFixture(kv.Value.Fixture, kv.Value.GameObject);
                else if (!MatchesEditingSelection(kv.Value.Fixture)) return;
                else AddToEditSelection(kv.Value.Fixture, kv.Value.GameObject);

                ExpandEditSelectionFrom(kv.Key);
            }
            else if (hasSelection && shiftHeld)
            {
                AddToEditSelection(kv.Value.Fixture, kv.Value.GameObject);
            }
            else
            {
                BeginEditFixture(kv.Value.Fixture, kv.Value.GameObject);
            }
            return;
        }
    }

    // startCell에서 시작해 상하좌우전후로 맞닿은 칸을 타고 나가며(BFS), 지금 선택 그룹과 종류·값이
    // 같은 기물(MatchesEditingSelection)을 만나는 대로 전부 선택에 추가한다 — 안 맞는 기물에서는
    // 그 방향으로 더 이상 퍼지지 않는다.
    void ExpandEditSelectionFrom(Vector3Int startCell)
    {
        var visited = new HashSet<Vector3Int> { startCell };
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(startCell);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var dir in SixDirections)
            {
                var next = cell + dir;
                if (!visited.Add(next)) continue;
                if (!cells.TryGetValue(next, out var placed) || placed.Fixture == null) continue;
                if (!MatchesEditingSelection(placed.Fixture)) continue;
                if (editingFixtures.Any(f => f.id == placed.Fixture.id)) continue;

                editingFixtures.Add(placed.Fixture);
                editingFixtureInstances.Add(placed.GameObject.GetComponent<MapObjectBase>());
                queue.Enqueue(next);
            }
        }

        UpdateMarkHighlight();
    }

    /// <summary>정답 순서 추가 모드의 화면 마크 클릭 시 호출 — 해당 기물을 지금 선택된 캔버스 카드의
    /// 정답 순서 끝에 추가한다(파라미터 패널 없이). 선택된 캔버스가 없으면 아무 일도 하지 않는다.
    /// 필터(ColorFilter/RgbFilter)는 같은 기물을 여러 번 통과하는 구성이 가능하도록 중복 추가를
    /// 허용하고, 그 외 기물은 이미 순서에 있으면 다시 추가하지 않는다.</summary>
    public void AddFixtureToOrder(int fixtureId)
    {
        var order = SelectedOrder;
        if (order == null) return; // 캔버스 카드를 먼저 선택해야 함

        var fixture = data.fixtures.Find(f => f.id == fixtureId);
        bool allowDuplicate = fixture != null && IsFilter(fixture.type);

        if (allowDuplicate || !order.orderFixtureIds.Contains(fixtureId))
        {
            order.orderFixtureIds.Add(fixtureId);
            RefreshOrderListUI();
        }
    }

    void LateUpdate()
    {
        if (mode != EditorMode.ValueEdit && mode != EditorMode.AddToOrder) return;
        var cam = Camera.main;
        if (cam == null) return;

        foreach (var (worldTransform, marker, _) in marks)
        {
            if (worldTransform == null || marker == null) continue;
            Vector3 screenPos = cam.WorldToScreenPoint(worldTransform.position);
            bool visible = screenPos.z > 0f
                && screenPos.x >= 0f && screenPos.x <= Screen.width
                && screenPos.y >= 0f && screenPos.y <= Screen.height;
            marker.gameObject.SetActive(visible);
            if (visible) marker.position = screenPos;
        }
    }

    // --- 파라미터 입력 UI 연결용 ---

    /// <summary>맵 제목을 지정한다.</summary>
    public void SetTitle(string title) => data.title = title;

    // 편집 중(editingFixtures가 비어있지 않음)이면 preset이 아니라 선택된 기물들 전부에 바로 쓰고 즉시
    // 시각 갱신, 아니면 지금 선택된 기물 종류(currentFixtureType) 전용 preset 필드에 쓴다.
    public void SetPresetR(string value)
    {
        if (!int.TryParse(value, out int v)) return;
        if (editingFixtures.Count > 0) { foreach (var f in editingFixtures) f.paramR = v; RefreshEditingVisual(); }
        else if (currentFixtureType.HasValue) GetPreset(currentFixtureType.Value).r = v;
    }

    public void SetPresetG(string value)
    {
        if (!int.TryParse(value, out int v)) return;
        if (editingFixtures.Count > 0) { foreach (var f in editingFixtures) f.paramG = v; RefreshEditingVisual(); }
        else if (currentFixtureType.HasValue) GetPreset(currentFixtureType.Value).g = v;
    }

    public void SetPresetB(string value)
    {
        if (!int.TryParse(value, out int v)) return;
        if (editingFixtures.Count > 0) { foreach (var f in editingFixtures) f.paramB = v; RefreshEditingVisual(); }
        else if (currentFixtureType.HasValue) GetPreset(currentFixtureType.Value).b = v;
    }

    /// <summary>RGBInput 입력창("RRGGBB" 6자리, 2자리씩 R/G/B)의 OnValueChanged에 연결.</summary>
    public void SetPresetRGB(string value)
    {
        if (value.Length < 6) return;
        SetPresetR(value.Substring(0, 2));
        SetPresetG(value.Substring(2, 2));
        SetPresetB(value.Substring(4, 2));
    }

    /// <summary>RGBSelect의 Red/Green/Blue 버튼(Toggle이 아닌 일반 Button, Confirm 없이 클릭 즉시 반영)
    /// OnClick에 0/1/2로 연결. 배치 모드·값 수정 모드 공용. 값 수정 모드는 그 기물 필드에 바로 반영하고,
    /// 배치 모드는 지금 선택된 기물 종류 전용 preset(다른 종류와 절대 안 섞임)에 반영한다. StackChanger는
    /// 색 2개(A/B)가 필요하므로 최근 클릭한 서로 다른 색 2개가 모여야 반영되고, 그 외(RgbFilter/Bucket)는
    /// 클릭한 색 1개가 바로 반영된다. 이미 선택된 색을 다시 클릭하면 무시한다(중복 선택 방지).</summary>
    public void ClickColorButton(int colorIndex)
    {
        FixtureType? type = editingFixtures.Count > 0 ? editingFixtures[0].type : currentFixtureType;
        if (!type.HasValue) return;

        if (editingFixtures.Count > 0)
        {
            if (!RegisterColorClick(editColorClicks, colorIndex, type.Value)) return;

            if (type.Value == FixtureType.StackChanger)
            {
                if (editColorClicks.Count == 2)
                {
                    foreach (var f in editingFixtures)
                    {
                        f.paramColorA = (LightColor)editColorClicks[0];
                        f.paramColorB = (LightColor)editColorClicks[1];
                    }
                }
            }
            else
            {
                foreach (var f in editingFixtures) f.paramColorA = (LightColor)colorIndex;
            }

            UpdateColorButtonHighlight(editColorButtonImages, editColorClicks);
            RefreshEditingVisual();
        }
        else
        {
            var preset = GetPreset(type.Value);
            if (!RegisterColorClick(preset.colorClicks, colorIndex, type.Value)) return;
            UpdateColorButtonHighlight(PlacePanelFor(type)?.colorButtonImages, preset.colorClicks);
        }
    }

    // --- 캔버스별 정답 순서 패널 연결용 ---

    /// <summary>지금 OrderList에 표시 중인 캔버스의 정답 순서. 선택된 캔버스가 없으면 null.</summary>
    CanvasOrderEntry SelectedOrder => data.canvasOrders.Find(c => c.canvasFixtureId == selectedCanvasFixtureId);

    readonly List<CanvasCardUI> canvasCardInstances = new();

    /// <summary>CanvasList의 캔버스 카드 클릭 시 호출 — 그 캔버스의 정답 순서를 OrderList에 표시한다.</summary>
    public void SelectCanvasOrder(int canvasFixtureId)
    {
        selectedCanvasFixtureId = canvasFixtureId;
        UpdateCanvasCardHighlight();
        RefreshOrderListUI();
    }

    // CanvasList를 지금 배치된 캔버스 개수(data.canvasOrders)에 맞춰 다시 그린다 — OrderList와 동일한
    // "숨긴 템플릿 복제" 패턴.
    void RefreshCanvasList()
    {
        if (canvasListContent == null || canvasCardTemplate == null) return;

        foreach (var c in canvasCardInstances) Object.Destroy(c.gameObject);
        canvasCardInstances.Clear();

        for (int i = 0; i < data.canvasOrders.Count; i++)
        {
            int canvasId = data.canvasOrders[i].canvasFixtureId;
            var card = Object.Instantiate(canvasCardTemplate, canvasListContent);
            card.gameObject.SetActive(true);
            card.NumberText.text = (i + 1).ToString();
            card.SelectButton.onClick.AddListener(() => SelectCanvasOrder(canvasId));
            canvasCardInstances.Add(card);
        }

        UpdateCanvasCardHighlight();
    }

    void UpdateCanvasCardHighlight()
    {
        for (int i = 0; i < canvasCardInstances.Count; i++)
            canvasCardInstances[i].CardImage.color =
                data.canvasOrders[i].canvasFixtureId == selectedCanvasFixtureId ? tabSelectedColor : tabNormalColor;
    }

    void RefreshOrderListUI()
    {
        if (orderListContent == null || orderItemTemplate == null) return;

        for (int i = orderListContent.childCount - 1; i >= 0; i--)
        {
            var child = orderListContent.GetChild(i);
            // 항목 템플릿과 "기물 추가" 버튼(OrderList 안에 항상 고정으로 같이 있음)은 갱신 대상이 아님.
            if (child == orderItemTemplate.transform) continue;
            if (addToOrderButton != null && child == addToOrderButton.transform) continue;
            Object.Destroy(child.gameObject);
        }

        var order = SelectedOrder;
        if (order == null) return; // 선택된 캔버스가 없으면 빈 목록

        for (int i = 0; i < order.orderFixtureIds.Count; i++)
        {
            int id = order.orderFixtureIds[i];
            var fixture = data.fixtures.Find(f => f.id == id);
            if (fixture == null) continue; // 방어적 처리(있을 수 없는 상태지만)

            var item = Object.Instantiate(orderItemTemplate, orderListContent);
            item.gameObject.SetActive(true);
            int index = i; // 클로저 캡처용
            item.OrderNumberText.text = $"{i + 1}";
            item.ObjectNameText.text = $"{fixture.type} ({fixture.x},{fixture.y},{fixture.z})";
            item.RemoveButton.onClick.AddListener(() => { SelectedOrder.orderFixtureIds.RemoveAt(index); RefreshOrderListUI(); });
            item.UpButton.onClick.AddListener(() =>
            {
                var ids = SelectedOrder.orderFixtureIds;
                if (index > 0) { (ids[index], ids[index - 1]) = (ids[index - 1], ids[index]); RefreshOrderListUI(); }
            });
            item.DownButton.onClick.AddListener(() =>
            {
                var ids = SelectedOrder.orderFixtureIds;
                if (index < ids.Count - 1) { (ids[index], ids[index + 1]) = (ids[index + 1], ids[index]); RefreshOrderListUI(); }
            });
        }

        // "기물 추가" 버튼은 항목이 몇 개든 항상 목록 가장 아래칸에 있어야 한다.
        if (addToOrderButton != null) addToOrderButton.transform.SetAsLastSibling();
    }

    // --- 저장/불러오기 UI 연결용 ---

    [SerializeField] GameObject saveNamePopup;
    [SerializeField] TMP_InputField saveNameInputField;
    [SerializeField] GameObject loadListPopup;
    [SerializeField] Transform loadListContent;
    [SerializeField] MyMapCardUI myMapCardTemplate;

    readonly List<MyMapCardUI> myMapCardInstances = new();

    // 저장 이름 팝업(SaveNamePopup)을 확인/취소했을 때 어떤 흐름으로 온 것인지 — 예전엔 bool 3개
    // (pendingSaveAsNew/pendingNewMap/pendingExitAfterSave)를 따로 두고 매번 서로를 false로 리셋해야
    // 했는데, 그중 하나를 리셋하는 걸 빠뜨리면 두 흐름이 동시에 true가 되는 상태가 가능했다.
    // enum 하나로 합쳐 항상 정확히 하나의 의도만 갖도록 타입으로 보장한다.
    enum SaveIntent { None, SaveAsNew, NewMap, ExitAfterSave }
    SaveIntent saveIntent = SaveIntent.None;
    const string SaveFilePrefix = "CustomMap_";
    static string SaveFileName(string id) => $"{SaveFilePrefix}{id}.json";

    /// <summary>SaveButton OnClick. 이미 이름이 있는(=처음 저장이 아닌) 맵이면 팝업 없이 바로 지금
    /// 파일에 덮어쓴다. 아직 이름이 없으면(첫 저장) 이름을 받아야 하니 팝업을 띄운다.</summary>
    public void OpenSavePopup()
    {
        saveIntent = SaveIntent.None;
        if (!string.IsNullOrEmpty(data.title)) { SaveCurrentMap(); return; }
        OpenSaveNamePopup();
    }

    /// <summary>SaveAsButton OnClick. 이미 이름이 있어도 새 이름을 받아야 하므로 항상 팝업을 띄운다.</summary>
    public void OpenSaveAsPopup()
    {
        saveIntent = SaveIntent.SaveAsNew;
        OpenSaveNamePopup();
    }

    /// <summary>SaveAndExitButton OnClick. 저장한 뒤(이름이 없으면 먼저 이름을 받고) 메인메뉴로
    /// 나간다 — 맵 에디터에서 나가는 유일한 경로(일시정지 메뉴는 더 이상 뜨지 않음).</summary>
    public void OnSaveAndExitButton()
    {
        saveIntent = SaveIntent.ExitAfterSave;
        if (!string.IsNullOrEmpty(data.title))
        {
            SaveCurrentMap();
            saveIntent = SaveIntent.None;
            ExitToMainMenu();
            return;
        }
        OpenSaveNamePopup();
    }

    /// <summary>맵 선택 화면의 NewMap 카드 OnClick — 새 맵 이름을 입력받는다. 확인해도 그 자리에서
    /// 파일을 저장하지 않고 이름만 기억한 채 편집 화면으로 들어간다(저장은 사용자가 나중에 직접
    /// "저장"을 눌러야 이뤄진다).</summary>
    public void OnNewMapButtonSelected()
    {
        saveIntent = SaveIntent.NewMap;
        CloseLoadPopup(); // 이름 입력 화면으로 넘어가는 동안 맵 선택 화면은 보이지 않게 한다
        OpenSaveNamePopup();
    }

    void OpenSaveNamePopup()
    {
        if (saveNameInputField != null) saveNameInputField.text = data.title;
        if (saveNamePopup != null) saveNamePopup.SetActive(true);
    }

    /// <summary>SaveNamePopup의 확인 버튼 OnClick.</summary>
    public void ConfirmSaveName()
    {
        string title = saveNameInputField != null ? saveNameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(title)) return; // 빈 제목이면 저장하지 않고 팝업 유지

        SetTitle(title);

        if (saveIntent == SaveIntent.NewMap)
        {
            saveIntent = SaveIntent.None;
            CancelSavePopup();
            ShowEditorUI();
            return;
        }

        if (saveIntent == SaveIntent.SaveAsNew) data.id = System.Guid.NewGuid().ToString();
        bool exitAfterSave = saveIntent == SaveIntent.ExitAfterSave;

        SaveCurrentMap();
        CancelSavePopup();

        if (exitAfterSave) ExitToMainMenu();
    }

    void SaveCurrentMap() => SaveManager.Instance.SaveJson(SaveFileName(data.id), data);

    /// <summary>SaveNamePopup의 취소 버튼 OnClick.</summary>
    public void CancelSavePopup()
    {
        if (saveNamePopup != null) saveNamePopup.SetActive(false);

        // New Map 이름 입력을 취소한 경우, 맵 선택 화면을 닫아둔 채였으므로 다시 열어준다.
        if (saveIntent == SaveIntent.NewMap) OpenLoadPopup();
        saveIntent = SaveIntent.None;
    }

    /// <summary>맵 선택 화면(저장된 맵 목록 + New Map)을 연다 — 씬 진입 시 Awake에서 호출된다.</summary>
    public void OpenLoadPopup()
    {
        RefreshLoadList();
        if (loadListPopup != null) loadListPopup.SetActive(true);
    }

    /// <summary>LoadListPopup의 닫기 버튼 OnClick.</summary>
    public void CloseLoadPopup()
    {
        if (loadListPopup != null) loadListPopup.SetActive(false);
    }

    // CanvasList/OrderList와 동일한 "숨긴 템플릿 복제" 패턴 — persistentDataPath에서 CustomMap_*.json을
    // 전부 열거해 카드로 보여준다. 별도 인덱스 파일 없이 매번 직접 읽는다(맵 개수가 많지 않을 전제).
    void RefreshLoadList()
    {
        if (loadListContent == null || myMapCardTemplate == null) return;

        foreach (var c in myMapCardInstances) Object.Destroy(c.gameObject);
        myMapCardInstances.Clear();

        foreach (var path in Directory.GetFiles(Application.persistentDataPath, $"{SaveFilePrefix}*.json"))
        {
            var loaded = SaveManager.Instance.LoadJson<CustomStageData>(Path.GetFileName(path));
            if (string.IsNullOrEmpty(loaded.id)) continue; // 손상된 파일 방어(LoadJson 실패 시 new CustomStageData())

            var card = Object.Instantiate(myMapCardTemplate, loadListContent);
            card.gameObject.SetActive(true);
            card.TitleText.text = string.IsNullOrEmpty(loaded.title) ? "(제목 없음)" : loaded.title;
            card.SelectButton.onClick.AddListener(() => LoadMap(loaded));
            myMapCardInstances.Add(card);
        }
    }

    // 선택한 저장 데이터로 현재 편집 상태를 완전히 교체한다. data는 같은 인스턴스를 유지한 채
    // 내용만 갈아끼운다 — Data 프로퍼티로 이 인스턴스를 들고 있을 수 있는 외부 코드가 있어도 참조가
    // 깨지지 않는다.
    void LoadMap(CustomStageData loaded)
    {
        foreach (var placed in cells.Values) Object.Destroy(placed.GameObject);
        cells.Clear();

        data.blocks.Clear();
        data.fixtures.Clear();
        data.canvasOrders.Clear();
        ResetEditingSelection();
        selectedCanvasFixtureId = -1;

        data.id = loaded.id;
        data.title = loaded.title;
        data.clearVerified = loaded.clearVerified;

        foreach (var block in loaded.blocks)
        {
            var entry = new BlockEntry { x = block.x, y = block.y, z = block.z };
            var go = CustomStageLoader.PlaceBlock(entry, prefabs, mazeRoot);
            Register(new Vector3Int(entry.x, entry.y, entry.z), go, entry, null);
            data.blocks.Add(entry);
        }

        int maxFixtureId = 0;
        foreach (var fixture in loaded.fixtures)
        {
            var entry = new FixtureEntry
            {
                id = fixture.id, type = fixture.type, x = fixture.x, y = fixture.y, z = fixture.z,
                paramR = fixture.paramR, paramG = fixture.paramG, paramB = fixture.paramB,
                paramColorA = fixture.paramColorA, paramColorB = fixture.paramColorB,
            };
            var instance = CustomStageLoader.PlaceFixture(entry, prefabs, mapObjectsRoot);
            if (instance == null) continue; // 팔레트 프리팹 누락 시 조용히 무시(PlaceFixtureAt과 동일)
            Register(new Vector3Int(entry.x, entry.y, entry.z), instance.gameObject, null, entry);
            data.fixtures.Add(entry);
            maxFixtureId = Mathf.Max(maxFixtureId, entry.id);
        }
        nextFixtureId = maxFixtureId + 1;

        foreach (var order in loaded.canvasOrders)
            data.canvasOrders.Add(new CanvasOrderEntry
            {
                canvasFixtureId = order.canvasFixtureId,
                orderFixtureIds = new List<int>(order.orderFixtureIds), // 별칭 방지용 깊은 복사
            });

        FilterBlockBase.RebuildAll(); // CustomStageLoader.Load()도 무조건 호출 — 필터 0개여도 안전

        RefreshCanvasList();
        RefreshOrderListUI();
        CloseLoadPopup();
        ShowEditorUI();
    }

    /// <summary>맵 선택 화면(LoadListPopup)을 닫고 편집 화면을 보여준다 — 기존 맵을 불러왔을 때와
    /// New Map으로 새 맵을 만들었을 때 둘 다 이 지점으로 합류한다.</summary>
    void ShowEditorUI()
    {
        if (selectionPanel != null) selectionPanel.SetActive(true);
        ShowPlaceTab();
    }

    /// <summary>맵 선택 화면(LoadListPopup)의 Back 버튼 OnClick — 편집으로 안 들어가고 메인메뉴로
    /// 나간다(아직 편집할 맵을 고르지 않은 상태이므로 "취소"가 아니라 "나가기"가 맞다).</summary>
    public void OnMapSelectBackButton() => ExitToMainMenu();

    void ExitToMainMenu()
    {
        GameAudio.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        GameManager.Instance.ChangeState(GameState.MainMenu);
        SceneLoader.Instance.Load("MainMenu");
    }

    // --- 플레이 테스트 UI 연결용 ---

    [SerializeField] GameObject selectionPanel;   // UICanvas/SelectionPanel 전체 — 테스트 중엔 통째로 숨김
    [SerializeField] GameObject playTestOverlay;  // UICanvas 바로 밑, 테스트 중에만 보이는 "돌아가기" 배너

    bool isPlayTesting;
    MazeGenerator playTestMaze; // 첫 테스트 때 한 번만 추가, 이후 재사용
    static readonly Vector3 PlayerSpawnPosition = new(0f, 0.5f, 0f);

    // 정답 순서 자동 기록용 — 테스트 중 상호작용한 기물 id를 순서대로 쌓다가, 캔버스가 클리어되면
    // 그때까지 쌓인 걸 그 캔버스 순서로 확정하고 비운다(다음 캔버스는 그 시점부터 새로 기록).
    readonly List<int> playTestUsedSequence = new();
    Dictionary<MapObjectBase, int> playTestFixtureIdByInstance;

    void OnEnable()
    {
        EventBus.Subscribe<StageCleared>(OnStageClearedDuringPlayTest);
        EventBus.Subscribe<MapObjectUsed>(OnMapObjectUsedDuringPlayTest);
        EventBus.Subscribe<CanvasCompleted>(OnCanvasCompletedDuringPlayTest);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<StageCleared>(OnStageClearedDuringPlayTest);
        EventBus.Unsubscribe<MapObjectUsed>(OnMapObjectUsedDuringPlayTest);
        EventBus.Unsubscribe<CanvasCompleted>(OnCanvasCompletedDuringPlayTest);
    }

    /// <summary>PlayModePanel의 PlayButton OnClick — 지금 편집 중인 맵을 그 자리에서 플레이 테스트한다.
    /// 새로 Instantiate하지 않고 이미 배치돼 있는 오브젝트를 그대로 재생 가능한 상태로 전환한다.</summary>
    public void StartPlayTest()
    {
        if (isPlayTesting) return;

        Time.timeScale = 1f; // 직전 클리어 화면 등으로 멈춰 있었을 수 있으니 항상 명시적으로 복구.

        ResetEditingSelection();
        HideParamPanels();
        ClearMarks();
        HideHoverPreview();
        ClearDragPreview();
        dragging = false;

        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (playTestOverlay != null) playTestOverlay.SetActive(true);

        var fpc = FirstPersonController.Instance;
        if (fpc != null)
        {
            fpc.Teleport(PlayerSpawnPosition, Quaternion.identity);
            var flyCam = fpc.GetComponent<EditorFlyCamera>();
            if (flyCam != null) flyCam.enabled = false;
            fpc.enabled = true;
            var interact = fpc.GetComponent<InteractionController>();
            if (interact != null) interact.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ColorStacks.Instance?.ResetAll();

        if (playTestMaze == null) playTestMaze = gameObject.AddComponent<MazeGenerator>();
        var byId = new Dictionary<int, MapObjectBase>();
        playTestFixtureIdByInstance = new Dictionary<MapObjectBase, int>();
        playTestUsedSequence.Clear();
        foreach (var placed in cells.Values)
            if (placed.Fixture != null)
            {
                var obj = placed.GameObject.GetComponent<MapObjectBase>();
                if (obj == null) continue;
                byId[placed.Fixture.id] = obj;
                playTestFixtureIdByInstance[obj] = placed.Fixture.id;
                // 씬을 다시 로드하지 않고 같은 오브젝트를 재사용하므로, 이전 테스트에서 완료된 캔버스가
                // 이번 테스트에도 이미 완료된 채로 남아있지 않도록 매번 초기화한다.
                if (obj is ClearObjectBase clearObj) clearObj.ResetCompletion();
            }

        playTestMaze.correctOrders.Clear();
        foreach (var order in data.canvasOrders)
        {
            var list = new List<MapObjectBase>();
            foreach (var id in order.orderFixtureIds)
                if (byId.TryGetValue(id, out var obj)) list.Add(obj);
            playTestMaze.correctOrders.Add(list);
        }

        FilterBlockBase.RebuildAll();
        EventBus.Publish(new SceneLoadCompleted { SceneName = SceneManager.GetActiveScene().name });

        isPlayTesting = true;
        // 일반 Playing이 아니라 별도 상태를 쓴다 — GameManager.StageClear()의 "Playing일 때만" 가드에
        // 걸려 실제 클리어(GameState.Cleared) 전환 자체가 막히므로, ClearScreenController 등 클리어에
        // 반응하는 쪽에서 별도로 플레이 테스트 여부를 확인할 필요가 없다.
        GameManager.Instance.ChangeState(GameState.MapEditorPlayTest);
    }

    /// <summary>PlayTestOverlay의 "에디터로 돌아가기" OnClick, 그리고 테스트 중 클리어 감지 시 자동 호출.</summary>
    public void StopPlayTest()
    {
        if (!isPlayTesting) return;

        Time.timeScale = 1f;
        GameManager.Instance.ChangeState(GameState.MapEditor);

        var fpc = FirstPersonController.Instance;
        if (fpc != null)
        {
            fpc.enabled = false;
            var interact = fpc.GetComponent<InteractionController>();
            if (interact != null) interact.enabled = false;
            var flyCam = fpc.GetComponent<EditorFlyCamera>();
            if (flyCam != null) flyCam.enabled = true;
        }

        ColorStacks.Instance?.ResetAll();
        RestoreConsumedFixtures();

        if (playTestOverlay != null) playTestOverlay.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);

        playTestUsedSequence.Clear();
        playTestFixtureIdByInstance = null;
        isPlayTesting = false;
    }

    // 버킷/팔레트/컬러 체인저/스택 체인저 같은 소모성 기물은 실제로 사용되면
    // ConsumableObjectBase.Consume()이 오브젝트를 완전히 Destroy()해버린다(캔버스처럼 완료 상태만
    // 잠그는 게 아니라 진짜로 사라짐). data.fixtures엔 그대로 남아있으니, 에디터로 돌아올 때 파괴된
    // 인스턴스만 골라 CustomStageLoader.PlaceFixture로 다시 만들어 원상 복구한다 — 저장 안 한 편집
    // 내용이 플레이 테스트 한 번으로 사라지면 안 되기 때문("클리어 후 에디터로 복귀시 사라진 기물이
    // 안 돌아온다" 버그로 발견됨).
    void RestoreConsumedFixtures()
    {
        foreach (var placed in cells.Values)
        {
            if (placed.Fixture == null || placed.GameObject != null) continue;
            var instance = CustomStageLoader.PlaceFixture(placed.Fixture, prefabs, mapObjectsRoot);
            if (instance != null) placed.GameObject = instance.gameObject;
        }
    }

    // 테스트 중 기물을 상호작용할 때마다 호출(필터 통과, 획득, 소모, 캔버스 완료 등 전부 포함).
    // 캔버스 자신의 완료는 "재료"가 아니라 목표이므로 순서 기록에서 제외한다.
    void OnMapObjectUsedDuringPlayTest(MapObjectUsed e)
    {
        if (!isPlayTesting || e.Source is ClearObjectBase) return;
        if (playTestFixtureIdByInstance != null && playTestFixtureIdByInstance.TryGetValue(e.Source, out int id))
            playTestUsedSequence.Add(id);
    }

    // 캔버스 하나가 클리어되면 그때까지 쌓인 상호작용 순서를 그 캔버스의 정답 순서로 확정(덮어쓰기)하고
    // 다음 캔버스를 위해 비운다 — 이렇게 실제 플레이로 검증된 순서가 수동 입력 없이 자동으로 저장된다.
    void OnCanvasCompletedDuringPlayTest(CanvasCompleted e)
    {
        if (!isPlayTesting) return;
        if (playTestFixtureIdByInstance == null || !playTestFixtureIdByInstance.TryGetValue(e.Source, out int canvasId)) return;

        var order = data.canvasOrders.Find(o => o.canvasFixtureId == canvasId);
        if (order != null)
        {
            order.orderFixtureIds.Clear();
            order.orderFixtureIds.AddRange(playTestUsedSequence);
        }
        playTestUsedSequence.Clear();

        RefreshOrderListUI(); // 지금 이 캔버스가 OrderList에 선택돼 있으면 자동 기록 결과가 바로 보이게
    }

    // 테스트 중 실제로 정답 순서를 전부 맞춰 클리어하면(LevelManager가 StageCleared를 발행하고 곧이어
    // GameManager.StageClear()를 호출), 진짜 클리어 화면 대신 자동으로 에디터로 돌아간다.
    // 플레이 테스트는 Playing이 아니라 MapEditorPlayTest 상태라 StageClear()의 가드에 걸려 실제
    // GameState.Cleared 전환 자체가 안 일어나고, ClearScreenController도 State로 판단해 클리어
    // 화면을 안 띄우므로, 여기서는 상태 복구만 하면 된다. 클리어까지 확인됐다는 표시로
    // data.clearVerified도 true로 세팅.
    void OnStageClearedDuringPlayTest(StageCleared e)
    {
        if (!isPlayTesting) return;
        data.clearVerified = true;
        StopPlayTest();
    }

    void MarkEdited() => data.clearVerified = false;
}

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
public partial class MapEditController : MonoBehaviour
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

    // ObjectButton(0=Block)~(7=StackChanger)과 동일한 인덱스. 파라미터가 필요 없는 기물(Block,
    // ColorChanger)은 배열 항목의 panel을 비워둔다. 평소엔 버튼 높이 80에 패널 비활성 상태이다가, 그
    // 버튼이 선택된 동안에만 높이 160으로 커지면서 패널이 활성화된다.
    [SerializeField] FixtureValuePanel[] fixtureValuePanels;
    [SerializeField] RectTransform objectListContent; // mode1Panel/ObjectList/Viewport/Content — 높이 변경 후 레이아웃 강제 재계산용

    [SerializeField] Color tabNormalColor = Color.white;
    [SerializeField] Color tabSelectedColor = Color.yellow;

    [SerializeField] GameObject mode1Panel, mode2Panel, mode4Panel, mode5Panel;
    [SerializeField] Image[] modeTabImages;      // Mode1, Mode2, (빈 자리), Mode4, Mode5 순서 — 정답 순서 패널(Mode3) 제거로 가운데 한 칸은 비워둠
    [SerializeField] Image[] objectButtonImages; // ObjectButton, (1)..(7) 순서 — 기물 팔레트 하이라이트용

    // mode2Panel 전용 값 수정 파라미터 패널 — mode1Panel의 fixtureValuePanels와는 별개 오브젝트
    [SerializeField] GameObject editRgbInputPanel;
    [SerializeField] GameObject editRgbSelectPanel;
    [SerializeField] TMP_InputField editRgbInputField;
    [SerializeField] Image[] editColorButtonImages; // editRgbSelectPanel 안의 Red/Green/Blue Image

    // 값 수정 모드에서 기물마다 뜨는 클릭 가능한 화면 마크
    [SerializeField] Button editMarkTemplate;    // UICanvas 밑에 항상 비활성 상태로 두는 템플릿
    [SerializeField] RectTransform editMarkRoot; // 화면 전체 크기 컨테이너

    readonly CustomStageData data = new();
    int nextFixtureId = 1;

    TabController tabController;
    SaveLoadController saveLoadController;
    PlayTestController playTestController;
    PlacementController placementController;
    FixtureEditController fixtureEditController;

    // 배치/제거(Place)·값 수정(ValueEdit)·저장/불러오기(SaveLoad)·플레이 테스트(PlayView)는 절대
    // 중첩되지 않고 항상 정확히 1개만 활성화된다 — 이후 모드가 늘어나도 이 enum에 추가하는 방식으로
    // 원칙을 유지한다.
    enum EditorMode { Place, ValueEdit, SaveLoad, PlayView }
    EditorMode mode = EditorMode.Place;

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
        tabController = new TabController(this);
        saveLoadController = new SaveLoadController(this);
        playTestController = new PlayTestController(this);
        placementController = new PlacementController(this);
        fixtureEditController = new FixtureEditController(this);

        GameManager.Instance.ChangeState(GameState.MapEditor);

        data.id = System.Guid.NewGuid().ToString();

        mazeRoot = new GameObject("Maze").transform;
        mazeRoot.SetParent(transform, false);

        mapObjectsRoot = new GameObject("MapObjects").transform;
        mapObjectsRoot.SetParent(transform, false);

        placementController.RegisterPreplacedBlocks();

        // 편집 화면 대신 맵 선택 화면(LoadListPopup)을 맨 처음에 띄운다 — 기존 맵을 고르거나
        // New Map으로 새로 만들어야 비로소 편집 화면(ShowEditorUI)으로 넘어간다.
        if (selectionPanel != null) selectionPanel.SetActive(false);
        saveLoadController.OpenLoadPopup();
    }

    void Update()
    {
        // 플레이 테스트 중엔 에디터의 모든 프레임 로직을 완전히 건너뛴다 — 플레이어 조작은
        // FirstPersonController가 알아서 처리한다. ESC는 일시정지 메뉴를 거치지 않고 곧장
        // 에디터로 돌아간다(테스트 중엔 마우스 커서가 잠겨 있어 PlayTestOverlay의 버튼을 직접
        // 클릭할 수 없으므로 — "Back to Editor 버튼이 무반응이다" 버그로 발견됨).
        if (playTestController.IsPlayTesting)
        {
            if (InputManager.Instance.ReadPause()) playTestController.StopPlayTest();
            return;
        }

        // 일시정지 중에는 Time.timeScale이 0이어도 Update()는 계속 돌기 때문에, 기물 선택·배치·제거
        // 등 편집 입력을 전부 막고 커서 관리도 PauseMenuController에 맡긴다.
        if (GameManager.Instance.State == GameState.Paused) { placementController.HideHoverPreview(); return; }

        UpdateCursorLock(); // 우클릭(시점 회전) 상태가 매 프레임 바뀌므로 매 프레임 갱신

        // 스페이스바 = 값 입력 패널을 닫는다. 값 수정 모드는 편집 대상 선택까지 해제(EndEdit)하고,
        // 배치 모드는 선택된 기물 종류는 유지한 채 패널만 접는다(연속 배치를 막지 않기 위해).
        if (InputManager.Instance.ReadConfirm())
        {
            if (fixtureEditController.HasEditSelection) fixtureEditController.EndEdit();
            else if (fixtureEditController.IsParamPanelOpen) fixtureEditController.HideParamPanels();
        }

        // 팔레트/입력창 등 UI를 클릭한 것까지 월드 배치로 새지 않게 막는다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            placementController.HideHoverPreview();
            return;
        }

        switch (mode)
        {
            case EditorMode.ValueEdit:
            case EditorMode.SaveLoad:
            case EditorMode.PlayView:
                placementController.HideHoverPreview(); // 카메라 이동만, 월드 좌클릭으로 하는 일 없음(대상은 전부 화면 마크로 지정)
                return; // 설치·제거·드래그 범위는 전부 비활성
        }

        placementController.HandleDragRect();
        if (placementController.dragging) { placementController.HideHoverPreview(); return; }

        placementController.UpdateHoverPreview();

        if (InputManager.Instance.ReadInteract()) placementController.TryPlace();
        if (InputManager.Instance.ReadRemove()) placementController.TryRemove();
    }

    void Register(Vector3Int cell, GameObject go, BlockEntry block, FixtureEntry fixture)
    {
        cells[cell] = new PlacedCell { GameObject = go, Block = block, Fixture = fixture };
    }

    static bool IsFilter(FixtureType type) => type == FixtureType.ColorFilter || type == FixtureType.RgbFilter;

    // --- 기물 팔레트 UI 연결용 ---

    /// <summary>다음 클릭부터 기본 블록을 설치하도록 전환한다.</summary>
    public void SelectBlockTool() => fixtureEditController.SelectBlockTool();

    /// <summary>Mode1 버튼 OnClick — 배치 모드 탭을 보여준다.</summary>
    public void ShowPlaceTab() => tabController.ShowPlaceTab();

    /// <summary>Mode2 버튼 OnClick — 값 수정 모드 탭을 보여준다.</summary>
    public void ShowValueEditTab() => tabController.ShowValueEditTab();

    /// <summary>Mode4 버튼 OnClick — 카메라 이동만 가능한 저장/불러오기 탭을 보여준다.</summary>
    public void ShowSaveLoadTab() => tabController.ShowSaveLoadTab();

    /// <summary>Mode5 버튼 OnClick — 카메라 이동만 가능한 플레이 테스트 탭을 보여준다. 안쪽 시작
    /// 버튼(StartPlayTest)을 눌러야 비로소 실제 플레이 테스트가 시작된다.</summary>
    public void ShowPlayTab() => tabController.ShowPlayTab();

    /// <summary>다음 클릭부터 지정한 기물을 설치하도록 전환한다. FixtureType의 int 값(순서)을 받는다
    /// (버튼 OnClick에서 정수 파라미터로 바로 연결 가능). 기물 종류에 맞는 파라미터 패널도 같이 연다.
    /// 이미 선택돼 있던 기물을 다시 선택하면(재선택) 패널 값을 초기화한다 — 다른 기물을 거쳐 돌아온
    /// 최초 복귀는 재선택이 아니므로 기존 값이 유지된다.</summary>
    public void SelectFixtureTool(int type) => fixtureEditController.SelectFixtureTool(type);

    // 우클릭(시점 회전, EditorFlyCamera) 중일 때만 커서를 잠근다 — 유니티 씬 뷰와 동일하게 그 외에는
    // 항상 커서가 보여야 Pan/Dolly나 RGBInput/RGBSelect 패널 조작이 가능하다.
    void UpdateCursorLock()
    {
        bool shouldLock = !fixtureEditController.IsParamPanelOpen && InputManager.Instance.ReadRotateHeld();
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    [SerializeField] Color markNormalColor = new(1f, 0.85f, 0.1f, 0.9f);  // 기존 마크 기본색
    [SerializeField] Color markSelectedColor = new(0.2f, 1f, 0.4f, 1f);   // 값 수정 대상으로 선택된 마크

    void LateUpdate() => fixtureEditController.UpdateMarkProjections();

    // --- 파라미터 입력 UI 연결용 ---

    /// <summary>맵 제목을 지정한다.</summary>
    public void SetTitle(string title) => data.title = title;

    public void SetPresetRGB(string value) => fixtureEditController.SetPresetRGB(value);

    public void ClickColorButton(int colorIndex) => fixtureEditController.ClickColorButton(colorIndex);

    // --- 저장/불러오기 UI 연결용 ---

    [SerializeField] GameObject saveNamePopup;
    [SerializeField] TMP_InputField saveNameInputField;
    [SerializeField] GameObject loadListPopup;
    [SerializeField] Transform loadListContent;
    [SerializeField] MyMapCardUI myMapCardTemplate;

    public void OpenSavePopup() => saveLoadController.OpenSavePopup();
    public void OpenSaveAsPopup() => saveLoadController.OpenSaveAsPopup();
    public void OnSaveAndExitButton() => saveLoadController.OnSaveAndExitButton();
    public void OnNewMapButtonSelected() => saveLoadController.OnNewMapButtonSelected();
    public void ConfirmSaveName() => saveLoadController.ConfirmSaveName();
    public void CancelSavePopup() => saveLoadController.CancelSavePopup();
    public void OnMapSelectBackButton() => saveLoadController.OnMapSelectBackButton();

    // --- 플레이 테스트 UI 연결용 ---

    [SerializeField] GameObject selectionPanel;   // UICanvas/SelectionPanel 전체 — 테스트 중엔 통째로 숨김
    [SerializeField] GameObject playTestOverlay;  // UICanvas 바로 밑, 테스트 중에만 보이는 "돌아가기" 배너

    void OnEnable()
    {
        EventBus.Subscribe<StageCleared>(playTestController.OnStageClearedDuringPlayTest);
        EventBus.Subscribe<MapObjectUsed>(playTestController.OnMapObjectUsedDuringPlayTest);
        EventBus.Subscribe<CanvasCompleted>(playTestController.OnCanvasCompletedDuringPlayTest);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<StageCleared>(playTestController.OnStageClearedDuringPlayTest);
        EventBus.Unsubscribe<MapObjectUsed>(playTestController.OnMapObjectUsedDuringPlayTest);
        EventBus.Unsubscribe<CanvasCompleted>(playTestController.OnCanvasCompletedDuringPlayTest);
    }

    public void StartPlayTest() => playTestController.StartPlayTest();
    public void StopPlayTest() => playTestController.StopPlayTest();

    void MarkEdited() => data.clearVerified = false;
}

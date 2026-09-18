using System.Collections.Generic;
using System.IO;
using Framework.Core;
using UnityEngine;

public partial class MapEditController
{
    // 저장/불러오기 + 맵 선택 흐름 전담.
    class SaveLoadController
    {
        readonly MapEditController owner;

        public SaveLoadController(MapEditController owner) => this.owner = owner;

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
            if (!string.IsNullOrEmpty(owner.data.title)) { SaveCurrentMap(); return; }
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
            if (!string.IsNullOrEmpty(owner.data.title))
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
            if (owner.saveNameInputField != null) owner.saveNameInputField.text = owner.data.title;
            if (owner.saveNamePopup != null) owner.saveNamePopup.SetActive(true);
        }

        /// <summary>SaveNamePopup의 확인 버튼 OnClick.</summary>
        public void ConfirmSaveName()
        {
            string title = owner.saveNameInputField != null ? owner.saveNameInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(title)) return; // 빈 제목이면 저장하지 않고 팝업 유지

            owner.SetTitle(title);

            if (saveIntent == SaveIntent.NewMap)
            {
                saveIntent = SaveIntent.None;
                CancelSavePopup();
                ShowEditorUI();
                return;
            }

            if (saveIntent == SaveIntent.SaveAsNew) owner.data.id = System.Guid.NewGuid().ToString();
            bool exitAfterSave = saveIntent == SaveIntent.ExitAfterSave;

            SaveCurrentMap();
            CancelSavePopup();

            if (exitAfterSave) ExitToMainMenu();
        }

        void SaveCurrentMap() => SaveManager.Instance.SaveJson(SaveFileName(owner.data.id), owner.data);

        /// <summary>SaveNamePopup의 취소 버튼 OnClick.</summary>
        public void CancelSavePopup()
        {
            if (owner.saveNamePopup != null) owner.saveNamePopup.SetActive(false);

            // New Map 이름 입력을 취소한 경우, 맵 선택 화면을 닫아둔 채였으므로 다시 열어준다.
            if (saveIntent == SaveIntent.NewMap) OpenLoadPopup();
            saveIntent = SaveIntent.None;
        }

        /// <summary>맵 선택 화면(저장된 맵 목록 + New Map)을 연다 — 씬 진입 시 Awake에서 호출된다.</summary>
        public void OpenLoadPopup()
        {
            RefreshLoadList();
            if (owner.loadListPopup != null) owner.loadListPopup.SetActive(true);
        }

        /// <summary>LoadListPopup의 닫기 버튼 OnClick.</summary>
        public void CloseLoadPopup()
        {
            if (owner.loadListPopup != null) owner.loadListPopup.SetActive(false);
        }

        // CanvasList/OrderList와 동일한 "숨긴 템플릿 복제" 패턴 — persistentDataPath에서 CustomMap_*.json을
        // 전부 열거해 카드로 보여준다. 별도 인덱스 파일 없이 매번 직접 읽는다(맵 개수가 많지 않을 전제).
        void RefreshLoadList()
        {
            if (owner.loadListContent == null || owner.myMapCardTemplate == null) return;

            foreach (var c in myMapCardInstances) Object.Destroy(c.gameObject);
            myMapCardInstances.Clear();

            foreach (var path in Directory.GetFiles(Application.persistentDataPath, $"{SaveFilePrefix}*.json"))
            {
                var loaded = SaveManager.Instance.LoadJson<CustomStageData>(Path.GetFileName(path));
                if (string.IsNullOrEmpty(loaded.id)) continue; // 손상된 파일 방어(LoadJson 실패 시 new CustomStageData())

                var card = Object.Instantiate(owner.myMapCardTemplate, owner.loadListContent);
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
            foreach (var placed in owner.cells.Values) Object.Destroy(placed.GameObject);
            owner.cells.Clear();

            owner.data.blocks.Clear();
            owner.data.fixtures.Clear();
            owner.data.canvasOrders.Clear();
            owner.fixtureEditController.ResetEditingSelection();

            owner.data.id = loaded.id;
            owner.data.title = loaded.title;
            owner.data.clearVerified = loaded.clearVerified;

            foreach (var block in loaded.blocks)
            {
                var entry = new BlockEntry { x = block.x, y = block.y, z = block.z };
                var go = CustomStageLoader.PlaceBlock(entry, owner.prefabs, owner.mazeRoot);
                owner.Register(new Vector3Int(entry.x, entry.y, entry.z), go, entry, null);
                owner.data.blocks.Add(entry);
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
                var instance = CustomStageLoader.PlaceFixture(entry, owner.prefabs, owner.mapObjectsRoot);
                if (instance == null) continue; // 팔레트 프리팹 누락 시 조용히 무시(PlaceFixtureAt과 동일)
                owner.Register(new Vector3Int(entry.x, entry.y, entry.z), instance.gameObject, null, entry);
                owner.data.fixtures.Add(entry);
                maxFixtureId = Mathf.Max(maxFixtureId, entry.id);
            }
            owner.nextFixtureId = maxFixtureId + 1;

            foreach (var order in loaded.canvasOrders)
                owner.data.canvasOrders.Add(new CanvasOrderEntry
                {
                    canvasFixtureId = order.canvasFixtureId,
                    orderFixtureIds = new List<int>(order.orderFixtureIds), // 별칭 방지용 깊은 복사
                });

            FilterBlockBase.RebuildAll(); // CustomStageLoader.Load()도 무조건 호출 — 필터 0개여도 안전

            CloseLoadPopup();
            ShowEditorUI();
        }

        /// <summary>맵 선택 화면(LoadListPopup)을 닫고 편집 화면을 보여준다 — 기존 맵을 불러왔을 때와
        /// New Map으로 새 맵을 만들었을 때 둘 다 이 지점으로 합류한다.</summary>
        void ShowEditorUI()
        {
            if (owner.selectionPanel != null) owner.selectionPanel.SetActive(true);
            owner.ShowPlaceTab();
        }

        /// <summary>맵 선택 화면(LoadListPopup)의 Back 버튼 OnClick — 편집으로 안 들어가고 메인메뉴로
        /// 나간다(아직 편집할 맵을 고르지 않은 상태이므로 "취소"가 아니라 "나가기"가 맞다).</summary>
        public void OnMapSelectBackButton() => ExitToMainMenu();

        void ExitToMainMenu() => GameManager.Instance.ExitToMainMenu();
    }
}

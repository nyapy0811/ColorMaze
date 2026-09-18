using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class MapEditController
{
    // 기물 팔레트(배치 파라미터) UI + 값 수정 모드 전담. 두 흐름이 SetPresetR/G/B·ClickColorButton·
    // HideParamPanels·IsParamPanelOpen 같은 메서드 몸체 안에서 editingFixtures.Count로 직접 분기하며
    // 얽혀 있어(배치 흐름 vs 수정 흐름) 서로 떼어낼 수 없다 — 하나의 컨트롤러로 묶어서 분리한다.
    class FixtureEditController
    {
        readonly MapEditController owner;

        public FixtureEditController(MapEditController owner) => this.owner = owner;

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

        FixtureType? currentFixtureType; // null = 기본 블록
        public FixtureType? CurrentFixtureType => currentFixtureType;

        // 배치 모드의 파라미터 preset. 기물 종류별로 완전히 독립적으로 저장한다(예: RgbFilter에서 고른 색이
        // Bucket이나 StackChanger에 새지 않도록) — 예전엔 전역 변수 하나를 모든 종류가 공유해서, 색을 안
        // 골라도 이전에 다른 기물에 골랐던 색이 그대로 적용되는 버그가 있었다.
        public class PresetValues
        {
            public int r, g, b;
            // RGBSelect류 최근 클릭 기록(중복 색 제거됨) — 단일 선택(RgbFilter/Bucket)은 1개,
            // StackChanger는 2개가 모여야 유효한 선택으로 친다(colorClicks[0]=A, [1]=B).
            public readonly List<int> colorClicks = new();
        }

        readonly Dictionary<FixtureType, PresetValues> presets = new();

        public PresetValues GetPreset(FixtureType type)
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
        public bool IsPlaceReady(FixtureType? type)
        {
            if (!type.HasValue) return true;
            if (UsesRgbInput(type.Value) || type.Value == FixtureType.ColorChanger) return true;
            return GetPreset(type.Value).colorClicks.Count == RequiredColorClicks(type.Value);
        }

        // 값 수정 모드에서 지금 선택된(편집 대상) 기물들 — 비어있으면 편집 대상 없음. 보통 1개지만, Shift+
        // 클릭으로 종류·현재 값이 완전히 같은 기물을 추가하면 여러 개를 한 번에 수정할 수 있다.
        // editingFixtures[i]와 editingFixtureInstances[i]는 인덱스로 1:1 대응.
        readonly List<FixtureEntry> editingFixtures = new();
        readonly List<MapObjectBase> editingFixtureInstances = new();
        public bool HasEditSelection => editingFixtures.Count > 0;

        // 값 수정 모드 전용 RGBSelect 최근 클릭 기록(Toggle이 아닌 일반 Button, Confirm 없이 클릭 즉시
        // 반영). 선택된 기물들이 전부 같은 값을 공유하는 상태에서 시작하므로 공유 필드 하나로 충분하다 —
        // 배치 모드는 기물 종류별로 완전히 분리된 PresetValues.colorClicks를 쓴다(값이 서로 새지 않도록).
        readonly List<int> editColorClicks = new();

        // ColorChanger 등 파라미터 없는 기물은 값 수정 모드의 마크 대상에서 애초에 제외되므로(RefreshMarks의
        // 필터 조건) 여기 fixture.type은 항상 UsesRgbInput 여부로 RGBInput/RGBSelect 중 하나가 정해진다.
        // 기존 선택을 전부 지우고 이 기물 하나만으로 새로 시작한다(Shift 없이 클릭했을 때의 동작).
        void BeginEditFixture(FixtureEntry fixture, GameObject go)
        {
            ResetEditingSelection();
            editingFixtures.Add(fixture);
            editingFixtureInstances.Add(go.GetComponent<MapObjectBase>());

            bool usesInput = UsesRgbInput(fixture.type);
            SetPanelActive(owner.editRgbInputPanel, usesInput);
            SetPanelActive(owner.editRgbSelectPanel, !usesInput);

            if (usesInput)
            {
                if (owner.editRgbInputField != null)
                    owner.editRgbInputField.text = $"{fixture.paramR:D2}{fixture.paramG:D2}{fixture.paramB:D2}";
            }
            else
            {
                editColorClicks.Clear();
                UpdateColorButtonHighlight(owner.editColorButtonImages, editColorClicks); // 클릭 전엔 셋 다 연하게(미선택)
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
            owner.MarkEdited();

            for (int i = 0; i < editingFixtureInstances.Count; i++)
                if (editingFixtureInstances[i] != null)
                    CustomStageLoader.ApplyParams(editingFixtureInstances[i], editingFixtures[i]);

            // 필터(ColorFilter/RgbFilter)는 같은 색끼리 묶인 병합 메시로 그려지고, 그 외형은 병합 그룹이
            // 다시 만들어질 때만 반영된다(FilterBlockBase.Configure 주석 참고) — 배치/제거 때와 동일하게 호출.
            if (editingFixtures.Count > 0 && IsFilter(editingFixtures[0].type)) FilterBlockBase.RebuildAll();
        }

        public void EndEdit()
        {
            ResetEditingSelection();
            HideParamPanels();
            UpdateMarkHighlight();
        }

        // 값 수정 대상 선택을 비운다. editingFixtures와 editingFixtureInstances는 인덱스로 1:1 대응하므로
        // 항상 같이 지워야 한다 — 모드 전환·선택 시작 등 여러 곳에서 반복되던 2줄짜리 패턴을 모았다.
        public void ResetEditingSelection()
        {
            editingFixtures.Clear();
            editingFixtureInstances.Clear();
        }

        /// <summary>다음 클릭부터 기본 블록을 설치하도록 전환한다.</summary>
        public void SelectBlockTool()
        {
            owner.mode = EditorMode.Place;
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
            owner.mode = EditorMode.ValueEdit;
            currentFixtureType = null;
            HideParamPanels();
            RefreshMarks(type => type != FixtureType.ColorChanger, SelectFixtureForEdit);
        }

        /// <summary>다음 클릭부터 지정한 기물을 설치하도록 전환한다. FixtureType의 int 값(순서)을 받는다
        /// (버튼 OnClick에서 정수 파라미터로 바로 연결 가능). 기물 종류에 맞는 파라미터 패널도 같이 연다.
        /// 이미 선택돼 있던 기물을 다시 선택하면(재선택) 패널 값을 초기화한다 — 다른 기물을 거쳐 돌아온
        /// 최초 복귀는 재선택이 아니므로 기존 값이 유지된다.</summary>
        public void SelectFixtureTool(int type)
        {
            owner.mode = EditorMode.Place;
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
            if (!type.HasValue || owner.fixtureValuePanels == null) return null;
            int index = (int)type.Value + 1;
            return index >= 0 && index < owner.fixtureValuePanels.Length ? owner.fixtureValuePanels[index] : null;
        }

        // active로 지정한 기물의 버튼만 높이 160 + 패널 활성으로 펼치고, 나머지는 전부 높이 80 + 패널
        // 비활성으로 접는다. VerticalLayoutGroup이 각 버튼의 현재 높이로 다른 버튼들의 위치를 자동 재배치
        // 하므로(겹침 없이), 높이를 바꾼 뒤 레이아웃을 강제로 즉시 재계산한다.
        void ApplyPlacePanelStates(FixtureType? active)
        {
            if (owner.fixtureValuePanels != null)
                for (int i = 0; i < owner.fixtureValuePanels.Length; i++)
                {
                    var p = owner.fixtureValuePanels[i];
                    if (p == null) continue;
                    bool expanded = active.HasValue && (int)active.Value + 1 == i;
                    SetButtonExpanded(p, expanded, (FixtureType)(i - 1));
                }

            if (owner.objectListContent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(owner.objectListContent);
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
            if (UsesRgbInput(type))
            {
                preset.r = preset.g = preset.b = 0;
                if (panel.rgbField != null) panel.rgbField.text = "";
            }
            else
            {
                preset.colorClicks.Clear();
                UpdateColorButtonHighlight(panel.colorButtonImages, preset.colorClicks);
            }
        }

        public void HideParamPanels()
        {
            ApplyPlacePanelStates(null);
            SetPanelActive(owner.editRgbInputPanel, false);
            SetPanelActive(owner.editRgbSelectPanel, false);
        }

        static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        public bool IsParamPanelOpen =>
            (owner.fixtureValuePanels != null && owner.fixtureValuePanels.Any(p => p != null && p.panel != null && p.panel.activeSelf)) ||
            (owner.editRgbInputPanel != null && owner.editRgbInputPanel.activeSelf) || (owner.editRgbSelectPanel != null && owner.editRgbSelectPanel.activeSelf);

        void UpdateObjectButtonHighlight(int index)
        {
            if (owner.objectButtonImages == null) return;
            for (int i = 0; i < owner.objectButtonImages.Length; i++)
                if (owner.objectButtonImages[i] != null)
                    owner.objectButtonImages[i].color = (i == index) ? owner.tabSelectedColor : owner.tabNormalColor;
        }

        // --- 값 수정 모드 화면 마크 ---
        // 카메라 이동만 가능하고 월드 클릭 대신 화면 마크를 눌러 대상을 지정한다 — 마크가 켜져 있는 동안
        // 배치된 기물 목록은 안 바뀌므로(설치·제거가 막혀 있음) 모드 진입 시 한 번만 만들면 된다.

        readonly List<(Transform worldTransform, RectTransform marker, int fixtureId)> marks = new();

        // 모드 진입 시(SelectEditTool) 호출 — include(기물 타입)가 true인 기물마다
        // 클릭 가능한 화면 마크를 하나씩 만들고, 클릭하면 onClick(기물 id)을 호출한다.
        void RefreshMarks(System.Func<FixtureType, bool> include, UnityEngine.Events.UnityAction<int> onClick)
        {
            ClearMarks();
            if (owner.editMarkTemplate == null || owner.editMarkRoot == null) return;

            foreach (var placed in owner.cells.Values)
            {
                if (placed.Fixture == null || !include(placed.Fixture.type)) continue;

                var marker = Object.Instantiate(owner.editMarkTemplate, owner.editMarkRoot);
                marker.gameObject.SetActive(true);
                int id = placed.Fixture.id;
                marker.onClick.AddListener(() => onClick(id));
                marks.Add((placed.GameObject.transform, marker.GetComponent<RectTransform>(), id));
            }

            UpdateMarkHighlight();
        }

        public void ClearMarks()
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
                img.color = editingFixtures.Any(f => f.id == fixtureId) ? owner.markSelectedColor : owner.markNormalColor;
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
            foreach (var kv in owner.cells)
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
                    if (!owner.cells.TryGetValue(next, out var placed) || placed.Fixture == null) continue;
                    if (!MatchesEditingSelection(placed.Fixture)) continue;
                    if (editingFixtures.Any(f => f.id == placed.Fixture.id)) continue;

                    editingFixtures.Add(placed.Fixture);
                    editingFixtureInstances.Add(placed.GameObject.GetComponent<MapObjectBase>());
                    queue.Enqueue(next);
                }
            }

            UpdateMarkHighlight();
        }

        public void UpdateMarkProjections()
        {
            if (owner.mode != EditorMode.ValueEdit) return;
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

                UpdateColorButtonHighlight(owner.editColorButtonImages, editColorClicks);
                RefreshEditingVisual();
            }
            else
            {
                var preset = GetPreset(type.Value);
                if (!RegisterColorClick(preset.colorClicks, colorIndex, type.Value)) return;
                UpdateColorButtonHighlight(PlacePanelFor(type)?.colorButtonImages, preset.colorClicks);
            }
        }
    }
}

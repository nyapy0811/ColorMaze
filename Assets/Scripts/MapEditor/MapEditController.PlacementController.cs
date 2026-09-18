using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class MapEditController
{
    // 배치/제거 + 호버 미리보기 + Shift 드래그 범위 설치/제거 전담.
    class PlacementController
    {
        readonly MapEditController owner;

        public PlacementController(MapEditController owner) => this.owner = owner;

        // 씬에 미리 배치해 둔 시작용 블록(예: 발판용 "Block")을 Maze 밑으로 옮기고 정식 배치 데이터로
        // 등록한다 — 그 결과 다른 블록과 완전히 동일하게 Ctrl+좌클릭으로 제거할 수 있고 data.blocks에도
        // 포함된다. 대상은 Awake 시점에 이미 컨트롤러의 자식으로 있던 오브젝트 전부(Maze/MapObjects 본인 제외).
        public void RegisterPreplacedBlocks()
        {
            var preplaced = new List<Transform>();
            foreach (Transform child in owner.transform)
                if (child != owner.mazeRoot && child != owner.mapObjectsRoot)
                    preplaced.Add(child);

            foreach (var child in preplaced)
            {
                Vector3Int cell = ToCell(child.position);
                if (owner.cells.ContainsKey(cell)) continue; // 이미 등록된 칸이면 건드리지 않음

                child.SetParent(owner.mazeRoot, true);
                var entry = new BlockEntry { x = cell.x, y = cell.y, z = cell.z };
                owner.Register(cell, child.gameObject, entry, null);
                owner.data.blocks.Add(entry);
            }
        }

        public void TryPlace()
        {
            if (!TryGetTargetCell(out Vector3 center, out _)) return;
            if (!owner.fixtureEditController.IsPlaceReady(owner.fixtureEditController.CurrentFixtureType)) return; // RGBSelect류인데 색 선택이 안 끝났으면 설치 안 함

            Vector3Int cell = ToCell(center);
            if (owner.cells.ContainsKey(cell)) return; // 이미 그 칸에 뭔가 있으면 무시(기존 에디터와 동일)

            if (owner.fixtureEditController.CurrentFixtureType == null) PlaceBlockAt(cell);
            else PlaceFixtureAt(cell, owner.fixtureEditController.CurrentFixtureType.Value);
        }

        public void TryRemove()
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

            foreach (var cell in owner.cells.Keys)
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
            var go = CustomStageLoader.PlaceBlock(entry, owner.prefabs, owner.mazeRoot);
            owner.Register(cell, go, entry, null);
            owner.data.blocks.Add(entry);
            owner.MarkEdited();
        }

        // 캔버스(FixtureType.Canvas)는 배치될 때마다 정답 순서가 하나씩 자동으로 생기므로, 무지개 7색
        // 마커 한도에 맞춰 최대 7개까지만 배치할 수 있다.
        int CanvasCount => owner.data.fixtures.Count(f => f.type == FixtureType.Canvas);

        void PlaceFixtureAt(Vector3Int cell, FixtureType type, bool suppressFilterRebuild = false)
        {
            if (type == FixtureType.Canvas && CanvasCount >= 7) return;

            var preset = owner.fixtureEditController.GetPreset(type);
            var entry = new FixtureEntry
            {
                id = owner.nextFixtureId++,
                type = type,
                x = cell.x, y = cell.y, z = cell.z,
                paramR = preset.r, paramG = preset.g, paramB = preset.b,
                paramColorA = preset.colorClicks.Count > 0 ? (LightColor)preset.colorClicks[0] : default,
                paramColorB = preset.colorClicks.Count > 1 ? (LightColor)preset.colorClicks[1] : default,
            };

            var instance = CustomStageLoader.PlaceFixture(entry, owner.prefabs, owner.mapObjectsRoot);
            if (instance == null) return; // 팔레트 프리팹이 연결 안 돼 있으면 조용히 무시

            owner.Register(cell, instance.gameObject, null, entry);
            owner.data.fixtures.Add(entry);
            owner.MarkEdited();

            if (type == FixtureType.Canvas)
                owner.data.canvasOrders.Add(new CanvasOrderEntry { canvasFixtureId = entry.id });

            if (IsFilter(type) && !suppressFilterRebuild) FilterBlockBase.RebuildAll();
        }

        void RemoveCell(Vector3Int cell, bool suppressFilterRebuild = false)
        {
            if (!owner.cells.TryGetValue(cell, out var placed)) return;
            owner.cells.Remove(cell);

            if (placed.Block != null) owner.data.blocks.Remove(placed.Block);
            if (placed.Fixture != null)
            {
                owner.data.fixtures.Remove(placed.Fixture);
                // 필터는 순서에 여러 번 들어갈 수 있으므로 하나만 지우지 않고 전부 지운다.
                foreach (var order in owner.data.canvasOrders) order.orderFixtureIds.RemoveAll(id => id == placed.Fixture.id);

                if (placed.Fixture.type == FixtureType.Canvas)
                    owner.data.canvasOrders.RemoveAll(c => c.canvasFixtureId == placed.Fixture.id);
            }

            bool wasFilter = placed.Fixture != null && IsFilter(placed.Fixture.type);

            Object.Destroy(placed.GameObject);

            if (wasFilter && !suppressFilterRebuild) FilterBlockBase.RebuildAll();
            owner.MarkEdited();
        }

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
            bool havePhysicsHit = Physics.Raycast(ray, out RaycastHit hit, owner.maxPlaceDistance, owner.placementMask, QueryTriggerInteraction.Collide);
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
        public void UpdateHoverPreview()
        {
            if (InputManager.Instance.ReadRemoveModifierHeld())
            {
                if (TryGetRemoveTargetCell(out Vector3Int cell))
                    ShowHoverPreview(CellCenter(cell), RedGhostMaterial());
                else
                    HideHoverPreview();
                return;
            }

            if (TryGetTargetCell(out Vector3 center, out _) && !owner.cells.ContainsKey(ToCell(center)))
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

        public void HideHoverPreview()
        {
            if (hoverPreview != null) hoverPreview.SetActive(false);
        }

        // --- Shift+드래그 직사각형 범위 설치/제거 (MazeGeneratorEditor의 Scene 뷰 기능을 런타임으로 이식) ---

        const int MaxDragCells = 1000; // 너무 큰 범위로 프레임 드랍/대량 생성되는 것 방지

        public bool dragging;
        bool dragRemove;
        int dragAxis; // 드래그 평면의 고정축 (0=x, 1=y, 2=z)
        Vector3 dragStartCenter, dragEndCenter;
        readonly List<GameObject> dragPreviewPool = new();
        static Material greenGhost, redGhost;

        public void HandleDragRect()
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
            bool placeReady = owner.fixtureEditController.IsPlaceReady(owner.fixtureEditController.CurrentFixtureType); // RGBSelect류인데 색 선택이 안 끝났으면 드래그 설치도 안 함

            // 드래그 범위가 필터일 때 칸마다 FilterBlockBase.RebuildAll()(씬 전체 필터 메시 재생성)을
            // 부르면 칸 수만큼 반복 비용이 든다 — 개별 호출은 억제하고 드래그가 끝난 뒤 한 번만 부른다.
            bool touchedFilter = false;
            foreach (var center in GetDragCenters())
            {
                Vector3Int cell = ToCell(center);
                if (dragRemove)
                {
                    if (owner.cells.TryGetValue(cell, out var placed))
                    {
                        if (placed.Fixture != null && IsFilter(placed.Fixture.type)) touchedFilter = true;
                        RemoveCell(cell, suppressFilterRebuild: true);
                    }
                }
                else if (placeReady && !owner.cells.ContainsKey(cell))
                {
                    if (owner.fixtureEditController.CurrentFixtureType == null) PlaceBlockAt(cell);
                    else
                    {
                        if (IsFilter(owner.fixtureEditController.CurrentFixtureType.Value)) touchedFilter = true;
                        PlaceFixtureAt(cell, owner.fixtureEditController.CurrentFixtureType.Value, suppressFilterRebuild: true);
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

        public void ClearDragPreview()
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
    }
}

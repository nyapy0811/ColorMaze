# 인게임 맵 에디터 설계 (초안)

작성일: 2026-09-09
최종 갱신: 2026-09-11
상태: 1~3단계 코드 구현 완료. `MapEditor.unity` 씬도 이미 만들어져 Build Settings에 등록되고
`MapEditController`(→`CustomStagePrefabs.asset` 연결됨)·UICanvas·HotBar·RGBInput 패널·카메라·
EventSystem까지 배치됨 — 남은 건 HotBar 버튼/RGB 입력창을 컨트롤러 public 메서드에 연결하는
이벤트 배선뿐(§ "남은 에디터(유니티) 작업" 참고).

## 목적 / 대상

- 플레이어용 UGC(사용자 제작 콘텐츠) 기능. 유니티 에디터 없이 빌드된 게임 안에서 스테이지를 직접 제작.
- 만들 수 있는 기물 범위: 전체(필터 포함) — 컬러 필터, RGB 필터, 버킷, 캔버스, 팔레트, 컬러 체인저, 스택 체인저, 일반 벽 블록.
- 정답 순서(correctOrder1/2, 가이드 기능용)도 처음부터 편집 가능.

## 핵심 아이디어

기존 스테이지는 `.unity` 씬에 실제 GameObject로 저장되지만, 빌드된 게임 안에서는 새 씬 파일을 만들 수 없다.
그래서 배치 정보를 순수 데이터(JSON 직렬화 가능한 구조)로 저장하고, 플레이 시점에 그 데이터로 실제 기물
프리팹을 런타임에 Instantiate하는 방식을 쓴다. 진짜 컴포넌트(ColorCanvas, FilterBlockBase 등)로만
소환되면 LevelManager, StageGuideController, MapObjectMarkerHUD 같은 기존 게임 로직은 전혀 수정할
필요가 없다 — 이게 이 설계의 핵심 이점.

## 참고: 기존 개발자용 에디터(MazeGeneratorEditor)

`Assets/Scripts/Editor/MazeGeneratorEditor.cs` — Scene 뷰에서 마인크래프트식으로 블록을 편집하는
[개발자 전용] 커스텀 에디터. 클릭=설치/Ctrl+클릭=제거, Shift+드래그=사각형 범위 설치/제거, 라디오
버튼으로 기물 종류 선택 + 프리셋 값(R/G/B 등) 입력 후 설치. 좌표 규칙: x,z는 정수, y는 정수+0.5.
런타임 에디터는 이 조작 방식을 그대로 옮기되, Scene뷰/Handles 대신 실제 카메라 레이캐스트 +
런타임 UI로 구현한다.

## 데이터 구조

Dictionary는 JsonUtility가 직렬화하지 못하므로(SaveData.cs 기존 제약과 동일) 전부 List 기반으로 설계.

```csharp
[Serializable]
public class CustomStageData
{
    public string id;       // GUID — 나중에 서버 공유로 확장해도 충돌 안 나게
    public string title;
    public List<BlockEntry> blocks = new();       // 일반 벽 블록
    public List<FixtureEntry> fixtures = new();   // 필터 포함 전체 기물
    public List<int> correctOrder1FixtureIds = new(); // fixtures의 id를 참조 (리스트 인덱스 아님 — 편집 중 순서 변경에 안전)
    public List<int> correctOrder2FixtureIds = new();
}

[Serializable]
public class BlockEntry { public int x, y, z; }

[Serializable]
public class FixtureEntry
{
    public int id;           // 이 스테이지 내부 고유 id (correctOrder가 참조)
    public FixtureType type; // ColorFilter, RgbFilter, Bucket, Canvas, Palette, ColorChanger, StackChanger
    public int x, y, z;
    public int paramR, paramG, paramB;          // 컬러필터/팔레트/캔버스용
    public LightColor paramColorA, paramColorB; // RGB필터/버킷/스택체인저용
}
```

## 런타임 로더

지정된 `CustomStageData`를 받아서:
1. 일반 블록 → `wallBlockPrefab`이 비어있으면 `PrimitiveType.Cube`(에디터 기본값과 동일), 나중에
   전용 벽 프리팹이 생기면 그 필드에 넣기만 하면 자동으로 교체되도록 옵션 필드로 설계.
2. 기물 → 타입별 프리팹(MazeGenerator가 들고 있는 것과 동일한 7종) Instantiate, 파라미터 적용.
3. 빈 GameObject에 `MazeGenerator` 컴포넌트를 동적으로 붙이고 `correctOrder1/2`를 방금 생성한
   실제 인스턴스 참조로 채움 → `StageGuideController`가 지금과 동일하게
   `FindFirstObjectByType<MazeGenerator>()`로 읽어감(가이드 시스템 변경 불필요).
4. `FilterBlockBase.RebuildAll()` 호출(필터 병합 메시 생성 — 개발자용 에디터/썸네일 캡처와 동일 처리).

## 인게임 에디터 UI (새 씬, 예: `MapEditor.unity`)

- 카메라 정면 레이캐스트로 블록 면 클릭 = 설치, 별도 입력(우클릭 등) = 제거. 기존 에디터의
  면 판정/그리드 스냅 로직(`TryGetTargetCell`)을 런타임 버전으로 그대로 이식.
- 화면 하단 기물 팔레트 UI: 블록/컬러필터/RGB필터/버킷/캔버스/팔레트/컬러체인저/스택체인저.
- 선택한 기물 타입에 맞는 파라미터 입력 UI(RGB 값 등 — 기존 에디터의 Preset 필드와 동일 개념).
- 이미 배치된 기물 재선택 → 파라미터 수정 + "정답 리스트1/2에 추가" 버튼 + 순서 편집(리스트1=빨강,
  리스트2=파랑 — 가이드 마커 색과 통일).

## 저장 / 공유

- 지금은 로컬 저장만: `SaveManager.Instance.SaveJson`(기존 범용 JSON API 재사용)으로 기기에 저장,
  "내 맵" 목록에서 불러오기/삭제/편집.
- 데이터 자체를 순수 JSON 직렬화 가능한 구조로만 만들어서, 나중에 내보내기/가져오기(문자열·파일)나
  서버 업로드로 확장하기 쉽게 열어둠. 저장 위치(로컬 파일 vs 서버)를 별도 계층으로 분리해서 로더/
  UI 코드는 안 건드리고 저장소만 교체 가능하게.

## 플레이 테스트

편집 중인 `CustomStageData`로 바로 플레이해볼 수 있는 "테스트" 버튼 — 런타임 로더를 그대로 재사용.

## 범위 밖

- 기존 챕터 스테이지(.unity 씬)는 JSON 방식으로 전환하지 않는다. 현재 수정 계획도 없고, 있어도
  소규모라 유니티 에디터에서 직접 고치는 쪽이 낫다고 판단(리스크 대비 이득 없음 — 상세 논의는
  대화 로그 참고). JSON 로더는 유저 제작 맵 전용.

## 구현 순서 (합의된 진행 계획)

1. ✅ `CustomStageData` + 관련 타입 정의 — 완료
2. ✅ 런타임 로더(데이터 → Instantiate → MazeGenerator 동적 부착 → RebuildAll) — 완료
3. ✅(코드) / ✅(씬 배치) / ⬜(이벤트 배선) 배치/제거 입력 + 기물 팔레트·파라미터 입력용 public API —
   로직 완료, `MapEditor.unity` 씬에 카메라·UICanvas·HotBar·RGBInput·`MapEditController`(prefabs 필드
   연결됨)까지 배치 완료. HotBar 버튼의 OnClick과 RGBInput 입력창의 OnValueChanged를
   `SelectBlockTool()`/`SelectFixtureTool(int)`/`SetPresetR/G/B(string)`/`SetPresetColorA/B(int)`에
   연결하는 것만 남음 — **다음 단계**. Player 프리팹 배치 여부(InteractionController 비활성화 포함)도
   아직 미확인.
4. ⬜ 배치된 기물 재선택 → 파라미터 수정 + 정답 리스트1/2 추가·순서 편집 UI
5. ⬜ 저장/불러오기(로컬 JSON) + "내 맵" 목록 UI
6. ⬜ 플레이 테스트 버튼(로더 재사용)

각 단계는 순서대로 구현하고 중간중간 확인받으며 진행.

## 진행 상황 (1~2단계 완료 내역)

**새 파일**
- `Assets/Scripts/MapEditor/CustomStageData.cs` — `FixtureType` enum, `BlockEntry`, `FixtureEntry`,
  `CustomStageData` 클래스.
- `Assets/Scripts/MapEditor/CustomStagePrefabs.cs` — 기물 7종 + 벽 블록 프리팹을 들고 있는
  `ScriptableObject`(`ColorMaze/Custom Stage Prefabs` 메뉴로 생성). 현재 애셋은
  `Assets/Scripts/MapEditor/CustomStagePrefabs.cs`(스크립트) 기준으로 만들어졌고, 실제 프리팹 참조는
  `.meta`의 `MonoImporter.defaultReferences`에 7종 기물 프리팹이 이미 연결돼 있음(wallBlockPrefab은
  아직 비어있음 — 전용 벽 프리팹 생기면 채우면 됨).
- `Assets/Scripts/MapEditor/CustomStageLoader.cs` — `CustomStageData`를 Instantiate해서
  `MazeGenerator`를 동적으로 만들고 `correctOrder1/2`를 채운 뒤 `FilterBlockBase.RebuildAll()` +
  `SceneLoadCompleted` 발행까지 처리하는 정적 로더.

**기존 파일 수정 (Configure 메서드 추가 — 로더가 프리셋 값을 런타임에 넣을 때 사용)**
- `ColorFilterBlock.Configure(int r, int g, int b)`
- `RgbFilterBlock.Configure(LightColor target)`
- `Bucket.Configure(LightColor target)`
- `ColorPalette.Configure(int r, int g, int b)`
- `StackChanger.Configure(LightColor a, LightColor b)`
- `ColorCanvas.Configure(int r, int g, int b)`

**핵심 설계 포인트(재확인용)**: 로더 마지막에 `SceneLoadCompleted`를 발행하는 것만으로 LevelManager
(캔버스 스캔), StageGuideController(정답 리스트 로드), StackChanger/ColorChanger 미리보기,
FilterBlockBase 초기화가 전부 기존 로직 그대로 자동으로 맞물려 돎 — 별도 훅 불필요.

**남은 미확정 사항**: 플레이어 스폰 지점을 에디터에서 어떻게 지정할지(3~4단계에서 정할 것).
아직 별도 스폰 마커 기물 종류나 기본 스폰 규칙이 정해지지 않음.

**3단계 코드 (신규 파일)**
- `Assets/Scripts/MapEditor/PlacedTag.cs` — 배치된 오브젝트에 그리드 좌표를 붙여두는 꼬리표
  (제거 입력 시 레이캐스트로 맞은 오브젝트가 어느 칸인지 역추적하는 용도).
- `Assets/Scripts/MapEditor/MapEditController.cs` — 배치/제거 핵심 로직.
  - `Update()`: `EventSystem.current.IsPointerOverGameObject()`로 UI 클릭은 걸러내고,
    `InputManager.ReadInteract()`(좌클릭)=배치, `InputManager.ReadRemove()`(우클릭, 이번에 InputManager에
    신규 추가)=제거.
  - 배치 레이캐스트/그리드 스냅 규칙은 `MazeGeneratorEditor.TryGetTargetCell`과 동일(x,z=정수, y=정수+0.5).
  - 내부적으로 `CustomStageLoader.PlaceBlock`/`PlaceFixture`(이번에 public으로 전환, 반환값도
    정리)를 그대로 재사용해서 개별 배치 — 로더와 에디터 컨트롤러가 인스턴스화 로직을 공유함.
  - 배치/제거할 때마다 `CustomStageData`(내부에서 계속 들고 있는 `data` 필드, `Data` 프로퍼티로 공개)를
    같이 갱신 — 별도 "내보내기" 변환 단계 없이 바로 저장 가능한 상태 유지.
  - 팔레트/파라미터 UI 연결용 public 메서드: `SelectBlockTool()`, `SelectFixtureTool(int)`,
    `SetTitle(string)`, `SetPresetR/G/B(string)`, `SetPresetColorA/B(int)`.
- `Assets/Scripts/Player/InputManager.cs`에 `ReadRemove()`(우클릭) 추가.
- `Assets/Scripts/MapEditor/CustomStageLoader.cs`의 `PlaceBlock`/`PlaceFixture`를
  `private static` → `public static`로 전환(위 재사용을 위함). 동작 변경 없음.

**남은 에디터(유니티) 작업 — 3단계를 실제로 쓸 수 있게 하려면**
1. ✅ `MapEditor.unity` 새 씬 생성 + Build Settings에 등록 — 완료(2026-09-11 확인).
2. ⬜ 카메라/이동: 기존 Player 프리팹(FirstPersonController)을 배치하되, `InteractionController`는
   비활성화(장거리 배치 클릭과 근접 상호작용 좌클릭이 같은 입력을 두고 충돌하므로) — 씬에 Player가
   배치됐는지 아직 미확인, 확인 필요.
3. ✅ 빈 오브젝트(`MapEditorController`)에 `MapEditController` 컴포넌트 추가, `prefabs` 필드에
   `CustomStagePrefabs.asset` 연결 완료(2026-09-11 확인).
4. ⬜ UI Canvas: 기물 팔레트 버튼(HotBar, 씬에 이미 배치됨) → OnClick을 `SelectBlockTool()` /
   `SelectFixtureTool(int)`(정수 파라미터로 FixtureType 순서 지정)에 연결 — **아직 미배선**
   (`m_Calls: []`로 확인됨, 2026-09-11).
5. ⬜ 파라미터 입력 UI(RGBInput 패널, 씬에 이미 배치됨) → `SetPresetR/G/B(string)`,
   `SetPresetColorA/B(int)`에 연결 — **아직 미배선**. 색상 A/B(스택체인저/RGB필터/버킷용) 선택 UI는
   씬에서 아직 확인 안 됨, 추가 배치가 필요할 수 있음.

**부수 작업(같은 세션에서 별도로 진행, 맵 에디터와 직접 관련은 없지만 ColorCanvas를 건드림)**
- `ColorCanvas`가 `LateUpdate()`에서 Y축만 기준으로 플레이어 쪽을 바라보도록 회전.
- 기존 Chapter1~7 모든 씬의 캔버스 27개 인스턴스의 Y축 회전을 0으로 일괄 정리(스크립트로 처리,
  자식 오브젝트 회전은 건드리지 않음).

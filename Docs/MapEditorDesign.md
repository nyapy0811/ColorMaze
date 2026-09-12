# 인게임 맵 에디터 설계 (초안)

작성일: 2026-09-09
최종 갱신: 2026-09-12
상태: 1~3단계(배치/제거 로직 + 이벤트 배선) 전부 완료, **실제 Play 모드 사용자 테스트까지 통과**.
`MapEditor.unity` 씬에서 숫자 1~8 키로 핫바 슬롯을 선택하고, 카메라 조작은 유니티 Scene 뷰와
동일하게 마우스로(우클릭 회전/휠클릭 Pan/스크롤 Dolly) 하며, 좌클릭 설치·Ctrl+좌클릭 제거·
Shift+드래그 범위 설치/제거까지 지원한다. 파라미터가 필요한 기물을 선택하면 RGBInput/RGBSelect
패널이 뜨며, Confirm으로 값을 확정한다. 남은 건 4~6단계(배치된 기물 재선택 편집, 저장/불러오기,
플레이 테스트 버튼)뿐 — § "구현 순서" 참고.

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
3. ✅ 배치/제거 입력 + 기물 팔레트·파라미터 입력용 public API + 이벤트 배선 — 전부 완료(2026-09-12).
   Player 프리팹도 씬에 배치돼 있고 `InteractionController.enabled = false`로 이미 비활성화 확인됨.
4. ⬜ 배치된 기물 재선택 → 파라미터 수정 + 정답 리스트1/2 추가·순서 편집 UI
5. ⬜ 저장/불러오기(로컬 JSON) + "내 맵" 목록 UI
6. ⬜ 플레이 테스트 버튼(로더 재사용)

각 단계는 순서대로 구현하고 중간중간 확인받으며 진행.

## 3단계 이벤트 배선 완료 내역 (2026-09-12)

문서에는 "HotBar 버튼 OnClick만 연결하면 됨"이라고 적혀 있었지만, 실제 씬을 열어보니 몇 가지가
문서와 달라서 사용자와 다시 협의해 아래처럼 확정·구현했다.

**HotBar는 버튼 클릭이 아니라 숫자 1~8 키로 선택한다** (마우스는 배치/제거용 좌/우클릭에 이미 쓰이고
있어서 핫바 선택까지 겹치지 않게 하려는 선택). HotBar의 8칸에는 애초에 `Button` 컴포넌트가 없었고,
라벨도 8칸 전부 `"1"`로 잘못 들어가 있었다 — 라벨을 1~8로 고치고, `Button`은 추가하지 않았다.

- **슬롯 매핑**: 1=기본 블록, 2=ColorFilter, 3=RgbFilter, 4=Bucket, 5=Canvas, 6=Palette,
  7=ColorChanger, 8=StackChanger (`FixtureType` enum 순서와 1:1).
- **선택 표시**: 선택된 슬롯의 `Image` 색을 `hotBarSelectedColor`(기본 노랑)로, 나머지는
  `hotBarNormalColor`(기본 흰색)로 바꿔서 하이라이트.
- **파라미터 패널은 "선택 시점"에 뜬다**(배치 시점이 아님) — 숫자키로 기물을 고르면 그 기물에 필요한
  패널(RGBInput 또는 RGBSelect)이 바로 뜨고, 필요 없는 기물(ColorChanger 등)은 아무 패널도 안 뜬다.
- **RGBInput은 입력창이 3개가 아니라 6자리("RRGGBB") 1개뿐** — 2자리씩 잘라 10진수 R/G/B로 해석
  (`SetPresetRGB`).
- **RGBSelect(Red/Green/Blue 토글 + Confirm)는 색 1개(RgbFilter/Bucket)와 2개(StackChanger의 A/B)
  선택에 재사용** — 토글은 정적 인자(0/1/2)로 `SetPendingColor`만 호출하고, 실제 A/B 반영은 Confirm
  클릭(`ConfirmColorSelect`)에서 처리. StackChanger는 첫 Confirm 후 패널을 유지한 채 두 번째 색을
  기다렸다가 반영.
- **값 유지**: `presetR/G/B`, `presetColorA/B`는 컨트롤러 인스턴스 필드라 아무 데서도 리셋하지 않음
  — 같은 기물을 여러 개 놓을 때 값을 새로 입력할 필요 없이 Confirm만 다시 누르면 직전 값 그대로
  적용된다.

**변경/추가된 코드**
- `Assets/Scripts/Player/InputManager.cs` — `ReadHotBarSlot(out int slot)` 추가(숫자 1~8, 메인
  자판/숫자패드 공용, 기존 `ReadDigit0()`과 같은 스타일).
- `Assets/Scripts/MapEditor/MapEditController.cs` — `rgbInputPanel`/`rgbSelectPanel`/
  `hotBarSlotImages`(8개) 필드 추가, `Update()`에서 `ReadHotBarSlot` 확인, `SelectBlockTool`/
  `SelectFixtureTool`이 패널 표시·하이라이트까지 담당, `SetPresetRGB`/`SetPendingColor`/
  `ConfirmRGBInput`/`ConfirmColorSelect` 추가.
- 씬(`MapEditor.unity`): HotBar 8칸 라벨 텍스트 수정, `hotBarSlotImages` 배열 연결, RGBInput/
  RGBSelect의 UnityEvent(OnValueChanged/OnClick)를 `UnityEditor.Events.UnityEventTools`로 배선
  (전용 Pipeline 명령이 없어 `eval_file`로 처리 — `GameObject.Find`는 비활성 오브젝트를 못 찾으므로
  `Transform.Find` 사용).
- 선택/패널 표시/StackChanger 2단계 색 선택 흐름은 `eval_file`로 자동 검증 완료(정상 동작 확인).

### 이벤트 배선 후 사용자 Play 테스트에서 발견된 버그 2건 수정 (2026-09-12)

자동화된 `eval_file` 검증은 실제 마우스/FPS 커서락/키보드 포커스 상호작용을 재현하지 못해서
아래 두 문제는 사용자가 직접 Play 모드에서 플레이해보고서야 발견됨. 둘 다 수정 완료, 사용자가
Play 모드에서 재확인하여 정상 동작 확인됨.

1. **패널이 떠도 마우스 커서가 안 보여서 클릭 불가**: `FirstPersonController`가 시작 시 커서를
   `CursorLockMode.Locked`/`visible=false`로 잠가둔 채라, RGBInput/RGBSelect 패널이 떠도 클릭할
   방법이 없었음. `MapEditController`에 `IsParamPanelOpen` 프로퍼티와 `UpdateCursorLock()`을 추가해
   패널이 하나라도 열려 있으면 커서를 풀고(`None`/`visible=true`), 전부 닫히면 다시 잠그도록 함
   (`PauseMenuController`가 쓰는 것과 동일한 기존 패턴 재사용). `SelectFixtureTool`/
   `HideParamPanels`에서 호출.
2. **RGB 입력창에 숫자를 타이핑하면 핫바 슬롯도 같이 바뀜**: `Update()`가 패널이 열려 있는지와
   무관하게 매 프레임 `ReadHotBarSlot`을 체크해서, RGBInput 입력창에 포커스가 있어도 숫자 키가
   핫바 전환으로도 처리됐음. `Update()`의 핫바 체크를 `!IsParamPanelOpen` 조건으로 감싸서 패널이
   열려 있는 동안(=텍스트 입력 중)은 핫바 전환을 무시하도록 수정.

**변경 파일**: `Assets/Scripts/MapEditor/MapEditController.cs`만 수정(`IsParamPanelOpen` private
프로퍼티 추가, `UpdateCursorLock()` 추가, `Update()`에 가드 추가). `InputManager.cs`나 씬 배선은
추가 변경 없음.

## 조작 방식을 유니티 Scene 뷰와 동일하게 전면 개편 + 배치 버그 3건 수정 (2026-09-12)

사용자가 "유니티 에디터처럼 자유 시점으로, 조작도 유니티 에디터처럼" 요청 — 기존 FPS 걷기 이동을
버리고, 마우스만으로 조작하는 방식으로 완전히 바꿨다. 이후 실사용 중 발견된 배치 관련 버그 3건도
같이 정리.

**조작키 (최종)**

| 입력 | 동작 |
|---|---|
| 우클릭 + 드래그 | 시점 회전 (누르는 동안 커서 잠김/숨김) |
| 휠 클릭 + 드래그 | 카메라 기준 평행 이동(Pan) |
| 휠 스크롤 | 카메라가 보는 방향으로 전/후진(Dolly) |
| 좌클릭 | 설치 |
| Ctrl + 좌클릭 | 제거 (기존엔 우클릭이었음) |
| Shift + 좌클릭 드래그 | 직사각형 범위 설치 (초록 미리보기) |
| Shift + Ctrl + 좌클릭 드래그 | 직사각형 범위 제거 (빨강 미리보기) |
| 숫자 1~8 | 핫바 슬롯 선택 (변경 없음) |

- 새 파일 `Assets/Scripts/MapEditor/EditorFlyCamera.cs` — `MapEditor.unity`의 Player 인스턴스에만
  붙는 전용 카메라 스크립트(공용 `Player.prefab`은 안 건드림). `FirstPersonController`와 같은
  "몸통 yaw + 카메라 pitch" 구조를 재사용하되 중력/충돌 없이 우클릭 홀드 중에만 회전한다.
  `MapEditor.unity`에서 `FirstPersonController`는 비활성화, `EditorFlyCamera` 추가.
- `InputManager.cs`에 `ReadRotateHeld`/`ReadPanHeld`/`ReadDolly`/`ReadRangeModifierHeld`/
  `ReadRemoveModifierHeld` 추가. `ReadInteract`/`ReadRemove`는 좌클릭 하나를 Ctrl 여부로 분기하도록
  통합.
- `MapEditController.cs`에 `MazeGeneratorEditor`(Scene 뷰 개발자 도구)의 Shift+드래그 직사각형
  범위 설치/제거 로직을 그대로 포팅(`DominantAxis`/`GetDragCenters`/`UpdateDragRect`/
  `CommitDragRect`). 미리보기는 런타임에 `Handles`가 없어서 풀링된 불투명 큐브(초록=설치,
  빨강=제거)로 대체. 커서 잠금 조건도 "패널 안 열림 && 우클릭 회전 중"으로 변경.

**배치 버그 3건 (실사용 중 발견)**

1. **필터 병합 메시가 클릭한 위치가 아니라 엉뚱한 곳에 뜸**: `FilterBlockBase.BuildGroup()`이
   메시 정점 좌표에 월드 좌표값을 그대로 굽고, 그 메시 오브젝트를 `mapObjectsRoot` 밑에 로컬
   원점으로 붙인다 — `mapObjectsRoot`가 월드 원점이어야만 맞는 구조. 그런데 `MapEditorController`
   GameObject 자체가 씬에서 원점이 아닌 곳(`(-4.84, -0.27, 1.73)`)에 있어서 `mapObjectsRoot`도
   같이 오프셋됐고, 병합 메시가 그만큼 어긋나 보였음(실제 필터 오브젝트·콜라이더는 항상 정확한
   위치였음 — 눈에 보이는 병합 메시만 문제). **해결: `MapEditorController` GameObject를 씬에서
   월드 원점 (0,0,0)으로 이동**(코드 변경 없이 씬 배치만으로 해결).
2. **캔버스가 플레이어 반대쪽을 보고 있음**: `ColorCanvas.LateUpdate()`가 표시면(로컬 -Z쪽)이
   아니라 +Z가 플레이어를 향하도록 `Quaternion.LookRotation(dir, ...)`을 쓰고 있었음. `-dir`로
   뒤집어서 수정([ColorCanvas.cs](../../Assets/Scripts/MapObjects/ColorCanvas.cs)) — 챕터 스테이지
   포함 모든 곳에 적용되는 공용 스크립트라 기존 챕터의 캔버스도 같이 정상화됨.
3. **기본 블록 외 기물(특히 필터)을 클릭할 수 없음 — 옆에 이어 놓기가 안 됨**:
   `TryGetTargetCell`/`TryRemove`의 레이캐스트가 `QueryTriggerInteraction.Ignore`였는데,
   `FilterBlockBase`는 플레이어 스택이 필터 조건과 일치하면 콜라이더를 트리거로 바꾼다(통과시켜야
   하니까). 기본값(0,0,0)으로 놓은 필터는 플레이어 초기 스택과 바로 일치해 트리거가 되고, 그러면
   편집용 레이캐스트가 아예 무시하고 지나가 클릭이 안 먹혔음. **해결**: 두 레이캐스트를
   `QueryTriggerInteraction.Collide`로 변경 — 편집 중에는 트리거 상태와 무관하게 항상 클릭 가능.

**시작용 블록을 정식 배치 데이터로 등록 (2026-09-12)**

바닥이 전혀 없으면 카메라 레이캐스트가 아무것도 못 맞혀 첫 배치 자체가 불가능한 문제(§ 별도 논의)
때문에, 사용자가 `MapEditor.unity`에 시작용 블록 하나("Block", 셀 `(0,-1,0)` = 월드 `(0,-0.5,0)`)를
직접 씬에 배치해뒀다. 이후 두 가지를 정리:

- 이 "Block"을 씬 루트에서 `/MapEditorController` 밑으로 재배치(`set_parent`, 위치는 그대로 유지)
  — 런타임에 배치되는 블록들과 같은 하이러키 영역에 있도록.
- **목적 확인**: 이 시작 블록도 다른 배치 블록과 완전히 동일하게 **제거 가능**해야 함. 이를 위해
  `MapEditController.Awake()`에 `RegisterPreplacedBlocks()`를 추가 — Awake 시점에 컨트롤러의
  기존 자식 오브젝트(방금 만든 `Maze`/`MapObjects` 본인은 제외)를 찾아 `Maze` 밑으로 옮기고,
  위치로부터 그리드 셀을 역산해 `cells`/`data.blocks`에 정식 등록한다. 그 결과 이 시작 블록도
  Ctrl+좌클릭으로 제거되고, 저장 데이터(`data.blocks`)에도 포함되며, 특정 오브젝트 이름에
  종속되지 않아 나중에 시작용 오브젝트를 더 추가해도 자동으로 같은 방식으로 등록된다.
  사용자가 Play 모드에서 직접 제거 확인 완료.

**스페이스바로 Confirm 버튼과 동일하게 확정 (2026-09-12)**

RGBInput/RGBSelect 패널의 "Confirm" 버튼을 마우스로 클릭하지 않고도 스페이스바로 확정할 수 있게
추가. `InputManager.ReadConfirm()`(스페이스바) 신설, `MapEditController.Update()`에서 패널이
열려 있을 때(`IsParamPanelOpen`) 스페이스바를 누르면 현재 활성 패널에 맞춰 `ConfirmRGBInput()`
또는 `ConfirmColorSelect()`를 그대로 호출(StackChanger의 2단계 색 선택도 기존 로직 그대로라 동일하게
동작). 마우스 포인터 위치와 무관하게 반응하도록 `IsPointerOverGameObject` 얼리 리턴보다 앞에 배치.
사용자가 Play 모드에서 RGBInput/RGBSelect(단일·2색) 전부 정상 동작 확인 완료.

**RGBSelect 토글 개수 검증 + 스택 체인저 3개 중 2개 선택 + 기물 재선택 시 패널 초기화 (2026-09-12)**

RGBSelect의 Red/Green/Blue 토글에는 `ToggleGroup`이 없어(`m_Group: null`) 몇 개를 켜든 유니티가
막아주지 않는다. `ConfirmColorSelect()`에서 `Toggle[] rgbSelectToggles`(씬 배선)의 `isOn` 상태를
직접 세어, RgbFilter/Bucket은 정확히 1개, StackChanger는 정확히 2개 선택돼 있어야만 확정하고 패널을
닫는다(개수가 안 맞으면 패널 유지). StackChanger는 기존에 "A 선택→Confirm→B 선택→Confirm" 2단계
(`colorPickStep`)였는데, 새 토글을 추가하지 않고 기존 3개 중 2개를 한 번에 선택하는 방식으로
바꿔서 1단계로 통합했다 — 배열 순서(Red=0,Green=1,Blue=2)상 먼저 켜진 인덱스가 아니라 배열에서
먼저 나오는 인덱스가 colorA, 나중 인덱스가 colorB.

같은 기물을 다시 선택(같은 핫바 슬롯 재입력)하면 패널 값이 초기화되도록 `SelectFixtureTool()`에
재선택 판정을 추가하고 `ResetPanelValues(FixtureType)`를 호출한다(RGBInput 계열은
`presetR/G/B=0`+`rgbInputField.text=""`, RGBSelect 계열은 토글 전부 `isOn=false`). 다른 기물을
거쳐 돌아오는 최초 복귀(재선택 아님)는 기존 "값 유지" 동작 그대로 보존된다. 이를 위해
`rgbInputField`(TMP_InputField) 필드를 신설해 `/UICanvas/RGBInput/RGB`에 배선했다.
사용자가 Play 모드에서 전부(토글 개수 검증, StackChanger 1단계 확정, 재선택 초기화, 값 유지 회귀
없음) 정상 동작 확인 완료.

**맵 에디터 HUD·1인칭 뷰모델 숨기기 + GameState를 프레임워크에서 프로젝트로 이관 (2026-09-12)**

맵 에디터 Play 모드에서 일반 플레이용 HUD(조준점·스택 표시)와 1인칭 뷰모델 붓이 계속 보이던 문제를
고쳤다. 두 컴포넌트(`HUDController`, `BrushViewmodel`)는 원래 `next != GameState.MainMenu`일
때만 보이는 구조였는데, `GameState`가 이 프로젝트가 아니라 별도 git 저장소인 프레임워크 패키지
(`com.nyapy.framework-core`)에 고정 `enum`으로 내장돼 있어서 새 상태를 넣으려면 프레임워크 자체를
고쳐야 했다.

이 기회에 "프로젝트가 상태를 자유롭게 추가/삭제할 수 있게" 프레임워크 구조를 바꿨다(실행 중 동적
등록은 불필요하다고 확인 — 컴파일 타임 enum으로 충분):
- 프레임워크 `Managers/GameManager.cs`(별도 저장소, `nyapy0811/FrameWorkCore.git`)를 구체 클래스
  → 제네릭 베이스 `GameManagerBase<TSelf, TState> : MonoSingleton<TSelf> where TState : struct, Enum`
  로 교체. `State`/`ChangeState`/`OnStateChanged`만 남기고, 상태 이름을 아는 이름 있는 편의 메서드
  (`StartGame/Pause/Resume/StageClear/BeginLoading/Quit`)는 전부 제거.
- 프로젝트 쪽에 새 파일 `Assets/Scripts/Core/GameState.cs`(진짜 C# enum: Boot/MainMenu/Loading/
  Playing/**MapEditor**/Paused/Cleared/Quitting)와 `Assets/Scripts/Core/GameManager.cs`
  (`GameManagerBase<GameManager, GameState>`를 구체화, 프레임워크에서 빠진 편의 메서드 전부 이관)를
  신설. 이름을 기존과 동일하게 유지해서 `MainMenuController`/`ClearScreenController`/
  `PauseMenuController`/`SceneRestarter`/`LevelManager`는 전혀 수정하지 않아도 그대로 컴파일됨.
- `GameStateListener.cs`/`HUDController.cs`는 `using Framework.Core;`가 더 이상 필요 없어 제거.
  `HUDController`/`BrushViewmodel`의 표시 조건에 `&& next != GameState.MapEditor` 추가.
  `MapEditController.Awake()` 맨 앞에 `GameManager.Instance.ChangeState(GameState.MapEditor);` 추가.

**부수 회귀 2건도 같은 자리에서 발견해 수정**:
1. `GameManager.Pause()`가 `State == Playing`일 때만 허용하고 `Resume()`이 무조건 `Playing`으로
   돌아가도록 하드코딩돼 있어서, 맵 에디터(`MapEditor` 상태)에서 ESC 일시정지가 아예 안 먹혔다.
   `Pause()`가 `Playing`/`MapEditor` 둘 다에서 허용되도록 하고, 일시정지 직전 상태를
   `stateBeforePause`에 저장해뒀다가 `Resume()`이 그 상태로 정확히 복귀하도록 수정
   (`PauseMenuController.Update()`의 ESC 판정에도 `GameState.MapEditor` 추가).
2. `Time.timeScale = 0`이어도 `Update()`는 계속 돌기 때문에, 일시정지 중에도 핫바 선택·설치·
   제거·드래그 범위 지정이 그대로 먹히고 있었다. `MapEditController.Update()` 맨 앞에
   `if (GameManager.Instance.State == GameState.Paused) return;` 가드를 추가해 일시정지 중에는
   커서 관리를 포함한 편집 입력 전체를 멈추고 `PauseMenuController`에 맡기도록 했다.

사용자가 Play 모드에서 HUD/뷰모델 숨김, 일시정지 진입/해제(맵 에디터로 정확히 복귀), 일시정지 중
편집 입력 차단, 일반 스테이지 회귀(메인 메뉴/Play/일시정지/클리어/재시작) 전부 정상 동작 확인 완료.

**단일 클릭 설치/제거 미리보기 + 기물도 블록 크기로 클릭 판정 (2026-09-12)**

기존 Shift+드래그 범위 설치/제거에만 있던 고스트 큐브 미리보기(초록=설치, 빨강=제거)를 일반 단일
클릭(좌클릭 설치/Ctrl+좌클릭 제거)에도 추가했다. `hoverPreview` 오브젝트 하나를 지연 생성해 매
프레임(`UpdateHoverPreview()`) 위치/재질을 갱신하며, `TryPlace`/`TryRemove`가 실제로 허용하는
조건과 동일하게 판정해서 클릭해도 아무 일 없는 상황(빈 곳 제거, 이미 막힌 칸 설치)에서는 미리보기도
뜨지 않는다. 일시정지, UI 위, 드래그 중에는 확실히 숨긴다.

이어서 "기물(필터·캔버스 등)도 블록처럼 클릭하기 쉽게 해달라"는 요청으로 클릭 판정 방식 자체를
바꿨다. 처음엔 기물에 보이지 않는 1x1x1 콜라이더를 붙이는 방법을 검토했으나,
`FilterBlockBase.OnTriggerEnter/Exit`가 플레이어의 필터 완전 통과를 감지해 `Player.ResetAll()`로
색 스택을 리셋하는데, 새 콜라이더가 같은 오브젝트에 붙으면 에디터에서 그 히트박스를 지나가기만
해도 이 콜백이 잘못 발동할 위험이 있어 기각했다. 대신 **콜라이더 없이 순수 계산**으로 처리한다:
- 새 헬퍼 `TryRaycastCells(ray, out hitCell, out normal, out distance)`가 `cells` 딕셔너리에 있는
  모든 칸에 대해 가상의 1x1x1 `Bounds`로 레이 교차를 검사해 가장 가까운 칸과 진입면을 반환한다.
  실제 모델 모양과 무관하게 항상 블록 하나 크기로 클릭된다.
- `TryGetTargetCell`(설치용)은 바닥/벽처럼 아직 배치되지 않은 표면을 위해 기존 물리 레이캐스트를
  유지하되, `TryRaycastCells` 결과와 "더 가까운 쪽"을 비교해서 쓴다.
- `TryGetRemoveTargetCell`(제거용)은 바닥 클릭이 의미 없으므로 `TryRaycastCells`만으로 충분해져
  훨씬 단순해졌다. 물리 레이캐스트로 콜라이더를 찾아 `PlacedTag`를 조회하던 기존 방식을 대체하면서,
  오직 그 용도로만 존재하던 `PlacedTag` 컴포넌트(`Assets/Scripts/MapEditor/PlacedTag.cs`)와
  `Register()`의 `AddComponent<PlacedTag>()` 호출도 제거했다.
- 필터의 `Col.isTrigger` 토글 상태와도 완전히 무관해져, 필터가 트리거 상태(통과 가능)일 때 클릭이
  씹히던 문제도 부수적으로 같이 해결됐다.

**버그 수정**: `Bounds.IntersectRay`는 레이 원점이 박스 안에 있으면 `dist=0`을 반환하는데,
`EditorFlyCamera`는 충돌 없는 자유 비행이라(특히 스크롤로 뒤로 Dolly할 때) 카메라가 블록 안/바로
옆으로 들어갈 수 있다. 이 상태를 그대로 두면 카메라 위치 기준의 임의 방향이 진입면으로 잘못 잡혀
플레이어 근처 허공에 초록 미리보기가 떴다. `TryRaycastCells`에서 각 칸을 검사하기 전에, 카메라가
그 칸의 1.2배 넉넉한 범위 안에 있으면 아예 그 칸을 판정에서 제외하도록 고쳐서 해결했다.

사용자가 Play 모드에서 얇은 기물 클릭/제거, 미리보기, 드래그 범위, 필터 트리거 상태, 뒤로 이동 시
허공 미리보기 버그까지 전부 정상 동작 확인 완료.

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
    `SetTitle(string)`, `SetPresetR/G/B(string)`, `SetPresetRGB(string)`(6자리 일괄),
    `SetPresetColorA/B(int)`, `SetPendingColor(int)`, `ConfirmRGBInput()`, `ConfirmColorSelect()`
    (마지막 4개는 2026-09-12 이벤트 배선 때 추가 — § "3단계 이벤트 배선 완료 내역" 참고).
- `Assets/Scripts/Player/InputManager.cs`에 `ReadRemove()`(우클릭) 추가.
- `Assets/Scripts/MapEditor/CustomStageLoader.cs`의 `PlaceBlock`/`PlaceFixture`를
  `private static` → `public static`로 전환(위 재사용을 위함). 동작 변경 없음.

**남은 에디터(유니티) 작업 — 3단계를 실제로 쓸 수 있게 하려면**
1. ✅ `MapEditor.unity` 새 씬 생성 + Build Settings에 등록 — 완료(2026-09-11 확인).
2. ✅ 카메라/이동: Player 프리팹 배치 확인됨, `InteractionController.enabled = false`로 이미
   비활성화되어 있음(2026-09-12 `eval`로 재확인).
3. ✅ 빈 오브젝트(`MapEditorController`)에 `MapEditController` 컴포넌트 추가, `prefabs` 필드에
   `CustomStagePrefabs.asset` 연결 완료(2026-09-11 확인).
4. ✅ HotBar 선택 → 숫자 1~8 키로 배선 완료(버튼 클릭 방식에서 변경, § "3단계 이벤트 배선 완료
   내역" 참고, 2026-09-12).
5. ✅ 파라미터 입력 UI(RGBInput 6자리 입력창, RGBSelect 토글+Confirm) → 전부 배선 완료, 색상
   A/B(스택체인저용) 2단계 선택 흐름까지 구현·검증 완료(2026-09-12).

**부수 작업(같은 세션에서 별도로 진행, 맵 에디터와 직접 관련은 없지만 ColorCanvas를 건드림)**
- `ColorCanvas`가 `LateUpdate()`에서 Y축만 기준으로 플레이어 쪽을 바라보도록 회전.
- 기존 Chapter1~7 모든 씬의 캔버스 27개 인스턴스의 Y축 회전을 0으로 일괄 정리(스크립트로 처리,
  자식 오브젝트 회전은 건드리지 않음).

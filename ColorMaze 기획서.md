# ColorMaze

**게임 기획서 (Game Design Document)**

빛의 삼원색으로 색을 맞춰 통과하는 3D 미로 퍼즐

장현창

---

## 목차

1. [게임 개요](#1-게임-개요)
2. [게임플레이 개요](#2-게임플레이-개요)
3. [시스템 작동 흐름](#3-시스템-작동-흐름)
4. [맵 기물](#4-맵-기물)
5. [레벨 구성](#5-레벨-구성)

---

## 1. 게임 개요

| 항목 | 내용 |
|---|---|
| 제목 | ColorMaze |
| 장르 | 3D 1인칭 미로 / 색 맞추기 퍼즐 |
| 플랫폼 | PC(Unity 6, URP) |
| 시점 | 1인칭 (자유 이동 + 마우스 시점) |
| 맵 구조 | 정육면체 블록(큐브)을 배치해 구성한 미로 |
| 한 줄 소개 | 빛의 삼원색(RGB) 스택을 모아 색을 맞춰 게이트를 통과하는 미로 퍼즐 |

## 2. 게임플레이 개요

### 2.1 조작

| 입력 | 동작 |
|---|---|
| W A S D / 방향키 | 이동 (카메라 기준) |
| 마우스 | 시점 (몸통 좌우 + 카메라 상하) |
| Space | 점프 |
| ESC | 일시정지 / 재개 |

*구현: InputManager가 새 Input System으로 키보드/마우스를 폴링해 ReadMove·ReadLook·ReadJump·ReadPause를 제공한다. FirstPersonController와 PauseMenuController가 이를 사용하며, 향후 터치/패드 입력도 이 클래스에만 추가하면 된다.*

## 3. 시스템 작동 흐름

### 3.1 메인 화면

- 스테이지 선택, 설정, 종료를 선택할수 있다
- 스테이지 선택은 챕터·스테이지 목록으로 이동해 해금된 스테이지를 고른다.
- 설정은 음량·마우스 감도 등 옵션을 조절한다.

*구현: MainMenuController가 담당한다. 메인 패널·스테이지 선택(챕터 목록) 패널·설정 패널을 서로 배타적으로 토글하고, 챕터 버튼을 누르면 공용 스테이지 목록 패널(stageListPanel)을 그 챕터 기준으로 보여준다(챕터마다 패널을 따로 두지 않고, 공용 데이터 애셋 StageTable에서 씬 이름만 갈아끼움 — 이 애셋은 클리어 화면(3.6)의 ClearScreenController와도 함께 참조해 스테이지 목록이 한 곳에만 존재한다). 챕터 목록은 ScrollRect로 스크롤된다. 해금 여부는 ProgressManager(5.1 참고)가 판정하며, 챕터/스테이지 목록이 열릴 때마다 잠긴 버튼은 interactable을 꺼서 회색으로 비활성화한다. GameManager.OnStateChanged 구독/해제와 "현재 상태로 1회 초기 갱신" 보일러플레이트는 공용 베이스 GameStateListener(Core 폴더)로 모아뒀다 — MainMenuController·HUDController·PauseMenuController·BrushViewmodel(Player 폴더) 모두 이 베이스를 상속해 OnGameStateChanged(previous, next)만 구현한다.*

### 3.2 이동 / 카메라 (1인칭)

- 플레이어는 카메라가 바라보는 방향을 기준으로 자유롭게 이동한다.
- 마우스로 시점을 돌린다 — 좌우는 몸을, 상하는 시야를 움직인다.
- 점프해서 올라가다 천장에 막히면 더 오르지 않고 즉시 아래로 떨어진다.
- 일시정지 상태에서는 이동과 시점 조작이 멈춘다.

*구현: FirstPersonController(CharacterController 기반)가 담당한다. 점프 높이·체공시간 값으로 중력과 초기 속도를 역산해 두며, 상승 중 CharacterController.collisionFlags에 Above(위쪽 충돌)가 실제로 찍히면 수직 속도를 즉시 반전시켜 하강시킨다(이동량 추정치 비교가 아니라 실제 충돌 판정이라 프레임레이트에 영향받지 않음). Time.timeScale이 0이면(일시정지) 입력을 무시한다.*

### 3.3 RGB 스택과 플레이어 컬러

- 플레이어는 Red / Green / Blue 세 종류의 스택을 가진다.
- 스택 값은 0 ~ 15 범위를 순환한다. 15를 넘으면 초과한 양만큼 0부터 다시 세고, 반대로 0 미만으로 내려가면 초과한 양만큼 15부터 다시 센다.
- 플레이어의 입력과 외부 요소(예: 특정 블록·아이템)에 따라 각 스택이 늘거나 준다.
- 각 스택 값이 채널별로 독립적인 밝기로 변환되어 '플레이어 컬러'로 적용된다(15가 채널당 최대 밝기, 15/15/15가 흰색).
- 스택이 바뀌면 그 색과 값이 HUD 등 관련 표시에 곧바로 반영된다.

*구현: ColorStacks가 담당한다. 씬에 하나뿐인 플레이어 인스턴스는 정적 프로퍼티 ColorStacks.Instance로 참조한다(Awake에서 등록, OnDestroy에서 해제) — 맵 기물(MapObjectBase.Player)·HUD·붓 뷰모델 등 여러 곳에서 각자 FindAnyObjectByType으로 찾던 것을 이 한 곳으로 모았다. FirstPersonController도 같은 방식으로 FirstPersonController.Instance를 제공한다(설정 화면의 마우스 감도 슬라이더가 참조). 상한은 색상별 개별 설정이 아니라 클래스 전체가 공유하는 `public const int StackMax = 15` 하나뿐이다(색상별로 다르게 설정할 수 있던 이전 구조를 없앰 — 일부 색상만 값을 다르게/깜빡하고 설정해서 생기는 버그를 원천 차단, 상한을 바꾸려면 이 상수 한 곳만 고치면 됨). 색상별로 남은 설정은 초기값(start)뿐이다. 값은 항상 [0, StackMax] 범위를 모듈러 연산으로 순환한다 — 하한은 0으로 고정이라 상한을 넘으면 초과분만큼 0부터, 0 아래로 내려가면 초과분만큼 상한부터 다시 센다. 값이 바뀌면 EventBus로 ColorStackChanged를 발행해 HUD 등에 알리고, 외부 기물은 ColorStackChangeRequest 이벤트로 값 변경을 요청한다. ToRGB 정적 함수가 세 스택을 정수 RGB로 변환한다 — 채널마다 독립적으로 [0, StackMax]를 [0, 255]로 매핑(반올림), 세 값의 상대 비율이 아니라 각 채널의 절대 스택 값이 그대로 밝기가 되어 StackMax/StackMax/StackMax가 흰색이 된다(이전에는 셋 중 최댓값 기준으로 정규화해, 예를 들어 10/10/10도 흰색으로 보였음 — 이번에 수정).*

### 3.4 HUD

- 현재 스택 값과, 플레이어 컬러, 목표 스택을 화면에 함께 보여준다.
- 스택이 바뀌면 표시가 즉시 갱신된다.

*구현: ColorStackHUD가 ColorStackChanged 이벤트를 구독해 스택 숫자를 갱신한다. 플레이어 컬러(변환 색)는 별도 HUD 스와치 대신 1인칭 뷰모델 붓(BrushViewmodel, Player 폴더 참고)의 붓 끝 색으로 대신 보여준다 — 붓이 항상 화면에 보이는 위치에 있어 별도 스와치 UI가 필요 없다. 목표 스택 표시는 중앙 HUD 숫자 하나 대신, 캔버스가 여러 개면 서로 다른 목표값을 가질 수 있어 각 캔버스 자신에게 라벨을 붙이는 방식으로 구현했다 — ColorCanvas.ApplyTargetLabel()이 첫 번째 자식의 -Z면(고정, 빌보드 아님)에 색으로 물들인 R/G/B 숫자로 목표값을 표시한다. 라벨 텍스트 형식은 공용 클래스 StackLabelFormat에 정리돼 있다 — 현재는 캔버스·컬러 필터 모두 정숫값 비교와 동치라(ToRGB가 채널별 절대값 변환이라 색 비교 = 정숫값 비교) "R G B"(공백 구분, ByValue) 형식만 쓴다. 비율(상대값)로 판정하는 기물이 새로 생기면 ByRatio()("R:G:B", 콜론 구분)를 대신 쓰면 된다. 라벨 오브젝트 자동 생성은 필터(FilterBlockBase)만 하고, 캔버스·팔레트는 자식으로 미리 배치해둔 라벨을 찾아 텍스트/위치만 갱신한다(없으면 아무것도 하지 않음). 추가로 필터를 제외한 모든 맵 기물의 화면상 위치를 알려주는 MapObjectMarkerHUD(기획서 미기재, 4장 참고)도 있어 캔버스를 포함한 기물을 찾아가는 데 도움을 준다 — 마커 프리팹은 기본값 하나(defaultMarkerPrefab)를 공유하고, 기물이 자기 markerPrefab(MapObjectBase 공통 필드)을 따로 지정하면 그걸 우선 쓴다(기물 종류별 switch 없이 확장 가능). 가림 판정 레이캐스트가 검사할 레이어(occlusionMask)도 인스펙터에서 조절할 수 있다(기본값 Everything).*

### 3.5 UI (일시정지 · 설정)

- UI는 게임 화면 위에 겹쳐 표시된다.
- 일시정지하면 메뉴(이어하기/처음부터/설정/종료)가 열리고 게임이 멈춘다.
- 설정에서 음량과 마우스 감도를 조절한다 — 설정은 일시정지 메뉴에서 연다.

*구현: UIManager가 UI 전용 씬(UIScene)을 additive로 로드한다. PauseMenuController가 GameManager의 상태(Playing/Paused) 변화와 ESC 입력을 연동해 패널을 토글하고 커서 잠금을 해제하며, SettingsController가 BGM/SFX 볼륨과 마우스 감도 슬라이더를 AudioManager·FirstPersonController에 연결한다. 설정 버튼을 누르면 일시정지 패널이 사라지고 설정 패널이 나오며, 설정의 뒤로가기 버튼(OnBackToPauseButton)으로 다시 일시정지 패널로 돌아간다. 설정 화면에는 저장 초기화 버튼도 있다 — 누르면 바로 지우지 않고 확인창을 띄우고, 확인창에서 다시 확인해야만 실제로 저장 파일이 삭제된다(3.7 참고). '처음부터' 재시작 버튼(OnRestartButton)은 SceneRestarter.RestartCurrentScene()으로 현재 스테이지 씬을 다시 로드한다(DeadZone·클리어 화면의 다시하기 버튼과 동일한 공용 로직, Core 폴더 참고).*

### 3.6 클리어 화면

- 메인화면/다음 스테이지/다시하기 를 선택할수 있다.
- 만약 챕터의 마지막 스테이지에서 다음 스테이지를 선택하면 다음 챕터의 첫번째 스테이지로 이동한다.

*구현: ClearScreenController(UIScene)가 담당한다. LevelManager가 StageCleared를 발행할 때 GameManager.StageClear()로 GameState.Cleared로 전환하는데, 이 상태에서는 PauseMenuController의 ESC 처리(Playing/Paused 기준)가 자동으로 무시된다. ClearScreenController는 StageCleared를 받으면 클리어 패널을 열고 Time.timeScale = 0으로 멈춘다(FirstPersonController가 timeScale 0일 때 입력을 무시하므로 캐릭터 조작도 같이 막힘). PauseMenuController가 상태 변화마다 커서를 되잠그던 버그가 있어(Playing/Paused 전환에서만 커서를 조정하도록 수정), 지금은 Cleared 상태의 커서를 ClearScreenController가 안전하게 관리한다.

"다음 스테이지"로 로드할 씬은 MainMenuController와 함께 참조하는 공용 데이터 애셋 StageTable에서 챕터들을 빈 자리까지 포함해 순서대로 이어붙인 뒤, 현재 씬 바로 다음 자리를 확인한다 — 그 자리에 씬 이름이 있으면 다음 스테이지로(챕터 경계도 다음 챕터 첫 스테이지로 자연스럽게 이어짐), 없으면(리스트 끝이든 챕터 중간에 비어있든) 마지막 스테이지로 취급해 해당 버튼이 비활성화된다. 뜬 채로 시간 제한은 없다.*

### 3.7 진행도 저장

- 클리어한 스테이지, 해금된 챕터를 외부에 저장한다.
- 게임을 실행하면 저장기록을 읽어와 진행사항을 유지한다.

*구현: SaveManager가 JSON으로 저장/불러오기를 담당한다(Application.persistentDataPath의 save.json, 실행 시 자동 로드). SaveData에 unlockedChapterCount(해금된 챕터 수)와 clearedStages(클리어한 씬 이름 리스트)를 추가했다 — 챕터 수에 맞춘 고정 크기 배열이 아니라 씬 이름 리스트라서 StageTable 구성이 나중에 바뀌어도 세이브 데이터가 깨지지 않는다. ProgressManager(5.1 참고)가 StageCleared를 구독해 클리어할 때마다 기록하고 바로 저장한다. 설정 화면(3.5)에서 저장 파일을 초기화할 수도 있다 — SettingsController.OnResetSaveButton()은 바로 지우지 않고 확인창을 띄우며, 확인창에서 다시 확인해야만 SaveManager.Instance.Delete()(파일 삭제 + Current를 새 SaveData()로 초기화)가 실행된다.*

### 3.8 조준+좌클릭 상호작용

- 필터를 제외한 맵 기물(컬러 팔레트·스택 체인저·컬러 체인저·버킷·캔버스)은 걸어서 닿는 것이 아니라, 카메라 정면 1.3칸 이내에서 조준하고 좌클릭해야 상호작용된다.
- 조준 중인 대상이 있으면 조준점 색이 바뀌고, 대상 자체에도 강조 표시(아웃라인)가 나타난다.
- 카메라와 기물 사이에 벽 등 가리는 것이 있으면 조준·상호작용 모두 되지 않는다.

*구현: Player/InteractionController.cs가 매 프레임 카메라 정면으로 1.3칸 레이캐스트를 쏴서 가장 가까운 대상 하나만 판정한다(레이캐스트 특성상 벽이나 닫힌 필터가 앞에 있으면 자연히 막혀 별도 가림 판정이 필요 없음). 레이캐스트가 검사할 레이어(interactMask)는 인스펙터에서 조절할 수 있다(기본값 Everything) — 좁히려면 벽·필터가 있는 레이어는 반드시 포함해야 "가로막히면 조준 안 됨" 동작이 유지된다. 대상이 IInteractable(ClearObjectBase/ConsumableObjectBase가 구현, 필터는 미구현)이면 조준 중 강조 표시를 하고, 좌클릭(InputManager.ReadInteract()) 시 TryInteract()를 호출한다. 이 두 베이스 클래스는 원래 걸어서 닿으면(OnTriggerEnter) 발동했으나, 이제 그 트리거 로직을 제거하고 TryInteract()로만 발동한다(필터는 대상이 아니므로 기존처럼 걸어서 통과하는 방식 그대로 유지). 강조 표시는 MapObjectBase.SetHighlighted(bool)로 처리하며, 라벨과 동일하게 "미리 배치해둔 자식 오브젝트(highlightRoot)를 켜고 끄기만" 한다 — 실제 시각 효과는 Shaders/InteractionHighlightOutline.shader(인버티드 헐 기법 아웃라인)를 쓰는 별도 메시(본체와 같은 메시를 참조, 여러 파츠로 된 기물은 빈 부모 밑에 파츠별로 둠)로 구현한다. 조준 대상 유무는 InteractableTargetChanged 이벤트로 알리고, UI/CrosshairController.cs가 이를 구독해 조준점 색을 바꾼다.*

## 4. 맵 기물

### 4.1 컬러 팔레트

- 플레이어가 컬러 팔레트를 조준하고 좌클릭하면 컬러 팔레트에 지정된 RGB 스택만큼 캐릭터의 RGB 스택이 증가한다.
- 컬러 팔레트는 1회용이다 — 실제로 스택 값이 하나라도 바뀐 경우에만 소모되어 사라진다(바뀐 게 없으면 그대로 남는다).
- 팔레트 자신의 외형 색이 지정된 R/G/B 스택량을 그대로 반영해, 어떤 색을 얼마나 주는지 눈으로 알 수 있다.
- 정확한 R/G/B 숫자도 텍스트로 함께 표시된다(컬러 필터와 같은 리치 텍스트 형식).
- 팔레트는 자신의 +Y축(정면)이 항상 플레이어(카메라)를 향하도록 계속 회전한다.
- 텍스트 라벨은 팔레트 내부의 고정된 위치에 있으며, 팔레트가 회전할 때 같이 따라 움직인다.

*구현: ColorPalette(StackModifierConsumable 기반)가 담당한다. 조준+좌클릭 상호작용(3.8 참고) 시 ApplyToStacks()에서 지정된 R/G/B만큼 ColorStacks.Add를 호출한다. 스택 체인저·컬러 체인저·버킷과 같은 규칙으로, 호출 전후 R/G/B 값을 비교해 실제로 하나라도 바뀐 경우에만 소모되어 사라진다(4.4~4.6 공통 로직 참고, 바뀐 게 없으면 그대로 남아 다시 클릭할 수 있다). Awake와 인스펙터 값 변경(OnValidate) 시 ColorStacks.ToRGB(필터와 같은 변환식)로 지정 R/G/B를 색으로 바꿔 인스펙터에서 지정한 stackColorRenderers 목록에 MaterialPropertyBlock으로 입힌다(메시 병합은 하지 않음, 블록별로 개별 표시). LateUpdate에서 `Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f)`로 로컬 +Y축이 카메라 방향을 향하도록 회전시킨다(기본 LookRotation은 +Z를 정면으로 삼으므로, 추가로 X축 기준 90도 돌려 +Y가 정면이 되게 만든다). 라벨 오브젝트 자동 생성은 필터(FilterBlockBase)만 하므로, 팔레트는 Start()에서 자식으로 미리 배치해둔 BillboardCenterLabel(CellGroupLabel의 하위 클래스)을 찾아 텍스트만 갱신한다(없으면 아무것도 안 함) — 컬러 필터와 동일한 리치 텍스트(R/G/B 숫자를 각 색으로 물들임)를 보여준다. BillboardCenterLabel은 더 이상 위치·회전을 매 프레임 재계산하지 않고 텍스트 표시 여부만 관리한다 — 팔레트 자신이 회전하므로 라벨은 에디터에서 배치한 고정 위치에 자식으로 붙어 자연스럽게 같이 회전한다.*

### 4.2 컬러 필터

- 플레이어 컬러와 컬러 필터의 컬러가 동일한 경우에만 통과할수 있다.
- 컬러 필터는 플레이어가 통과하여도 사라지지 않는다.
- 같은 색끼리 맞닿아 이어진 필터 덩어리마다 요구하는 R/G/B 스택 값을 텍스트로 하나만 표시해 플레이어가 한눈에 알아볼 수 있다(테두리 색 + 텍스트를 함께 사용).
- 텍스트는 플레이어(카메라)와 가장 가까운 블록의, 카메라를 향한 면 위에 나타난다.
- 플레이어가 지금 통과 가능한 상태면 필터의 면(채움, 테두리 제외)이 투명해지고, 통과 불가능해지면 다시 원래대로 보인다.
- 테두리는 필터 자기 자신의 채움(fill) 메시에 가려지지 않고 항상 그 위에 보인다(벽 등 다른 오브젝트에는 정상적으로 가려짐).

*구현: ColorFilterBlock(FilterBlockBase 기반)이 담당한다. 매 프레임 폴링하는 대신 ColorStackChanged 이벤트(플레이어 스택이 바뀔 때만 발행)를 구독해, 값이 바뀔 때만 플레이어의 변환 RGB와 필터의 RGB를 비교한다 — 같으면 콜라이더를 트리거로 전환(통과 허용)하고 다르면 솔리드로 되돌린다. 플레이어가 완전히 통과하면(OnTriggerExit) ColorStacks.ResetAll()을 호출해 스택을 초기화한다. 메시 병합·테두리 로직은 FilterBlockBase에 내장되어 있어 별도 매니저 컴포넌트 없이 필터 스크립트만으로 동작한다 — 같은 색 필터들을 우선 색상으로 묶고, 그중에서도 6방향으로 실제 맞닿아 이어진 덩어리끼리만 다시 나눠(멀리 떨어진 같은 색 블록은 별개 그룹) 채움(fill)·테두리(border) 메시 한 쌍으로 합친다. 내부 면은 생기지 않고, 통합된 메시의 바깥 모서리에만 불투명한 테두리를 둘러 "테두리가 무지개색"을 시각적으로 표현한다 — 같은 그룹 블록끼리 맞닿아 이어지는 내부 경계에는 테두리가 생기지 않는다(콜라이더·통과 로직은 블록별로 유지). RGB 필터와 판정 로직만 다른 형제 클래스로 분리되어 있다(FilterBlockBase). 라벨은 그룹(덩어리)당 하나만 생성되며(범용 컴포넌트 CellGroupLabel), 매 프레임 그룹의 칸(셀) 중 카메라와 가장 가까운 칸을 찾아 그 칸의 카메라 쪽 면 위에 배치한다(라벨의 위치·회전만 카메라가 움직일 수 있으므로 매 프레임 갱신, 통과 판정과는 별개). 회전은 카메라를 따라 도는 빌보드가 아니라 그 면의 바깥 법선 방향으로 고정된다. 텍스트 내용은 R/G/B 숫자만(각 숫자를 해당 색으로 물들인 리치 텍스트) 표시해, RGB 필터(라벨 없이 테두리만 사용)와 시각적으로 구분된다. 라벨 오브젝트는 `FilterBlockBase.labelPrefab` 필드로 그룹(첫 블록 기준)마다 지정한 프리팹을 인스턴스화해서 쓸 수 있다 — 비워두면 기존처럼 코드에서 기본 스타일(폰트 크기 3, 가운데 정렬, 흰색)로 새로 만든다. 프리팹을 지정하면 TextMeshPro나 CellGroupLabel 컴포넌트가 없어도 자동으로 붙여준다. ColorStackChanged 이벤트가 올 때마다 그룹의 채움 메시 알파를 0(통과 가능)과 원래 gateAlpha(통과 불가) 사이로 전환한다(테두리 메시는 별도 렌더러라 영향받지 않음). 테두리 머티리얼은 `FilterBlockBase.borderMaterial` 필드로 그룹(첫 블록 기준)마다 지정할 수 있다 — 비워두면 기존처럼 전용 셰이더(Assets/Shaders/FilterBorderAlwaysVisible.shader, Custom/FilterBorderAlwaysVisible)로 자동 생성한 공용 머티리얼을 쓴다. 이 기본 셰이더는 렌더 큐를 채움보다 뒤(Transparent+10)로 둬서 항상 채움 위에 그려지게 한다 — 깊이 테스트 자체는 기본값(LEqual)이라 벽 등 다른 오브젝트에는 정상적으로 가려진다. 인스펙터에서 값이 바뀔 때(OnValidate)의 RebuildAll() 호출은 TMP가 라벨 생성 중 DestroyImmediate를 호출해 경고가 뜨는 것을 막기 위해 EditorApplication.delayCall로 한 프레임 미뤄 실행한다(에디터 전용).*

### 4.3 RGB 필터

- 플레이어의 RGB 스택 중 RGB 필터가 지정한 색이 동률 없이 유일한 최댓값일 경우에만 통과할 수 있다(다른 색과 값이 같으면 통과 불가).
- RGB 필터는 플레이어가 통과하여도 사라지지 않는다.
- 요구하는 색을 테두리의 순수 원색(빨강/초록/파랑)만으로 알려준다(별도 텍스트 없음).
- 플레이어가 지금 통과 가능한 상태면 필터의 면(채움, 테두리 제외)이 투명해지고, 통과 불가능해지면 다시 원래대로 보인다(컬러 필터와 동일).

*구현: RgbFilterBlock(FilterBlockBase 기반)이 담당한다. ColorStacks.GetMaxColors()로 최댓값 채널 목록을 받아, 그 목록의 개수가 정확히 1개이고 그 하나가 필터의 지정 색과 같을 때만 통과 가능으로 판정한다(동률이면 목록에 2개 이상 담기므로 자동으로 통과 불가 처리됨). 통과·리셋·외형·메시 병합·테두리 흐름은 컬러 필터와 동일(FilterBlockBase 공통) — 외형은 지정 채널의 순수 원색(빨강/초록/파랑)이며, 컬러 필터와 같은 색·같은 덩어리끼리 채움·테두리 메시로 자동 합쳐진다(FilterBlockBase 내장 로직, 별도 컴포넌트 불필요). 텍스트 라벨은 만들지 않아(GetLabelText가 null 반환) 컬러 필터(테두리+텍스트)와 시각적으로 구분된다.*

### 4.4 스택 체인저

- 플레이어가 스택 체인저를 조준하고 좌클릭하면 체인저에 지정된 두 색과 동일한 색의 RGB스택 값이 서로 변경된다.
- 스택 체인저는 실제로 스택 값이 바뀐 경우에만 사라진다(바뀐 게 없으면 그대로 남는다).
- 자식으로 붙은 작은 구 2개 중 0번째는 플레이어의 현재 색을, 1번째는 발동 시 변경될 색(미리보기)을 보여준다.

*구현: StackChanger(PreviewingStackModifier 기반)가 담당한다. 지정된 두 색의 현재 값을 서로 SetValue로 맞바꾼다. 미리보기 갱신 시점(ColorStackChanged·SceneLoadCompleted 구독 + Start 초기 1회)은 PreviewingStackModifier가 공통으로 처리하고, StackChanger는 RefreshPreview()에서 결과색만 계산한다(currentColorRenderers = Player.CurrentRGB, resultColorRenderers = colorA·colorB 값만 서로 바꿔서 미리 계산한 결과색).*

### 4.5 컬러 체인저

- 플레이어가 컬러 체인저를 조준하고 좌클릭하면 플레이어의 각 색상 스택을 (세 스택 중 최댓값 - 해당 색상의 현재 값)으로 변경한다.
- 컬러 체인저는 실제로 스택 값이 바뀐 경우에만 사라진다(바뀐 게 없으면 그대로 남는다).
- 자식으로 붙은 작은 구 2개 중 0번째는 플레이어의 현재 색을, 1번째는 발동 시 변경될 색(미리보기)을 보여준다.

*구현: ColorChanger(PreviewingStackModifier 기반)가 담당한다. 세 스택 중 최댓값을 구한 뒤 각 채널에 SetValue(max - 현재값)를 적용한다. 미리보기 갱신 시점은 StackChanger와 마찬가지로 PreviewingStackModifier가 공통으로 처리하고, ColorChanger는 RefreshPreview()에서 결과색만 계산한다(currentColorRenderers = Player.CurrentRGB, resultColorRenderers = 변환식으로 미리 계산한 결과색).*

### 4.6 버킷

- 플레이어가 버킷을 조준하고 좌클릭하면 버킷에 지정된 색과 동일한 색의 RGB스택 값을 0으로 만든다.
- 버킷은 실제로 스택 값이 바뀐 경우에만 사라진다(이미 0인 색을 지정했다면 그대로 남는다).
- 버킷 자신의 색은 지정된 타겟 색을 그대로 보여준다.

*구현: Bucket(StackModifierConsumable 기반)이 담당한다. 지정된 색에 SetValue(0)을 적용한다. Start()와 인스펙터 값 변경(OnValidate) 시 targetColor의 순수 원색을 인스펙터에서 지정한 stackColorRenderers 목록에 MaterialPropertyBlock으로 입힌다.*

*공통(4.1, 4.4~4.6): ConsumableObjectBase에 ShouldConsume() 가상 메서드가 추가되었고, StackModifierConsumable이 이를 오버라이드해 Apply() 전후로 R/G/B 값을 비교한 뒤 실제로 하나라도 바뀐 경우에만 true를 반환한다 — 아무 변화가 없으면 소모되지 않고 남아있는다. 컬러 팔레트·스택 체인저·컬러 체인저·버킷 모두 StackModifierConsumable을 상속해 이 1회용 규칙을 공유한다(팔레트만 예외적으로 카메라를 향해 회전하는 애니메이션이 있다, 4.1 참고 — 스택 체인저·컬러 체인저·버킷은 배치된 그대로 고정).*

### 4.7 캔버스

- 플레이어가 캔버스를 조준하고 좌클릭시 플레이어의 RGB 스택 값이 캔버스에 지정된 스택 값과 정확히 일치할 경우 맵을 클리어 할수 있다.

*구현: ColorCanvas(ClearObjectBase 기반)가 담당한다. 조준+좌클릭 상호작용(3.8 참고) 시점에 스택 값이 목표와 정확히 일치하면 완료 상태로 잠기고(재판정 없음) CanvasCompleted 이벤트를 발행한다. 캔버스가 여러 개면 각각 순차로 완료하면 된다(동시에 만족할 필요 없음) — LevelManager가 모든 캔버스의 완료 여부를 모아 StageCleared를 발행한다.*

### 4.8 튜토리얼 문구

- 맵 중간 특정 위치에 안내용 텍스트가 항상 고정된 위치·방향으로 떠 있다(플레이어를 따라 회전하지 않음).

*구현: 3D 공간에 배치한 TextMeshPro 오브젝트로 구현한다 — 위치와 회전 모두 고정값이라 게임 로직 스크립트는 필요 없다. 다만 하이어라키 정리 도구(MapObjectOrganizer, 5.2 참고)가 다른 맵 기물과 함께 이 오브젝트를 찾아 MapObjects 폴더로 모을 수 있도록, 아무 동작도 하지 않는 식별용 컴포넌트 TutorialTextMarker만 붙여둔다. 한글 표시를 위해 Dynamic Atlas Population Mode의 TMP 폰트 애셋을 사용한다(전체 한글 음절 11,172자를 Static으로 미리 굽기엔 너무 많아 실패하므로, 런타임에 필요한 글리프만 채우는 Dynamic 모드를 쓴다).*

## 5. 레벨 구성

### 5.1 챕터 구성

- 10개의 스테이지를 하나의 챕터로 구성한다.
- 각 챕터마다 기물들이 하나씩 추가된다.

*구현: LevelManager가 CanvasCompleted 이벤트를 구독해 씬 내 모든 ColorCanvas의 완료 여부를 확인, 전부 완료되면 StageCleared를 발행한다. ProgressManager가 이 이벤트를 받아 저장과 챕터 해금을 처리한다(5.2 참고).*

### 5.2 스테이지 구성

- 각 챕터별로 튜토리얼 스테이지 2개, 노멀 스테이지 3개, 응용 스테이지 3개, 챌린지 스테이지 2개로 구성한다.
- 각 챕터에서 응용 스테이지까지 통과한 경우 다음 챕터가 해금이 된다.
- 챌린지 스테이지에서는 캔버스가 2개가 되어서 전부 완성해야 한다.

*구현: 미로 블록은 런타임 절차 생성이 아니라 MazeGeneratorEditor(에디터 전용 Scene 뷰 툴)로 배치되어 씬에 직접 저장된다(클릭 설치/Shift+클릭 제거, Ctrl+드래그로 직사각형 범위 설치/제거, 정수 그리드 스냅). 기본 블록(큐브) 대신 자유 프리팹 칸에 아무 기물 프리팹이나 끌어 넣어 설치할 수 있고, 그중 자주 쓰는 3종류(기본 블록/컬러 필터/RGB 필터)는 라디오 버튼으로 바로 고를 수 있다 — 컬러 필터·RGB 필터를 고르면 설치 시 적용할 R/G/B 값(또는 목표 색)도 미리 지정해둘 수 있다. 설치된 필터·기물은 Maze가 아니라 씬 바로 아래 MapObjects 폴더에 자동으로 모이며, 필터는 그중에서도 메시가 병합되는 것과 같은 기준(같은 색 + 6방향 인접, FilterClusterOrganizer 공용 로직)으로 ColorFilterN/RGBFilterN 하위 폴더에 묶인다 — 필터를 설치·제거할 때마다 폴더 구성과 병합 메시가 함께 갱신된다. 이미 씬에 있는 기물이나 튜토리얼 텍스트(4.8 참고)를 한 번에 다시 정리하고 싶을 때는 메뉴 ColorMaze › 특수 블록 하이어라키 정리(MapObjectOrganizer)를 수동으로 실행하면 된다. 캔버스 여러 개 조건은 ColorCanvas의 순차 완료 방식으로 지원된다(4.7 참고). 스테이지 클리어 후 챕터·스테이지 해금은 ProgressManager(Level/ProgressManager.cs)가 담당한다 — StageTable(각 챕터의 스테이지 씬 이름 배열, Resources 폴더에 있어 Resources.Load로 불러옴)에서 클리어한 씬이 챕터의 배열 인덱스 7(8번째, 응용 스테이지 마지막)이면 다음 챕터를 해금한다. 챕터 안의 개별 스테이지는 별도 저장 없이 clearedStages만으로 판정한다 — 0번째 스테이지는 챕터가 해금돼 있으면 항상 열려있고, 나머지는 바로 앞 스테이지가 클리어돼 있어야 열린다. MainMenuController가 챕터/스테이지 버튼의 interactable을 이 판정에 맞춰 갱신해 잠긴 항목은 회색으로 비활성화된다.*

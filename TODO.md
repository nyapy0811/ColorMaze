# ColorMaze 구현 투두리스트

`ColorMaze 기획서.md` 기준으로, 현재 코드가 기획서 수준을 따라가지 못하는 부분을 정리한 목록. 각 항목은 관련 기획서 절과 담당(예정) 스크립트를 함께 표기했다.

## 1순위 — 기반 규칙 확정

- [x] **RGB 스택 오버플로우 규칙 구현** (3.3) — `ColorStacks.cs`에 0~31 범위 모듈러 순환 적용 완료.
- [x] **스택 상한값 통일** (3.3) — `Config.max` 기본값을 31로 변경해 기획서와 일치시킴.
- [x] **오버플로우 버그 수정** (3.3) — 이전 `Wrap` 공식이 하한을 `max-31`로 계산해, `max`가 31이 아닌 값(예: 10)이면 10 초과 시 음수(예: -21)로 튀는 문제가 있었음. 하한을 항상 0으로 고정하고 폭을 `max+1`로 바꿔, `max`를 무엇으로 설정하든 항상 [0, max] 범위로 순환하도록 수정.

## 2순위 — 맵 기물 6종 구현 (4장)

설계: `맵기물_구현설계.md` 참고. 아래 항목 모두 구현 완료.

- [x] **ColorStacks API 확장** — `SetValue(LightColor, int)`(절대값 지정), `GetMaxColors()`(최댓값 채널 목록) 추가.
- [x] **FilterBlockBase 추출 + RGB 필터** (4.2, 4.3) — `ColorFilterBlock`을 `FilterBlockBase`로 리팩터링, `RgbFilterBlock` 신규 구현.
- [x] **ConsumableObjectBase/StackModifierConsumable 베이스 + 스택 체인저/컬러 체인저/스포이드** (4.4~4.6) — `StackChanger`, `ColorChanger`, `Dropper` 구현.
- [x] **컬러 팔레트** (4.1) — `AcquireObjectBase` 기반 `ColorPalette` 구현.
- [x] **캔버스(`ColorCanvas`)** (4.7) — `ClearObjectBase` 기반, 완료 시 `CanvasCompleted` 발행.

- [x] RGB 필터(`RgbFilterBlock`) 외형·메시 병합 — `FilterBlockBase`가 컬러 필터·RGB 필터를 색상 기준으로 함께 그룹화.
- [x] 필터 테두리 렌더링 — 노출된 외부 면마다 채움(fill)/테두리(border) 메시를 분리 생성. `borderWidth`/`borderAlpha`로 두께·불투명도 조절 가능.
- [x] **GateMeshCombiner를 FilterBlockBase로 흡수** — 별도 매니저 컴포넌트 없이 필터 스크립트만으로 메시 병합·테두리 렌더링이 동작하도록 정적 `RebuildAll()`로 통합. `GateMeshCombiner.cs`/`Editor/GateMeshCombinerEditor.cs`는 삭제 권한 문제로 내용만 비워둔 상태(`!!!!` 접두사 붙여둠) — Unity에서 직접 삭제 필요(둘 다 파일+`.meta`).
- [x] 필터 테두리, 통합 메시 바깥 모서리에만 그리도록 수정 — 같은 그룹 블록끼리 맞닿은 내부 경계는 테두리 없이 이어 붙임(`FilterBlockBase.BuildGroup`의 per-edge 경계 판정).
- [x] **같은 색이라도 물리적으로 떨어진 덩어리는 별도 그룹으로 분리** — `SplitConnected`가 6방향 연결 기준 flood fill로 색상 그룹을 다시 나눔. 서로 멀리 떨어진 같은 색 필터가 하나의 메시/라벨로 뒤섞이는 문제를 해결.
- [x] **필터 기준값 라벨** — `FilterBlockBase`가 그룹(덩어리)당 하나의 월드스페이스 TextMeshPro 라벨을 자동 생성(`GetLabelText()`가 null/빈 문자열이면 생성 안 함). `ColorFilterBlock`은 테두리(스택 색) + R/G/B 숫자만(각 숫자를 해당 색으로 물들인 리치 텍스트) 표시, `RgbFilterBlock`은 텍스트 없이 테두리(순수 원색)만으로 구분되도록 함.
- [x] **그룹 라벨 위치** — `FilterGroupLabel`이 매 프레임 그룹의 칸(셀) 중 카메라와 가장 가까운 칸을 찾아 그 칸의 카메라 쪽 면 위에 라벨을 배치. 회전은 빌보드가 아니라 그 면의 바깥 법선 방향으로 고정(카메라를 따라 돌지 않음).

남은 확인 사항:
- [ ] 각 기물 프리팹 제작 및 인스펙터 값(팔레트 증가량, 체인저 대상 색, 캔버스 목표값 등) 설정 — 스크립트는 완료됐으나 씬에 배치할 프리팹/블록은 아직 없음.

## 3순위 — 진행/레벨 시스템 (5장, 3.7)

- [x] **LevelManager 구현** (5.1) — `CanvasCompleted`를 구독해 씬 내 모든 `ColorCanvas` 완료 시 `StageCleared` 발행. 씬 전환 시(`SceneLoadCompleted`) 캔버스 목록 재탐색.
- [ ] **SaveData 확장** (3.7) — `SaveData.cs`에 클리어한 스테이지·해금된 챕터 필드 추가(현재는 level/gold/playTime뿐). `StageCleared`를 구독해 저장하도록 연결.
- [ ] **챕터 해금 로직** (5.1) — `StageCleared` 발행 이후 응용 스테이지까지 클리어 시 다음 챕터 해금하는 처리는 아직 없음(현재는 이벤트 발행까지만 구현).
- [x] **챌린지 스테이지 다중 캔버스 조건** (5.2) — 캔버스 여러 개를 각각 순차 완료(잔금)하면 전체 클리어되는 로직 구현(`ColorCanvas`/`LevelManager`).

## 4순위 — UI 화면

- [ ] **메인 화면** (3.1) — 스테이지 선택(챕터·스테이지 목록), 설정, 종료.
- [ ] **클리어 화면** (3.6) — 메인화면/다음 스테이지/다시하기 선택, 챕터 마지막 스테이지에서 '다음 스테이지' 선택 시 다음 챕터 첫 스테이지로 이동.
- [ ] **HUD 목표 스택 표시** (3.4) — `ColorStackHUD`에 현재 스테이지의 목표(캔버스) 스택 값 표시 추가.
- [ ] **일시정지 메뉴 '처음부터' 버튼** (3.5) — `PauseMenuController`에 재시작(스테이지 초기화) 기능 추가.

## 폴더 구조

`Assets/Scripts`를 역할별로 정리함(각 .cs와 .cs.meta를 함께 이동해 GUID·씬 참조 보존):

- `Core/` — Bootstrap
- `Player/` — FirstPersonController, InputManager, ColorStacks, ColorStackInput
- `MapObjects/` — 베이스 클래스 6종(`MapObjectBase`, `FilterBlockBase`, `AcquireObjectBase`, `ClearObjectBase`, `ConsumableObjectBase`, `StackModifierConsumable`) + 기물 7종(ColorFilterBlock, RgbFilterBlock, ColorPalette, StackChanger, ColorChanger, Dropper, ColorCanvas). 옛 `GateMeshCombiner`는 `FilterBlockBase`로 기능이 흡수되어 내용을 비워두고 `!!!!GateMeshCombiner.cs`로 이름을 바꿔둠(삭제 권한 문제로 직접 못 지움, Unity에서 파일+`.meta` 삭제 필요 — 이름 앞 `!!!!`가 삭제 대상 표시).
- `Level/` — LevelManager, MazeGenerator
- `UI/` — UIManager, ColorStackHUD, HUDController, PauseMenuController, SettingsController
- `Editor/` — MazeGeneratorEditor, `MapObjectOrganizer`(메뉴 `ColorMaze/특수 블록 하이어라키 정리` — 씬의 특수 블록을 타입별 폴더로 재배치, 위치는 그대로 유지). 옛 `GateMeshCombinerEditor`도 같은 이유로 `!!!!GateMeshCombinerEditor.cs`로 이름을 바꾸고 내용을 비워둠(삭제 필요).

## 참고

- 이미 기획서 수준으로 구현된 부분: 이동/카메라(`FirstPersonController`), 입력(`InputManager`), RGB 스택 코어 로직과 이벤트(`ColorStacks`, EventBus), 맵 기물 7종(`MapObjectBase` 계층 — `ColorFilterBlock`, `RgbFilterBlock`, `ColorPalette`, `StackChanger`, `ColorChanger`, `Dropper`, `ColorCanvas`, 메시 병합/테두리/라벨 로직은 `FilterBlockBase`에 내장), 일시정지·설정 UI 기본 동작(`PauseMenuController`, `SettingsController`), 저장 시스템 뼈대(`SaveManager`), 미로 블록 배치 툴(`MazeGeneratorEditor`), 특수 블록 하이어라키 정리 툴(`MapObjectOrganizer`), 스테이지 클리어 감지(`LevelManager`).
- **씬 인스펙터 다중 편집**: 같은 타입(예: `ColorFilterBlock`)의 블록 여러 개를 하이어라키에서 함께 선택하면(Shift/Ctrl+클릭) Inspector가 자동으로 다중 편집 모드로 바뀌어 값 하나를 입력하면 선택된 전부에 적용된다(Unity 기본 기능, 커스텀 Editor 없이 동작). `MapObjectOrganizer`로 타입별 폴더에 모아두면 이 다중 선택이 쉬워진다.
- 우선순위는 기반(스택 규칙) → 기물 → 진행 시스템 → UI 순으로, 기물이 스택 규칙에 의존하고 진행 시스템이 기물 클리어 판정에 의존하는 순서를 따랐다.

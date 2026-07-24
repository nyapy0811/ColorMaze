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
- [x] **ConsumableObjectBase/StackModifierConsumable 베이스 + 스택 체인저/컬러 체인저/지우개** (4.4~4.6) — `StackChanger`, `ColorChanger`, `Eraser` 구현.
- [x] **컬러 팔레트** (4.1) — `AcquireObjectBase` 기반 `ColorPalette` 구현. 지정된 R/G/B 스택량을 `ColorStacks.ToRGB`로 변환해 자신의 렌더러 색에 그대로 반영(Awake/OnValidate). `Start()`에서 `CellGroupLabel`의 하위 클래스 `BillboardCenterLabel`을 단일 칸짜리 그룹으로 생성해 컬러 필터와 동일한 형식의 R/G/B 숫자 텍스트를 표시하되, 필터와 달리 오브젝트 중심에 고정되어 항상 카메라를 바라보게 함(면 고정 X, 빌보드).
- [x] **캔버스(`ColorCanvas`)** (4.7) — `ClearObjectBase` 기반, 완료 시 `CanvasCompleted` 발행.

- [x] RGB 필터(`RgbFilterBlock`) 외형·메시 병합 — `FilterBlockBase`가 컬러 필터·RGB 필터를 색상 기준으로 함께 그룹화.
- [x] 필터 테두리 렌더링 — 노출된 외부 면마다 채움(fill)/테두리(border) 메시를 분리 생성. `borderWidth`/`borderAlpha`로 두께·불투명도 조절 가능.
- [x] **GateMeshCombiner를 FilterBlockBase로 흡수** — 별도 매니저 컴포넌트 없이 필터 스크립트만으로 메시 병합·테두리 렌더링이 동작하도록 정적 `RebuildAll()`로 통합. `GateMeshCombiner.cs`/`Editor/GateMeshCombinerEditor.cs`는 삭제 권한 문제로 내용만 비워둔 상태(`!!!!` 접두사 붙여둠) — Unity에서 직접 삭제 필요(둘 다 파일+`.meta`).
- [x] 필터 테두리, 통합 메시 바깥 모서리에만 그리도록 수정 — 같은 그룹 블록끼리 맞닿은 내부 경계는 테두리 없이 이어 붙임(`FilterBlockBase.BuildGroup`의 per-edge 경계 판정).
- [x] **같은 색이라도 물리적으로 떨어진 덩어리는 별도 그룹으로 분리** — `SplitConnected`가 6방향 연결 기준 flood fill로 색상 그룹을 다시 나눔. 서로 멀리 떨어진 같은 색 필터가 하나의 메시/라벨로 뒤섞이는 문제를 해결.
- [x] **필터 기준값 라벨** — `FilterBlockBase`가 그룹(덩어리)당 하나의 월드스페이스 TextMeshPro 라벨을 자동 생성(`GetLabelText()`가 null/빈 문자열이면 생성 안 함). `ColorFilterBlock`은 테두리(스택 색) + R/G/B 숫자만(각 숫자를 해당 색으로 물들인 리치 텍스트) 표시, `RgbFilterBlock`은 텍스트 없이 테두리(순수 원색)만으로 구분되도록 함.
- [x] **그룹 라벨 위치** — 매 프레임 그룹의 칸(셀) 중 카메라와 가장 가까운 칸을 찾아 그 칸의 카메라 쪽 면 위에 라벨을 배치. 회전은 빌보드가 아니라 그 면의 바깥 법선 방향으로 고정(카메라를 따라 돌지 않음).
- [x] **라벨 기능을 별도 스크립트로 분리** — 셀 그룹 위에 텍스트를 띄우는 로직을 `FilterBlockBase`에서 떼어내 `MapObjects/CellGroupLabel.cs`(범용 컴포넌트)로 독립. 위치·회전 계산이 `protected virtual` 메서드로 열려 있어, 필터가 아닌 다른 기물도 필요하면 상속해서 재사용할 수 있다. `FilterBlockBase`는 `CellGroupLabel.Create(...)`를 호출하는 정도로만 사용. `Create<T>(...)` 제네릭 팩토리도 추가해 하위 클래스로 바로 생성 가능(`ColorPalette`가 `Create<BillboardCenterLabel>(...)`로 사용).
- [x] **오브젝트 중심에서 항상 카메라를 바라보는 라벨 변형** — `CellGroupLabel`을 상속한 `BillboardCenterLabel` 추가. 면에 고정되는 기본 동작과 달리 `LateUpdate()`를 오버라이드해 위치는 항상 등록된 칸의 중심, 회전은 항상 카메라를 향하도록(빌보드) 함. `ColorPalette`의 라벨에 사용.
- [x] **통과 가능할 때 채움 메시 투명화** — 그룹의 채움(fill) 메시 알파를 0(통과 가능)과 원래 `gateAlpha`(통과 불가) 사이로 전환. 그룹의 여러 블록이 같은 `fillRenderer`를 공유하도록 `BuildGroup`에서 배정하며(빌드 때의 `gateAlpha`도 함께 캐싱), 테두리 메시는 별도 렌더러라 영향받지 않는다.
- [x] **매 프레임 폴링 제거 → 이벤트 기반으로 전환** — `FilterBlockBase`가 더 이상 `Update()`에서 매 프레임 `Matches()`를 재계산하지 않고, `ColorStackChanged` 이벤트(플레이어 스택이 실제로 바뀔 때만 발행)를 구독해 그때만 콜라이더 트리거 상태와 채움 메시 투명도를 갱신(`Refresh()`). 초기 상태는 `Start()`에서 한 번 직접 반영. 라벨 위치만 카메라가 움직이므로 `CellGroupLabel` 쪽은 여전히 `LateUpdate()`로 매 프레임 갱신.
- [x] **스테이지 시작 시에도 재판정** — `Framework.Core.SceneLoadCompleted` 이벤트도 함께 구독해 `Refresh()` 호출(`LevelManager`가 같은 이벤트로 캔버스 목록을 새로고침하는 것과 동일한 패턴). `Start()` 시점엔 플레이어 스택이 아직 이번 스테이지용으로 정리되지 않았을 수 있어, 씬 로드가 끝난 뒤 한 번 더 확인한다.
- [x] **필터 테두리, 채움 메시에 스스로 가려지는 문제 수정** — 테두리 전용 셰이더(`Assets/Shaders/FilterBorderAlwaysVisible.shader`, `Custom/FilterBorderAlwaysVisible`)를 새로 만들어 렌더 큐를 채움보다 뒤(`Transparent+10`)로 둠. 깊이 테스트는 기본값(LEqual)이라 벽 등 다른 오브젝트에는 정상적으로 가려지고, 그리는 순서(페인터 알고리즘)만 강제해 자기 자신의 채움 메시에 테두리가 덮이는 문제만 해결. `FilterBlockBase.BuildGroup`이 테두리 메시에만 이 머티리얼을 사용(채움은 기존 `gateMaterial` 그대로).
- [x] **`OnValidate` 콘솔 경고 제거** — `FilterBlockBase.OnValidate()`가 즉시 `RebuildAll()`을 호출하면 TMP가 라벨 생성 중 `DestroyImmediate`를 호출해 경고가 떴음(OnValidate 도중 금지된 호출). `#if UNITY_EDITOR`로 감싸고 `EditorApplication.delayCall`로 한 프레임 미뤄 실행하도록 수정(빌드에는 영향 없음).
- [x] **부유(FloatingBob) 애니메이션 추가** — `MapObjects/FloatingBob.cs`(범용 컴포넌트, 로컬 Y축 sin 파형으로 위아래 이동, 진폭/속도 인스펙터 조절). `ColorPalette`/`StackChanger`/`Eraser`에 `[RequireComponent(typeof(FloatingBob))]`로 연결. `BillboardCenterLabel`은 그리드 셀 대신 부모(팔레트)의 실제 월드 위치를 매 프레임 따라가도록 수정해, 팔레트가 떠도 라벨이 같이 움직임.
- [x] **1회성 기물, 실제 스택 변화가 있을 때만 소모** — `ConsumableObjectBase`에 `protected virtual bool ShouldConsume() => true;` 추가하고 `Apply()` 이후 이 값이 참일 때만 `Consume()`을 호출하도록 변경. `StackModifierConsumable`이 이를 오버라이드해 `ApplyToStacks()` 전후 R/G/B 값을 비교, 실제로 바뀐 게 있을 때만 소모되도록 함(스택 체인저·컬러 체인저·지우개 공통 적용, 컬러 팔레트는 원래부터 안 사라지는 기물이라 무관).
- [x] **스포이드 → 지우개로 이름 변경** — `Dropper.cs`를 `Eraser.cs`로 리네임(클래스명 포함), 관련 스크립트 주석·기획서·TODO·구현설계 문서 전체 반영.
- [x] **지우개 색 순환 애니메이션** — `Eraser.Update()`에서 sin 곡선(기본 2초 주기, `cycleDuration`)으로 `targetColor`와 검정 사이를 `Color.Lerp`해 렌더러에 적용. `OnValidate`에서도 기본 색을 적용해 플레이 전 인스펙터 미리보기도 지원.
- [x] **스택 체인저 자식 구 2개에 교환 대상 색 표시** — `StackChanger.Start()`/`OnValidate()`에서 0번째/1번째 자식 구 렌더러에 각각 `colorA`/`colorB` 색을 `MaterialPropertyBlock`으로 적용.
- [x] **스택 체인저·컬러 체인저, 플레이어를 바라보며 회전** — 회전 로직을 `StackModifierConsumable`을 상속하는 새 중간 클래스 `MapObjects/SpinningStackModifier.cs`로 추출(기존에 `StackChanger`에만 있던 `LateUpdate`를 별도 스크립트로 분리). `StackChanger`·`ColorChanger`가 `StackModifierConsumable` 대신 `SpinningStackModifier`를 상속해 공유(지우개는 상속하지 않음, 대신 색 순환 사용).
- [x] **컬러 체인저 자식 구 2개에 현재/변경 예정 색 미리보기** — 0번째 구 = `Player.CurrentRGB`(현재 플레이어 색), 1번째 구 = 변환식으로 미리 계산한 결과색. `ColorStackChanged`(스택 변경 시)·`SceneLoadCompleted`(스테이지 시작 시) 이벤트를 구독해 그때만 갱신하고 `Start()`에서 초기 1회도 반영(매 프레임 갱신 아님, `FilterBlockBase`와 동일한 이벤트 기반 패턴).
- [x] **맵 진입 시 필터 통과 판정이 반영 안 되는 버그 수정** — 씬에 필터가 여러 개면 각 필터의 `Start()`가 전부 정적 `RebuildAll()`을 호출해 모든 그룹 메시를 통째로 새로 만드는데, 기존에는 자기 자신만 `Refresh()`해서 가장 나중에 `Start()`가 실행된 필터의 그룹만 정확한 투명도가 반영되고 나머지는 방금 새로 만들어진 기본(불투명) 상태로 남아 있었음. `RefreshAll()`(씬의 모든 필터를 한 번에 `Refresh()`)을 추가해 `Start()`와 `OnValidate()`(플레이 모드 중) 양쪽에서 `RebuildAll()` 직후 호출하도록 수정 — 특히 `OnValidate`는 Play 모드에서도(에디터가 컴포넌트를 재검증할 때) 발동해 메시를 불투명 기본값으로 되돌릴 수 있어, 여기서도 `RefreshAll()`을 호출해야 함.

- [x] **각 기물 프리팹 제작** — 구상했던 모든 기물의 프리팹 완성(인스펙터 값 설정 포함).

## 3순위 — 진행/레벨 시스템 (5장, 3.7)

- [x] **LevelManager 구현** (5.1) — `CanvasCompleted`를 구독해 씬 내 모든 `ColorCanvas` 완료 시 `StageCleared` 발행. 씬 전환 시(`SceneLoadCompleted`) 캔버스 목록 재탐색.
- [ ] **SaveData 확장** (3.7) — `SaveData.cs`에 클리어한 스테이지·해금된 챕터 필드 추가(현재는 level/gold/playTime뿐). `StageCleared`를 구독해 저장하도록 연결.
- [ ] **챕터 해금 로직** (5.1) — `StageCleared` 발행 이후 응용 스테이지까지 클리어 시 다음 챕터 해금하는 처리는 아직 없음(현재는 이벤트 발행까지만 구현).
- [x] **챌린지 스테이지 다중 캔버스 조건** (5.2) — 캔버스 여러 개를 각각 순차 완료(잔금)하면 전체 클리어되는 로직 구현(`ColorCanvas`/`LevelManager`).

## 4순위 — UI 화면

- [x] **메인 화면** (3.1) — `MainMenuController` 구현: 메인 패널·스테이지 선택 패널·설정 패널을 토글(스테이지 선택/설정은 서로 동시에 켜지지 않음), 챕터 버튼으로 공용 스테이지 목록 패널(`stageListPanel`)을 그 챕터 기준으로 표시, 스테이지 버튼은 `OnStageButton(스테이지 인덱스)`로 `StageTable`(공용 데이터 애셋)의 씬을 로드 + `GameManager.StartGame()`. `Bootstrap`이 더 이상 강제로 `Playing` 상태로 넘기지 않아 부팅 시 `MainMenu` 상태로 시작. **미구현**: 해금된 스테이지만 선택 가능하게 하는 것(3.7 SaveData 확장에 의존), `StageTable`은 현재 전부 `InGame`(테스트용) 플레이스홀더.
- [x] **챕터별 스테이지 목록 패널을 패널 7개 → 패널 1개 재사용으로 변경** — 챕터마다 동일한 형태의 패널을 중복 배치하던 `chapterStagePanels[]` 배열을 없애고, 챕터별 씬 이름만 담는 데이터로 대체. `OnChapterButton(챕터 인덱스)`는 현재 챕터만 기억하고 공용 패널을 보여주며, 스테이지 버튼 OnClick도 씬 이름 문자열 대신 스테이지 인덱스(0~9)를 넘기도록 재배선함.
- [x] **스테이지 씬 목록을 StageTable 애셋으로 분리** — `Level/StageTable.cs`(`ScriptableObject`, `[CreateAssetMenu]`) 신규. 챕터별 씬 이름 배열(`ChapterStageScenes[] chapters`)을 애셋 파일 하나로 두고, `MainMenuController`(스테이지 선택)와 `ClearScreenController`(다음 스테이지 계산)가 같은 애셋을 참조 — 스테이지 목록이 한 곳에만 존재. `Flattened()`가 전체 챕터를 순서대로 이어붙인 목록을 반환하는데, 빈 자리도 그대로 포함한다(아래 클리어 화면 항목 참고).
- [x] **챕터 선택 화면 ScrollRect** (3.1) — 유니티 기본 `ScrollRect` 적용. 스크롤 시작 위치가 가운데였던 것을 `Content`의 anchor/pivot을 좌측 기준으로 바꿔 왼쪽부터 시작하도록 수정. `Viewport`에 `RectMask2D`만 있고 레이캐스트 가능한 `Graphic`이 없어 버튼 위에서만 스크롤이 먹던 문제를, 투명(alpha=0)·레이캐스트 활성 `Image`를 `Viewport`에 추가해 해결. 챕터 버튼 오브젝트도 `Chapter1~7` → `ChapterButton1~7`로 정리.
- [x] **클리어 화면** (3.6) — `ClearScreenController`(UI) 신규. `StageCleared` 이벤트로 클리어 패널을 열고 `Time.timeScale = 0`(FirstPersonController가 timeScale 0일 때 입력 무시하므로 캐릭터 조작도 같이 막힘), 커서 해제. `GameState`에 `Cleared`가 추가돼(FrameworkCore 패키지) `LevelManager`가 `StageCleared` 발행과 함께 `GameManager.StageClear()`로 전환하며, 이 상태에서는 `PauseMenuController`의 ESC 처리가 자동으로 무시됨(별도 방어 코드 없음). 메인화면/다음 스테이지/다시하기 버튼 제공, 뜬 채로 시간 제한 없음.
  - **"다음 스테이지" 판정 기준**: 리스트 끝인지가 아니라 "바로 다음 자리에 씬 이름이 있는지"로 판단한다. `StageTable.Flattened()`가 챕터별 배열을 빈 자리 포함 그대로 이어붙이고, `ClearScreenController`가 현재 씬 다음 자리를 확인해 비어있으면(챕터 중간에 비어있어도, 리스트 끝이어도) 다음 스테이지 없음으로 처리해 해당 버튼을 비활성화한다. → `StageTable`에 스테이지를 채울 때, 뒤에 이어질 스테이지가 있는 자리는 비워두면 안 된다(비우면 그 자리에서 진행이 끊긴 것으로 취급됨).
  - **일시정지와 커서 충돌 버그 수정**: `PauseMenuController.OnStateChanged`가 모든 상태 변화에 반응해 "Paused가 아니면 커서 잠금"으로 처리하고 있어서, `Cleared` 상태에서 `ClearScreenController`가 커서를 풀어줘도 같은 이벤트 체인에서 다시 잠겨버리는 문제가 있었음. `Playing`↔`Paused` 전환에서만 커서를 조정하도록 수정해, 다른 상태(`Cleared` 등)는 각자의 컨트롤러가 커서를 관리하도록 분리.
  - UIScene에 클리어 패널 UI 구성 + `ClearScreenController`의 `clearPanel`/`nextStageButton`/`stageTable` 연결 + 버튼 OnClick 연결(`OnMainMenuButton`/`OnNextStageButton`/`OnRetryButton`) 완료. `StageTable` 애셋도 생성해 `MainMenuController`/`ClearScreenController` 양쪽에 할당 완료(스테이지 씬 이름은 계속 채워나가는 중).
- [x] **HUD 목표 스택 표시** (3.4) — 중앙 HUD 숫자 하나 대신, 캔버스별로 다른 목표값을 가질 수 있어 캔버스 자신에게 라벨을 붙이는 방식으로 구현(아래 `ColorCanvas` 항목 참고).
- [x] **캔버스 목표값 라벨** — `ColorCanvas.ApplyTargetLabel()`: 첫 번째 자식의 -Z면(고정, 카메라를 따라 돌지 않음 — 필터 라벨과 달리 빌보드 아님)에 목표 R/G/B 값을 표시. 위치/회전 계산은 `CellGroupLabel`의 "normal 방향으로 띄우고 -normal을 forward로" 공식을 그대로 재사용(뒤집힘 방지).
- [x] **라벨 텍스트 형식을 공용 클래스로 분리** — `MapObjects/StackLabelFormat.cs` 신규. 판정 방식에 따라 형식이 다름: 정숫값 자체로 판정하는 기물(캔버스) → `ByValue()`("R G B", 공백 구분), 비율(변환 색)로 판정하는 기물(필터) → `ByRatio()`("R:G:B", 콜론 구분). `ColorCanvas`/`ColorFilterBlock` 둘 다 각자 판정 방식에 맞는 메서드를 호출하도록 변경(기존엔 둘 다 필터 쪽 형식을 그대로 복붙해 캔버스가 잘못된 형식을 쓰고 있었음). 앞으로 라벨 붙는 기물이 추가되면 이 두 메서드 중 판정 방식에 맞는 걸 쓰면 됨.
- [x] **기물 위치 마커 HUD (기획서 미기재, 신규)** — `UI/MapObjectMarkerHUD.cs` 신규. 필터를 제외한 맵 기물(컬러 팔레트/스택 체인저/컬러 체인저/지우개/캔버스)마다 화면에 위치 마커를 띄운다. 기물 종류별로 다른 마커 프리팹(5개 필드)을 쓰고, `Camera.main.WorldToScreenPoint`로 매 프레임 위치를 갱신하며 화면 밖(카메라 뒤쪽 포함)이면 가장자리로 클램프하고 방향을 가리키도록 회전(마커 프리팹에 `Arrow`라는 자식이 있으면 화면 밖일 때만 그걸 켜서 회전시키고 아이콘 본체는 끔 — 화면 안이면 반대로 아이콘만). 화면 안에 있을 때는 `Physics.Raycast`로 카메라-기물 사이가 막혀있는지(트리거 콜라이더는 무시) 확인해서, 가려져 있을 때만 원형 아이콘을 표시하고 직접 보이면 마커 자체를 끈다. `Start()`와 `SceneLoadCompleted` 양쪽에서 스캔해 메인메뉴를 거치지 않고 스테이지를 바로 Play해도 마커가 뜨도록 함(필터 초기 투명도 버그와 같은 종류의 실수라 처음부터 같이 넣음). 기물이 파괴(소모)되면 마커도 자동으로 사라짐(별도 on/off 없이 오브젝트 존재 여부로만 판단) — 캔버스는 완료돼도 오브젝트 자체가 파괴되지 않으므로 마커가 계속 남는다. **필요 작업**: UIScene에 `MapObjectMarkerHUD` 배치 + `markerContainer`(Screen Space - Overlay 캔버스 하위 RectTransform) 연결 + 기물 종류별 마커 프리팹 5개 제작·연결(방향 표시가 필요하면 `Arrow`라는 자식 오브젝트를 만들어서 위쪽을 향하는 화살표 아트로 구성, 반드시 `UI > Image`로 RectTransform 기반으로 만들어야 함 — Sprite Renderer 방식은 동작 안 함). 테스트용 흰색 원/화살표 스프라이트를 `Assets/Sprites/`에 만들어둠.
- [ ] **HUD 색상 스와치 인스펙터 미할당** (3.4) — `UIScene`의 `ColorStackHUD.colorSwatch` 필드가 비어있어 스택 색상 스와치가 안 나옴(값 텍스트는 정상). 인스펙터에서 연결 필요.
- [x] **일시정지 메뉴 '처음부터' 버튼** (3.5) — `PauseMenuController.OnRestartButton()` 추가: 시간 복구 후 `SceneManager.GetActiveScene().name`(일시정지 중에도 UIScene이 아니라 게임 씬이 active scene이라 안전)을 다시 로드.
- [x] **일시정지 메뉴 설정 열기/닫기 패널 전환** (3.5) — `OnSettingsButton()`이 일시정지 패널을 숨기고 설정 패널을 보여주도록 수정(기존엔 설정 패널만 켜져서 두 패널이 겹쳐 보였음), 설정에서 다시 일시정지 패널로 돌아가는 `OnBackToPauseButton()` 추가.
- [x] **일시정지 메뉴 종료 버튼, 메인 화면으로 복귀하도록 변경** — 기존에는 앱을 완전히 종료했으나, `Time.timeScale`을 되돌리고 `GameManager` 상태를 `MainMenu`로 바꾼 뒤 `MainMenu` 씬을 로드하도록 수정(앱 자체 종료는 메인 화면 자체의 종료 버튼만 담당).
- [x] **HUD, MainMenu 상태에서 숨김 처리** — `UIScene`이 항상 additive로 로드되다 보니 메인 화면에서도 게임 HUD(조준점·스택 표시)가 같이 보이는 문제가 있었음. `HUDController`가 `GameManager.State`를 구독해 `MainMenu`일 때는 `hudRoot`를 끄고 그 외에는 켜도록 수정.
- [x] **씬 전환 시 UI 씬이 사라지는 문제 수정** — `SceneLoader`가 Single 모드로 씬을 로드하면 이전에 additive로 얹혀 있던 `UIScene`까지 함께 언로드됨. `UIManager`가 `SceneLoadCompleted` 이벤트를 구독해 씬 전환마다 `UIScene`이 로드돼 있는지 확인하고 없으면 재로드하도록 수정.
- [x] **중복 EventSystem 제거** — `MainMenu.unity`와 `UIScene.unity`에 각각 EventSystem이 있어 두 씬이 함께 로드되면 "2 event systems" 경고가 떴음. `MainMenu.unity` 쪽 EventSystem을 제거하고 `UIScene`의 것만 남김.

## 폴더 구조

`Assets/Scripts`를 역할별로 정리함(각 .cs와 .cs.meta를 함께 이동해 GUID·씬 참조 보존):

- `Core/` — Bootstrap
- `Player/` — FirstPersonController, InputManager, ColorStacks, ColorStackInput
- `MapObjects/` — 베이스 클래스 7종(`MapObjectBase`, `FilterBlockBase`, `AcquireObjectBase`, `ClearObjectBase`, `ConsumableObjectBase`, `StackModifierConsumable`, `SpinningStackModifier`) + 기물 7종(ColorFilterBlock, RgbFilterBlock, ColorPalette, StackChanger, ColorChanger, Eraser, ColorCanvas) + 범용 라벨 컴포넌트 `CellGroupLabel`(상속해서 위치/회전 계산을 커스터마이즈 가능, 하위 클래스 `BillboardCenterLabel` 포함) + 범용 부유 애니메이션 `FloatingBob` + 라벨 텍스트 형식 공용 클래스 `StackLabelFormat`. 옛 `GateMeshCombiner`는 `FilterBlockBase`로 기능이 흡수되어 내용을 비워두고 `!!!!GateMeshCombiner.cs`로 이름을 바꿔둠(삭제 권한 문제로 직접 못 지움, Unity에서 파일+`.meta` 삭제 필요 — 이름 앞 `!!!!`가 삭제 대상 표시).
- `Shaders/` — `FilterBorderAlwaysVisible.shader`(필터 테두리 전용, 렌더 큐를 채움보다 뒤로 둬서 자기 채움 메시에 가려지지 않게 함).
- `Level/` — LevelManager, MazeGenerator, StageTable(챕터별 스테이지 씬 이름을 담는 공용 데이터 애셋)
- `UI/` — UIManager, ColorStackHUD, HUDController, PauseMenuController, SettingsController, MainMenuController, ClearScreenController, MapObjectMarkerHUD
- `Editor/` — MazeGeneratorEditor, `MapObjectOrganizer`(메뉴 `ColorMaze/특수 블록 하이어라키 정리` — 씬의 특수 블록을 타입별 폴더로 재배치, 위치는 그대로 유지). 옛 `GateMeshCombinerEditor`도 같은 이유로 `!!!!GateMeshCombinerEditor.cs`로 이름을 바꾸고 내용을 비워둠(삭제 필요).

## 참고

- 이미 기획서 수준으로 구현된 부분: 이동/카메라(`FirstPersonController`), 입력(`InputManager`), RGB 스택 코어 로직과 이벤트(`ColorStacks`, EventBus), 맵 기물 7종(`MapObjectBase` 계층 — `ColorFilterBlock`, `RgbFilterBlock`, `ColorPalette`, `StackChanger`, `ColorChanger`, `Eraser`, `ColorCanvas`, 메시 병합/테두리/라벨 로직은 `FilterBlockBase`에 내장), 일시정지·설정 UI 기본 동작(`PauseMenuController`, `SettingsController`), 저장 시스템 뼈대(`SaveManager`), 미로 블록 배치 툴(`MazeGeneratorEditor`), 특수 블록 하이어라키 정리 툴(`MapObjectOrganizer`), 스테이지 클리어 감지(`LevelManager`).
- **씬 인스펙터 다중 편집**: 같은 타입(예: `ColorFilterBlock`)의 블록 여러 개를 하이어라키에서 함께 선택하면(Shift/Ctrl+클릭) Inspector가 자동으로 다중 편집 모드로 바뀌어 값 하나를 입력하면 선택된 전부에 적용된다(Unity 기본 기능, 커스텀 Editor 없이 동작). `MapObjectOrganizer`로 타입별 폴더에 모아두면 이 다중 선택이 쉬워진다.
- 우선순위는 기반(스택 규칙) → 기물 → 진행 시스템 → UI 순으로, 기물이 스택 규칙에 의존하고 진행 시스템이 기물 클리어 판정에 의존하는 순서를 따랐다.

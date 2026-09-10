# ColorMaze 프로젝트 컨텍스트 (Claude Project 지식 베이스용 — 인덱스)

claude.ai Projects의 "프로젝트 지식"에 이 파일과 함께 `Docs/` 폴더의 다른 문서(`ColorMaze 기획서.md`,
`MapDesignRules.md`, `MapEditorDesign.md`)를 같이 업로드하면, 새 대화에서도 아래 배경을 다시 설명할
필요 없이 이어서 작업할 수 있습니다.

**이 파일은 인덱스 역할만 합니다.** 실제 내용은 각 문서가 단일 출처(single source of truth)로
갖고 있고, 여기서는 어디를 보면 되는지만 안내합니다 — 같은 정보를 여러 문서에 복사해두면 한쪽만
갱신했을 때 서로 어긋나는 문제가 실제로 여러 번 있었기 때문입니다(2026-09-11 통합 정리에서 확정한
원칙).

## 프로젝트 개요

- **ColorMaze** — 1인칭 3D 미로 퍼즐 게임, Unity 6(6000.5.0f1), URP, uGUI, C# 스크립팅
- 타겟: Windows Standalone / 입력: new Input System (`UnityEngine.InputSystem`)
- 프로젝트 경로: `C:\Users\thoth\Desktop\unity project\ColorMaze`
- 게임 개요·조작·시스템 흐름·맵 기물 7종·레벨 구성 등 완성된 기능 전체: **`Docs/ColorMaze 기획서.md`**

## 작업 방식상 제약 및 프로젝트 규칙

프로젝트 루트의 **`CLAUDE.md`** 참고 — 작업 전 동의 구하기, 삭제 시 파일명 앞에 "!!!!" 표시, 새로
만드는 문서는 `Docs/` 폴더에 저장 등. 규칙 원문은 그 파일에만 있고 여기 복사하지 않습니다.

(2026-09-11 기준) 로컬 PC에는 Unity CLI + Claude Code + `com.unity.pipeline` 패키지가 연동되어,
로컬 Claude Code 세션에서는 살아있는 Unity Editor를 직접 조작 가능(GameObject 생성, 씬 편집, C# 실행).
원격 샌드박스 기반 세션(Cowork 등)에서는 이 라이브 연동이 닿지 않아 파일 직접 편집 방식을 씀.

## 공용 패키지: com.nyapy.framework-core

- `MonoSingleton<T>` — 싱글턴 베이스
- `EventBus` / `IEvent` — pub-sub 이벤트. 구조체로 `IEvent` 구현, 발행 클래스 근처에 선언,
  `OnEnable`/`OnDisable`에서 구독/해제
- `GameManager` — 게임 상태 관리, `OnStateChanged` 이벤트
- `SaveManager` — `Current`/`Save()`/`Load()`(항상 `save.json`) + 범용 `SaveJson<T>`/`LoadJson<T>`/
  `HasJson`/`DeleteJson`(임의 타입/파일명). `JsonUtility`는 `Dictionary`를 직렬화 못하므로 전부 `List`
  기반으로 설계해야 함.

## 씬/HUD 핵심 패턴

- `SceneLoader.LoadAsync`는 기본 `LoadSceneMode.Single` — 다른 로드된 씬(`UIScene` 포함)을 전부
  언로드함.
- `UIManager`는 `SceneLoadCompleted` 이벤트를 구독해서 매번 `SceneManager.LoadScene(uiSceneName,
  LoadSceneMode.Additive)`로 `UIScene`을 다시 로드.
- `SceneLoadCompleted`(Framework.Core, `SceneName` 필드 있음)는 "스테이지/씬 준비 완료" 중앙 신호.
  LevelManager 캔버스 스캔, StageGuideController 리스트 로드, FilterBlockBase depth reset,
  PreviewingStackModifier 미리보기, MapObjectMarkerHUD refresh, UIManager 전부 이걸 구독.
  `SceneLoader`를 거치지 않고 수동으로 발행하는 것도 정상적인 패턴(런타임 커스텀 스테이지 로더가
  이 방식을 씀).

## 기물(Fixture) 동작 및 맵 설계 규칙

- 기물 7종의 클래스 계층·동작·구현 상세: **`Docs/ColorMaze 기획서.md`** 3.8~3.9장, 4장
- 스테이지 퍼즐 값을 설계/검증할 때 지켜야 하는 운영 규칙(더미·함정 설계, 이동 경로 검증 절차 등):
  **`Docs/MapDesignRules.md`**

## MazeGeneratorEditor 그리드 규칙

`Assets/Scripts/Editor/MazeGeneratorEditor.cs` — Scene 뷰 마인크래프트식 블록 에디터(개발자 전용).
- x, z는 정수, y는 정수+0.5(칸 중심)
- 클릭=설치, Ctrl+클릭=제거, Shift+드래그=사각형 범위 채우기/제거
- Bucket은 다른 기물보다 0.5 낮게 배치
- `SerializedObject`로 새로 배치된 기물의 private preset 필드를 설정

## 진행 중인 작업

- **인게임 맵 에디터(UGC)** — 진행 상황(현재 단계, 남은 작업)의 단일 출처는 **`Docs/MapEditorDesign.md`**
  입니다. 완료되면 `ColorMaze 기획서.md` 6장의 자리표시자를 정식 절로 승격합니다.

## 미해결/보류 항목

- `UIScene`이 `MapEditor.unity`에도 additive로 자동 로드되는 것(ColorStackHUD, MapObjectMarkerHUD 등
  불필요한 HUD가 뜸) — 우선 그대로 두고, 실제 거슬리면 그때 필요한 것만 숨기기로 잠정 결정. 최종
  확답은 아직 없음.
- 인게임 맵 에디터 UI 배선(HotBar 버튼/RGB 입력창을 `MapEditController`의 public 메서드에 연결) 및
  4~6단계(파라미터 수정+정답 리스트 UI, 저장/불러오기, 플레이 테스트) — 상세는 `MapEditorDesign.md`.

## 대화 스타일/작업 습관 참고

- 큰 변경 전 설계 → 동의 → 실행 순서를 지킴
- 서로게이트로 추가 질문할 땐 AskUserQuestion으로 선택지 제시
- 파일 대량 수정(씬 YAML 등)은 Python 스크립트로 정확히 스코프를 좁혀서 처리하고, 이후 별도 스캔으로
  재검증
- 문서는 단일 출처 원칙 유지 — 같은 정보를 여러 문서에 복사하지 말고 이 인덱스 문서에서는 링크(참고
  위치)만 남길 것

# Framework.Core 안정성 수정 요청서 (2026-09-17)

## 배경

ColorMaze 프로젝트의 전체 시스템 안정성 리팩토링(§ `Docs/MapEditorDesign.md`의 "전체 시스템
리팩토링" 참고) 과정에서, `Framework.Core` 패키지(`Packages/manifest.json`의 git URL —
`https://github.com/nyapy0811/FrameWorkCore.git`, 로컬에는 `Library/PackageCache/
com.nyapy.framework-core@f6b8f3bda153/` 하위로 해석됨) 안의 핵심 코드에서 안정성 이슈 3건을
확인했다. 이 패키지는 ColorMaze 저장소에 속하지 않는 외부 Git 패키지라, `Library/PackageCache`
하위 파일을 ColorMaze 쪽에서 직접 고쳐도 Unity가 패키지를 재해석할 때 덮어써 저장되지 않는다.
그래서 ColorMaze 세션에서는 코드를 고치지 않고, 이 문서에 구체적인 수정 요청만 정리해 프레임워크를
담당하는 세션/저장소로 전달한다.

각 항목의 파일 경로는 로컬 캐시 경로 기준이며, 실제 수정은 FrameWorkCore 저장소 원본에서 이뤄져야
한다.

## 1. SaveManager — 로드/저장 경로에 예외 처리 없음

**파일**: `Library/PackageCache/com.nyapy.framework-core@f6b8f3bda153/Managers/SaveManager.cs`
(정확한 클래스/메서드명은 저장소에서 확인 필요 — ColorMaze에서는 `SaveManager.Instance.
SaveJson<T>(fileName, data)` / `LoadJson<T>(fileName)` 형태로 호출)

**문제**: JSON 파일을 읽거나 쓰는 경로에 try/catch가 없다. 저장 파일이 손상되거나(디스크 쓰기 중
강제 종료 등), 디스크가 가득 찼거나, 권한 문제로 IO 예외가 발생하면 게임 전체가 그 자리에서
크래시한다.

**재현 시나리오**: 세이브 파일을 텍스트 에디터로 열어 JSON 문법을 깨뜨린 뒤 게임을 실행하면(또는
`LoadJson<T>`를 호출하는 어떤 화면이든 진입하면) 예외가 잡히지 않고 그대로 전파될 가능성이 높다.

**제안 수정**: `SaveJson`/`LoadJson` 내부를 try/catch로 감싸고, 로드 실패 시에는 해당 타입의 기본
인스턴스(`new T()`)를 반환하며 `Debug.LogError`로 원인을 남긴다. 저장 실패 시에도 예외를 삼키고
로그만 남기거나, 호출자가 성공 여부를 알 수 있게 `bool` 반환값을 추가하는 방향을 검토.

## 2. EventBus — 구독자 하나의 예외가 나머지 구독자를 전부 막음

**파일**: `Library/PackageCache/com.nyapy.framework-core@f6b8f3bda153/Events/EventBus.cs`

**문제**: `Publish<T>`가 `Dictionary<Type, Delegate>`에 등록된 멀티캐스트 델리게이트를 통째로
`Invoke`하는 방식(`(existing as Action<T>)?.Invoke(evt)`)이라, 구독자 중 하나가 예외를 던지면 그
뒤에 등록된 나머지 구독자는 전부 호출되지 않고 조용히 스킵된다. ColorMaze는 여러 시스템
(`LevelManager`, `StageGuideController`, `ProgressManager`, `FilterBlockBase`, UI 컨트롤러 등)이
같은 이벤트(`SceneLoadCompleted`, `StageCleared` 등)를 구독하므로, 한 시스템의 버그가 다른 무관한
시스템의 초기화까지 연쇄로 막을 수 있다.

**재현 시나리오**: `SceneLoadCompleted` 구독자 중 하나가 null 참조 등으로 예외를 던지면, 구독
순서상 그 뒤에 등록된 구독자(예: `FilterBlockBase.OnStageStart`)는 그 프레임에 전혀 실행되지 않아
필터 상태가 갱신 안 된 채로 스테이지가 시작될 수 있다.

**제안 수정**: `Publish<T>`에서 멀티캐스트 델리게이트를 한 번에 `Invoke`하지 않고,
`GetInvocationList()`로 개별 구독자를 순회하며 각각 try/catch로 감싸 호출한다. 한 구독자의 예외는
`Debug.LogError`로 남기고 다음 구독자 호출을 막지 않는다.

## 3. MonoSingleton — Awake() 순서를 강제하지 않음

**파일**: `Library/PackageCache/com.nyapy.framework-core@f6b8f3bda153/Singleton/MonoSingleton.cs`

**문제**: `Instance` getter는 `_instance`가 없으면 새 GameObject를 만들어 즉시 컴포넌트를 붙이는
지연 생성(lazy) 방식이라 자체적으로는 안전하지만(`_instance` 필드가 `OnAwake()` 호출 전에 먼저
할당됨), 여러 싱글톤이 서로의 `Awake()`/`OnAwake()` 안에서 다른 싱글톤의 `.Instance`를 참조할 때
실행 순서를 프레임워크 차원에서 보장하지 않는다. ColorMaze의 `Bootstrap.cs`가 `_ = X.Instance;`를
명시적 순서로 나열해 이 문제를 우회하고 있지만, 그 우회가 항상 지켜진다는 보장은 없다(예: 어떤
싱글톤이 `Bootstrap`보다 먼저 다른 씬 오브젝트의 `Awake()`에서 우연히 먼저 접근되면 그 시점에
지연 생성되어 `Bootstrap`이 의도한 순서와 달라질 수 있음).

**제안 수정**: 당장 구조를 바꾸기보다, 각 구체 싱글톤 클래스에 `[DefaultExecutionOrder]`를 지정할
수 있는 명시적 가이드를 문서화하거나, `MonoSingleton<T>`에 "이 싱글톤이 의존하는 다른 싱글톤 타입"을
선언하고 그 의존 관계에 따라 초기화 순서를 검증/경고하는 가벼운 장치를 추가하는 정도를 우선 검토.
전면적인 재설계보다는 안전장치 추가 쪽을 권장.

## 참고

- 이 세 항목은 전부 "안정성" 관점의 방어적 보강이며, 지금 당장 재현되는 크래시가 확인된 것은
  아니다(1, 3번은 잠재적 위험, 2번은 실제로 여러 시스템이 같은 이벤트를 공유하므로 발생 가능성이
  상대적으로 높음).
- ColorMaze 쪽에서는 이번 리팩토링에서 이 문제들의 영향 범위를 줄이기 위해 `Bootstrap.Awake()`의
  싱글톤 초기화 호출을 각각 try/catch로 감싸고 `[DefaultExecutionOrder(-1000)]`을 추가하는 선에서만
  대응했다(§ `Docs/MapEditorDesign.md`의 "전체 시스템 리팩토링" 참고) — 이는 패키지 자체의 근본
  수정을 대체하지 않는다.

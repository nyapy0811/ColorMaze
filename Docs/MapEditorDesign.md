# 인게임 맵 에디터 설계 (초안)

작성일: 2026-09-09
상태: 설계만 확정, 구현 착수 전

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

1. `CustomStageData` + 관련 타입 정의
2. 런타임 로더(데이터 → Instantiate → MazeGenerator 동적 부착 → RebuildAll)
3. `MapEditor.unity` 새 씬 + 배치/제거 입력 + 기물 팔레트 UI + 파라미터 입력 UI
4. 배치된 기물 재선택 → 파라미터 수정 + 정답 리스트1/2 추가·순서 편집 UI
5. 저장/불러오기(로컬 JSON) + "내 맵" 목록 UI
6. 플레이 테스트 버튼(로더 재사용)

각 단계는 순서대로 구현하고 중간중간 확인받으며 진행.

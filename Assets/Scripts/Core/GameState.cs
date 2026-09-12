/// <summary>이 프로젝트가 쓰는 게임 전역 상태. 프레임워크는 이 enum의 존재를 모르며(제네릭
/// 파라미터로만 다룸), 새 상태 추가/삭제는 이 파일만 고치면 된다.</summary>
public enum GameState
{
    Boot,       // 초기화 중
    MainMenu,   // 메뉴
    Loading,    // 스테이지 이동/메뉴 복귀 등 전환 대기(딜레이)
    Playing,    // 게임 진행
    MapEditor,  // 인게임 맵 에디터 — HUD·1인칭 뷰모델 등 게임 플레이 전용 표시를 숨기는 데 쓰인다
    Paused,     // 일시정지
    Cleared,    // 스테이지 클리어
    Quitting    // 종료 전 저장 등 마무리 작업
}

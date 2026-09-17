using UnityEngine;
using Framework.Core;

/// <summary>
/// 게임의 단일 진입점.
/// 씬의 빈 GameObject에 붙여 두면, 시작 시 핵심 매니저들을 결정적 순서로 깨운다(지연 생성 대신).
/// GameManager는 부팅 즉시 MainMenu 상태가 되며(GameManager.OnAwake), 이 스크립트는 더 이상
/// 강제로 Playing 상태로 넘기지 않는다 — 메인 화면에서 스테이지를 선택해야 게임이 시작된다
/// (MainMenuController.OnStageButton이 GameManager.StartGame()을 호출).
/// </summary>
[DefaultExecutionOrder(-1000)] // 다른 스크립트가 어떤 싱글톤을 Awake에서 참조하기 전에 먼저 전부 깨워둔다.
public class Bootstrap : MonoBehaviour
{
    void Awake()
    {
        // .Instance 접근만으로 각 싱글톤이 생성된다. 순서를 명시해 기동.
        // 하나가 예외를 던져도(예: 애셋 누락) 나머지 매니저는 계속 초기화되도록 각각 개별로 감싼다 —
        // 감싸지 않으면 그 아래 매니저 전부가 생성되지 않은 채 부팅이 멈춘다.
        Init(() => _ = GameManager.Instance);
        Init(() => _ = SaveManager.Instance);
        Init(() => _ = AudioManager.Instance);
        Init(() => _ = GameAudio.Instance);
        Init(() => _ = SceneLoader.Instance);
        Init(() => _ = PoolManager.Instance);
        Init(() => _ = InputManager.Instance);
        Init(() => _ = LevelManager.Instance);
        Init(() => _ = ProgressManager.Instance);
        Init(() => _ = StageGuideController.Instance);
        Init(() => _ = UIManager.Instance); // UI 씬을 additive로 로드
        Init(() => _ = FPSCounter.Instance); // 개발 빌드에서만 화면에 FPS 표시
    }

    static void Init(System.Action init)
    {
        try { init(); }
        catch (System.Exception ex) { Debug.LogError($"[Bootstrap] 매니저 초기화 실패: {ex}"); }
    }
}

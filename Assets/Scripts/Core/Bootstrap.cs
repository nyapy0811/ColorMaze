using UnityEngine;
using Framework.Core;

/// <summary>
/// 게임의 단일 진입점.
/// 씬의 빈 GameObject에 붙여 두면, 시작 시 핵심 매니저들을 결정적 순서로 깨운다(지연 생성 대신).
/// GameManager는 부팅 즉시 MainMenu 상태가 되며(GameManager.OnAwake), 이 스크립트는 더 이상
/// 강제로 Playing 상태로 넘기지 않는다 — 메인 화면에서 스테이지를 선택해야 게임이 시작된다
/// (MainMenuController.OnStageButton이 GameManager.StartGame()을 호출).
/// </summary>
public class Bootstrap : MonoBehaviour
{
    void Awake()
    {
        // .Instance 접근만으로 각 싱글톤이 생성된다. 순서를 명시해 기동.
        _ = GameManager.Instance;
        _ = SaveManager.Instance;
        _ = AudioManager.Instance;
        _ = SceneLoader.Instance;
        _ = PoolManager.Instance;
        _ = InputManager.Instance;
        _ = LevelManager.Instance;
        _ = UIManager.Instance; // UI 씬을 additive로 로드
    }
}

using UnityEngine;
using Framework.Core;

/// <summary>
/// 게임의 단일 진입점.
/// 씬의 빈 GameObject에 붙여 두면, 시작 시 핵심 매니저들을 결정적 순서로
/// 깨우고(지연 생성 대신) 레벨을 빌드한 뒤 게임을 시작한다.
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

    void Start()
    {
        GameManager.Instance.StartGame();
    }
}

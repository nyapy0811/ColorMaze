using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UI 씬을 게임 위에 additive로 얹어 관리한다.
/// UI는 별도 씬(Canvas: Screen Space - Overlay)에 있고, 게임과는 EventBus/싱글톤으로 통신한다.
/// SceneLoader가 씬을 Single 모드로 불러오면 이전에 additive로 얹혀 있던 UI 씬도 함께 언로드되므로,
/// 씬 전환이 끝날 때마다(SceneLoadCompleted) UI 씬이 남아있는지 다시 확인해 없으면 재로드한다.
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    [Tooltip("additive로 로드할 UI 씬 이름 (Build Settings에 포함돼 있어야 함)")]
    public string uiSceneName = "UIScene";

    protected override void OnAwake() => EnsureUISceneLoaded();

    void OnEnable() => EventBus.Subscribe<SceneLoadCompleted>(OnSceneLoadCompleted);
    void OnDisable() => EventBus.Unsubscribe<SceneLoadCompleted>(OnSceneLoadCompleted);

    void OnSceneLoadCompleted(SceneLoadCompleted e) => EnsureUISceneLoaded();

    void EnsureUISceneLoaded()
    {
        if (!SceneManager.GetSceneByName(uiSceneName).isLoaded)
            SceneManager.LoadScene(uiSceneName, LoadSceneMode.Additive);
    }
}

using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UI 씬을 게임 위에 additive로 얹어 관리한다.
/// UI는 별도 씬(Canvas: Screen Space - Overlay)에 있고, 게임과는 EventBus/싱글톤으로 통신한다.
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    [Tooltip("additive로 로드할 UI 씬 이름 (Build Settings에 포함돼 있어야 함)")]
    public string uiSceneName = "UIScene";

    protected override void OnAwake()
    {
        if (!SceneManager.GetSceneByName(uiSceneName).isLoaded)
            SceneManager.LoadScene(uiSceneName, LoadSceneMode.Additive);
    }
}

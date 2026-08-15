#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// [개발자 전용] 어느 씬을 열어놓고 에디터 플레이 버튼을 눌러도 항상 MainMenu 씬부터 시작하게 한다.
/// Unity의 기본 기능(EditorSceneManager.playModeStartScene, Project Settings > Editor의 Play Mode Start Scene과 동일)을 사용하므로,
/// 플레이를 마치면 자동으로 편집 중이던 원래 씬으로 돌아온다(별도 복원 로직 불필요).
/// </summary>
[InitializeOnLoad]
static class PlayFromMainMenu
{
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        if (EditorSceneManager.playModeStartScene != null &&
            AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) == MainMenuScenePath)
            return;

        var mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenuScene != null)
            EditorSceneManager.playModeStartScene = mainMenuScene;
    }
}
#endif

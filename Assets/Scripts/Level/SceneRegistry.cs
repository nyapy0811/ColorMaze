using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 이름이 Build Settings에 실제로 등록되어 있는지 확인하는 유틸리티.
/// StageTable에는 아직 만들지 않은 스테이지도 "N-M" 규칙으로 미리 채워져 있을 수 있어서,
/// 실제로 로드를 시도하기 전에 이걸로 걸러낸다.
/// </summary>
public static class SceneRegistry
{
    public static bool IsRegistered(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) return true;
        }
        return false;
    }
}

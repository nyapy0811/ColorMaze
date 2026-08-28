using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 현재 스테이지를 처음부터 다시 시작하는 공용 시퀀스.
/// DeadZone(맵 밖으로 떨어짐)·일시정지 메뉴의 다시하기·클리어 화면의 다시하기가 모두 동일한 순서를 쓴다.
/// </summary>
public static class SceneRestarter
{
    /// <summary>keepGuide가 false(기본값)면 진행 중이던 스테이지 가이드를 끈다.
    /// 가이드 버튼을 통한 재시작(StageGuideController.ActivateGuide)만 true를 넘긴다.</summary>
    public static void RestartCurrentScene(bool keepGuide = false)
    {
        Time.timeScale = 1f; // 일시정지/클리어로 멈춰뒀던 시간을 되돌린다.
        string sceneName = SceneManager.GetActiveScene().name;
        ProgressManager.Instance.RecordStageAttempt(sceneName);
        if (!keepGuide) StageGuideController.Instance.Deactivate();
        GameManager.Instance.StartGame();
        SceneLoader.Instance.Load(sceneName);
    }
}

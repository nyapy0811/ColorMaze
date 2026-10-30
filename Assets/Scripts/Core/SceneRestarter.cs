using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorMaze
{
    /// <summary>
    /// 코어 루프 동안의 '처음부터': 지금 맵 씬을 다시 불러온다(UI 씬은 UI 관리자가 다시 얹고, 상태는 맵 매니저가 Playing으로 맞춤).
    /// 체크포인트 단계에서 체크포인트 기록을 지우는 '처음부터'로 바꾼다(2차 Q312).
    /// </summary>
    public static class SceneRestarter
    {
        public static void Restart()
        {
            Time.timeScale = 1f;
            SceneLoader.Instance.Load(SceneManager.GetActiveScene().name);
        }
    }
}

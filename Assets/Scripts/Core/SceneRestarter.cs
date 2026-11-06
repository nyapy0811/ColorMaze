using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorMaze
{
    /// <summary>
    /// '처음부터': 검은 화면으로 어두워진 뒤 지금 맵 씬을 다시 불러온다(씬 재로드, 진행 시스템 구상 채팅).
    /// UI 씬은 UI 관리자가 다시 얹고(새 화면 페이드가 검은 화면에서 밝아짐), 상태는 맵 매니저가 Playing으로 맞춘다.
    /// 다시 불러오기 전에 그 맵의 체크포인트 기록을 지우고 바로 저장한다(2차 Q449, 이어하기가 지운 기록을 읽지 않게).
    /// </summary>
    public static class SceneRestarter
    {
        public static void Restart()
        {
            if (MapManager.Current != null && ProgressStore.Current.ClearCheckpoint(MapManager.Current.MapId)) ProgressStore.Save();
            ScreenFade.FadeOut(Reload);
        }

        static void Reload()
        {
            Time.timeScale = 1f;
            SceneLoader.Instance.Load(SceneManager.GetActiveScene().name);
        }
    }
}

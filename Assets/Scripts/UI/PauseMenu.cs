using UnityEngine;
using UnityEngine.UI;

namespace ColorMaze
{
    /// <summary>
    /// 최소 일시정지 메뉴(UIScene, v1 PauseMenuController를 참고해 새로 씀). ESC를 읽는 곳이기도 하다:
    /// 뒤로 가기 스택에 닫을 화면이 있으면 맨 위를 닫고, 스택이 비어 있고 게임 중일 때만 일시정지를 연다(설계 문서 7.1, 2차 Q194).
    /// 일시정지는 잠금이 아니라 <c>Time.timeScale = 0</c>으로 멈추고, 열린 동안 스택에 '계속'을 넣어 ESC로 닫힌다.
    /// 지금은 계속·처음부터·체크포인트로·종료를 두고, 확인 창과 설정·메인메뉴는 메뉴 흐름에서 더한다.
    /// 화면 페이드 중에는 ESC를 받지 않는다.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button checkpointButton;
        [SerializeField] Button quitButton;

        void Awake()
        {
            panel.SetActive(false);
            resumeButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(Restart);
            checkpointButton.onClick.AddListener(ReturnToCheckpoint);
            quitButton.onClick.AddListener(GameManager.Instance.QuitGame);
        }

        void OnDisable() => BackStack.Remove(Resume);

        void Update()
        {
            if (!InputManager.Instance.ReadCancel() || ScreenFade.IsFading) return;
            if (BackStack.TryCloseTop()) return;
            if (GameManager.Instance.State == GameState.Playing) Pause();
        }

        void Pause()
        {
            GameManager.Instance.ChangeState(GameState.Paused);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            panel.SetActive(true);
            BackStack.Push(Resume);
        }

        void Resume()
        {
            BackStack.Remove(Resume);
            panel.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance.ChangeState(GameState.Playing);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 맵 씬을 다시 불러오면 UI 씬도 다시 얹히므로 메뉴는 그대로 둔다.
        void Restart()
        {
            BackStack.Remove(Resume);
            SceneRestarter.Restart();
        }

        // 맵 씬은 그대로이므로 메뉴를 닫고 게임으로 돌아간 뒤 되돌린다.
        void ReturnToCheckpoint()
        {
            Resume();
            MapManager.Current.ReturnToCheckpoint();
        }
    }
}

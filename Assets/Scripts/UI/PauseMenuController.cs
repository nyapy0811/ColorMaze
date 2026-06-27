using Framework.Core;
using UnityEngine;

/// <summary>
/// 일시정지 메뉴. ESC로 열고 닫으며 GameManager의 Pause/Resume과 연동한다.
/// 패널 표시 여부는 GameState 변화에 반응해 자동으로 갱신된다.
/// 버튼(이어하기/설정/종료)은 인스펙터에서 OnClick에 아래 public 메서드를 연결한다.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject settingsPanel;

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void Update()
    {
        if (!InputManager.Instance.ReadPause()) return;

        var gm = GameManager.Instance;
        if (gm.State == GameState.Playing) gm.Pause();
        else if (gm.State == GameState.Paused) gm.Resume();
    }

    void OnStateChanged(GameState previous, GameState next)
    {
        bool paused = next == GameState.Paused;
        if (pausePanel) pausePanel.SetActive(paused);
        if (!paused && settingsPanel) settingsPanel.SetActive(false);

        // 일시정지 중에는 커서를 보이고 풀어준다.
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    // --- 버튼 OnClick 연결용 ---
    public void OnResumeButton() => GameManager.Instance.Resume();

    public void OnSettingsButton()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void OnCloseSettingsButton()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public void OnQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

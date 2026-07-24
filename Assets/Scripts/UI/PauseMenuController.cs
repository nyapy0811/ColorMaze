using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        // 커서는 이 컨트롤러가 다루는 Playing/Paused 전환에서만 조정한다.
        // Cleared 같은 다른 상태는 각자의 컨트롤러(ClearScreenController 등)가 커서를 관리하므로 여기서 건드리지 않는다.
        if (next == GameState.Paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (next == GameState.Playing)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // --- 버튼 OnClick 연결용 ---
    public void OnResumeButton() => GameManager.Instance.Resume();

    /// <summary>현재 스테이지 씬을 처음부터 다시 로드한다.</summary>
    public void OnRestartButton()
    {
        Time.timeScale = 1f; // 일시정지 중 멈춰뒀던 시간을 되돌린다.
        string sceneName = SceneManager.GetActiveScene().name;
        GameManager.Instance.StartGame();
        SceneLoader.Instance.Load(sceneName);
    }

    public void OnSettingsButton()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    /// <summary>설정 패널을 숨기고 일시정지 패널로 돌아간다.</summary>
    public void OnBackToPauseButton()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void OnCloseSettingsButton()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    /// <summary>게임을 종료하지 않고 메인 화면으로 돌아간다.</summary>
    public void OnQuitButton()
    {
        Time.timeScale = 1f; // 일시정지 중 멈춰뒀던 시간을 되돌린다.
        GameManager.Instance.ChangeState(GameState.MainMenu);
        SceneLoader.Instance.Load("MainMenu");
    }
}

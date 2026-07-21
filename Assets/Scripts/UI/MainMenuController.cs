using Framework.Core;
using UnityEngine;

/// <summary>
/// 메인 화면(3.1) — 스테이지 선택(챕터→스테이지 목록), 설정, 종료.
/// GameManager.State가 MainMenu일 때만 보이고, Playing으로 바뀌면 자동으로 숨는다(PauseMenuController와 동일한 패턴).
/// 챕터/스테이지 버튼은 별도 데이터 없이 인스펙터에서 직접 배치하고, 각 버튼 OnClick에
/// OnStageButton(씬 이름)을 연결해 쓴다(씬 이름은 Build Settings에 등록된 이름과 같아야 함).
/// 해금 여부(3.7 저장 데이터 연동)는 아직 붙어있지 않다 — 지금은 모든 스테이지 버튼이 항상 선택 가능하다.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject stageSelectPanel;
    [SerializeField] GameObject settingsPanel;

    [Header("챕터별 스테이지 목록 패널 (인덱스 = 챕터 버튼에 연결한 번호)")]
    [SerializeField] GameObject[] chapterStagePanels;

    void Start()
    {
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        HideAllChapterStagePanels();
        GameManager.Instance.OnStateChanged += OnStateChanged;
        Refresh(GameManager.Instance.State);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState previous, GameState next) => Refresh(next);

    void Refresh(GameState state)
    {
        bool show = state == GameState.MainMenu;
        if (mainPanel) mainPanel.SetActive(show);
        if (!show)
        {
            if (stageSelectPanel) stageSelectPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }

    // --- 버튼 OnClick 연결용 ---

    /// <summary>메인 패널은 그대로 둔 채 스테이지 목록만 추가로 켜고 끈다(토글). 설정 패널과는 동시에 켜지지 않는다.</summary>
    public void OnStageSelectButton()
    {
        if (!stageSelectPanel) return;
        bool show = !stageSelectPanel.activeSelf;
        stageSelectPanel.SetActive(show);
        if (show && settingsPanel) settingsPanel.SetActive(false);
    }

    /// <summary>챕터 버튼 OnClick에 챕터 번호(0부터, chapterStagePanels의 인덱스)를 인자로 연결한다.
    /// 해당 챕터의 스테이지 목록 패널만 보이고 나머지 챕터의 패널은 숨는다.</summary>
    public void OnChapterButton(int chapterIndex)
    {
        for (int i = 0; i < chapterStagePanels.Length; i++)
            if (chapterStagePanels[i]) chapterStagePanels[i].SetActive(i == chapterIndex);
    }

    void HideAllChapterStagePanels()
    {
        foreach (var panel in chapterStagePanels)
            if (panel) panel.SetActive(false);
    }

    /// <summary>스테이지 버튼 OnClick에 로드할 씬 이름을 인자로 연결한다.</summary>
    public void OnStageButton(string sceneName)
    {
        GameManager.Instance.StartGame();
        SceneLoader.Instance.Load(sceneName);
    }

    /// <summary>메인 패널은 그대로 둔 채 설정 패널만 추가로 켜고 끈다(토글). 스테이지 패널과는 동시에 켜지지 않는다.</summary>
    public void OnSettingsButton()
    {
        if (!settingsPanel) return;
        bool show = !settingsPanel.activeSelf;
        settingsPanel.SetActive(show);
        if (show && stageSelectPanel) stageSelectPanel.SetActive(false);
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

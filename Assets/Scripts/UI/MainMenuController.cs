using Framework.Core;
using UnityEngine;

/// <summary>
/// 메인 화면(3.1) — 스테이지 선택(챕터→스테이지 목록), 설정, 종료.
/// GameManager.State가 MainMenu일 때만 보이고, Playing으로 바뀌면 자동으로 숨는다(PauseMenuController와 동일한 패턴).
/// 챕터마다 스테이지 목록 패널을 따로 두지 않고 하나의 stageListPanel을 재사용한다.
/// 챕터 버튼을 누르면 currentChapterIndex만 바뀌고, 스테이지 버튼(OnStageButton(int))이 그 인덱스로
/// stageTable(공용 데이터 애셋)에서 씬 이름을 찾아 로드한다(씬 이름은 Build Settings에 등록된 이름과 같아야 함).
/// 해금 여부(3.7 저장 데이터 연동)는 아직 붙어있지 않다 — 지금은 모든 스테이지 버튼이 항상 선택 가능하다.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject stageSelectPanel; // 챕터 목록 패널
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject stageListPanel; // 챕터 공용 스테이지 목록 패널

    [Header("스테이지 데이터 (ClearScreenController와 공유하는 애셋)")]
    [SerializeField] StageTable stageTable;

    int currentChapterIndex = -1;

    void Start()
    {
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
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
            if (stageListPanel) stageListPanel.SetActive(false);
        }

        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }

    // --- 버튼 OnClick 연결용 ---

    /// <summary>메인 패널을 숨기고 스테이지 선택 패널을 보여준다.</summary>
    public void OnStageSelectButton()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
        if (stageSelectPanel) stageSelectPanel.SetActive(true);
    }

    /// <summary>챕터 버튼 OnClick에 챕터 번호(0부터, stageTable.chapters의 인덱스)를 인자로 연결한다.
    /// 챕터 목록 패널을 숨기고, 공용 스테이지 목록 패널을 그 챕터 기준으로 보여준다.</summary>
    public void OnChapterButton(int chapterIndex)
    {
        currentChapterIndex = chapterIndex;
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(true);
    }

    /// <summary>스테이지 목록 패널을 숨기고 챕터 목록 패널로 돌아간다.</summary>
    public void OnBackToChapterButton()
    {
        if (stageListPanel) stageListPanel.SetActive(false);
        if (stageSelectPanel) stageSelectPanel.SetActive(true);
    }

    /// <summary>스테이지 버튼 OnClick에 스테이지 번호(0부터)를 인자로 연결한다.
    /// 현재 선택된 챕터(currentChapterIndex) 기준으로 stageTable에서 씬 이름을 찾아 로드한다.</summary>
    public void OnStageButton(int stageIndex)
    {
        if (stageTable?.chapters == null) return;
        if (currentChapterIndex < 0 || currentChapterIndex >= stageTable.chapters.Length) return;
        var scenes = stageTable.chapters[currentChapterIndex].sceneNames;
        if (stageIndex < 0 || stageIndex >= scenes.Length) return;
        string sceneName = scenes[stageIndex];
        if (string.IsNullOrEmpty(sceneName)) return;

        GameManager.Instance.StartGame();
        SceneLoader.Instance.Load(sceneName);
    }

    /// <summary>메인 패널을 숨기고 설정 패널을 보여준다.</summary>
    public void OnSettingsButton()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    /// <summary>스테이지 선택/설정 패널을 숨기고 메인 패널로 돌아간다.</summary>
    public void OnBackToMainButton()
    {
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);
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

using Framework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 화면(3.1) — 스테이지 선택(챕터→스테이지 목록), 설정, 종료.
/// GameManager.State가 MainMenu일 때만 보이고, Playing으로 바뀌면 자동으로 숨는다(PauseMenuController와 동일한 패턴).
/// 챕터마다 스테이지 목록 패널을 따로 두지 않고 하나의 stageListPanel을 재사용한다.
/// 챕터 버튼을 누르면 currentChapterIndex만 바뀌고, 스테이지 버튼(OnStageButton(int))이 그 인덱스로
/// stageTable(공용 데이터 애셋)에서 씬 이름을 찾아 로드한다(씬 이름은 Build Settings에 등록된 이름과 같아야 함).
/// 해금 여부(3.7, 5.2)는 ProgressManager가 판정한다 — 버튼은 항상 클릭 가능한 상태로 두고(잠긴 버튼을
/// 눌렀을 때도 반응이 오도록), 챕터 목록/스테이지 목록 패널이 열릴 때마다 잠김/해금/클리어 상태에 맞춰
/// 각 버튼의 ChapterStageButtonVisual 스프라이트를 갱신한다. 실제 잠금 판정은 OnChapterButton/OnStageButton 안에서 한다.
/// </summary>
public class MainMenuController : GameStateListener
{
    [Header("패널")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject stageSelectPanel; // 챕터 목록 패널
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject stageListPanel; // 챕터 공용 스테이지 목록 패널

    [Header("스테이지 데이터 (ClearScreenController와 공유하는 애셋)")]
    [SerializeField] StageTable stageTable;

    [Header("해금 표시용 버튼 (인덱스 = 챕터/스테이지 번호)")]
    [SerializeField] Button[] chapterButtons;
    [SerializeField] Button[] stageButtons;

    int currentChapterIndex = -1;

    protected override void Start()
    {
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
        base.Start();
    }

    protected override void OnGameStateChanged(GameState previous, GameState next)
    {
        bool show = next == GameState.MainMenu;
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
        GameAudio.Instance.PlayButtonClick();
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
        if (stageSelectPanel) stageSelectPanel.SetActive(true);

        RefreshChapterButtons();
    }

    void RefreshChapterButtons()
    {
        if (chapterButtons == null) return;
        for (int i = 0; i < chapterButtons.Length; i++)
            SetChapterVisual(chapterButtons[i], i);
    }

    static void SetChapterVisual(Button button, int chapterIndex)
    {
        var visual = button ? button.GetComponent<ChapterStageButtonVisual>() : null;
        if (!visual) return;

        var state = !ProgressManager.Instance.IsChapterUnlocked(chapterIndex) ? ChapterStageButtonVisual.State.Locked
            : ProgressManager.Instance.IsChapterCleared(chapterIndex) ? ChapterStageButtonVisual.State.Cleared
            : ChapterStageButtonVisual.State.Unlocked;
        visual.SetState(state);
    }

    /// <summary>챕터 버튼 OnClick에 챕터 번호(0부터, stageTable.chapters의 인덱스)를 인자로 연결한다.
    /// 잠긴 챕터면 목록을 열지 않고 돌아간다. 챕터 목록 패널을 숨기고, 공용 스테이지 목록 패널을 그 챕터 기준으로 보여준다.</summary>
    public void OnChapterButton(int chapterIndex)
    {
        if (!ProgressManager.Instance.IsChapterUnlocked(chapterIndex))
        {
            GameAudio.Instance.PlayNope();
            return;
        }

        GameAudio.Instance.PlayButtonClick();
        currentChapterIndex = chapterIndex;
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(true);

        if (stageButtons != null)
            for (int i = 0; i < stageButtons.Length; i++)
                SetStageVisual(stageButtons[i], chapterIndex, i);
    }

    static void SetStageVisual(Button button, int chapterIndex, int stageIndex)
    {
        var visual = button ? button.GetComponent<ChapterStageButtonVisual>() : null;
        if (!visual) return;

        var state = !ProgressManager.Instance.IsStageUnlocked(chapterIndex, stageIndex) ? ChapterStageButtonVisual.State.Locked
            : ProgressManager.Instance.IsStageCleared(chapterIndex, stageIndex) ? ChapterStageButtonVisual.State.Cleared
            : ChapterStageButtonVisual.State.Unlocked;
        visual.SetState(state);
    }

    /// <summary>스테이지 목록 패널을 숨기고 챕터 목록 패널로 돌아간다.</summary>
    public void OnBackToChapterButton()
    {
        GameAudio.Instance.PlayButtonClick();
        if (stageListPanel) stageListPanel.SetActive(false);
        if (stageSelectPanel) stageSelectPanel.SetActive(true);
    }

    /// <summary>스테이지 버튼 OnClick에 스테이지 번호(0부터)를 인자로 연결한다.
    /// 현재 선택된 챕터(currentChapterIndex) 기준으로 stageTable에서 씬 이름을 찾아 로드한다.</summary>
    public void OnStageButton(int stageIndex)
    {
        if (stageTable?.chapters == null) return;
        if (currentChapterIndex < 0 || currentChapterIndex >= stageTable.chapters.Length) return;
        if (!ProgressManager.Instance.IsStageUnlocked(currentChapterIndex, stageIndex))
        {
            GameAudio.Instance.PlayNope();
            return;
        }

        var scenes = stageTable.chapters[currentChapterIndex].sceneNames;
        if (stageIndex < 0 || stageIndex >= scenes.Length) return;
        string sceneName = scenes[stageIndex];
        if (string.IsNullOrEmpty(sceneName)) return;

        if (!SceneRegistry.IsRegistered(sceneName))
        {
            Debug.Log($"[MainMenu] '{sceneName}' 스테이지는 아직 제작 중입니다.");
            return;
        }

        GameAudio.Instance.PlayButtonClick();
        GameManager.Instance.StartGame();
        SceneLoader.Instance.Load(sceneName);
    }

    /// <summary>메인 패널을 숨기고 설정 패널을 보여준다.</summary>
    public void OnSettingsButton()
    {
        GameAudio.Instance.PlayButtonClick();
        if (mainPanel) mainPanel.SetActive(false);
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    /// <summary>스테이지 선택/설정 패널을 숨기고 메인 패널로 돌아간다.</summary>
    public void OnBackToMainButton()
    {
        GameAudio.Instance.PlayButtonClick();
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (stageListPanel) stageListPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);
    }

    public void OnQuitButton()
    {
        GameAudio.Instance.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

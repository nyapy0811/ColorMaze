using Framework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 화면(3.1) — 스테이지 선택(챕터→스테이지→미리보기), 설정, 종료.
/// GameManager.State가 MainMenu일 때만 보이고, Playing으로 바뀌면 자동으로 숨는다(PauseMenuController와 동일한 패턴).
/// 챕터 목록/스테이지 목록/미리보기가 전부 stageSelectPanel 하나 안에 같이 들어있고, 챕터·스테이지 토글
/// 상태에 따라 stagePanelRoot(스테이지 스크롤)와 previewPanel(미리보기+Play 버튼)만 켜고 끈다.
/// 챕터/스테이지 버튼은 Button이 아니라 Toggle이다(각각 ToggleGroup으로 묶인 라디오 버튼, 다시 누르면 꺼짐).
/// OnClick 대신 Start()에서 코드로 onValueChanged 리스너를 등록한다. 실제 씬 로드는 스테이지를 고르는 시점이
/// 아니라 미리보기의 Play 버튼(OnPlayButton)을 눌렀을 때 일어난다.
/// 해금 여부(3.7, 5.2)는 ProgressManager가 판정한다 — 토글은 항상 Interactable로 두고(잠긴 걸 눌렀을 때도
/// 반응이 오도록), 켜지려는 순간 잠겨있으면 코드에서 즉시 다시 끈다. 챕터/스테이지 목록이 갱신될 때마다
/// 잠김/해금/클리어 상태에 맞춰 각 토글의 ChapterStageButtonVisual 스프라이트도 갱신한다.
/// </summary>
public class MainMenuController : GameStateListener
{
    [Header("패널")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject stageSelectPanel; // 챕터 목록 패널 (챕터+스테이지+미리보기 전부 포함)
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject stagePanelRoot; // 스테이지 스크롤 (챕터를 골라야 보임)
    [SerializeField] GameObject previewPanel; // 미리보기 + Play 버튼 (스테이지를 골라야 보임)
    [SerializeField] Image previewImage;
    [SerializeField] Sprite defaultPreviewSprite; // 스테이지에 썸네일이 없을 때 대신 쓸 기본 이미지

    [Header("스테이지 데이터 (ClearScreenController와 공유하는 애셋)")]
    [SerializeField] StageTable stageTable;

    [Header("해금 표시용 토글 (인덱스 = 챕터/스테이지 번호)")]
    [SerializeField] Toggle[] chapterButtons;
    [SerializeField] Toggle[] stageButtons;

    int currentChapterIndex = -1;
    int currentStageIndex = -1;

    protected override void Start()
    {
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (stagePanelRoot) stagePanelRoot.SetActive(false);
        if (previewPanel) previewPanel.SetActive(false);

        RegisterToggleListeners();

        base.Start();
    }

    void RegisterToggleListeners()
    {
        if (chapterButtons != null)
            for (int i = 0; i < chapterButtons.Length; i++)
            {
                int index = i;
                if (chapterButtons[i]) chapterButtons[i].onValueChanged.AddListener(isOn => OnChapterToggleChanged(index, isOn));
            }

        if (stageButtons != null)
            for (int i = 0; i < stageButtons.Length; i++)
            {
                int index = i;
                if (stageButtons[i]) stageButtons[i].onValueChanged.AddListener(isOn => OnStageToggleChanged(index, isOn));
            }
    }

    /// <summary>스테이지 선택 패널이나 설정 패널이 열려있을 때 Esc를 누르면 메인 패널로 돌아간다
    /// (기존 "뒤로가기" 버튼과 동일한 동작).</summary>
    void Update()
    {
        if (!InputManager.Instance.ReadPause()) return;

        bool settingsOpen = settingsPanel && settingsPanel.activeSelf;
        bool stageSelectOpen = stageSelectPanel && stageSelectPanel.activeSelf;
        if (settingsOpen || stageSelectOpen) OnBackToMainButton();
    }

    protected override void OnGameStateChanged(GameState previous, GameState next)
    {
        bool show = next == GameState.MainMenu;
        if (mainPanel) mainPanel.SetActive(show);
        if (!show)
        {
            if (stageSelectPanel) stageSelectPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
            if (stagePanelRoot) stagePanelRoot.SetActive(false);
            if (previewPanel) previewPanel.SetActive(false);
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
        if (stageSelectPanel) stageSelectPanel.SetActive(true);

        ResetSelection();
        RefreshChapterButtons();
    }

    /// <summary>챕터/스테이지 선택 상태를 전부 초기화한다(챕터 목록에 다시 들어올 때마다 호출).</summary>
    void ResetSelection()
    {
        currentChapterIndex = -1;
        currentStageIndex = -1;
        if (stagePanelRoot) stagePanelRoot.SetActive(false);
        if (previewPanel) previewPanel.SetActive(false);

        if (chapterButtons != null)
            foreach (var t in chapterButtons)
                if (t) t.SetIsOnWithoutNotify(false);
        if (stageButtons != null)
            foreach (var t in stageButtons)
                if (t) t.SetIsOnWithoutNotify(false);
    }

    void RefreshChapterButtons()
    {
        if (chapterButtons == null) return;
        for (int i = 0; i < chapterButtons.Length; i++)
            SetChapterVisual(chapterButtons[i], i);
    }

    static void SetChapterVisual(Toggle toggle, int chapterIndex)
    {
        var visual = toggle ? toggle.GetComponent<ChapterStageButtonVisual>() : null;
        if (!visual) return;

        var state = !ProgressManager.Instance.IsChapterUnlocked(chapterIndex) ? ChapterStageButtonVisual.State.Locked
            : ProgressManager.Instance.IsChapterCleared(chapterIndex) ? ChapterStageButtonVisual.State.Cleared
            : ChapterStageButtonVisual.State.Unlocked;
        visual.SetState(state);
    }

    static void SetStageVisual(Toggle toggle, int chapterIndex, int stageIndex)
    {
        var visual = toggle ? toggle.GetComponent<ChapterStageButtonVisual>() : null;
        if (!visual) return;

        var state = !ProgressManager.Instance.IsStageUnlocked(chapterIndex, stageIndex) ? ChapterStageButtonVisual.State.Locked
            : ProgressManager.Instance.IsStageCleared(chapterIndex, stageIndex) ? ChapterStageButtonVisual.State.Cleared
            : ChapterStageButtonVisual.State.Unlocked;
        visual.SetState(state);
    }

    /// <summary>챕터 토글이 켜지거나 꺼질 때 호출된다(ToggleGroup으로 라디오 동작, allowSwitchOff로 다시 끄기 가능).
    /// 켜질 때 잠겨있으면 즉시 다시 끄고 Nope만 재생한다.</summary>
    void OnChapterToggleChanged(int chapterIndex, bool isOn)
    {
        if (isOn)
        {
            if (!ProgressManager.Instance.IsChapterUnlocked(chapterIndex))
            {
                chapterButtons[chapterIndex].SetIsOnWithoutNotify(false);
                GameAudio.Instance.PlayNope();
                return;
            }

            GameAudio.Instance.PlayButtonClick();
            currentChapterIndex = chapterIndex;
            currentStageIndex = -1;
            if (previewPanel) previewPanel.SetActive(false);
            if (stagePanelRoot) stagePanelRoot.SetActive(true);

            if (stageButtons != null)
            {
                foreach (var t in stageButtons)
                    if (t) t.SetIsOnWithoutNotify(false);
                for (int i = 0; i < stageButtons.Length; i++)
                    SetStageVisual(stageButtons[i], chapterIndex, i);
            }
        }
        else
        {
            GameAudio.Instance.PlayButtonClick();
            currentChapterIndex = -1;
            currentStageIndex = -1;
            if (stagePanelRoot) stagePanelRoot.SetActive(false);
            if (previewPanel) previewPanel.SetActive(false);
        }
    }

    /// <summary>스테이지 토글이 켜지거나 꺼질 때 호출된다. 켜지면 미리보기 패널을 보여주고,
    /// 실제 씬 로드는 하지 않는다(Play 버튼을 눌러야 시작됨).</summary>
    void OnStageToggleChanged(int stageIndex, bool isOn)
    {
        if (isOn)
        {
            if (currentChapterIndex < 0 || !ProgressManager.Instance.IsStageUnlocked(currentChapterIndex, stageIndex))
            {
                stageButtons[stageIndex].SetIsOnWithoutNotify(false);
                GameAudio.Instance.PlayNope();
                return;
            }

            GameAudio.Instance.PlayButtonClick();
            currentStageIndex = stageIndex;
            ShowPreview(currentChapterIndex, stageIndex);
        }
        else
        {
            GameAudio.Instance.PlayButtonClick();
            currentStageIndex = -1;
            if (previewPanel) previewPanel.SetActive(false);
        }
    }

    void ShowPreview(int chapterIndex, int stageIndex)
    {
        if (previewPanel) previewPanel.SetActive(true);
        if (!previewImage) return;

        Sprite thumbnail = null;
        var chapters = stageTable?.chapters;
        if (chapters != null && chapterIndex < chapters.Length)
        {
            var thumbnails = chapters[chapterIndex].thumbnails;
            if (thumbnails != null && stageIndex < thumbnails.Length) thumbnail = thumbnails[stageIndex];
        }
        previewImage.sprite = thumbnail ? thumbnail : defaultPreviewSprite;
    }

    /// <summary>미리보기의 Play 버튼: 현재 선택된 챕터/스테이지 기준으로 stageTable에서 씬 이름을 찾아 로드한다.</summary>
    public void OnPlayButton()
    {
        if (stageTable?.chapters == null) return;
        if (currentChapterIndex < 0 || currentChapterIndex >= stageTable.chapters.Length) return;
        if (currentStageIndex < 0) return;

        var scenes = stageTable.chapters[currentChapterIndex].sceneNames;
        if (currentStageIndex >= scenes.Length) return;
        string sceneName = scenes[currentStageIndex];
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
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    /// <summary>스테이지 선택/설정 패널을 숨기고 메인 패널로 돌아간다.</summary>
    public void OnBackToMainButton()
    {
        GameAudio.Instance.PlayButtonClick();
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
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

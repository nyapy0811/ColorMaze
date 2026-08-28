using System.Collections.Generic;
using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 클리어 진행도를 저장하고(SaveData.clearedStages), 챕터 해금 조건(5.2 — 챕터의
/// 8번째 스테이지(배열 인덱스 7, 응용 스테이지 마지막)를 클리어하면 다음 챕터 해금)을 판정한다.
/// StageTable은 Resources 폴더에서 불러온다 — 이 매니저는 MonoSingleton이라 씬 배치 없이
/// 자동 생성되므로, 인스펙터로 애셋을 연결할 방법이 없어 Resources.Load를 쓴다.
/// </summary>
public class ProgressManager : MonoSingleton<ProgressManager>
{
    const int UnlockStageIndex = 7;

    StageTable stageTable;

    /// <summary>히든 코드 등으로 활성화되는 전체 해금 오버라이드. 세이브 파일은 건드리지 않고
    /// 이번 실행 동안만 모든 챕터/스테이지를 해금 상태로 취급한다(클리어 여부 표시는 그대로 유지).</summary>
    bool unlockAllOverride;

    // ── 스테이지별 시도 횟수 ─────────────────────────────────────────
    // SceneRestarter가 씬을 다시 불러올 때마다 1 증가(=그 스테이지를 다시 시작한 횟수).
    // Framework.Core의 공용 SaveData(다른 게임과 공유)는 안 건드리고, ColorMaze 전용 파일로 따로 저장한다.

    const string AttemptsFileName = "stage_attempts.json";

    [System.Serializable]
    class StageAttemptEntry
    {
        public string sceneName;
        public int count;
    }

    [System.Serializable]
    class StageAttemptSaveData
    {
        public List<StageAttemptEntry> entries = new();
    }

    readonly Dictionary<string, int> attemptCounts = new();

    void LoadAttemptCounts()
    {
        var data = SaveManager.Instance.LoadJson<StageAttemptSaveData>(AttemptsFileName);
        foreach (var entry in data.entries)
            attemptCounts[entry.sceneName] = entry.count;
    }

    void SaveAttemptCounts()
    {
        var data = new StageAttemptSaveData();
        foreach (var kv in attemptCounts)
            data.entries.Add(new StageAttemptEntry { sceneName = kv.Key, count = kv.Value });
        SaveManager.Instance.SaveJson(AttemptsFileName, data);
    }

    /// <summary>SceneRestarter가 씬을 다시 불러올 때마다 호출한다.</summary>
    public void RecordStageAttempt(string sceneName)
    {
        attemptCounts.TryGetValue(sceneName, out int count);
        attemptCounts[sceneName] = count + 1;
        SaveAttemptCounts();
    }

    public int GetAttemptCount(string sceneName) => attemptCounts.GetValueOrDefault(sceneName, 0);

    public void UnlockAllStages()
    {
        unlockAllOverride = true;
        Debug.Log("[ProgressManager] 히든 코드로 모든 스테이지를 해금했습니다.");
    }

    protected override void OnAwake()
    {
        stageTable = Resources.Load<StageTable>("StageTable");
        if (stageTable == null)
            Debug.LogError("[ProgressManager] Resources/StageTable.asset을 찾을 수 없음.");

        LoadAttemptCounts();
    }

    void OnEnable() => EventBus.Subscribe<StageCleared>(OnStageCleared);
    void OnDisable() => EventBus.Unsubscribe<StageCleared>(OnStageCleared);

    void OnStageCleared(StageCleared e)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        SaveManager.Instance.Current.MarkStageCleared(sceneName);
        TryUnlockNextChapter(sceneName);
        SaveManager.Instance.Save();
    }

    void TryUnlockNextChapter(string clearedSceneName)
    {
        if (stageTable?.chapters == null) return;

        for (int chapter = 0; chapter < stageTable.chapters.Length; chapter++)
        {
            string[] scenes = stageTable.chapters[chapter].sceneNames;
            if (UnlockStageIndex >= scenes.Length || scenes[UnlockStageIndex] != clearedSceneName) continue;

            int nextChapter = chapter + 1;
            var save = SaveManager.Instance.Current;
            if (nextChapter < stageTable.chapters.Length && save.unlockedChapterCount <= chapter + 1)
                save.unlockedChapterCount = nextChapter + 1;
            return;
        }
    }

    public bool IsChapterUnlocked(int chapterIndex) => unlockAllOverride || chapterIndex < SaveManager.Instance.Current.unlockedChapterCount;

    /// <summary>챕터의 0번째 스테이지는 챕터만 해금돼 있으면 항상 열려있고, 그 외엔 바로 앞 스테이지를 클리어해야 열린다.</summary>
    public bool IsStageUnlocked(int chapterIndex, int stageIndex)
    {
        if (unlockAllOverride) return true;
        if (!IsChapterUnlocked(chapterIndex)) return false;
        if (stageIndex <= 0) return true;
        if (stageTable?.chapters == null || chapterIndex >= stageTable.chapters.Length) return false;

        string[] scenes = stageTable.chapters[chapterIndex].sceneNames;
        if (stageIndex - 1 >= scenes.Length) return false;

        string previousScene = scenes[stageIndex - 1];
        return !string.IsNullOrEmpty(previousScene) && SaveManager.Instance.Current.IsStageCleared(previousScene);
    }

    public bool IsStageCleared(int chapterIndex, int stageIndex)
    {
        if (stageTable?.chapters == null || chapterIndex >= stageTable.chapters.Length) return false;

        string[] scenes = stageTable.chapters[chapterIndex].sceneNames;
        if (stageIndex < 0 || stageIndex >= scenes.Length) return false;

        string sceneName = scenes[stageIndex];
        return !string.IsNullOrEmpty(sceneName) && SaveManager.Instance.Current.IsStageCleared(sceneName);
    }

    /// <summary>챕터의 모든 스테이지를 클리어했으면 챕터 클리어로 판정한다.</summary>
    public bool IsChapterCleared(int chapterIndex)
    {
        if (stageTable?.chapters == null || chapterIndex >= stageTable.chapters.Length) return false;

        string[] scenes = stageTable.chapters[chapterIndex].sceneNames;
        for (int i = 0; i < scenes.Length; i++)
            if (!IsStageCleared(chapterIndex, i)) return false;

        return true;
    }
}

using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 클리어 화면(3.6). 스테이지의 모든 캔버스를 완료하면(StageCleared) 열리고, GameManager는 이미
/// LevelManager에 의해 Cleared 상태로 바뀌어 있다(ESC 등 다른 입력은 Playing/Paused 기준이라 자동 무시됨).
/// 메인화면/다음 스테이지/다시하기 중 하나를 고를 수 있고, 뜬 채로 시간 제한은 없다.
/// "다음 스테이지"는 StageTable(공용 데이터 애셋)에서 현재 씬 다음 항목을 찾아 로드하며,
/// 마지막 스테이지라 다음이 없으면 해당 버튼이 비활성화된다.
/// </summary>
public class ClearScreenController : MonoBehaviour
{
    [SerializeField] GameObject clearPanel;
    [SerializeField] Button nextStageButton;
    [SerializeField] StageTable stageTable;

    string nextSceneName;

    void Start()
    {
        if (clearPanel) clearPanel.SetActive(false);
    }

    void OnEnable() => EventBus.Subscribe<StageCleared>(OnStageCleared);
    void OnDisable() => EventBus.Unsubscribe<StageCleared>(OnStageCleared);

    void OnStageCleared(StageCleared e)
    {
        nextSceneName = FindNextSceneName();
        if (nextStageButton) nextStageButton.interactable = !string.IsNullOrEmpty(nextSceneName);

        if (clearPanel) clearPanel.SetActive(true);

        // 시간을 멈춰서 캐릭터 조작도 같이 막는다(FirstPersonController가 Time.timeScale == 0이면 입력을 무시).
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>바로 다음 자리에 씬 이름이 없으면(리스트 끝이든, 중간에 비어있든) 마지막 스테이지로 취급한다.</summary>
    string FindNextSceneName()
    {
        if (stageTable == null) return null;
        var stages = stageTable.Flattened();
        string current = SceneManager.GetActiveScene().name;
        int index = stages.IndexOf(current);
        if (index < 0 || index + 1 >= stages.Count) return null;

        string next = stages[index + 1];
        return string.IsNullOrEmpty(next) ? null : next;
    }

    // --- 버튼 OnClick 연결용 ---

    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        GameManager.Instance.ChangeState(GameState.MainMenu);
        SceneLoader.Instance.Load("MainMenu");
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        string sceneName = SceneManager.GetActiveScene().name;
        GameManager.Instance.StartGame();
        SceneLoader.Instance.Load(sceneName);
    }

    /// <summary>다음 스테이지가 없으면(마지막 스테이지) 버튼이 비활성화돼 있어 호출되지 않는다.</summary>
    public void OnNextStageButton()
    {
        if (string.IsNullOrEmpty(nextSceneName)) return;
        Time.timeScale = 1f;
        GameManager.Instance.StartGame();
        SceneLoader.Instance.Load(nextSceneName);
    }
}

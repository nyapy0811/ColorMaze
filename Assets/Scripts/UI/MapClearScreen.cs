using Framework.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorMaze
{
    /// <summary>
    /// 임시 클리어 화면(UIScene, 7.6). '맵 클리어'를 받아 클리어 시간을 보이고, 시간을 멈추고 커서를 보인다(v1과 같은 방식).
    /// 버튼은 다시 하기와 종료다. 메인 메뉴·'다음 맵'·통계는 메뉴 흐름과 맵 목록 애셋을 만들 때 정식 클리어 화면에서 더한다.
    /// 상태가 Cleared라 ESC로 일시정지가 열리지 않는다.
    /// </summary>
    public class MapClearScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text timeText;
        [SerializeField] Button retryButton;
        [SerializeField] Button quitButton;

        void Awake()
        {
            panel.SetActive(false);
            retryButton.onClick.AddListener(SceneRestarter.Restart);
            quitButton.onClick.AddListener(GameManager.Instance.QuitGame);
        }

        void OnEnable() => EventBus.Subscribe<MapCleared>(OnMapCleared);
        void OnDisable() => EventBus.Unsubscribe<MapCleared>(OnMapCleared);

        void OnMapCleared(MapCleared e)
        {
            int minutes = (int)(e.ClearTime / 60f);
            timeText.text = $"클리어 시간 {minutes:00}:{e.ClearTime - minutes * 60f:00.00}";
            panel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

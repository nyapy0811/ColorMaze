using TMPro;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 진행 시간(화면 오른쪽 위, mm:ss). 맵 매니저의 클리어 시간을 그대로 보이므로 일시정지·클리어 뒤에는 멈추고
    /// '체크포인트로' 뒤에도 이어서 흐른다. 맵이 없으면(메뉴 등) 숨긴다. 초가 바뀔 때만 글자를 고친다.
    /// </summary>
    public class PlayTimeHud : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        int shownSeconds = -1;

        void Update()
        {
            var map = MapManager.Current;
            label.enabled = map != null;
            if (map == null) return;
            int seconds = (int)map.ClearTime;
            if (seconds == shownSeconds) return;
            shownSeconds = seconds;
            label.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}

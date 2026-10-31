using TMPro;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 'R G B' 값 상자(예: 2 0 0, 그림 2·3·5). 채널마다 글자가 하나라 프리팹 배치만 다르게 해서 같이 쓴다:
    /// 오브 칸의 마우스 올림 상자는 칸 없이 늘어놓고, 제출 화면의 요구 값은 칸을 나눈다(2|1|3). 조준 값 표시와 인벤토리 창의 채널 한도도 쓴다.
    /// </summary>
    public class OrbValueLabel : MonoBehaviour
    {
        [Tooltip("R, G, B 순서의 글자")]
        [SerializeField] TMP_Text[] channels = new TMP_Text[3];

        public void Show(OrbValue value)
        {
            channels[0].text = value.R.ToString();
            channels[1].text = value.G.ToString();
            channels[2].text = value.B.ToString();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}

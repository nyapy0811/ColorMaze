using TMPro;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 'R G B' 값 상자(예: 2 0 0, 그림 2·3·5). 오브 칸의 마우스 올림 값 상자가 쓰고, 조준 값 표시(8단계)도 같이 쓴다.
    /// </summary>
    public class OrbValueLabel : MonoBehaviour
    {
        [SerializeField] TMP_Text text;

        public void Show(OrbValue value)
        {
            text.text = value.ToString();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}

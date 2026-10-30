using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 연결색 목록(10.2). 문 계열은 이 목록의 번호만 들고, 연결색 표시가 여기서 색을 읽는다.
    /// 색 수와 색은 디자인할 때 정한다(3차 Q492). 지금은 임시 색이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ColorMaze/연결색 목록", fileName = "LinkColorConfig")]
    public class LinkColorConfig : ScriptableObject
    {
        public Color[] colors =
        {
            new(0f, 0.9f, 0.9f),
            new(0.95f, 0.2f, 0.85f),
            new(1f, 0.9f, 0.1f),
            new(1f, 0.5f, 0f),
        };
    }
}

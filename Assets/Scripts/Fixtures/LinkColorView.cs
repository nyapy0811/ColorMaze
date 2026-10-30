using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 연결색 표시의 겉모습(문 계열과 작동기가 같이 씀). 렌더러를 연결색 목록의 색으로 칠한다.
    /// 임시 표시라 실행 중에만 칠한다(색 목록은 맵 매니저가 든다).
    /// </summary>
    public class LinkColorView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Renderer[] renderers;

        MaterialPropertyBlock block;

        public void Show(int index)
        {
            var colors = MapManager.Current.LinkColors.colors;
            if (index < 0 || index >= colors.Length)
            {
                Debug.LogError($"[LinkColorView] 연결색 번호 {index}가 목록({colors.Length}개) 밖이다.", this);
                return;
            }
            block ??= new MaterialPropertyBlock();
            foreach (var target in renderers)
            {
                target.GetPropertyBlock(block);
                block.SetColor(BaseColorId, colors[index]);
                target.SetPropertyBlock(block);
            }
        }
    }
}

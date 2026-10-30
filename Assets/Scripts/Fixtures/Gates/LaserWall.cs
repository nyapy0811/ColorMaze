using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 레이저 벽(6.5). 모양은 같은 오브젝트의 <see cref="LaserSegment"/>(노드 시스템)가 맡고, 이 컴포넌트는 켜짐·꺼짐을 맡는다(11.4).
    /// 닫힘 = 레이저가 켜져 길을 막음, 열림 = 레이저가 꺼짐.
    /// </summary>
    [RequireComponent(typeof(LaserSegment))]
    public class LaserWall : GateBase
    {
        [SerializeField] LaserWallView view;

        protected override void ShowOpen(bool open) => view.SetOpen(open);
    }
}

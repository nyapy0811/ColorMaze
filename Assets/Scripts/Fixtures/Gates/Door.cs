using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 끼운 문(6.5). 모양은 같은 오브젝트의 <see cref="DoorPiece"/>(노드 시스템)가 맡고, 이 컴포넌트는 열림·닫힘을 맡는다(11.4).
    /// 문 프리팹에 붙어 있어 노드 시스템이 만든 문마다 생기고, 다시 만들어도 끼운 문 id로 남아 인스펙터 값이 지켜진다.
    /// </summary>
    [RequireComponent(typeof(DoorPiece))]
    public class Door : GateBase
    {
        [SerializeField] DoorView view;

        protected override void ShowOpen(bool open) => view.SetOpen(open);
    }
}

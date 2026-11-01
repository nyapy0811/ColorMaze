using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 조준 값 표시(UIScene HUD, 2차 Q461). '조준 대상 바뀜'을 받아 조준점 오른쪽 위에 'R G B' 상자를 보이거나 숨긴다.
    /// 팝업이 열리면 입력이 잠겨 조준 대상이 비므로 따로 듣지 않아도 숨는다.
    /// </summary>
    public class AimValueLabel : MonoBehaviour
    {
        [Tooltip("값 상자. 숨길 때 이 오브젝트를 끈다(이 컴포넌트는 늘 켜진 부모에 둔다).")]
        [SerializeField] ColorValueLabel label;

        void Awake() => label.Hide();

        void OnEnable() => EventBus.Subscribe<AimTargetChanged>(OnAimTargetChanged);
        void OnDisable() => EventBus.Unsubscribe<AimTargetChanged>(OnAimTargetChanged);

        void OnAimTargetChanged(AimTargetChanged e)
        {
            if (e.HasValue) label.Show(e.Value);
            else label.Hide();
        }
    }
}

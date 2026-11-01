using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 색 원천. 사물마다 다른 프리팹(이름은 사물 이름, 예: 소화기)에 붙인다. 렌더러를 값의 색(정규화 RGB × 오브젝트 밝기)으로
    /// 칠하고(에디터에서도 보임), 조준해 상호작용하면 값을 지금 색에 바로 더한다(채널마다 한도에서 자름). 한 번만 흡수할 수 있고,
    /// 흡수하면 흑백이 되고 강조를 끄고 '색 흡수'를 보낸다. 조준하면 흡수 전에만 값을 보인다. 흡수 상태의 저장은 체크포인트 단계에서 더한다.
    /// </summary>
    public class ColorSource : FixtureBase, IInteractable, IValueProvider
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("흡수되는 값. (0,0,0)과 세 채널이 같은 값은 쓰지 않는다(맵 검사가 알림).")]
        [SerializeField] ColorValue value = new(1, 0, 0);

        [Tooltip("값의 색으로 칠할 렌더러")]
        [SerializeField] Renderer[] coloredRenderers;

        [Tooltip("오브젝트 밝기(아트용, 값과 무관)")]
        [SerializeField, Range(0f, 1f)] float brightness = 1f;

        MaterialPropertyBlock block;
        bool absorbed;

        public ColorValue Value => value;
        public bool CanInteract => !absorbed;

        public bool TryGetDisplayValue(out ColorValue shown)
        {
            shown = value;
            return !absorbed;
        }

        void Awake() => Paint();

        void OnValidate() => Paint();

        public void Interact(PlayerContext player)
        {
            if (absorbed) return;
            player.Color.Absorb(value);
            absorbed = true;
            Paint();
            RefreshHighlight();
            EventBus.Publish(new ColorAbsorbed(Center, value));
        }

        Vector3 Center => coloredRenderers.Length > 0 ? coloredRenderers[0].bounds.center : transform.position;

        // 흡수 전은 값의 색, 흡수한 뒤는 그 색의 흑백.
        void Paint()
        {
            if (coloredRenderers == null) return;
            var color = ColorRules.ToColor(value) * brightness;
            color.a = 1f;
            if (absorbed) color = ColorRules.ToGrayscale(color);
            block ??= new MaterialPropertyBlock();
            foreach (var target in coloredRenderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                target.SetPropertyBlock(block);
            }
        }
    }
}

using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 추출 오브젝트. 사물마다 다른 프리팹(이름은 사물 이름, 예: 소화기)에 붙인다. 렌더러를 값의 색(정규화 RGB × 오브젝트 밝기)으로
    /// 칠하고(에디터에서도 보임), 조준해 상호작용하면 소지 오브에 값을 더한다. 성공하면 추출됨(흑백, 강조 끔, '추출함'),
    /// 소지 오브가 가득 차 있으면 '추출 불가'를 보낸다. 추출 상태의 저장은 체크포인트 단계에서 더한다.
    /// </summary>
    public class ColorSource : FixtureBase, IInteractable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("추출되는 오브 값. (0,0,0)과 세 채널이 같은 값은 쓰지 않는다(맵 검사가 알림).")]
        [SerializeField] OrbValue value = new(1, 0, 0);

        [Tooltip("값의 색으로 칠할 렌더러")]
        [SerializeField] Renderer[] coloredRenderers;

        [Tooltip("오브젝트 밝기(아트용, 값과 무관)")]
        [SerializeField, Range(0f, 1f)] float brightness = 1f;

        MaterialPropertyBlock block;
        bool extracted;

        public OrbValue Value => value;
        public bool CanInteract => !extracted;

        void Awake() => Paint();

        void OnValidate() => Paint();

        public void Interact(PlayerContext player)
        {
            if (extracted) return;
            if (!player.Inventory.Orbs.TryAdd(value, out _))
            {
                EventBus.Publish(new ExtractRejected(Center));
                return;
            }
            extracted = true;
            Paint();
            RefreshHighlight();
            EventBus.Publish(new OrbExtracted(Center, value));
        }

        Vector3 Center => coloredRenderers.Length > 0 ? coloredRenderers[0].bounds.center : transform.position;

        // 추출 전은 값의 색, 추출한 뒤는 그 색의 흑백.
        void Paint()
        {
            if (coloredRenderers == null) return;
            var color = OrbRules.ToColor(value) * brightness;
            color.a = 1f;
            if (extracted) color = OrbRules.ToGrayscale(color);
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

using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 작동기 안에 든 오브의 겉모습(2차 Q155). 든 오브가 있으면 그 값의 색과 발광으로 구를 보이고, 없으면 숨긴다.
    /// </summary>
    public class StoredOrbView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Renderer orb;

        MaterialPropertyBlock block;

        public void Show(OrbValue? stored)
        {
            orb.gameObject.SetActive(stored.HasValue);
            if (!stored.HasValue) return;

            var color = OrbRules.ToColor(stored.Value);
            block ??= new MaterialPropertyBlock();
            orb.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(EmissionColorId, color * OrbRules.GlowIntensity(stored.Value, MapManager.Current.Rules.glowFactor));
            orb.SetPropertyBlock(block);
        }
    }
}

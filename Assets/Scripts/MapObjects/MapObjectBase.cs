using UnityEngine;

/// <summary>
/// 모든 맵 기물(4장)의 공통 베이스.
/// 콜라이더 참조와 "이 콜라이더가 플레이어인가" 판정을 한 곳에서 제공해,
/// 각 그룹(판정형/획득형/클리어형/소모성) 베이스가 자기 역할만 신경 쓰면 되게 한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class MapObjectBase : MonoBehaviour
{
    [Header("상호작용 강조 표시 (없으면 무시)")]
    [SerializeField] GameObject highlightRoot;

    [Header("HUD 마커 프리팹 (없으면 MapObjectMarkerHUD의 기본 마커 사용)")]
    [SerializeField] GameObject markerPrefab;

    /// <summary>이 기물의 HUD 마커 프리팹. 비어 있으면 MapObjectMarkerHUD가 자기 기본 마커를 대신 쓴다.</summary>
    public GameObject MarkerPrefab => markerPrefab;

    protected Collider Col { get; private set; }

    /// <summary>씬의 플레이어(ColorStacks). ColorStacks.Awake에서 등록된 것을 그대로 참조한다.</summary>
    protected ColorStacks Player => ColorStacks.Instance;

    protected virtual void Awake() => Col = GetComponent<Collider>();

    /// <summary>이 콜라이더가 플레이어(의 자식 콜라이더)인지 확인한다.</summary>
    protected bool IsPlayer(Collider other) => Player != null && other.GetComponentInParent<ColorStacks>() == Player;

    /// <summary>상호작용 가능 강조 표시를 켜고 끈다. highlightRoot가 비어있으면 아무 일도 안 한다.</summary>
    public void SetHighlighted(bool on)
    {
        if (highlightRoot) highlightRoot.SetActive(on);
    }

    /// <summary>렌더러 목록 전체에 같은 색을 MaterialPropertyBlock으로 입힌다(인스펙터에서 대상을 직접 지정).</summary>
    protected static void ApplyColorTo(Renderer[] renderers, Color color)
    {
        if (renderers == null) return;

        var mpb = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            // _BaseColor = URP Lit/Unlit 색 프로퍼티.
            mpb.SetColor("_BaseColor", color);
            r.SetPropertyBlock(mpb);
        }
    }
}

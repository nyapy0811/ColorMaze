using UnityEngine;

/// <summary>
/// 색상 통과 블록(게이트).
/// 블록별로 R/G/B 스택을 지정하고, 같은 변환식으로 RGB를 만든다.
/// 플레이어의 변환 RGB가 이 게이트의 RGB와 같을 때만 통과 가능(그 외엔 솔리드로 막음).
/// 플레이어가 게이트를 통과 완료하면 플레이어의 스택을 초기화한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ColorGateBlock : MonoBehaviour
{
    [Header("이 게이트가 요구하는 스택")]
    [SerializeField, Min(0)] int red;
    [SerializeField, Min(0)] int green;
    [SerializeField, Min(0)] int blue;

    Collider col;
    ColorStacks player;
    bool passing;

    Color32 GateRGB => ColorStacks.ToRGB(red, green, blue);

    /// <summary>이 게이트의 변환 RGB(메시 병합 시 그룹 키).</summary>
    public Color32 RGB => GateRGB;

    /// <summary>정수 그리드 칸 좌표. 블록 중심 = (x, y+0.5, z).</summary>
    public Vector3Int GridCell => new Vector3Int(
        Mathf.RoundToInt(transform.position.x),
        Mathf.RoundToInt(transform.position.y - 0.5f),
        Mathf.RoundToInt(transform.position.z));

    /// <summary>개별 렌더러 표시 여부(병합 메시가 대신 그릴 때 끔).</summary>
    public void SetRendererEnabled(bool enabled)
    {
        if (TryGetComponent<Renderer>(out var r)) r.enabled = enabled;
    }

    void Awake() => col = GetComponent<Collider>();

    void Start()
    {
        player = FindAnyObjectByType<ColorStacks>();
        ApplyAppearance();
    }

    void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<ColorStacks>();
            if (player == null) return;
        }

        // 색이 일치하면 통과 가능(트리거로 전환), 아니면 솔리드로 막음.
        col.isTrigger = ColorEquals(player.CurrentRGB, GateRGB);
    }

    void OnTriggerEnter(Collider other)
    {
        if (player != null && other.GetComponentInParent<ColorStacks>() == player)
            passing = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (passing && player != null && other.GetComponentInParent<ColorStacks>() == player)
        {
            passing = false;
            player.ResetAll(); // 통과 완료 → 스택 초기화
        }
    }

    static bool ColorEquals(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;

    // GateMeshCombiner의 공용 머티리얼·투명도를 읽어 적용하고, 게이트 색을 입힌다(URP _BaseColor).
    public void ApplyAppearance()
    {
        if (!TryGetComponent<Renderer>(out var rend)) return;

        var config = FindAnyObjectByType<GateMeshCombiner>();

        // 미리 만들어 둔 Transparent 머티리얼을 공유 적용(인스턴스 생성 안 함).
        if (config != null && config.gateMaterial != null)
            rend.sharedMaterial = config.gateMaterial;

        float alpha = config != null ? config.gateAlpha : 0.5f;
        Color32 c = GateRGB;
        c.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);

        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        rend.SetPropertyBlock(mpb);
    }

    void OnValidate() => ApplyAppearance();
}

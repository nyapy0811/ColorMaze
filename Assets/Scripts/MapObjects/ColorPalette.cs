using UnityEngine;

/// <summary>
/// 컬러 팔레트(4.1).
/// 충돌하면 지정된 만큼 RGB 스택이 증가한다. 사라지지 않아 반복 획득할 수 있다.
/// </summary>
[RequireComponent(typeof(FloatingBob))]
public class ColorPalette : AcquireObjectBase
{
    [Header("증가시킬 스택량")]
    [SerializeField] int red;
    [SerializeField] int green;
    [SerializeField] int blue;

    protected override void Awake()
    {
        base.Awake();
        ApplyColor();
    }

    // 오브젝트 중심에 고정되어 항상 플레이어(카메라)를 바라보는 라벨로 R/G/B 스택량을 표시한다.
    void Start()
    {
        Vector3Int cell = new Vector3Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y - 0.5f),
            Mathf.RoundToInt(transform.position.z));

        string text = $"<color=#FF0000>{red}</color> <color=#00FF00>{green}</color> <color=#0000FF>{blue}</color>";
        CellGroupLabel.Create<BillboardCenterLabel>(transform, "Label", new[] { cell }, text);
    }

    // 인스펙터에서 값을 바꾸면 에디터에서도 바로 색이 반영되게 한다.
    void OnValidate() => ApplyColor();

    // 지정된 R/G/B 스택량을 색으로 변환해 이 블록의 외형에 그대로 반영한다(필터와 같은 변환식 사용).
    void ApplyColor()
    {
        if (!TryGetComponent<Renderer>(out var r)) return;
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", ColorStacks.ToRGB(red, green, blue));
        r.SetPropertyBlock(mpb);
    }

    protected override void OnAcquire(ColorStacks player)
    {
        if (red != 0) player.Add(LightColor.Red, red);
        if (green != 0) player.Add(LightColor.Green, green);
        if (blue != 0) player.Add(LightColor.Blue, blue);
    }
}

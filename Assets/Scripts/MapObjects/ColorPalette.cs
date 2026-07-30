using TMPro;
using UnityEngine;

/// <summary>
/// 컬러 팔레트(4.1).
/// 충돌하면 지정된 만큼 RGB 스택이 증가한다. 사라지지 않아 반복 획득할 수 있다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ColorPalette : AcquireObjectBase
{
    [Header("증가시킬 스택량")]
    [SerializeField] int red;
    [SerializeField] int green;
    [SerializeField] int blue;

    [Header("스택 색을 입힐 렌더러 목록")]
    [SerializeField] Renderer[] stackColorRenderers;

    protected override void Awake()
    {
        base.Awake();
        FitCollider();
        ApplyColor();
    }

    // 오브젝트 중심에 고정되어 항상 플레이어(카메라)를 바라보는 라벨로 R/G/B 스택량을 표시한다.
    // 라벨 자동 생성은 필터만 하므로(FilterBlockBase), 팔레트는 자식으로 이미 있는 라벨을 찾아서 쓴다.
    void Start()
    {
        var label = GetComponentInChildren<BillboardCenterLabel>();
        if (label == null) return; // 자식에 라벨이 없으면 아무것도 안 함(자동 생성하지 않음)
        if (!label.TryGetComponent<TextMeshPro>(out var tmp)) return;

        Vector3Int cell = new Vector3Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y - 0.5f),
            Mathf.RoundToInt(transform.position.z));

        tmp.text = StackLabelFormat.ByValue(red, green, blue);
        label.Init(new[] { cell }, tmp);
    }

    // 인스펙터에서 값을 바꾸면 에디터에서도 바로 색이 반영되게 한다.
    void OnValidate()
    {
        FitCollider();
        ApplyColor();
    }

    // 콜라이더(BoxCollider)를 자식 렌더러들의 실제 형태에 맞춰 자동으로 재계산한다.
    // 팔레트 모델이 여러 조각(실린더 등)으로 구성되어 있고, 외부 리모델링 툴을 쓸 때마다
    // MeshCollider가 초기화되는 문제가 있어서, 메시를 참조하지 않는 BoxCollider로 대체하고
    // 매번 자동으로 크기를 맞춘다.
    void FitCollider()
    {
        if (!TryGetComponent<BoxCollider>(out var box)) return;

        var renderers = GetComponentsInChildren<Renderer>(true);
        bool has = false;
        Bounds b = default;
        foreach (var r in renderers)
        {
            if (!r.enabled) continue; // 비활성 렌더러(아웃라인 등)는 제외
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!has) return;

        Vector3 s = transform.lossyScale;
        box.center = transform.InverseTransformPoint(b.center);
        box.size = new Vector3(
            b.size.x / Mathf.Max(Mathf.Abs(s.x), 0.0001f),
            b.size.y / Mathf.Max(Mathf.Abs(s.y), 0.0001f),
            b.size.z / Mathf.Max(Mathf.Abs(s.z), 0.0001f));
    }

    // +Y축을 정면으로 삼아 항상 플레이어(카메라)를 바라본다.
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = cam.transform.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f);
    }

    // 지정된 R/G/B 스택량을 색으로 변환해 stackColorRenderers에 그대로 반영한다(필터와 같은 변환식 사용).
    void ApplyColor() => ApplyColorTo(stackColorRenderers, ColorStacks.ToRGB(red, green, blue));

    protected override void OnAcquire(ColorStacks player)
    {
        if (red != 0) player.Add(LightColor.Red, red);
        if (green != 0) player.Add(LightColor.Green, green);
        if (blue != 0) player.Add(LightColor.Blue, blue);
    }
}

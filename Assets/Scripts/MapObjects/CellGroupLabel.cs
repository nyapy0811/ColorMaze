using TMPro;
using UnityEngine;

/// <summary>
/// 그리드 셀 그룹 위에 텍스트 라벨 하나를 띄우는 범용 컴포넌트(필터 전용이 아님).
/// 매 프레임 그룹의 칸(셀)들 중 카메라와 가장 가까운 칸을 찾아, 그 칸의 카메라 쪽 면 위에
/// 라벨을 배치한다. 회전은 빌보드가 아니라 그 면의 바깥 법선 방향으로 고정된다.
/// 위치/회전 계산 방식을 바꾸고 싶으면 이 클래스를 상속해 protected virtual 메서드를 오버라이드하면 된다.
/// </summary>
public class CellGroupLabel : MonoBehaviour
{
    protected Vector3Int[] cells;
    protected TextMeshPro text;

    /// <summary>parent 아래에 텍스트 라벨 오브젝트를 만들고 셀 그룹을 등록한다.</summary>
    public static CellGroupLabel Create(Transform parent, string name, Vector3Int[] groupCells, string labelText)
        => Create<CellGroupLabel>(parent, name, groupCells, labelText);

    /// <summary>위와 같지만, 위치/회전 계산을 다르게 오버라이드한 하위 클래스로 만들고 싶을 때 사용한다.</summary>
    public static T Create<T>(Transform parent, string name, Vector3Int[] groupCells, string labelText) where T : CellGroupLabel
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = labelText;
        tmp.fontSize = 3f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var label = go.AddComponent<T>();
        label.Init(groupCells, tmp);
        return label;
    }

    public void Init(Vector3Int[] groupCells, TextMeshPro tmp)
    {
        cells = groupCells;
        text = tmp;
    }

    protected virtual void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) { text.enabled = false; return; }

        Vector3 camPos = cam.transform.position;
        if (!FindNearestCellCenter(camPos, out Vector3 cellCenter))
        {
            text.enabled = false;
            return;
        }

        Vector3 normal = DominantAxisNormal(camPos - cellCenter);

        text.enabled = true;
        transform.position = GetLabelPosition(cellCenter, normal);
        transform.rotation = GetLabelRotation(normal);
    }

    /// <summary>카메라와 가장 가까운 셀의 중심을 찾는다.</summary>
    protected virtual bool FindNearestCellCenter(Vector3 camPos, out Vector3 cellCenter)
    {
        float bestDist = float.MaxValue;
        cellCenter = default;
        bool found = false;
        foreach (var cell in cells)
        {
            Vector3 c = new Vector3(cell.x, cell.y + 0.5f, cell.z);
            float d = (c - camPos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; cellCenter = c; found = true; }
        }
        return found;
    }

    /// <summary>toCam 방향에서 가장 두드러진 축을 기준으로 바깥 법선을 구한다.</summary>
    protected static Vector3 DominantAxisNormal(Vector3 toCam)
    {
        float ax = Mathf.Abs(toCam.x), ay = Mathf.Abs(toCam.y), az = Mathf.Abs(toCam.z);
        return (ax >= ay && ax >= az) ? new Vector3(Mathf.Sign(toCam.x), 0f, 0f)
            : (ay >= az) ? new Vector3(0f, Mathf.Sign(toCam.y), 0f)
            : new Vector3(0f, 0f, Mathf.Sign(toCam.z));
    }

    protected virtual Vector3 GetLabelPosition(Vector3 cellCenter, Vector3 normal) => cellCenter + normal * 0.52f;

    // TextMeshPro는 카메라 반대 방향(뷰어에서 멀어지는 쪽)을 forward로 둬야 좌우가 뒤집히지 않는다.
    protected virtual Quaternion GetLabelRotation(Vector3 normal)
    {
        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(-normal, up);
    }
}

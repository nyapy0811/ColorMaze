using System.Collections.Generic;
using Framework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 필터를 제외한 맵 기물(컬러 팔레트·스택 체인저·컬러 체인저·지우개·캔버스)마다 화면에 위치 마커를 띄운다.
/// 기물 종류별로 다른 마커 프리팹을 쓰며, 화면 밖(카메라 뒤쪽 포함)에 있으면 가장자리로 클램프한다.
/// 기물이 파괴(소모)되면 그 마커도 같이 사라진다.
/// </summary>
public class MapObjectMarkerHUD : MonoBehaviour
{
    [Header("기물 종류별 마커 프리팹 (RectTransform 필요)")]
    [SerializeField] GameObject colorPaletteMarkerPrefab;
    [SerializeField] GameObject stackChangerMarkerPrefab;
    [SerializeField] GameObject colorChangerMarkerPrefab;
    [SerializeField] GameObject eraserMarkerPrefab;
    [SerializeField] GameObject colorCanvasMarkerPrefab;

    [SerializeField] RectTransform markerContainer;

    [Tooltip("화면 가장자리 클램프 여백(픽셀)")]
    [SerializeField] float edgeMargin = 40f;

    readonly Dictionary<MapObjectBase, RectTransform> markers = new();

    void Start() => Refresh();

    void OnEnable() => EventBus.Subscribe<SceneLoadCompleted>(OnSceneLoaded);
    void OnDisable() => EventBus.Unsubscribe<SceneLoadCompleted>(OnSceneLoaded);

    void OnSceneLoaded(SceneLoadCompleted e) => Refresh();

    void Refresh()
    {
        foreach (var marker in markers.Values)
            if (marker) Destroy(marker.gameObject);
        markers.Clear();

        var objects = FindObjectsByType<MapObjectBase>(FindObjectsSortMode.None);
        foreach (var obj in objects)
        {
            if (obj is FilterBlockBase) continue;

            var prefab = PrefabFor(obj);
            if (prefab == null) continue;

            var marker = Instantiate(prefab, markerContainer).GetComponent<RectTransform>();
            markers[obj] = marker;
        }
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        List<MapObjectBase> destroyed = null;
        foreach (var pair in markers)
        {
            if (pair.Key == null)
            {
                if (pair.Value) Destroy(pair.Value.gameObject);
                (destroyed ??= new List<MapObjectBase>()).Add(pair.Key);
                continue;
            }
            PositionMarker(cam, pair.Key.transform.position, pair.Value);
        }

        if (destroyed != null)
            foreach (var key in destroyed)
                markers.Remove(key);
    }

    void PositionMarker(Camera cam, Vector3 worldPos, RectTransform marker)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        bool behind = screenPos.z < 0f;
        if (behind)
        {
            // 카메라 뒤쪽이면 좌표를 화면 중심 기준으로 뒤집어서 클램프 방향이 맞도록 한다(흔한 웨이포인트 처리 방식).
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 pos = new Vector2(screenPos.x, screenPos.y);

        float minX = edgeMargin, maxX = Screen.width - edgeMargin;
        float minY = edgeMargin, maxY = Screen.height - edgeMargin;

        bool clamped = behind || pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY;
        Vector2 dir = Vector2.up;

        if (clamped)
        {
            dir = (pos - center).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;

            float tx = dir.x != 0f ? ((dir.x > 0 ? maxX : minX) - center.x) / dir.x : float.PositiveInfinity;
            float ty = dir.y != 0f ? ((dir.y > 0 ? maxY : minY) - center.y) / dir.y : float.PositiveInfinity;
            float t = Mathf.Min(Mathf.Abs(tx), Mathf.Abs(ty));

            pos = center + dir * t;
        }

        marker.position = pos; // Screen Space - Overlay 캔버스라 스크린 좌표를 그대로 써도 된다.

        // 화면 밖이면 화살표로 방향만 표시. 화면 안이면 지형에 가려서 안 보일 때만 원형 아이콘을 표시한다
        // (직접 보이는 기물은 마커가 필요 없으므로 아이콘도 끈다).
        bool occluded = !clamped && IsOccluded(cam, worldPos);

        var arrow = marker.Find("Arrow") as RectTransform;
        if (arrow != null)
        {
            arrow.gameObject.SetActive(clamped);
            if (clamped)
                arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

            var icon = marker.GetComponent<Image>();
            if (icon) icon.enabled = occluded;
        }
        else
        {
            marker.localRotation = clamped
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f)
                : Quaternion.identity;
        }
    }

    /// <summary>카메라와 목표 사이에 트리거가 아닌 콜라이더(벽 등)가 있으면 가려진 것으로 본다.</summary>
    static bool IsOccluded(Camera cam, Vector3 worldPos)
    {
        Vector3 origin = cam.transform.position;
        Vector3 offset = worldPos - origin;
        float dist = offset.magnitude;
        if (dist < 0.01f) return false;

        return Physics.Raycast(origin, offset / dist, dist - 0.1f, ~0, QueryTriggerInteraction.Ignore);
    }

    GameObject PrefabFor(MapObjectBase obj) => obj switch
    {
        ColorCanvas => colorCanvasMarkerPrefab,
        ColorPalette => colorPaletteMarkerPrefab,
        StackChanger => stackChangerMarkerPrefab,
        ColorChanger => colorChangerMarkerPrefab,
        Eraser => eraserMarkerPrefab,
        _ => null
    };
}

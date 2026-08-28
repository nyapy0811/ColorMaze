using System.Collections.Generic;
using Framework.Core;
using UnityEngine;

/// <summary>
/// 필터를 제외한 맵 기물(컬러 팔레트·스택 체인저·컬러 체인저·버킷·캔버스)마다 화면에 위치 마커를 띄운다.
/// 마커 프리팹은 기본값 하나(defaultMarkerPrefab)를 공유하고, 기물별로 다른 마커가 필요하면
/// MapObjectBase.markerPrefab에 개별 지정하면 된다(지정 안 하면 기본값 사용). 화면 밖(카메라 뒤쪽 포함)에 있으면 가장자리로 클램프한다.
/// 기물이 파괴(소모)되면 그 마커도 같이 사라진다.
/// </summary>
public class MapObjectMarkerHUD : MonoBehaviour
{
    [Header("기본 마커 프리팹 (RectTransform 필요, 기물이 자기 markerPrefab을 안 지정했을 때 사용)")]
    [SerializeField] GameObject defaultMarkerPrefab;

    [SerializeField] RectTransform markerContainer;

    [Tooltip("화면 가장자리 클램프 여백(픽셀)")]
    [SerializeField] float edgeMargin = 40f;

    [Tooltip("가림(occlusion) 판정 레이캐스트가 검사할 레이어. 필요 없는 레이어를 빼면 비용을 줄일 수 있다")]
    [SerializeField] LayerMask occlusionMask = ~0;

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

        var guide = StageGuideController.Instance;

        List<MapObjectBase> destroyed = null;
        foreach (var pair in markers)
        {
            if (pair.Key == null)
            {
                if (pair.Value) Destroy(pair.Value.gameObject);
                (destroyed ??= new List<MapObjectBase>()).Add(pair.Key);
                continue;
            }

            // 가이드 마커가 뜬 기물은 일반 마커를 끈다(같은 자리에 둘이 겹치지 않게 — 가이드가 우선).
            bool isGuideTarget = guide.GuideActive && (pair.Key == guide.Current1 || pair.Key == guide.Current2);
            pair.Value.gameObject.SetActive(!isGuideTarget);
            if (isGuideTarget) continue;

            PositionMarker(cam, pair.Key.transform.position, pair.Value);
        }

        if (destroyed != null)
            foreach (var key in destroyed)
                markers.Remove(key);
    }

    void PositionMarker(Camera cam, Vector3 worldPos, RectTransform marker)
        => ScreenMarkerUtil.Position(cam, worldPos, marker, edgeMargin, occlusionMask);

    /// <summary>기물이 자기 markerPrefab을 지정했으면 그것을, 아니면 기본 마커를 쓴다.
    /// 새 기물 타입이 추가돼도 이 파일을 고칠 필요가 없다(MapObjectBase.markerPrefab 참고).</summary>
    GameObject PrefabFor(MapObjectBase obj) => obj.MarkerPrefab != null ? obj.MarkerPrefab : defaultMarkerPrefab;
}

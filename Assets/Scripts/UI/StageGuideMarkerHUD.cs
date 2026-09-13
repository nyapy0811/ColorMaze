using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 가이드가 켜져 있을 때, 정답 리스트마다 "다음 순번" 기물 위치에 마커를 띄운다.
/// 리스트 순서대로 무지개 7색(빨/주/노/초/파/남/보)을 배정한다 — 챕터 스테이지는 리스트가 최대 2개라
/// 빨강/파랑만 쓰이고, 맵 에디터 커스텀 스테이지는 캔버스 개수(최대 7개)만큼 쓰인다. 기존
/// MapObjectMarkerHUD(모든 기물 마커)와는 별개로 같이 표시된다. 필터는 병합 그룹
/// 중앙(GroupFillRenderer.bounds.center)을 가리킨다.
/// </summary>
public class StageGuideMarkerHUD : MonoBehaviour
{
    [Header("마커 프리팹 (RectTransform 필요, Image 색으로 리스트를 구분한다)")]
    [SerializeField] GameObject markerPrefab;

    [SerializeField] RectTransform markerContainer;

    [Tooltip("화면 가장자리 클램프 여백(픽셀)")]
    [SerializeField] float edgeMargin = 40f;

    [Tooltip("가림(occlusion) 판정 레이캐스트가 검사할 레이어")]
    [SerializeField] LayerMask occlusionMask = ~0;

    [Tooltip("일반 마커와 구분되도록 가이드 마커에 곱할 배율")]
    [SerializeField] float markerScale = 2f;

    static readonly Color[] RainbowColors =
    {
        Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.green,
        Color.blue, new Color(0.29f, 0f, 0.51f), new Color(0.56f, 0f, 1f),
    }; // 빨/주/노/초/파/남/보

    RectTransform[] markers;

    void Awake()
    {
        markers = new RectTransform[RainbowColors.Length];
        for (int i = 0; i < markers.Length; i++)
            markers[i] = CreateMarker(RainbowColors[i]);
    }

    RectTransform CreateMarker(Color color)
    {
        if (markerPrefab == null || markerContainer == null) return null;

        var rt = Instantiate(markerPrefab, markerContainer).GetComponent<RectTransform>();
        foreach (var img in rt.GetComponentsInChildren<Image>(true))
            img.color = color;
        rt.localScale *= markerScale;
        rt.gameObject.SetActive(false);
        return rt;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        var guide = StageGuideController.Instance;

        for (int i = 0; i < markers.Length; i++)
            UpdateMarker(markers[i], cam, guide.CurrentTarget(i));
    }

    void UpdateMarker(RectTransform marker, Camera cam, MapObjectBase target)
    {
        if (marker == null) return;

        if (cam == null || target == null)
        {
            marker.gameObject.SetActive(false);
            return;
        }

        marker.gameObject.SetActive(true);

        // 필터는 개별 셀이 아니라 병합된 그룹(같은 색 + 서로 붙어있는 셀들) 전체의 중앙을 가리킨다.
        Vector3 worldPos = target is FilterBlockBase filter && filter.GroupFillRenderer != null
            ? filter.GroupFillRenderer.bounds.center
            : target.transform.position;

        ScreenMarkerUtil.Position(cam, worldPos, marker, edgeMargin, occlusionMask, alwaysVisible: true);
    }
}

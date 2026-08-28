using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 가이드가 켜져 있을 때, 정답 리스트의 "다음 순번" 기물 위치에 마커를 띄운다.
/// 리스트1의 현재 타깃 = 빨강, 리스트2의 현재 타깃 = 파랑. 최대 2개만 뜨며, 기존 MapObjectMarkerHUD(모든
/// 기물 마커)와는 별개로 같이 표시된다. 필터는 병합 그룹 중앙(GroupFillRenderer.bounds.center)을 가리킨다.
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

    RectTransform marker1; // 리스트1 — 빨강
    RectTransform marker2; // 리스트2 — 파랑

    void Awake()
    {
        marker1 = CreateMarker(Color.red);
        marker2 = CreateMarker(Color.blue);
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

        UpdateMarker(marker1, cam, guide.Current1);
        UpdateMarker(marker2, cam, guide.Current2);
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

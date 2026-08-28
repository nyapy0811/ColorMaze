using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 월드 좌표를 화면 마커 위치로 투영하는 공용 로직.
/// MapObjectMarkerHUD(모든 기물 마커)와 StageGuideMarkerHUD(가이드 마커)가 공유한다.
/// </summary>
public static class ScreenMarkerUtil
{
    /// <summary>월드 좌표를 화면 마커 위치로 투영한다. 화면 밖(카메라 뒤쪽 포함)이면 가장자리로 클램프하고
    /// "Arrow" 자식이 있으면 화살표로, 없으면 마커 자신을 회전시켜 방향을 표시한다. 화면 안인데
    /// 가려져 있으면(occlusionMask 기준 레이캐스트) "Arrow" 옆의 Image를 켠다.
    /// alwaysVisible이 true면 화면 안에 있을 때 가려짐 여부와 상관없이 항상 아이콘을 켠다(가이드 마커용).</summary>
    public static void Position(Camera cam, Vector3 worldPos, RectTransform marker, float edgeMargin, LayerMask occlusionMask, bool alwaysVisible = false)
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
        // (직접 보이는 기물은 마커가 필요 없으므로 아이콘도 끈다) — alwaysVisible이면 이 예외 없이 항상 켠다.
        bool occluded = !clamped && IsOccluded(cam, worldPos, occlusionMask);
        bool showIcon = !clamped && (alwaysVisible || occluded);

        var arrow = marker.Find("Arrow") as RectTransform;
        if (arrow != null)
        {
            arrow.gameObject.SetActive(clamped);
            if (clamped)
                arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

            var icon = marker.GetComponent<Image>();
            if (icon) icon.enabled = showIcon;
        }
        else
        {
            marker.localRotation = clamped
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f)
                : Quaternion.identity;
        }
    }

    /// <summary>카메라와 목표 사이에 트리거가 아닌 콜라이더(벽 등)가 있으면 가려진 것으로 본다.</summary>
    public static bool IsOccluded(Camera cam, Vector3 worldPos, LayerMask occlusionMask)
    {
        Vector3 origin = cam.transform.position;
        Vector3 offset = worldPos - origin;
        float dist = offset.magnitude;
        if (dist < 0.01f) return false;

        return Physics.Raycast(origin, offset / dist, dist - 0.1f, occlusionMask, QueryTriggerInteraction.Ignore);
    }
}

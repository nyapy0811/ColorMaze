using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CellGroupLabel의 변형: 컬러 필터 전용 라벨 배치 규칙.
/// 라벨을 카메라(플레이어) 시선의 중심선이 이 필터 그룹과 만나는 지점에 놓고, 항상 카메라를 바라본다(빌보드).
/// - 시선 중심선이 이 그룹과 전혀 만나지 않으면 라벨을 숨긴다.
/// - 만난 지점이 그룹의 바깥 경계에 너무 가까우면(라벨이 테두리를 벗어날 수 있는 위치) 안쪽으로 밀어 넣는다.
/// - 시선이 여러 컬러 필터 그룹을 동시에 지나가면(예: 앞뒤로 겹친 필터), 교차점이 카메라와 가장 가까운
///   그룹의 라벨만 보이게 하고 나머지는 숨긴다 — 그래서 모든 인스턴스가 매 프레임 결과를 공유해야 하며,
///   프레임당 한 번만(첫 인스턴스가) 전체를 다시 계산하고 나머지는 그 결과를 읽기만 한다.
/// </summary>
public class CenterFacingLabel : CellGroupLabel
{
    // 교차점이 그룹 경계에서 이만큼(유닛)은 떨어지도록 안쪽으로 들여쓴다(텍스트가 테두리를 넘지 않도록).
    const float BorderMargin = 0.12f;

    static readonly List<CenterFacingLabel> instances = new();
    static int computedFrame = -1;

    bool queueFixed;
    bool boundsReady;
    Vector3 groupMin, groupMax;

    bool hasIntersection;
    Vector3 intersectionPoint;
    float hitDistance;
    bool isNearestHit;

    void OnEnable() => instances.Add(this);
    void OnDisable() => instances.Remove(this);

    protected override void LateUpdate()
    {
        if (text == null) return; // Init()이 아직 안 됨

        // 라벨이 큐브 중앙(필터의 반투명 채움 메시 내부)에 놓이기 때문에, 같은 Transparent 큐 안에서
        // 카메라 거리 기준으로 그리기 순서가 매 프레임 조금씩 바뀌며 깜빡이는 문제가 생긴다.
        // 렌더 큐를 채움/테두리 메시보다 뒤로 고정해서 항상 그 위에 그려지게 한다(한 번만 적용).
        if (!queueFixed)
        {
            queueFixed = true;
            var mat = text.fontMaterial; // 접근 시 이 텍스트 전용 인스턴스가 자동 생성됨(공유 애셋 변경 아님)
            if (mat != null) mat.renderQueue += 100;
        }

        // 같은 프레임에 이미 전체(모든 컬러 필터 라벨)를 계산했으면 다시 계산하지 않는다.
        if (Time.frameCount != computedFrame)
        {
            computedFrame = Time.frameCount;
            RecomputeAll();
        }

        ApplyResult();
    }

    // 카메라 시선 중심선과 각 라벨 그룹의 교차 여부/거리를 전부 계산하고, 그중 가장 가까운 것 하나만 표시 대상으로 정한다.
    static void RecomputeAll()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            foreach (var inst in instances) inst.hasIntersection = false;
            return;
        }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        CenterFacingLabel nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var inst in instances)
        {
            inst.hasIntersection = inst.FindSightIntersection(origin, dir, out inst.intersectionPoint, out inst.hitDistance);
            if (inst.hasIntersection && inst.hitDistance < nearestDist)
            {
                nearestDist = inst.hitDistance;
                nearest = inst;
            }
        }

        // 시선상에서 이 컬러 필터보다 더 가까운 다른 물체(기본 블록 등)에 가로막혀 있으면 라벨을 보이지 않는다.
        if (nearest != null && IsOccluded(origin, dir, nearestDist))
            nearest = null;

        foreach (var inst in instances)
            inst.isNearestHit = inst == nearest;
    }

    // 목표 지점(targetDist)보다 가까운 곳에 뭔가 있으면 가로막힌 것으로 본다. targetDist 바로 앞은
    // 필터 자신의 콜라이더라 제외해야 하므로, 살짝(OcclusionEpsilon만큼) 짧은 거리까지만 검사한다.
    const float OcclusionEpsilon = 0.05f;

    static bool IsOccluded(Vector3 origin, Vector3 dir, float targetDist)
    {
        float checkDist = targetDist - OcclusionEpsilon;
        return checkDist > 0f && Physics.Raycast(origin, dir, checkDist, ~0, QueryTriggerInteraction.Collide);
    }

    // 이 거리에서 스케일 1(원래 크기)이 되도록 기준을 잡는다 — 카메라와의 거리에 비례해서 스케일을
    // 조절하면(원근 축소를 상쇄) 가까이서 보든 멀리서 보든 화면상 크기가 항상 똑같아 보인다.
    const float ReferenceDistance = 50f;

    void ApplyResult()
    {
        if (!hasIntersection || !isNearestHit)
        {
            text.enabled = false;
            return;
        }

        text.enabled = true;
        transform.position = intersectionPoint;

        Vector3 toCam = Camera.main.transform.position - intersectionPoint;
        float dist = toCam.magnitude;
        if (dist > 0.0001f)
            transform.rotation = Quaternion.LookRotation(-toCam, Vector3.up);

        transform.localScale = Vector3.one * (dist / ReferenceDistance);
    }

    // 시선 중심선(origin에서 dir 방향 반직선)이 이 그룹의 칸들과 만나는 가장 가까운 지점을 찾는다.
    // 찾으면 그 지점을 그룹 경계 안쪽으로 보정해서 point/dist에 담고 true를 반환한다.
    bool FindSightIntersection(Vector3 origin, Vector3 dir, out Vector3 point, out float dist)
    {
        EnsureBounds();

        float bestT = float.MaxValue;
        int bestAxis = -1;
        bool found = false;

        foreach (var cell in cells)
        {
            if (RayIntersectsCell(origin, dir, cell, out float t, out int axis) && t < bestT)
            {
                bestT = t;
                bestAxis = axis;
                found = true;
            }
        }

        if (!found)
        {
            point = default;
            dist = 0f;
            return false;
        }

        point = ClampToGroupBounds(origin + dir * bestT, bestAxis);
        dist = bestT;
        return true;
    }

    void EnsureBounds()
    {
        if (boundsReady) return;
        boundsReady = true;

        groupMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        groupMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var cell in cells)
        {
            Vector3 min = new Vector3(cell.x - 0.5f, cell.y, cell.z - 0.5f);
            Vector3 max = new Vector3(cell.x + 0.5f, cell.y + 1f, cell.z + 0.5f);
            groupMin = Vector3.Min(groupMin, min);
            groupMax = Vector3.Max(groupMax, max);
        }
    }

    // 교차점을 grupMin~groupMax 안쪽(테두리 여유 BorderMargin만큼 들여쓴 범위)으로 클램프한다.
    // 필터 면의 법선 축(flatAxis)은 그대로 두고, 면 위의 나머지 두 축만 보정한다.
    // (그룹이 오목한 모양이면 완벽하진 않지만, 지금까지 만든 필터는 대부분 평평한 사각/계단형이라 충분하다.)
    Vector3 ClampToGroupBounds(Vector3 p, int flatAxis)
    {
        for (int i = 0; i < 3; i++)
        {
            if (i == flatAxis) continue;

            float lo = groupMin[i] + BorderMargin;
            float hi = groupMax[i] - BorderMargin;
            if (lo > hi) { lo = hi = (groupMin[i] + groupMax[i]) * 0.5f; } // 그룹이 한 칸이라 여유가 없으면 중앙으로

            p[i] = Mathf.Clamp(p[i], lo, hi);
        }
        return p;
    }

    // 반직선(origin, dir)이 그리드 셀 하나(단위 정육면체, 중심은 (x, y+0.5, z))와 만나는 가장 가까운 지점을
    // 슬랩(slab) 방식으로 구한다. axis에는 그 지점이 놓인 면의 법선 축(0=X, 1=Y, 2=Z)을 담는다.
    static bool RayIntersectsCell(Vector3 origin, Vector3 dir, Vector3Int cell, out float t, out int axis)
    {
        Vector3 center = new Vector3(cell.x, cell.y + 0.5f, cell.z);
        Vector3 min = center - Vector3.one * 0.5f;
        Vector3 max = center + Vector3.one * 0.5f;

        float tMin = 0f, tMax = float.MaxValue;
        axis = -1;

        for (int i = 0; i < 3; i++)
        {
            float o = origin[i], d = dir[i], lo = min[i], hi = max[i];
            if (Mathf.Abs(d) < 1e-8f)
            {
                if (o < lo || o > hi) { t = 0f; return false; } // 이 축과 평행하고 슬랩 밖 → 절대 못 만남
                continue;
            }

            float inv = 1f / d;
            float t1 = (lo - o) * inv;
            float t2 = (hi - o) * inv;
            if (t1 > t2) (t1, t2) = (t2, t1);

            if (t1 > tMin) { tMin = t1; axis = i; }
            tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax) { t = 0f; return false; }
        }

        t = tMin;
        return axis >= 0 && t > 0f; // axis<0이면 카메라가 이미 칸 안에 있는 것(표면 교차점 없음) → 취급 안 함
    }
}

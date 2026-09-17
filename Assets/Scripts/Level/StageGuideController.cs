using System.Collections.Generic;
using Framework.Core;
using UnityEngine;

/// <summary>
/// 스테이지 가이드(일시정지 메뉴의 가이드 버튼)를 관리한다.
/// 켜지면 그 스테이지의 MazeGenerator.correctOrder1/2를 읽어 순서대로 "다음 타깃"을 추적하고,
/// MapObjectUsed 이벤트로 진행시킨다. 실제 마커 표시는 StageGuideMarkerHUD가 담당한다.
/// 씬이 재로드돼도(가이드 버튼으로 재시작) 켜진 상태가 유지되도록 싱글톤으로 둔다.
/// </summary>
public class StageGuideController : MonoSingleton<StageGuideController>
{
    public bool GuideActive { get; private set; }

    List<List<MapObjectBase>> lists = new();
    List<int> indices = new();

    /// <summary>지금 추적 중인 순서 목록 개수(챕터 스테이지는 최대 2, 맵 에디터 스테이지는 최대 7).</summary>
    public int ListCount => lists.Count;

    /// <summary>i번째 순서 목록의 현재 타깃(다음에 써야 할 기물). 범위를 벗어나거나 다 썼거나 가이드가
    /// 꺼져 있으면 null.</summary>
    public MapObjectBase CurrentTarget(int i) =>
        GuideActive && i >= 0 && i < lists.Count && indices[i] < lists[i].Count ? lists[i][indices[i]] : null;

    void OnEnable()
    {
        EventBus.Subscribe<SceneLoadCompleted>(OnSceneLoaded);
        EventBus.Subscribe<MapObjectUsed>(OnMapObjectUsed);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<SceneLoadCompleted>(OnSceneLoaded);
        EventBus.Unsubscribe<MapObjectUsed>(OnMapObjectUsed);
    }

    /// <summary>가이드를 켜고 스테이지를 처음부터 다시 시작한다(일시정지 메뉴의 가이드 버튼용).</summary>
    public void ActivateGuide()
    {
        GuideActive = true;
        SceneRestarter.RestartCurrentScene(keepGuide: true);
    }

    /// <summary>가이드를 끈다. 가이드 경유가 아닌 일반 재시작 시 SceneRestarter가 호출한다.</summary>
    public void Deactivate()
    {
        GuideActive = false;
        lists.Clear();
        indices.Clear();
    }

    // 가이드가 켜진 채로 씬이 (다시) 로드됐을 때만 그 씬의 정답 순서를 읽어와 처음부터 추적을 시작한다.
    // 맵 에디터 커스텀 스테이지(correctOrders 사용)와 기존 챕터 스테이지(correctOrder1/2 고정 2개)를
    // 여기서 하나로 합류시켜서, 마커 HUD 등 하위 소비자는 어느 쪽 스테이지인지 신경 쓸 필요가 없다.
    void OnSceneLoaded(SceneLoadCompleted e)
    {
        if (!GuideActive) return;
        // 가이드는 실제 스테이지 플레이(Playing) 전용이다. Deactivate()는 SceneRestarter를 거친
        // 재시작에서만 호출되므로, 메인메뉴를 거쳐 맵 에디터로 들어가는 등 SceneRestarter를 거치지
        // 않는 경로로는 GuideActive가 켜진 채로 남을 수 있다 — 그 상태로 맵 에디터 씬이 로드되거나
        // 플레이 테스트가 시작되면 엉뚱한(또는 비어있는) MazeGenerator를 추적하게 되므로 걸러낸다.
        if (GameManager.Instance.State != GameState.Playing) return;

        var maze = FindFirstObjectByType<MazeGenerator>();
        lists = maze != null && maze.correctOrders.Count > 0
            ? maze.correctOrders
            : maze != null ? new List<List<MapObjectBase>> { maze.correctOrder1, maze.correctOrder2 } : new();
        indices = new List<int>(new int[lists.Count]);
    }

    void OnMapObjectUsed(MapObjectUsed e)
    {
        if (!GuideActive) return;

        for (int i = 0; i < lists.Count; i++)
            if (indices[i] < lists[i].Count && lists[i][indices[i]] == e.Source) indices[i]++;
    }
}

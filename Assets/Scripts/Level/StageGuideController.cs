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

    List<MapObjectBase> list1 = new();
    List<MapObjectBase> list2 = new();
    int index1;
    int index2;

    /// <summary>리스트1의 현재 타깃(다음에 써야 할 기물). 다 썼거나 가이드가 꺼져 있으면 null.</summary>
    public MapObjectBase Current1 => GuideActive && index1 < list1.Count ? list1[index1] : null;

    /// <summary>리스트2의 현재 타깃. 다 썼거나 가이드가 꺼져 있으면 null.</summary>
    public MapObjectBase Current2 => GuideActive && index2 < list2.Count ? list2[index2] : null;

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
        list1.Clear();
        list2.Clear();
        index1 = 0;
        index2 = 0;
    }

    // 가이드가 켜진 채로 씬이 (다시) 로드됐을 때만 그 씬의 정답 순서를 읽어와 처음부터 추적을 시작한다.
    void OnSceneLoaded(SceneLoadCompleted e)
    {
        if (!GuideActive) return;

        var maze = FindFirstObjectByType<MazeGenerator>();
        list1 = maze != null ? maze.correctOrder1 : new List<MapObjectBase>();
        list2 = maze != null ? maze.correctOrder2 : new List<MapObjectBase>();
        index1 = 0;
        index2 = 0;
    }

    void OnMapObjectUsed(MapObjectUsed e)
    {
        if (!GuideActive) return;

        if (index1 < list1.Count && list1[index1] == e.Source) index1++;
        if (index2 < list2.Count && list2[index2] == e.Source) index2++;
    }
}

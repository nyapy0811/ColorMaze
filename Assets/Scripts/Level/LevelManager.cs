using System.Linq;
using Framework.Core;
using UnityEngine;

/// <summary>스테이지 내 모든 캔버스(4.7)가 완료됐을 때 발행된다.</summary>
public struct StageCleared : IEvent { }

/// <summary>
/// 레벨 전역 로직을 담을 게임 매니저.
/// 미로 블록은 Scene 뷰 편집으로 씬에 직접 저장되므로 런타임 생성은 하지 않는다.
/// 씬 내 모든 ColorCanvas가 완료되면 StageCleared를 발행한다.
/// </summary>
public class LevelManager : MonoSingleton<LevelManager>
{
    ColorCanvas[] canvases;

    void OnEnable()
    {
        EventBus.Subscribe<CanvasCompleted>(OnCanvasCompleted);
        EventBus.Subscribe<SceneLoadCompleted>(OnSceneLoaded);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CanvasCompleted>(OnCanvasCompleted);
        EventBus.Unsubscribe<SceneLoadCompleted>(OnSceneLoaded);
    }

    void Start() => RefreshCanvases();

    // LevelManager는 씬이 바뀌어도 유지되는 싱글톤이라, 새 스테이지 씬이 로드될 때마다
    // 캔버스 목록을 다시 긁어와야 한다(그렇지 않으면 첫 스테이지의 캔버스만 계속 참조하게 됨).
    void OnSceneLoaded(SceneLoadCompleted e) => RefreshCanvases();

    void RefreshCanvases() => canvases = FindObjectsByType<ColorCanvas>(FindObjectsSortMode.None);

    void OnCanvasCompleted(CanvasCompleted e)
    {
        if (canvases == null || canvases.Length == 0) return;
        if (canvases.All(c => c.Completed))
            EventBus.Publish(new StageCleared());
    }
}

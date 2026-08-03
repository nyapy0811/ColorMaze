using Framework.Core;

/// <summary>
/// 스택 체인저(4.4)·컬러 체인저(4.5)처럼 "발동 전/후 색 미리보기"가 있는 StackModifierConsumable 전용 중간 계층.
/// 미리보기는 매 프레임이 아니라 스테이지 시작 시(SceneLoadCompleted)와 플레이어 스택이 바뀔 때(ColorStackChanged)만 갱신한다.
/// 하위 클래스는 RefreshPreview()만 구현하면 된다(구독/해제/초기 1회 호출은 여기서 처리).
/// </summary>
public abstract class PreviewingStackModifier : StackModifierConsumable
{
    /// <summary>현재 색·발동 후 색 미리보기를 갱신한다. 하위 클래스가 구현한다.</summary>
    protected abstract void RefreshPreview();

    void OnEnable()
    {
        EventBus.Subscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Subscribe<SceneLoadCompleted>(OnStageStart);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Unsubscribe<SceneLoadCompleted>(OnStageStart);
    }

    void Start() => RefreshPreview();

    void OnStackChanged(ColorStackChanged e) => RefreshPreview();
    void OnStageStart(SceneLoadCompleted e) => RefreshPreview();
}

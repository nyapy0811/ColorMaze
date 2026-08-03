using Framework.Core;
using UnityEngine;

/// <summary>
/// 1인칭 뷰모델 붓. 붓 끝(팁/모) 렌더러 색을 플레이어의 현재 RGB 스택 색(ColorStacks.CurrentRGB)으로
/// 표시한다. HUDController와 동일한 규칙으로 MainMenu 상태에서는 붓을 숨긴다.
/// 이 스크립트 자신은 항상 활성 상태인 오브젝트(오버레이 카메라 등)에 붙이고, 실제로 켜고 끄는
/// 대상은 brushRoot로 분리한다 — 스크립트 자신을 껐다 켜면 이벤트 구독이 끊겨 다시 켜질 방법이
/// 없어지기 때문이다.
/// </summary>
public class BrushViewmodel : GameStateListener
{
    [SerializeField] GameObject brushRoot;
    [SerializeField] Renderer brushTipRenderer;

    ColorStacks Player => ColorStacks.Instance;

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

    protected override void Start()
    {
        RefreshColor();
        base.Start();
    }

    void OnStackChanged(ColorStackChanged e) => RefreshColor();
    void OnStageStart(SceneLoadCompleted e) => RefreshColor();

    protected override void OnGameStateChanged(GameState previous, GameState next)
    {
        if (brushRoot) brushRoot.SetActive(next != GameState.MainMenu);
    }

    void RefreshColor()
    {
        if (Player == null || brushTipRenderer == null) return;

        var mpb = new MaterialPropertyBlock();
        brushTipRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", (Color)Player.CurrentRGB);
        brushTipRenderer.SetPropertyBlock(mpb);
    }
}

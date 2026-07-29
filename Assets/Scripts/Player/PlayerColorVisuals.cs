using Framework.Core;
using UnityEngine;

/// <summary>
/// 플레이어의 현재 RGB 스택 색(ColorStacks.CurrentRGB)을 보여주는 시각 요소들을 담당한다.
/// 1인칭 뷰모델 붓(팁/모) 렌더러 색과 메인 카메라 배경색을 함께 갱신한다.
/// HUDController와 동일한 규칙으로 MainMenu 상태에서는 붓을 숨긴다.
/// 이 스크립트 자신은 항상 활성 상태인 오브젝트(오버레이 카메라 등)에 붙이고, 실제로 켜고 끄는
/// 대상은 brushRoot로 분리한다 — 스크립트 자신을 껐다 켜면 이벤트 구독이 끊겨 다시 켜질 방법이
/// 없어지기 때문이다.
/// </summary>
public class PlayerColorVisuals : MonoBehaviour
{
    [SerializeField] GameObject brushRoot;
    [SerializeField] Renderer brushTipRenderer;
    [SerializeField] Camera mainCamera;

    ColorStacks player;

    ColorStacks Player
    {
        get
        {
            if (player == null) player = FindAnyObjectByType<ColorStacks>();
            return player;
        }
    }

    void OnEnable()
    {
        EventBus.Subscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Subscribe<SceneLoadCompleted>(OnStageStart);
        GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Unsubscribe<SceneLoadCompleted>(OnStageStart);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void Start()
    {
        RefreshColor();
        Refresh(GameManager.Instance.State);
    }

    void OnStackChanged(ColorStackChanged e) => RefreshColor();
    void OnStageStart(SceneLoadCompleted e) => RefreshColor();
    void OnStateChanged(GameState previous, GameState next) => Refresh(next);

    void Refresh(GameState state)
    {
        if (brushRoot) brushRoot.SetActive(state != GameState.MainMenu);
    }

    void RefreshColor()
    {
        if (Player == null) return;

        Color color = Player.CurrentRGB;

        if (brushTipRenderer != null)
        {
            var mpb = new MaterialPropertyBlock();
            brushTipRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            brushTipRenderer.SetPropertyBlock(mpb);
        }

        if (mainCamera != null) mainCamera.backgroundColor = color;
    }
}

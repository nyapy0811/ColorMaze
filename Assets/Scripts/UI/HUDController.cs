using Framework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD(조준점·스택 표시 등) 표시 여부를 GameState에 맞춰 토글한다.
/// MainMenu 상태에서는 숨기고, 그 외(Playing/Paused)에는 보인다 — UIScene이 항상 additive로
/// 함께 로드되기 때문에, 메인 화면에서 게임 HUD가 같이 보이지 않게 하려면 이 처리가 필요하다.
/// 조준점 자체는 화면 중앙 Image로 두면 되며 별도 스크립트가 필요 없다.
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] GameObject hudRoot;

    void Start()
    {
        GameManager.Instance.OnStateChanged += OnStateChanged;
        Refresh(GameManager.Instance.State);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState previous, GameState next) => Refresh(next);

    void Refresh(GameState state)
    {
        if (hudRoot) hudRoot.SetActive(state != GameState.MainMenu);
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 HUD. 경과 시간을 표시한다(일시정지 중에는 timeScale 0이라 멈춤).
/// 조준점은 화면 중앙 Image로 두면 되며 별도 스크립트가 필요 없다.
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] Text timerText;

    float elapsed;

    void Update()
    {
        elapsed += Time.deltaTime;
        if (timerText) timerText.text = Format(elapsed);
    }

    static string Format(float t)
    {
        int m = (int)(t / 60f);
        int s = (int)(t % 60f);
        return $"{m:00}:{s:00}";
    }
}

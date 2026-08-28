using UnityEngine;
using Framework.Core;

/// <summary>
/// 개발 빌드에서만 화면 좌상단에 실시간 FPS를 표시한다(Debug.isDebugBuild — Development Build 체크 시에만 true,
/// 최종 배포 빌드에서는 자동으로 아무것도 표시하지 않는다). Bootstrap에서 .Instance로 깨운다.
/// </summary>
public class FPSCounter : MonoSingleton<FPSCounter>
{
    const float UpdateInterval = 0.5f; // 너무 자주 갱신하면 숫자가 떨려서 읽기 힘들어 0.5초마다 갱신

    float timer;
    int frameCount;
    float fps;

    void Update()
    {
        if (!Debug.isDebugBuild) return;

        frameCount++;
        timer += Time.unscaledDeltaTime;
        if (timer < UpdateInterval) return;

        fps = frameCount / timer;
        frameCount = 0;
        timer = 0f;
    }

    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUI.Label(new Rect(10, 10, 200, 30), $"FPS: {fps:0.}");
    }
}

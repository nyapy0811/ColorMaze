using UnityEngine;

/// <summary>
/// 플레이어가 닿으면(맵 밖으로 떨어졌을 때 등) 현재 스테이지를 처음부터 다시 시작한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DeadZone : MonoBehaviour
{
    void Awake()
    {
        // 콜라이더가 트리거가 아니면(리모델링 등으로 초기화된 경우 포함) 강제로 트리거로 맞춘다.
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ColorStacks>() == null) return;

        SceneRestarter.RestartCurrentScene();
    }
}

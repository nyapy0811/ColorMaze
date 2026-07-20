using UnityEngine;

/// <summary>
/// 플레이어(카메라)를 바라보면서 그 방향(z축) 기준으로 계속 회전하는 스택 조작형 기물 공통 베이스.
/// 스택 체인저·컬러 체인저처럼 항상 플레이어를 향한 채 빙글빙글 도는 기물은 이 클래스를 상속한다.
/// </summary>
public abstract class SpinningStackModifier : StackModifierConsumable
{
    [Header("회전")]
    [Tooltip("플레이어를 바라본 채로 내부 Z축 기준 회전하는 속도(도/초)")]
    [SerializeField] float spinSpeed = 180f;

    float spinAngle;

    // 플레이어(카메라)를 바라보는 방향을 유지하면서, 그 방향(z축) 기준으로 계속 회전한다.
    protected virtual void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = cam.transform.position - transform.position;
        spinAngle += spinSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, spinAngle);
    }
}

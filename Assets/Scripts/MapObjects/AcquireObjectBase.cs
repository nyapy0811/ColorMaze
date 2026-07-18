using UnityEngine;

/// <summary>
/// 획득형 기물 공통 베이스(4.1 컬러 팔레트).
/// 발동해도 사라지지 않아, 플레이어가 나갔다 다시 들어오면 반복 획득할 수 있다.
/// </summary>
public abstract class AcquireObjectBase : MapObjectBase
{
    /// <summary>획득 효과. 하위 클래스가 구현한다.</summary>
    protected abstract void OnAcquire(ColorStacks player);

    protected override void Awake()
    {
        base.Awake();
        Col.isTrigger = true; // 획득형 기물은 항상 트리거
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other)) OnAcquire(Player);
    }
}

using UnityEngine;

/// <summary>
/// 획득형 기물 공통 베이스(4.1 컬러 팔레트).
/// 발동해도 사라지지 않아, 여러 번 클릭해서 반복 획득할 수 있다.
/// </summary>
public abstract class AcquireObjectBase : MapObjectBase, IInteractable
{
    /// <summary>획득 효과. 하위 클래스가 구현한다.</summary>
    protected abstract void OnAcquire(ColorStacks player);

    protected override void Awake()
    {
        base.Awake();
        Col.isTrigger = true; // 획득형 기물은 항상 트리거
    }

    /// <summary>조준+좌클릭 상호작용으로만 발동한다(걸어서 닿는 것으로는 발동하지 않음).</summary>
    public void TryInteract()
    {
        if (Player != null) OnAcquire(Player);
    }
}

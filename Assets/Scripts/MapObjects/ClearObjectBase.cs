using Framework.Core;
using UnityEngine;

/// <summary>스테이지 목표 기물(4.7 캔버스) 하나가 조건을 만족했을 때 발행된다.</summary>
public struct CanvasCompleted : IEvent
{
    public ClearObjectBase Source;
}

/// <summary>
/// 클리어형 기물 공통 베이스(4.7 캔버스).
/// 조건을 만족하면 완료 상태로 잠기고(Completed = true), 이후 다시 판정하지 않는다
/// (순차 완료 방식 — 캔버스가 여러 개여도 하나씩 맞춰 나가면 된다).
/// </summary>
public abstract class ClearObjectBase : MapObjectBase
{
    public bool Completed { get; private set; }

    /// <summary>클리어 조건 판정. 하위 클래스가 구현한다.</summary>
    protected abstract bool CheckCondition(ColorStacks player);

    /// <summary>완료 시 연출(외형 변경 등). 하위 클래스가 구현한다.</summary>
    protected abstract void OnCompleted();

    protected override void Awake()
    {
        base.Awake();
        Col.isTrigger = true; // 클리어형 기물은 항상 트리거
    }

    void OnTriggerEnter(Collider other)
    {
        if (Completed || !IsPlayer(other)) return;
        if (!CheckCondition(Player)) return;

        Completed = true;
        OnCompleted();
        EventBus.Publish(new CanvasCompleted { Source = this });
    }
}

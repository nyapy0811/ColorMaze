using UnityEngine;

/// <summary>
/// 소모성·1회성 기물 공통 베이스.
/// 플레이어가 닿으면 Apply()를 한 번 실행하고 스스로를 비활성화한다.
/// 스택과 무관한 미래의 1회성 기물도 이 클래스만 상속하면 된다
/// (스택을 다루는 기물은 한 단계 더 구체적인 StackModifierConsumable을 상속할 것).
/// </summary>
public abstract class ConsumableObjectBase : MapObjectBase
{
    public bool Consumed { get; private set; }

    /// <summary>발동 시 실행할 효과. 무엇을 다루는지는 하위 클래스가 정의한다.</summary>
    protected abstract void Apply();

    protected override void Awake()
    {
        base.Awake();
        Col.isTrigger = true; // 소모성 기물은 항상 트리거
    }

    void OnTriggerEnter(Collider other)
    {
        if (Consumed || !IsPlayer(other)) return;
        Apply();
        Consume();
    }

    /// <summary>소모 처리(콜라이더 비활성화 + 파괴). 필요하면 오버라이드해 풀링 등으로 교체 가능.</summary>
    protected virtual void Consume()
    {
        Consumed = true;
        Col.enabled = false;
        Destroy(gameObject);
    }
}

/// <summary>
/// 조준+좌클릭으로 상호작용 가능한 기물이 구현하는 인터페이스.
/// 걸어서 닿았을 때(OnTriggerEnter)와 완전히 같은 효과를 TryInteract()로도 낼 수 있게 한다.
/// 필터(FilterBlockBase)는 구현하지 않는다 — 걸어서 통과하는 것 자체가 상호작용이라 별도 클릭이 필요 없음.
/// </summary>
public interface IInteractable
{
    /// <summary>클릭 상호작용 발동. 조건 미충족/이미 소모됨 등은 구현부에서 알아서 무시한다.</summary>
    void TryInteract();
}

/// <summary>
/// 스택을 다루는 소모성 기물 전용 중간 계층(4.4 스택 체인저, 4.5 컬러 체인저, 4.6 스포이드).
/// ConsumableObjectBase의 Apply()를 대신 구현해, 하위 클래스는 ApplyToStacks()만 신경 쓰면 된다.
/// </summary>
public abstract class StackModifierConsumable : ConsumableObjectBase
{
    /// <summary>플레이어의 ColorStacks를 다루는 효과. 하위 클래스가 구현한다.</summary>
    protected abstract void ApplyToStacks(ColorStacks player);

    protected override void Apply() => ApplyToStacks(Player);
}

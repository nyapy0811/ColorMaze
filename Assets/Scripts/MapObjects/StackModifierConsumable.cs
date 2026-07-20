/// <summary>
/// 스택을 다루는 소모성 기물 전용 중간 계층(4.4 스택 체인저, 4.5 컬러 체인저, 4.6 지우개).
/// ConsumableObjectBase의 Apply()를 대신 구현해, 하위 클래스는 ApplyToStacks()만 신경 쓰면 된다.
/// 실제로 스택 값이 바뀐 경우에만 사라진다(바뀐 게 없으면 그대로 남는다).
/// </summary>
public abstract class StackModifierConsumable : ConsumableObjectBase
{
    /// <summary>플레이어의 ColorStacks를 다루는 효과. 하위 클래스가 구현한다.</summary>
    protected abstract void ApplyToStacks(ColorStacks player);

    bool changedStack;

    protected override void Apply()
    {
        int r0 = Player.Get(LightColor.Red);
        int g0 = Player.Get(LightColor.Green);
        int b0 = Player.Get(LightColor.Blue);

        ApplyToStacks(Player);

        changedStack = Player.Get(LightColor.Red) != r0
            || Player.Get(LightColor.Green) != g0
            || Player.Get(LightColor.Blue) != b0;
    }

    protected override bool ShouldConsume() => changedStack;
}

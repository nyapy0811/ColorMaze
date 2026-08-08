using Framework.Core;

/// <summary>PlayerMoveStateChanged 이벤트의 IsMoving 값을 Animator의 Moving bool에 그대로 반영한다.</summary>
public class MovingAnimatorBoolBridge : EventAnimatorBoolBridge<PlayerMoveStateChanged>
{
    protected override bool GetValue(PlayerMoveStateChanged e) => e.IsMoving;
}

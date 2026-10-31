using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 작동기 기반(6.4): 켜짐이 바뀌면 '작동기 바뀜'을 보내고 작동 대상 하나에 알리며, 대상의 연결색을 보인다. 대상이 막으면 꺼질 회수를 거부한다.
    /// 연결은 문 계열이 시작할 때 '네 대상은 나'라고 알려 만든다(작동기 하나는 대상 하나에만, 3차 Q477 뒤 채팅).
    /// </summary>
    public abstract class ActuatorBase : SubmittableObjectBase
    {
        [SerializeField] LinkColorView linkColorView;

        IActuatorTarget target;

        /// <summary>작동 대상이 시작할 때 부른다.</summary>
        public void Connect(IActuatorTarget actuatorTarget)
        {
            target = actuatorTarget;
            linkColorView.Show(target.LinkColorIndex);
        }

        protected override bool CanRelease => target == null || target.CanActuatorTurnOff;

        protected override void OnSwitched()
        {
            EventBus.Publish(new ActuatorStateChanged(IsOn));
            target?.OnActuatorChanged();
        }
    }
}

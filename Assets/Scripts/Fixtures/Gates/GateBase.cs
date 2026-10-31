using System;
using System.Collections.Generic;
using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 문 계열의 기반(6.5). 연결된 작동기 목록을 들고, 시작할 때 목록의 작동기마다 '네 대상은 나'라고 알린다.
    /// 작동기에서 알림을 받을 때마다 목록 전체를 다시 보고 모두 켜져 있으면 연다(AND만, 목록이 비면 닫힘, 3차 Q479).
    /// 열림이 바뀌면 상태 함수 하나가 막는 콜라이더와 겉모습을 바꾸고 '문 계열 바뀜'을 보낸다(루트와 스크립트는 늘 켜 둠, 2차 Q277).
    /// 열려 있을 때 막는 자리에 플레이어가 있으면 작동기를 끄지 못하게 해(회수 거부) 플레이어 위로 닫히는 일이 없다(6단계 확인 채팅).
    /// </summary>
    public abstract class GateBase : FixtureBase, IActuatorTarget
    {
        [Tooltip("연결된 작동기. 모두 켜져야 열린다. 작동기 하나는 기물 하나에만 넣는다(맵 검사가 알림).")]
        [SerializeField] ActuatorBase[] actuators;

        [Tooltip("연결색 목록(LinkColorConfig)의 번호")]
        [SerializeField] int linkColorIndex;

        [Tooltip("닫혀 있을 때 길을 막는 콜라이더")]
        [SerializeField] BoxCollider[] blockers;

        [SerializeField] LinkColorView linkColorView;

        (Vector3 center, Vector3 halfExtents, Quaternion rotation)[] blockedAreas;

        public bool IsOpen { get; private set; }
        public int LinkColorIndex => linkColorIndex;
        public IReadOnlyList<ActuatorBase> Actuators => actuators;
        public bool CanActuatorTurnOff => !IsOpen || !PlayerInBlockedArea();

        // 닫혀 있을 때의 막는 자리를 기억한다(문짝은 열리면 벽 속으로 움직임).
        void Awake()
        {
            blockedAreas = Array.ConvertAll(blockers, box =>
                (box.transform.TransformPoint(box.center), Vector3.Scale(box.size, box.transform.lossyScale) / 2f, box.transform.rotation));
        }

        protected override void Start()
        {
            base.Start();
            linkColorView.Show(linkColorIndex);
            foreach (var actuator in actuators) actuator.Connect(this);
        }

        public void OnActuatorChanged()
        {
            bool open = actuators.Length > 0 && Array.TrueForAll(actuators, a => a.IsOn);
            if (open != IsOpen) SetOpen(open);
        }

        /// <summary>겉모습에 열림·닫힘을 알린다(문짝 미닫이, 레이저 오르내림).</summary>
        protected abstract void ShowOpen(bool open);

        void SetOpen(bool open)
        {
            IsOpen = open;
            foreach (var blocker in blockers) blocker.enabled = !open;
            ShowOpen(open);
            EventBus.Publish(new GateStateChanged(open));
        }

        bool PlayerInBlockedArea()
        {
            var body = MapManager.Current.Player.GetComponent<CharacterController>();
            foreach (var (center, halfExtents, rotation) in blockedAreas)
                if (Array.IndexOf(Physics.OverlapBox(center, halfExtents, rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore), body) >= 0) return true;
            return false;
        }
    }
}

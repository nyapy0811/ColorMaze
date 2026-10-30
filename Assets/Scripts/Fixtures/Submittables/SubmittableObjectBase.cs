using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 작동기 계열의 기반(6.4): 요구 값, 작동기 상태, 제출 팝업. 상호작용하면 제출 세션을 만들어 팝업으로 열고,
    /// 팝업이 닫히면(호스트가 입력 잠금을 먼저 푼 뒤) 세션 결과로 상태를 바꾼 다음 든 오브 겉모습과 강조를 고친다.
    /// 켜짐이 바뀌면 종류마다의 응답을 부른다. 판정은 <see cref="TrySubmit"/> 한 곳이다(값이 정확히 같은가, 2차 Q147).
    /// '제출 결과'·'작동기 바뀜'은 받는 곳(소리)을 만드는 8단계에서 더하고, 상태 저장은 체크포인트 단계에서 더한다.
    /// </summary>
    public abstract class SubmittableObjectBase : FixtureBase, IInteractable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("요구 값. 모든 채널 1 이상이고 세 채널이 모두 같지 않아야 한다(맵 검사가 알림).")]
        [SerializeField] OrbValue requiredOrb = new(2, 1, 1);

        [Tooltip("요구 색(요구 값의 색)으로 칠할 렌더러")]
        [SerializeField] Renderer[] requiredColorRenderers;

        [SerializeField] StoredOrbView storedOrbView;

        ActuatorState state;
        MaterialPropertyBlock block;

        public OrbValue RequiredOrb => requiredOrb;
        public bool IsOn => state.IsOn;
        public bool CanInteract => state.CanOpenPopup;

        /// <summary>일회용·반복 작동기, 목적지.</summary>
        protected abstract SubmitKind Kind { get; }

        /// <summary>켜짐이 바뀌었을 때의 응답(종류마다 재정의).</summary>
        protected abstract void OnSwitched();

        /// <summary>슬롯 오브를 빼도 되는가. 작동기는 대상이 막으면 안 된다(닫힐 문 계열에 플레이어가 있음).</summary>
        protected virtual bool CanRelease => true;

        void Awake()
        {
            state = new ActuatorState(Kind);
            PaintRequired();
        }

        void OnValidate() => PaintRequired();

        protected override void Start()
        {
            base.Start();
            storedOrbView.Show(state.StoredOrb);
        }

        /// <summary>제출 판정: 값이 정확히 같은가.</summary>
        public bool TrySubmit(OrbValue value) => value == requiredOrb;

        public void Interact(PlayerContext player)
        {
            if (!CanInteract) return;
            var session = new SubmitSession(player.Inventory.Orbs, requiredOrb, TrySubmit, () => CanRelease, Kind, state.StoredOrb);
            Popups.TryOpen(session, () => Apply(session.Close()));
        }

        void Apply(SubmitOutcome outcome)
        {
            state = state.Apply(outcome, out bool onChanged);
            storedOrbView.Show(state.StoredOrb);
            RefreshHighlight();
            if (onChanged) OnSwitched();
        }

        void PaintRequired()
        {
            if (requiredColorRenderers == null) return;
            block ??= new MaterialPropertyBlock();
            foreach (var target in requiredColorRenderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(block);
                block.SetColor(BaseColorId, OrbRules.ToColor(requiredOrb));
                target.SetPropertyBlock(block);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace ColorMaze
{
    /// <summary>
    /// 제출 팝업 한 번 동안의 상태. 맞는지는 오브를 누를 때 판정하고, 결과는 닫을 때 작동기에 적용한다.
    /// 슬롯에 넣은 오브는 소지 오브에서 바로 빠진다.
    /// </summary>
    public sealed class SubmitSession
    {
        readonly OrbInventory inventory;
        readonly Func<OrbValue, bool> judge;
        readonly Func<bool> canRelease;
        bool placedThisSession;

        /// <param name="required">요구 값. 제출 화면이 그리는 값이고, 판정은 <paramref name="judge"/>가 한다.</param>
        /// <param name="judge">맞는지 판정(작동기 계열 기반의 TrySubmit).</param>
        /// <param name="canRelease">슬롯 오브를 빼도 되는가(작동기 계열 기반이 정함: 빼서 닫힐 문 계열의 막는 자리에 플레이어가 있으면 안 됨).</param>
        /// <param name="storedAtOpen">열 때 제출 슬롯에 든 오브. 반복 작동기만 있을 수 있다.</param>
        public SubmitSession(OrbInventory inventory, OrbValue required, Func<OrbValue, bool> judge, Func<bool> canRelease, SubmitKind kind, OrbValue? storedAtOpen)
        {
            this.inventory = inventory;
            this.judge = judge;
            this.canRelease = canRelease;
            Required = required;
            Kind = kind;
            SlotOrb = storedAtOpen;
        }

        public OrbValue Required { get; }
        public SubmitKind Kind { get; }

        /// <summary>고를 수 있는 소지 오브(들어온 순서).</summary>
        public IReadOnlyList<HeldOrb> Orbs => inventory.Items;

        /// <summary>제출 슬롯에 든 오브.</summary>
        public OrbValue? SlotOrb { get; private set; }

        /// <summary>넣기·되돌리기·회수를 할 때마다 받아들임 여부와 거부 이유(받아들이면 기본값)로 알린다(작동기 계열이 '제출 결과'로 보냄).</summary>
        public event Action<bool, SubmitRejectReason> Attempted;

        /// <summary>소지 오브를 누름: 슬롯이 비어 있고 판정이 맞으면 소지 오브에서 빼서 슬롯에 넣는다.</summary>
        public bool TrySubmit(HeldOrb orb, out SubmitRejectReason reason)
        {
            reason = default;
            if (SlotOrb.HasValue)
            {
                reason = SubmitRejectReason.SlotFull;
                return Report(false, reason);
            }
            if (!judge(orb.Value))
            {
                reason = SubmitRejectReason.WrongValue;
                return Report(false, reason);
            }
            inventory.Remove(orb);
            SlotOrb = orb.Value;
            placedThisSession = true;
            return Report(true, reason);
        }

        /// <summary>슬롯 오브를 되돌리거나 회수할 수 있는가: 반복 작동기이거나 이번에 넣은 오브.</summary>
        public bool CanRetrieve => SlotOrb.HasValue && (Kind == SubmitKind.Reusable || placedThisSession);

        /// <summary>슬롯 오브를 누름: 빼도 되고 소지 오브가 가득 차지 않았으면 소지 오브 끝으로 옮긴다.</summary>
        public bool TryRetrieve(out SubmitRejectReason reason)
        {
            if (!CanRetrieve) throw new InvalidOperationException("되돌릴 수 있는 슬롯 오브가 없다.");
            reason = default;
            if (!canRelease())
            {
                reason = SubmitRejectReason.GateOccupied;
                return Report(false, reason);
            }
            if (!inventory.TryAdd(SlotOrb.Value, out _))
            {
                reason = SubmitRejectReason.InventoryFull;
                return Report(false, reason);
            }
            SlotOrb = null;
            placedThisSession = false;
            return Report(true, reason);
        }

        /// <summary>팝업을 닫을 때: 슬롯에 든 오브를 결과로 돌려준다.</summary>
        public SubmitOutcome Close() => new(SlotOrb);

        bool Report(bool accepted, SubmitRejectReason reason)
        {
            Attempted?.Invoke(accepted, reason);
            return accepted;
        }
    }
}

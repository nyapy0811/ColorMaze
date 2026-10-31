using System;
using System.Collections.Generic;

namespace ColorMaze
{
    /// <summary>
    /// 합성 팝업 한 번 동안의 선택 상태. 합성기는 오브를 보관하지 않으므로 닫으면 선택을 버린다.
    /// 모든 소지 오브를 고를 수 있고(한도를 넘게 되어도 막지 않음), 결과는 채널 한도로 자른다.
    /// </summary>
    public sealed class MixSession
    {
        readonly OrbInventory inventory;
        readonly List<HeldOrb> selected = new();

        public MixSession(OrbInventory inventory)
        {
            this.inventory = inventory;
        }

        /// <summary>고를 수 있는 소지 오브(들어온 순서).</summary>
        public IReadOnlyList<HeldOrb> Orbs => inventory.Items;

        public IReadOnlyList<HeldOrb> Selected => selected;
        public bool IsSelected(HeldOrb orb) => selected.Contains(orb);

        /// <summary>합성했을 때 결과 값으로 알린다(합성기가 '합성함'으로 보냄). 결과가 (0,0,0)이어도 알린다.</summary>
        public event Action<OrbValue> Mixed;

        /// <summary>고르기. 이미 고른 오브를 다시 누르면 취소한다.</summary>
        public void Toggle(HeldOrb orb)
        {
            if (!selected.Remove(orb)) selected.Add(orb);
        }

        /// <summary>1개 이상 고르면 합성할 수 있다(1개만 골라도 됨).</summary>
        public bool CanMix => selected.Count > 0;

        /// <summary>결과 미리보기: 고른 오브의 합을 채널 한도로 자른 값.</summary>
        public OrbValue Preview
        {
            get
            {
                var sum = OrbValue.Zero;
                foreach (var orb in selected) sum += orb.Value;
                return OrbRules.Clamp(sum, inventory.ChannelLimit);
            }
        }

        /// <summary>
        /// 고른 오브를 빼고 결과를 소지 오브 끝에 더한 뒤 선택을 비운다. 재료가 빠지므로 늘 성공한다.
        /// 결과가 (0,0,0)이면(재료가 모두 (0,0,0)) 더하지 않고 null을 돌려준다(재료만 사라짐).
        /// </summary>
        public HeldOrb Mix()
        {
            if (!CanMix) throw new InvalidOperationException("고른 오브가 없다.");
            var result = Preview;
            foreach (var orb in selected) inventory.Remove(orb);
            selected.Clear();
            HeldOrb added = null;
            if (result != OrbValue.Zero) inventory.TryAdd(result, out added);
            Mixed?.Invoke(result);
            return added;
        }

        /// <summary>팝업을 닫을 때: 선택을 버린다.</summary>
        public void Close() => selected.Clear();
    }
}

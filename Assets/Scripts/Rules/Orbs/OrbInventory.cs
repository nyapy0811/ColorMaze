using System;
using System.Collections.Generic;

namespace ColorMaze
{
    /// <summary>
    /// 소지 오브. 들어온 순서의 목록(용량까지)과 지금 채널 한도를 든다.
    /// 빈칸은 따로 나타내지 않는다(빼면 뒤의 오브가 앞으로 당겨짐). 바뀌면 <see cref="Changed"/>로 알린다.
    /// </summary>
    public sealed class OrbInventory
    {
        readonly List<HeldOrb> items = new();
        int nextId = 1;

        public OrbInventory(int capacity, OrbValue channelLimit)
        {
            Capacity = capacity;
            ChannelLimit = channelLimit;
        }

        /// <summary>슬롯 수. 생성할 때 받는다.</summary>
        public int Capacity { get; }

        /// <summary>지금 채널 한도(채널마다). 처음 값은 설정 애셋의 기본 한도다.</summary>
        public OrbValue ChannelLimit { get; private set; }

        public IReadOnlyList<HeldOrb> Items => items;
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;

        public event Action Changed;

        /// <summary>끝에 더한다. 가득 차 있으면 실패한다.</summary>
        public bool TryAdd(OrbValue value, out HeldOrb added)
        {
            if (IsFull)
            {
                added = null;
                return false;
            }
            added = new HeldOrb(nextId++, value);
            items.Add(added);
            Changed?.Invoke();
            return true;
        }

        public bool Contains(HeldOrb orb) => items.Contains(orb);

        public bool Remove(HeldOrb orb)
        {
            if (!items.Remove(orb)) return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>스냅샷용: 값 목록(들어온 순서 그대로).</summary>
        public List<OrbValue> ExportValues() => items.ConvertAll(orb => orb.Value);

        /// <summary>스냅샷에서 불러온다. 지금 목록과 채널 한도를 바꾼다.</summary>
        public void Import(IReadOnlyList<OrbValue> values, OrbValue channelLimit)
        {
            if (values.Count > Capacity)
                throw new ArgumentException($"오브 {values.Count}개가 용량 {Capacity}를 넘는다.", nameof(values));
            items.Clear();
            foreach (var value in values) items.Add(new HeldOrb(nextId++, value));
            ChannelLimit = channelLimit;
            Changed?.Invoke();
        }
    }
}

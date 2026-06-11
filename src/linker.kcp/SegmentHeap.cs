using System;
using System.Collections.Generic;

namespace linker.kcp;

internal sealed class SegmentHeap
{
    private readonly List<KcpSegment> _items;
    private readonly HashSet<uint> _marks;

    public SegmentHeap(int capacity = Kcp.DefaultReceiveWindow * 2)
    {
        _items = new List<KcpSegment>(capacity);
        _marks = new HashSet<uint>(capacity);
    }

    public int Count => _items.Count;

    public bool Has(uint sn)
    {
        return _marks.Contains(sn);
    }

    public void Push(KcpSegment segment)
    {
        _items.Add(segment);
        _marks.Add(segment.Sn);
        SiftUp(_items.Count - 1);
    }

    public KcpSegment Pop()
    {
        var result = _items[0];
        _marks.Remove(result.Sn);
        var last = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        if (_items.Count > 0)
        {
            _items[0] = last;
            SiftDown(0);
        }

        return result;
    }

    public KcpSegment? Peek()
    {
        return _items.Count == 0 ? null : _items[0];
    }

    public void Clear(Action<KcpSegment> onItem)
    {
        foreach (var item in _items)
        {
            onItem(item);
        }

        _items.Clear();
        _marks.Clear();
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (!Less(index, parent))
            {
                break;
            }

            Swap(index, parent);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            var left = index * 2 + 1;
            var right = left + 1;
            var smallest = index;

            if (left < _items.Count && Less(left, smallest))
            {
                smallest = left;
            }

            if (right < _items.Count && Less(right, smallest))
            {
                smallest = right;
            }

            if (smallest == index)
            {
                break;
            }

            Swap(index, smallest);
            index = smallest;
        }
    }

    private bool Less(int left, int right)
    {
        return unchecked((int)(_items[right].Sn - _items[left].Sn)) > 0;
    }

    private void Swap(int left, int right)
    {
        (_items[left], _items[right]) = (_items[right], _items[left]);
    }
}

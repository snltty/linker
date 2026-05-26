using System;

namespace linker.kcp;

internal sealed class RingBuffer<T>
    where T : class
{
    private int _head;
    private int _tail;
    private int _count;
    private T?[] _items;

    public RingBuffer(int capacity)
    {
        _items = new T?[Math.Max(capacity, 8)];
    }

    public int Count => _count;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var actualIndex = _head + index;
            if (actualIndex >= _items.Length)
            {
                actualIndex -= _items.Length;
            }

            return _items[actualIndex]!;
        }
    }

    public void Push(T item)
    {
        if (IsFull)
        {
            Grow();
        }

        _items[_tail] = item;
        _tail++;
        if (_tail == _items.Length)
        {
            _tail = 0;
        }

        _count++;
    }

    public bool Pop(out T? item)
    {
        if (_count == 0)
        {
            item = null;
            return false;
        }

        item = _items[_head];
        _items[_head] = null;
        _head++;
        if (_head == _items.Length)
        {
            _head = 0;
        }

        _count--;
        return true;
    }

    public T? Peek()
    {
        return _count == 0 ? null : _items[_head];
    }

    public T? PeekTail()
    {
        if (_count == 0)
        {
            return null;
        }

        var index = _tail == 0 ? _items.Length - 1 : _tail - 1;
        return _items[index];
    }

    public void Discard(int count)
    {
        count = Math.Min(count, _count);
        for (var i = 0; i < count; i++)
        {
            _items[_head] = null;
            _head++;
            if (_head == _items.Length)
            {
                _head = 0;
            }
        }

        _count -= count;
        ResetIndicesIfEmpty();
    }

    public void Clear(Action<T> onItem)
    {
        for (var i = 0; i < _count; i++)
        {
            var index = _head + i;
            if (index >= _items.Length)
            {
                index -= _items.Length;
            }

            var item = _items[index];
            if (item is not null)
            {
                onItem(item);
            }
        }

        Array.Clear(_items);
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    private bool IsFull => _count == _items.Length;

    private void Grow()
    {
        var newSize = _items.Length < 1024
            ? _items.Length * 2
            : _items.Length + (_items.Length + 9) / 10;
        var newItems = new T?[newSize];
        for (var i = 0; i < _count; i++)
        {
            var index = _head + i;
            if (index >= _items.Length)
            {
                index -= _items.Length;
            }

            newItems[i] = _items[index];
        }

        _items = newItems;
        _head = 0;
        _tail = _count;
    }

    private void ResetIndicesIfEmpty()
    {
        if (_count == 0)
        {
            _head = 0;
            _tail = 0;
        }
    }
}

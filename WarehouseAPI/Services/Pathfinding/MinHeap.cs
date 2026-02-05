namespace WarehouseAPI.Services.Pathfinding;

/// <summary>
/// Min-Heap (Priority Queue) cho thuật toán A*
/// </summary>
internal class MinHeap
{
    private readonly List<HeapNode> _data = new();

    public int Size => _data.Count;

    public void Push(HeapNode node)
    {
        _data.Add(node);
        BubbleUp(_data.Count - 1);
    }

    public HeapNode? Pop()
    {
        if (_data.Count == 0) return null;

        var top = _data[0];
        var end = _data[^1];
        _data.RemoveAt(_data.Count - 1);

        if (_data.Count > 0)
        {
            _data[0] = end;
            BubbleDown(0);
        }

        return top;
    }

    private void BubbleUp(int index)
    {
        var node = _data[index];
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;
            var parent = _data[parentIndex];
            if (node.F >= parent.F) break;

            _data[parentIndex] = node;
            _data[index] = parent;
            index = parentIndex;
        }
    }

    private void BubbleDown(int index)
    {
        var length = _data.Count;
        var node = _data[index];

        while (true)
        {
            var leftIndex = index * 2 + 1;
            var rightIndex = index * 2 + 2;
            var smallest = index;

            if (leftIndex < length && _data[leftIndex].F < _data[smallest].F)
            {
                smallest = leftIndex;
            }
            if (rightIndex < length && _data[rightIndex].F < _data[smallest].F)
            {
                smallest = rightIndex;
            }

            if (smallest == index) break;

            _data[index] = _data[smallest];
            _data[smallest] = node;
            index = smallest;
        }
    }

    public void Clear()
    {
        _data.Clear();
    }
}

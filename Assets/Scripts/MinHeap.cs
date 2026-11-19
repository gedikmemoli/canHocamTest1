using System;
using System.Collections.Generic;

public class MinHeap<T>
{
    private readonly List<(T item, float priority)> elements = new();

    public int Count => elements.Count;

    public void Clear() => elements.Clear();

    public void Enqueue(T item, float priority)
    {
        elements.Add((item, priority));
        SiftUp(elements.Count - 1);
    }

    public T Dequeue()
    {
        if (elements.Count == 0) throw new InvalidOperationException("MinHeap is empty.");
        T bestItem = elements[0].item;
        int lastIndex = elements.Count - 1;
        elements[0] = elements[lastIndex];
        elements.RemoveAt(lastIndex);
        if (elements.Count > 0) SiftDown(0);
        return bestItem;
    }

    private void SiftUp(int i)
    {
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (elements[i].priority >= elements[p].priority) break;
            (elements[i], elements[p]) = (elements[p], elements[i]);
            i = p;
        }
    }

    private void SiftDown(int i)
    {
        int n = elements.Count;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < n && elements[left].priority < elements[smallest].priority) smallest = left;
            if (right < n && elements[right].priority < elements[smallest].priority) smallest = right;

            if (smallest == i) break;
            (elements[i], elements[smallest]) = (elements[smallest], elements[i]);
            i = smallest;
        }
    }
}

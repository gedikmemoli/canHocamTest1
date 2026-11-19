using System.Collections.Generic;

public interface IFrontier<T>
{
    int Count { get; }
    void Push(T item, float priority = 0f);
    T Pop();
    void Clear();
}

public class StackFrontier<T> : IFrontier<T>
{
    private readonly Stack<T> stack = new();
    public int Count => stack.Count;
    public void Push(T item, float priority = 0f) => stack.Push(item);
    public T Pop() => stack.Pop();
    public void Clear() => stack.Clear();
}

public class QueueFrontier<T> : IFrontier<T>
{
    private readonly Queue<T> queue = new();
    public int Count => queue.Count;
    public void Push(T item, float priority = 0f) => queue.Enqueue(item);
    public T Pop() => queue.Dequeue();
    public void Clear() => queue.Clear();
}

public class PriorityFrontier<T> : IFrontier<T>
{
    private readonly MinHeap<T> heap = new();
    public int Count => heap.Count;
    public void Push(T item, float priority = 0f) => heap.Enqueue(item, priority);
    public T Pop() => heap.Dequeue();
    public void Clear() => heap.Clear();
}

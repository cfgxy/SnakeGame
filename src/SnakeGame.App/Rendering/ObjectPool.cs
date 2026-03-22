using System.Collections.Concurrent;

namespace SnakeGame.App.Rendering;

/// <summary>
/// 通用对象池 - 减少频繁创建销毁对象的 GC 压力
/// </summary>
/// <typeparam name="T">池化对象类型</typeparam>
public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Action<T>? _resetAction;
    private int _count;
    private readonly int _maxSize;

    /// <summary>
    /// 创建对象池
    /// </summary>
    /// <param name="maxSize">池最大容量</param>
    /// <param name="resetAction">对象重置操作（归还池时调用）</param>
    public ObjectPool(int maxSize = 256, Action<T>? resetAction = null)
    {
        _maxSize = maxSize;
        _resetAction = resetAction;
    }

    /// <summary>
    /// 从池中获取对象，池为空则创建新对象
    /// </summary>
    public T Rent()
    {
        if (_pool.TryTake(out var item))
        {
            System.Threading.Interlocked.Decrement(ref _count);
            return item;
        }
        return new T();
    }

    /// <summary>
    /// 将对象归还池中
    /// </summary>
    public void Return(T item)
    {
        if (_count >= _maxSize)
        {
            // 池已满，丢弃对象
            return;
        }

        _resetAction?.Invoke(item);
        _pool.Add(item);
        System.Threading.Interlocked.Increment(ref _count);
    }

    /// <summary>
    /// 当前池中对象数量
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// 清空池
    /// </summary>
    public void Clear()
    {
        while (_pool.TryTake(out _))
        {
            System.Threading.Interlocked.Decrement(ref _count);
        }
    }
}

/// <summary>
/// 可池化对象接口
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 重置对象状态（归还池时调用）
    /// </summary>
    void Reset();
}
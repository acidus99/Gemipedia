using System.Threading;

namespace Gemipedia.Console;

/// <summary>
/// Simple, thread safe counter
/// </summary>
public class ThreadSafeCounter
{
    private int counter;

    public ThreadSafeCounter(int initialValue = 0)
    {
        counter = initialValue;
    }

    public int Increment()
    {
        int tmp = Interlocked.Increment(ref counter);
        return tmp;
    }

    public int Decrement()
    {
        int tmp = Interlocked.Decrement(ref counter);
        return tmp;
    }

    public int Count
        => counter;
}
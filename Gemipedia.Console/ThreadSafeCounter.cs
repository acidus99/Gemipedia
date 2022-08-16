using System;

using System.Threading;
namespace Gemipedia.Console
{
    /// <summary>
    /// Simple, thread safe counter
    /// </summary>
    public class ThreadSafeCounter
    {

        private int counter;

        public ThreadSafeCounter()
            : this(0) { }

        public ThreadSafeCounter(int initialValue)
        {
            this.counter = initialValue;
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
        {
            get
            {
                return this.counter;
            }
        }


    }
}


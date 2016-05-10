using System;
using System.Threading;

namespace Coroutines
{
    public class AsyncIterator : SynchronizationContext
    {
        private Action _continuation;

        public AsyncIterator(Action continuation)
        {
            _continuation = continuation;
        }

        public bool MoveNext()
        {
            var current = Current;
            try
            {
                var cb = _continuation;
                if (cb == null)
                {
                    return false;
                }

                _continuation = null;

                SetSynchronizationContext(this);
                cb();
            }
            finally
            {
                SetSynchronizationContext(current);
            }
            return true;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _continuation = () => d(state);
        }
    }

}

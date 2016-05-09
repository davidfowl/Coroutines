using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public class Iterator : ICriticalNotifyCompletion
    {
        public bool IsCompleted => false;

        public object Current { get; set; }

        public Iterator GetAwaiter() => this;

        private readonly Action<Iterator> _moveNext;

        private Action _continuation;

        public Iterator(Action<Iterator> moveNext)
        {
            _moveNext = moveNext;
            _continuation = () => moveNext(this);
        }

        public bool MoveNext()
        {
            var cb = _continuation;
            if (cb == null)
            {
                return false;
            }

            _continuation = null;
            cb();
            return true;
        }

        public void GetResult()
        {

        }

        public void OnCompleted(Action continuation)
        {
            throw new NotImplementedException();
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _continuation = continuation;
        }
    }

}

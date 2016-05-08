using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Coroutines
{
    public class SocketInput : ICriticalNotifyCompletion, IDisposable
    {
        private static readonly Action _awaitableIsCompleted = () => { };
        private static readonly Action _awaitableIsNotCompleted = () => { };

        private readonly IMemoryPool _memory;
        private readonly ManualResetEventSlim _manualResetEvent = new ManualResetEventSlim(false, 0);

        private Action _awaitableState;
        private Exception _awaitableError;

        private IMemoryPoolBlock _head;
        private IMemoryPoolBlock _tail;

        private int _consumingState;
        private object _sync = new object();

        public SocketInput(IMemoryPool memory)
        {
            _memory = memory;
            _awaitableState = _awaitableIsNotCompleted;
        }

        public bool DataComplete { get; set; }

        public bool IsCompleted => ReferenceEquals(_awaitableState, _awaitableIsCompleted);

        public MemoryPoolIterator IncomingStart()
        {
            if (_tail == null)
            {
                _tail = _memory.Lease();
            }

            if (_head == null)
            {
                _head = _tail;
            }

            return new MemoryPoolIterator(_tail, _tail.End);
        }

        public void IncomingComplete(bool completed, Exception error)
        {
            IncomingComplete(default(MemoryPoolIterator), completed, error);
        }

        public void IncomingComplete(MemoryPoolIterator iter, bool completed, Exception error)
        {
            lock (_sync)
            {
                if (!iter.IsDefault)
                {
                    _tail = iter.Block;
                    _tail.End = iter.Index;
                }

                DataComplete = completed;

                if (error != null)
                {
                    _awaitableError = error;
                }

                Complete();
            }
        }

        private void Complete()
        {
            var awaitableState = Interlocked.Exchange(
                ref _awaitableState,
                _awaitableIsCompleted);

            _manualResetEvent.Set();

            if (!ReferenceEquals(awaitableState, _awaitableIsCompleted) &&
                !ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                awaitableState();
            }
        }

        public MemoryPoolIterator ConsumingStart()
        {
            if (Interlocked.CompareExchange(ref _consumingState, 1, 0) != 0)
            {
                throw new InvalidOperationException("Already consuming input.");
            }

            return new MemoryPoolIterator(_head);
        }

        public void ConsumingComplete(
            MemoryPoolIterator consumed,
            MemoryPoolIterator examined)
        {
            lock (_sync)
            {
                IMemoryPoolBlock returnStart = null;
                IMemoryPoolBlock returnEnd = null;

                if (!consumed.IsDefault)
                {
                    returnStart = _head;
                    returnEnd = consumed.Block;
                    _head = consumed.Block;
                    _head.Start = consumed.Index;
                }

                if (!examined.IsDefault &&
                    examined.IsEnd &&
                    DataComplete == false &&
                    _awaitableError == null)
                {
                    _manualResetEvent.Reset();

                    Interlocked.CompareExchange(
                        ref _awaitableState,
                        _awaitableIsNotCompleted,
                        _awaitableIsCompleted);
                }

                while (returnStart != returnEnd)
                {
                    var returnBlock = returnStart;
                    returnStart = returnStart.Next;
                    returnBlock.Pool.Return(returnBlock);
                }

                if (Interlocked.CompareExchange(ref _consumingState, 0, 1) != 1)
                {
                    throw new InvalidOperationException("No ongoing consuming operation to complete.");
                }
            }
        }

        public void CompleteAwaiting()
        {
            Complete();
        }

        public void AbortAwaiting()
        {
            _awaitableError = new TaskCanceledException("The request was aborted");

            Complete();
        }

        public SocketInput GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            var awaitableState = Interlocked.CompareExchange(
                ref _awaitableState,
                continuation,
                _awaitableIsNotCompleted);

            if (ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                return;
            }
            else if (ReferenceEquals(awaitableState, _awaitableIsCompleted))
            {
                continuation();
            }
            else
            {
                _awaitableError = new InvalidOperationException("Concurrent reads are not supported.");

                Interlocked.Exchange(
                    ref _awaitableState,
                    _awaitableIsCompleted);

                _manualResetEvent.Set();

                continuation();
                awaitableState();
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void GetResult()
        {
            if (!IsCompleted)
            {
                _manualResetEvent.Wait();
            }
            var error = _awaitableError;
            if (error != null)
            {
                if (error is TaskCanceledException || error is InvalidOperationException)
                {
                    throw error;
                }
                throw new IOException(error.Message, error);
            }
        }

        public void Dispose()
        {
            AbortAwaiting();

            // Return all blocks
            var block = _head;
            while (block != null)
            {
                var returnBlock = block;
                block = block.Next;

                returnBlock.Pool.Return(returnBlock);
            }

            _head = null;
            _tail = null;
        }
    }
}

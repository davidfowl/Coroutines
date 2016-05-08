using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public class Scheduler : ICriticalNotifyCompletion
    {
        [ThreadStatic]
        public static int CurrentSlot;

        private readonly State[] _state;
        public Scheduler(int slots)
        {
            _state = new State[slots];
            for (int i = 0; i < _state.Length; i++)
            {
                _state[i] = new State();
            }
        }

        public bool IsCompleted => false;

        public void GetResult()
        {

        }

        public Scheduler GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            _state[CurrentSlot] = new State
            {
                Callback = continuation
            };
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _state[CurrentSlot] = new State
            {
                Callback = continuation
            };
        }

        public bool IsFinished(int slot)
        {
            return _state[slot]?.Completed == true;
        }

        public bool HasContinuation(int slot)
        {
            return _state[slot]?.Callback != null;
        }

        public Action GetContinuation(int slot)
        {
            var state = _state[slot];
            Action cb = null;

            if (state.Callback != null)
            {
                cb = state.Callback;
                state.Callback = null;
            }
            return cb;
        }

        public void MarkCompleted(int slot)
        {
            _state[slot].Completed = true;
        }

        public bool AllDone
        {
            get
            {
                for (int i = 0; i < _state.Length; i++)
                {
                    if (!_state[i].Completed)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private class State
        {
            public bool Completed;
            public Action Callback;
        }
    }
}

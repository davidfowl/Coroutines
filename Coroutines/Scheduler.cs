using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public class Scheduler
    {
        private readonly State[] _state;
        private int _completed;

        public Scheduler(SocketInput[] inputs, Action<SocketInput> callback)
        {
            _state = new State[inputs.Length];
            for (int i = 0; i < _state.Length; i++)
            {
                _state[i] = new State(inputs[i], callback);
            }
        }

        public bool Completed
        {
            get
            {
                return _completed == _state.Length;
            }
        }
        public bool Run(int slot)
        {
            var state = _state[slot];

            if (state.Completed)
            {
                return false;
            }

            if (state.Iterator.MoveNext())
            {
                return true;
            }

            state.Completed = true;
            _completed++;
            return false;
        }

        private class State
        {
            public bool Completed;
            public AsyncIterator Iterator;

            public State(SocketInput socketInput, Action<SocketInput> callback)
            {
                Iterator = new AsyncIterator(() => callback(socketInput));
            }
        }
    }
}

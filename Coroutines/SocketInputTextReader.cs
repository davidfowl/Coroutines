using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public class SocketInputTextReader : TextReader
    {
        private readonly SocketInput _input;
        private readonly Encoding _encoding;

        public SocketInputTextReader(SocketInput input, Encoding encoding)
        {
            _input = input;
            _encoding = encoding;
        }

        protected SocketInputTextReader()
        {
        }

        public override int Peek()
        {
            var scan = _input.ConsumingStart();
            int peeked = scan.Peek();
            _input.ConsumingComplete(scan, scan);
            return peeked;
        }

        public override int Read()
        {
            var scan = _input.ConsumingStart();
            int took = scan.Peek();
            _input.ConsumingComplete(scan, scan);
            return took;
        }

        public override async Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            while (true)
            {
                await _input;

                var fin = _input.DataComplete;

                var begin = _input.ConsumingStart();
                int actual;

                var end = begin.CopyTo(_encoding, buffer, index, count, out actual);
                _input.ConsumingComplete(end, end);

                if (actual != 0)
                {
                    return actual;
                }
                else if (fin)
                {
                    return 0;
                }
            }
        }

        public override async Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            while (true)
            {
                await _input;

                var fin = _input.DataComplete;

                var begin = _input.ConsumingStart();
                int actual;

                var end = begin.CopyTo(_encoding, buffer, index, count, out actual);
                _input.ConsumingComplete(end, end);

                if (actual != 0)
                {
                    return actual;
                }
                else if (fin)
                {
                    return 0;
                }
            }
        }
    }
}

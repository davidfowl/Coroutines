using System;
using System.Buffers;
using System.Text;

namespace Coroutines
{
    internal class MemoryPool : IMemoryPool
    {
        public IMemoryPoolBlock Lease()
        {
            return new MemoryPoolBlock()
            {
                Data = new ArraySegment<byte>(ArrayPool<byte>.Shared.Rent(4096)),
                Pool = this
            };
        }

        public void Return(IMemoryPoolBlock leased)
        {
            ArrayPool<byte>.Shared.Return(leased.Data.Array);
        }
    }

    internal class MemoryPoolBlock : IMemoryPoolBlock
    {
        public byte[] Array => Data.Array;

        public ArraySegment<byte> Data { get; set; }

        public int Start { get; set; }
        public int End { get; set; }

        public IMemoryPoolBlock Next { get; set; }

        public IMemoryPool Pool { get; set; }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(Data.Array, Data.Offset, Data.Count);
        }
    }
}
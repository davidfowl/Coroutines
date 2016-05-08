using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public interface IMemoryPoolBlock
    {
        ArraySegment<byte> Data { get; set; }

        int Start { get; set; }
        int End { get; set; }
        IMemoryPoolBlock Next { get; set; }
        IMemoryPool Pool { get; }
    }

}

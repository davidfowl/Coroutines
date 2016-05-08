using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public interface IMemoryPool
    {
        IMemoryPoolBlock Lease();
        void Return(IMemoryPoolBlock leased);
    }
}

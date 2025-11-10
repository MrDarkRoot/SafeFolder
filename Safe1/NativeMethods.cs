using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Safe1
{
     public static class NativeMethods
    {
        // Khai báo hàm C# để gọi hàm C++
        [DllImport("SafeFolder.NativeCrypto.dll",
                    EntryPoint = "TestPInvokeConnection",
                    CallingConvention = CallingConvention.Cdecl)]
        public static extern int TestPInvokeConnection(int value);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using static Vanara.PInvoke.Kernel32;

namespace SuspendableProcess.Interop
{
    [System.Security.SecurityCritical]
    internal static class VanaraToSafeHandles
    {
        public static SafeFileHandle SafeHFILEToSafeFileHandle(SafeHFILE input, bool ownsHandle)
        {
            return new SafeFileHandle(input.DangerousGetHandle(), ownsHandle);
        }

        public static SafeProcessHandle SafeHPROCESSToSafeProcessHandle(SafeHPROCESS input, bool ownsHandle)
        {
            return new SafeProcessHandle(input.DangerousGetHandle(), ownsHandle);
        }
    }
}

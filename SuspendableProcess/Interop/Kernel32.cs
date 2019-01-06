using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SuspendableProcess.Interop
{
    // This file also contains code from https://www.pinvoke.net/index.aspx. The license for this is unknown, but the PInvoke.net homepage encourages copy/pasting.
    public class Kernel32
    {
        private const int MAX_DEFAULTCHAR = 2;
        private const int MAX_LEADBYTES = 12;
        private const int MAX_PATH = 260;

        /// <summary>
        /// Gets information on a named code page.
        /// </summary>
        /// <param name="codePage">The code page number.</param>
        /// <param name="dwFlags">Reserved.  Must be 0.</param>
        /// <param name="lpCPInfoEx">The CPINFOEX struct to initialize.</param>
        /// <returns><c>true</c> if the operation completed successfully; <c>false</c> otherwise.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetCPInfoEx([MarshalAs(UnmanagedType.U4)] int codePage, [MarshalAs(UnmanagedType.U4)] int dwFlags, out CPINFOEX lpCPInfoEx);

        [DllImport("kernel32.dll")]
        public static extern unsafe int WideCharToMultiByte(
            uint codePage,
            uint dwFlags,
            char* lpWideCharStr,
            int cchWideChar,
            byte* lpMultiByteStr,
            int cbMultiByte,
            IntPtr lpDefaultChar,
            IntPtr lpUsedDefaultChar);

        [DllImport("kernel32.dll")]
        public static extern unsafe int MultiByteToWideChar(
            uint codePage,
            uint dwFlags,
            byte* lpMultiByteStr,
            int cbMultiByte,
            char* lpWideCharStr,
            int cchWideChar);

        public static int GetLeadByteRanges(int codePage, byte[] leadByteRanges)
        {
            int count = 0;
            Interop.Kernel32.CPINFOEX cpInfo;
            if (Interop.Kernel32.GetCPInfoEx((int)codePage, 0, out cpInfo))
            {
                // we don't care about the last 2 bytes as those are nulls
                for (int i = 0; i < 10 && leadByteRanges[i] != 0; i += 2)
                {
                    leadByteRanges[i] = cpInfo.LeadBytes[i];
                    leadByteRanges[i + 1] = cpInfo.LeadBytes[i + 1];
                    count++;
                }
            }
            return count;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CPINFOEX
        {
            [MarshalAs(UnmanagedType.U4)]
            public int MaxCharSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DEFAULTCHAR)]
            public byte[] DefaultChar;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_LEADBYTES)]
            public byte[] LeadBytes;

            public char UnicodeDefaultChar;

            [MarshalAs(UnmanagedType.U4)]
            public int CodePage;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string CodePageName;
        }
    }
}
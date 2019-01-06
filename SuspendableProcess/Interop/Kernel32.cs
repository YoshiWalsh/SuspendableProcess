using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SA1121 // Use built-in type alias
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
namespace SuspendableProcess.Interop
{
    // This file contains code from https://www.codeproject.com/Articles/230005/%2FArticles%2F230005%2FLaunch-a-process-suspended which is used in accordance with the CPOLicense
    // This file also contains code from https://www.pinvoke.net/index.aspx. The license for this is unknown, but the PInvoke.net homepage encourages copy/pasting.
    public class Kernel32
    {
        private const uint STARTF_USESHOWWINDOW = 0x00000001;
        private const uint STARTF_USESTDHANDLES = 0x00000100;
        private const uint STARTF_FORCEONFEEDBACK = 0x00000040;
        private const uint NORMAL_PRIORITY_CLASS = 0x00000020;
        private const uint CREATE_BREAKAWAY_FROM_JOB = 0x01000000;
        private const uint CREATE_NO_WINDOW = 0x08000000;
        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const short SW_SHOW = 5;
        private const short SW_HIDE = 0;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int HANDLE_FLAG_INHERIT = 1;
        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const int OPEN_EXISTING = 3;
        private const uint CREATE_NEW_CONSOLE = 0x00000010;
        private const uint STILL_ACTIVE = 0x00000103;

        private const int MAX_DEFAULTCHAR = 2;
        private const int MAX_LEADBYTES = 12;
        private const int MAX_PATH = 260;

        [Flags]
        public enum ProcessCreationFlags : uint
        {
            ZERO_FLAG = 0x00000000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00001000,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }

        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(int hObject);
        [DllImport("kernel32.dll")]
        public static extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);
        [DllImport("kernel32.dll")]
        public static extern int GetExitCodeProcess(int hProcess, ref int lpExitCode);
        [DllImport("kernel32.dll")]
        public static extern bool CreatePipe(out IntPtr phReadPipe, out IntPtr phWritePipe, IntPtr lpPipeAttributes, uint nSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern unsafe bool ReadFile(
            IntPtr hfile,
            void* pBuffer,
            int numberOfBytesToRead,
            int* pNumberOfBytesRead,
            int pOverlapped);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetStdHandle(int stdHandle);
        [DllImport("kernel32.dll")]
        public static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);
        [DllImport("kernel32.dll")]
        public static extern bool SetHandleInformation(IntPtr hObject, int dwMask, uint dwFlags);
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr CreateFile(
            string filename,
            uint desiredAccess,
            uint shareMode,
            IntPtr attributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        public static unsafe int Read(byte[] buffer, int index, int count, IntPtr hStdOut)
        {
            int n = 0;
            fixed (byte* p = buffer)
            {
                if (!ReadFile(hStdOut, p + index, count, &n, 0))
                    return 0;
            }
            return n;
        }

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

        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
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
#pragma warning restore SA1121 // Use built-in type alias
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter

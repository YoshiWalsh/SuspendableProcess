using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using SuspendableProcess.CodeFromCoreFX;
using static SuspendableProcess.Interop.VanaraToSafeHandles;

namespace SuspendableProcess
{
    public class SuspendableProcess : Process
    {
        private Kernel32.SafeHPROCESS processHandle = null;
        private Kernel32.SafeHTHREAD threadHandle = null;
        private int? processId = null;

        private const uint ERROR_BAD_EXE_FORMAT = 0xc1;
        private const uint ERROR_EXE_MACHINE_TYPE_MISMATCH = 0xd8;

        private static object createProcessLock = new object();

        public bool StartSuspended()
        {
            var startInfo = this.StartInfo;

            Kernel32.STARTUPINFO startupInfo = default(Kernel32.STARTUPINFO);
            Kernel32.PROCESS_INFORMATION processInfo = null;
            var securityAttributes = new SECURITY_ATTRIBUTES
            {
                bInheritHandle = false
            };

            var argumentsBuilder = new StringBuilder(startInfo.Arguments);

            Kernel32.SafeHFILE parentInputPipeHandle = null;
            Kernel32.SafeHFILE childInputPipeHandle = null;
            Kernel32.SafeHFILE parentOutputPipeHandle = null;
            Kernel32.SafeHFILE childOutputPipeHandle = null;
            Kernel32.SafeHFILE parentErrorPipeHandle = null;
            Kernel32.SafeHFILE childErrorPipeHandle = null;

            lock (createProcessLock)
            {
                try
                {
                    if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                    {
                        if (startInfo.RedirectStandardInput)
                        {
                            CreatePipe(out parentInputPipeHandle, out childInputPipeHandle, true);
                        }
                        else
                        {
                            childInputPipeHandle = new Kernel32.SafeHFILE(Kernel32.GetStdHandle(Kernel32.StdHandleType.STD_INPUT_HANDLE).DangerousGetHandle(), false);
                        }

                        if (startInfo.RedirectStandardOutput)
                        {
                            CreatePipe(out parentOutputPipeHandle, out childOutputPipeHandle, false);
                        }
                        else
                        {
                            childOutputPipeHandle = new Kernel32.SafeHFILE(Kernel32.GetStdHandle(Kernel32.StdHandleType.STD_OUTPUT_HANDLE).DangerousGetHandle(), false);
                        }

                        if (startInfo.RedirectStandardError)
                        {
                            CreatePipe(out parentErrorPipeHandle, out childErrorPipeHandle, false);
                        }
                        else
                        {
                            childErrorPipeHandle = new Kernel32.SafeHFILE(Kernel32.GetStdHandle(Kernel32.StdHandleType.STD_ERROR_HANDLE).DangerousGetHandle(), false);
                        }

                        startupInfo.hStdInput = childInputPipeHandle.DangerousGetHandle();
                        startupInfo.hStdOutput = childOutputPipeHandle.DangerousGetHandle();
                        startupInfo.hStdError = childErrorPipeHandle.DangerousGetHandle();

                        startupInfo.dwFlags = Kernel32.STARTF.STARTF_USESTDHANDLES;
                    }

                    Kernel32.CREATE_PROCESS creationFlags = default(Kernel32.CREATE_PROCESS);
                    if (startInfo.CreateNoWindow)
                    {
                        creationFlags |= Kernel32.CREATE_PROCESS.CREATE_NO_WINDOW;
                    }
                    string environmentBlock = null;
                    if (startInfo.EnvironmentVariables != null)
                    {
                        creationFlags |= Kernel32.CREATE_PROCESS.CREATE_UNICODE_ENVIRONMENT;
                        environmentBlock = GetEnvironmentVariablesBlock(startInfo.EnvironmentVariables);
                    }
                    creationFlags |= Kernel32.CREATE_PROCESS.CREATE_SUSPENDED; // Here's where we make the process suspended. All of this code, just for this.
                    IntPtr environmentBlockPtr = Marshal.StringToHGlobalUni(environmentBlock);

                    string workingDirectory = startInfo.WorkingDirectory;
                    if (workingDirectory == string.Empty)
                        workingDirectory = Directory.GetCurrentDirectory();

                    bool retVal = false;
                    int? errorCode = null;

                    if (startInfo.UserName.Length != 0)
                    {
                        IntPtr passwordPtr = (startInfo.Password != null) ?
                                Marshal.SecureStringToGlobalAllocUnicode(startInfo.Password) : IntPtr.Zero;

                        AdvApi32.LogonUser(startInfo.UserName, startInfo.Domain, Marshal.PtrToStringUni(passwordPtr), AdvApi32.LogonUserType.LOGON32_LOGON_NETWORK, AdvApi32.LogonUserProvider.LOGON32_PROVIDER_DEFAULT, out AdvApi32.SafeHTOKEN phObject);
                        try
                        {
                            retVal = Kernel32.CreateProcessAsUser(phObject, startInfo.FileName, argumentsBuilder, securityAttributes, securityAttributes, true, creationFlags, environmentBlockPtr, workingDirectory, startupInfo, out processInfo);

                            if (!retVal)
                            {
                                errorCode = Marshal.GetLastWin32Error();
                            }
                        }
                        finally
                        {
                            if (passwordPtr != IntPtr.Zero)
                            {
                                Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
                            }
                        }
                    }
                    else
                    {
                        retVal = Kernel32.CreateProcess(startInfo.FileName, argumentsBuilder, securityAttributes, securityAttributes, true, creationFlags, environmentBlockPtr, workingDirectory, startupInfo, out processInfo);

                        if (!retVal)
                        {
                            errorCode = Marshal.GetLastWin32Error();
                        }
                    }

                    if (!retVal)
                    {
                        if (errorCode != null)
                        {
                            throw new Win32Exception((int)errorCode);
                        }

                        throw new Win32Exception();
                    }

                    if (!processInfo.hProcess.IsNull && !processInfo.hProcess.IsInvalid)
                    {
                        processHandle = processInfo.hProcess;
                    }
                    if (!processInfo.hThread.IsNull && !processInfo.hThread.IsInvalid)
                    {
                        threadHandle = processInfo.hThread;
                    }
                }
                finally
                {
                    childInputPipeHandle?.Dispose();
                    childOutputPipeHandle?.Dispose();
                    childErrorPipeHandle?.Dispose();
                }
            }

            StreamWriter standardInput = null;
            StreamReader standardOutput = null;
            StreamReader standardError = null;
            if (startInfo.RedirectStandardInput)
            {
                Encoding enc = GetEncoding((int)Kernel32.GetConsoleCP());
                standardInput = new StreamWriter(new FileStream(SafeHFILEToSafeFileHandle(parentInputPipeHandle, false), FileAccess.Write, 4096, false), enc, 4096);
                standardInput.AutoFlush = true;
                this.GetType().BaseType.GetField("standardInput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, standardInput);
            }
            if (startInfo.RedirectStandardOutput)
            {
                Encoding enc = GetEncoding((int)Kernel32.GetConsoleOutputCP());
                standardOutput = new StreamReader(new FileStream(SafeHFILEToSafeFileHandle(parentOutputPipeHandle, false), FileAccess.Read, 4096, false), enc, true, 4096);
                this.GetType().BaseType.GetField("standardOutput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, standardOutput);
            }
            if (startInfo.RedirectStandardError)
            {
                Encoding enc = GetEncoding((int)Kernel32.GetConsoleOutputCP());
                standardError = new StreamReader(new FileStream(SafeHFILEToSafeFileHandle(parentErrorPipeHandle, false), FileAccess.Read, 4096, false), enc, true, 4096);
                this.GetType().BaseType.GetField("standardError", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, standardError);
            }

            if (processHandle == null)
            {
                return false;
            }

            this.GetType().BaseType.GetMethod("SetProcessHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, new object[] { SafeHPROCESSToSafeProcessHandle(processHandle, false) });
            this.GetType().BaseType.GetMethod("SetProcessId", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, new object[] { (int)processInfo.dwProcessId });
            
            processId = (int)processInfo.dwProcessId;

            return true;
        }

        public int Resume()
        {
            var retVal = (int)Kernel32.ResumeThread(threadHandle);
            if(retVal == -1)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode);
            }
            return retVal;
        }

        public int Suspend()
        {
            var retVal = (int)Kernel32.SuspendThread(threadHandle);
            if (retVal == -1)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode);
            }
            return retVal;
        }

        private static Encoding GetSupportedConsoleEncoding(int codepage)
        {
            int defaultEncCodePage = Encoding.GetEncoding(0).CodePage;

            if ((defaultEncCodePage == codepage) || defaultEncCodePage != Encoding.UTF8.CodePage)
            {
                return Encoding.GetEncoding(codepage);
            }

            if (codepage != Encoding.UTF8.CodePage)
            {
                return new OSEncoding(codepage);
            }

            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        private static Encoding GetEncoding(int codePage)
        {
            Encoding enc = GetSupportedConsoleEncoding(codePage);
            return new ConsoleEncoding(enc); // ensure encoding doesn't output a preamble
        }

        private static string GetEnvironmentVariablesBlock(StringDictionary sd)
        {
            // get the keys
            string[] keys = new string[sd.Count];
            sd.Keys.CopyTo(keys, 0);

            // sort both by the keys
            // Windows 2000 requires the environment block to be sorted by the key
            // It will first converting the case the strings and do ordinal comparison.

            // We do not use Array.Sort(keys, values, IComparer) since it is only supported
            // in System.Runtime contract from 4.20.0.0 and Test.Net depends on System.Runtime 4.0.10.0
            // we workaround this by sorting only the keys and then lookup the values form the keys.
            Array.Sort(keys, StringComparer.OrdinalIgnoreCase);

            // create a list of null terminated "key=val" strings
            StringBuilder stringBuff = new StringBuilder();
            for (int i = 0; i < sd.Count; ++i)
            {
                stringBuff.Append(keys[i]);
                stringBuff.Append('=');
                stringBuff.Append(sd[keys[i]]);
                stringBuff.Append('\0');
            }

            // an extra null at the end that indicates end of list will come from the string.
            return stringBuff.ToString();
        }

        private static void CreatePipe(out Kernel32.SafeHFILE parentHandle, out Kernel32.SafeHFILE childHandle, bool parentInputs)
        {
            SECURITY_ATTRIBUTES securityAttributesParent = new SECURITY_ATTRIBUTES
            {
                bInheritHandle = true
            };

            Kernel32.SafeHFILE hTmp = null;

            try
            {
                if (parentInputs)
                {
                    Kernel32.CreatePipe(out childHandle, out hTmp, securityAttributesParent, 0);
                }
                else
                {
                    Kernel32.CreatePipe(out hTmp, out childHandle, securityAttributesParent, 0);
                }

                HPROCESS currentProcHandle = Kernel32.GetCurrentProcess();
                if (!Kernel32.DuplicateHandle(currentProcHandle, hTmp.DangerousGetHandle(), currentProcHandle, out IntPtr parentHandlePtr, 0, false, Kernel32.DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS))
                {
                    throw new Win32Exception();
                }
                parentHandle = new Kernel32.SafeHFILE(parentHandlePtr);
            }
            finally
            {
                if (hTmp != null && !hTmp.IsInvalid)
                {
                    hTmp.Dispose();
                }
            }
        }
    }
}

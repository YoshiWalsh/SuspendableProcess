using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuspendableProcess;

namespace SuspendableProcess.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var proc = new SuspendableProcess()
            {
                StartInfo =
                {
                    FileName = @"C:\Windows\System32\cmd.exe",
                    Arguments = "/C echo hi",
                    RedirectStandardOutput = true
                }
            };
            proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                Console.WriteLine(e.Data);
            };
            proc.StartSuspended();
            proc.Resume();
            proc.BeginOutputReadLine();
            //var text = proc.StandardOutput.ReadToEnd();
            //Console.WriteLine(text);
            proc.WaitForExit();
            Console.ReadKey();
        }
    }
}

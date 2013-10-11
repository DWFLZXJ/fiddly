using System;
using System.Diagnostics;

namespace HTTPTrafficFiddler.Components
{
    static class SystemInterface
    {
        /// <summary>
        /// Executes a command via "Command Prompt".
        /// </summary>
        public static void ShellExecute(String command)
        {
            var p = new Process();

            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = "/k " + command + " && exit";

            p.Start();
            p.WaitForExit();

            p.Dispose();
        }
    }
}

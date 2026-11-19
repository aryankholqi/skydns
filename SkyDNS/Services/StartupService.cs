using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace SkyDNS;

/// <summary>
/// Launch at sign-in via Task Scheduler with highest privileges,
/// so the elevated app starts without a UAC prompt.
/// </summary>
public static class StartupService
{
    const string TaskName = "SkyDNS";

    public static void Set(bool enabled)
    {
        if (enabled)
        {
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Unknown exe path");
            var user = SecurityElement.Escape(WindowsIdentity.GetCurrent().Name);
            var xml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>{user}</UserId></LogonTrigger></Triggers>
  <Principals><Principal id=""Author""><UserId>{user}</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author""><Exec><Command>{SecurityElement.Escape(exe)}</Command><Arguments>--tray</Arguments></Exec></Actions>
</Task>";
            var tmp = Path.Combine(Path.GetTempPath(), "skydns-task.xml");
            File.WriteAllText(tmp, xml, Encoding.Unicode);
            try { Run($"/create /tn \"{TaskName}\" /xml \"{tmp}\" /f"); }
            finally { try { File.Delete(tmp); } catch { } }
        }
        else
        {
            Run($"/delete /tn \"{TaskName}\" /f", ignoreErrors: true);
        }
    }

    static void Run(string args, bool ignoreErrors = false)
    {
        var psi = new ProcessStartInfo("schtasks", args)
        {
            CreateNoWindow = true, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0 && !ignoreErrors) throw new InvalidOperationException(err.Trim());
    }
}

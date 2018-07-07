using LeagueFPSBoost.Text;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace LeagueFPSBoost.ProcessManagement
{
    public static class SelfElevation
    {
        public static bool Elevate()
        {
            if (IsAdministrator() == false)
            {
                // Restart program and run as admin
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName)
                {
                    Verb = "runas"
                };
                startInfo.Arguments = Program.ArgumentsStr + " -" + Strings.RestartReasonArg.Split('|')[0] + "=" + Program.RestartReason.SelfElevation.ToString();
                if (MessageBox.Show("Do you want to launch LeagueFPSBoost in Administrator mode?", "LeagueFPSBoost: Startup Failed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start(startInfo);
                }
                return false;
            }
            Program.PreNLog($"[{nameof(SelfElevation)}]: Process already elevated.");
            return true;
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}

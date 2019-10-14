using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeagueFPSBoost.Extensions;
using LeagueFPSBoost.Text;
using NLog;


namespace LeagueFPSBoost.Native.WMI
{
    public static class WMIManagment
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Fired when VerifyRepository() finishes.
        /// </summary>
        public static event EventHandler<WMIVerifyResultReceivedEventArgs> WMIVerifyResultReceived = delegate { };

        /// <summary>
        /// Fired when SalvageRepository() finishes.
        /// </summary>
        public static event EventHandler<WMISalvageResultReceivedEventArgs> WMISalvageResultReceived = delegate { };
        
        /// <summary>
        /// Fired when ResetRepository() finishes.
        /// </summary>
        public static event EventHandler<WMIResetResultReceivedEventArgs> WMIResetResultReceived = delegate { };

        /// <summary>
        /// Performs a consistency check on the WMI repository.
        /// When finished fires WMIVerifyResultReceived.
        /// </summary>
        public static void VerifyRepository()
        {
            Logger.Debug("Verifying WMI repository.");
            var output = RunWinMgmt(WinmgmtArgument.Verify);
            Logger.Debug($"Verifying WMI repository finished.");
            WMIVerifyResultReceived?.Invoke(null, new WMIVerifyResultReceivedEventArgs(output));
        }

        /// <summary>
        /// Performs a consistency check on the WMI repository, and if an inconsistency is detected, rebuilds the repository.
        /// When finished fires WMIVerifyResultReceived.
        /// </summary>
        public static void SalvageRepository()
        {
            Logger.Debug("Salvaging WMI repository.");
            var output = RunWinMgmt(WinmgmtArgument.Salvage);
            Logger.Debug($"Salvaging WMI repository finished.");
            WMISalvageResultReceived?.Invoke(null, new WMISalvageResultReceivedEventArgs(output));
        }

        /// <summary>
        /// The repository is reset to the initial state when the operating system is first installed.
        /// </summary>
        public static void ResetRepository()
        {
            Logger.Debug("Resetting WMI repository.");
            var output = RunWinMgmt(WinmgmtArgument.Reset);
            Logger.Debug($"Resetting WMI repository finished.");
            WMIResetResultReceived?.Invoke(null, new WMIResetResultReceivedEventArgs(output));
        }

        /// <summary>
        /// Runs WinMgmt with specified argument and waits for the exit then returns the console output.
        /// </summary>
        /// <param name="winmgmtArgument">WinMgmt command line switch.</param>
        /// <returns>Console output.</returns>
        private static string RunWinMgmt(WinmgmtArgument winmgmtArgument)
        {
            string argument;

            switch (winmgmtArgument)
            {
                case WinmgmtArgument.Verify: argument = WinMgmtResources.ArgumentVerifyRepository; break;
                case WinmgmtArgument.Salvage: argument = WinMgmtResources.ArgumentSalvageRepository; break;
                case WinmgmtArgument.Reset: argument = WinMgmtResources.ArgumentResetRepository; break;
                default: throw new ArgumentException(nameof(winmgmtArgument));
            }

            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = WinMgmtResources.Command;
            p.StartInfo.Arguments = argument;
            Logger.Debug($"Executing: {p.StartInfo.FileName} {p.StartInfo.Arguments}");
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

        /// <summary>
        /// Command line switches for WinMgmt
        /// </summary>
        private enum WinmgmtArgument
        {
            Verify,
            Salvage,
            Reset
        }
    }

    public class WMIVerifyResultReceivedEventArgs : WMIResultReceivedEventArgs
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// True if repository is consistent.
        /// </summary>
        public bool Consistent { get; private set; }

        public WMIVerifyResultReceivedEventArgs(string _output) : base(_output)
        {
            Logger.Debug("Creating new instance.");
            Success = Consistent = _output.ToLower().Contains(WinMgmtResources.RepositoryConsistent.ToLower());
            Logger.Debug("New instance created: " + Environment.NewLine + 
                "Consistent: " + Consistent + Environment.NewLine +
                "Success: " + Success + Environment.NewLine +
                "Output: " + Environment.NewLine + Output);
        }
    }

    public class WMIResetResultReceivedEventArgs : WMIResultReceivedEventArgs
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// True if repository is consistent.
        /// </summary>
        public bool Resetted { get; private set; }

        public WMIResetResultReceivedEventArgs(string _output) : base(_output)
        {
            Logger.Debug("Creating new instance.");
            Success = Resetted = _output.ToLower().Contains(WinMgmtResources.RepositoryResetted.ToLower());
            Logger.Debug("New instance created: " + Environment.NewLine +
                "Resetted: " + Resetted + Environment.NewLine +
                "Success: " + Success + Environment.NewLine +
                "Output: " + Environment.NewLine + Output);
        }
    }

    public class WMISalvageResultReceivedEventArgs : WMIVerifyResultReceivedEventArgs
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// True if salvaging repository succeeded.
        /// </summary>
        public bool Salvaged { get; private set; }

        public WMISalvageResultReceivedEventArgs(string _output) : base(_output)
        {
            Logger.Debug("Creating new instance.");
            Salvaged = Output.ToLower().Contains(WinMgmtResources.RepositorySalvaged.ToLower());
            Success = Salvaged ? Salvaged : Consistent;
            Logger.Debug("New instance created: " + Environment.NewLine +
                "Consistent: " + Consistent + Environment.NewLine +
                "Salvaged: " + Salvaged + Environment.NewLine +
                "Success: " + Success + Environment.NewLine +
                "Output: " + Environment.NewLine + Output);
        }
    }

    public class WMIResultReceivedEventArgs : EventArgs
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Console output.
        /// </summary>
        public string Output { get; private set; }

        /// <summary>
        /// True when command is successful.
        /// </summary>
        public bool Success { get; protected set; }

        /// <summary>
        /// True if winmgmt fails with error code: 0x8007007E Often triggers if service is not running
        /// or the winmgmt is called in SystemWOW64 on 64bit os.
        /// </summary>
        public bool WOW64Error { get; private set; }

        /// <summary>
        /// True if winmgmt throws any error code.
        /// </summary>
        public bool Error { get; private set; }

        public WMIResultReceivedEventArgs(string _output)
        {
            //Logger.Debug("Creating new instance.");
            Output = _output;
            WOW64Error = Output.ToLower().Contains(WinMgmtResources.WOW64Error.ToLower());
            Error = Output.ToLower().Contains(WinMgmtResources.Error.ToLower());
            if (Error) Logger.Error("WinMgmt.exe failed with error code.");
            if (WOW64Error) Logger.Error("WinMgmt.exe failed with error code: 0x8007007E. Is the program running in WOW64?");
        }

    }
}

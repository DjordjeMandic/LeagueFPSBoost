using LeagueFPSBoost.Logging;
using LeagueFPSBoost.Native.Unmanaged;
using LeagueFPSBoost.Native.WMI;
using LeagueFPSBoost.Text;
using LeagueFPSBoost.Updater;
using NLog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace LeagueFPSBoost.ProcessManagement
{
    public class LeagueBoostErrorEventArgs : LeagueBoostEventArgs
    {
        public LeagueBoostErrorEventArgs(bool _clientRunning, Exception _exception) : base(_clientRunning)
        {
            Exception = _exception;
        }

        public LeagueBoostErrorEventArgs(bool _clientRunning, Exception _exception, string _reason) : base(_clientRunning)
        {
            Exception = _exception;
            Reason = _reason;
        }

        public string Reason { get; private set; }

        public Exception Exception { get; private set; }
    }

    public class LeagueBoostEventArgs : EventArgs
    {
        public bool ClientRunning { get; private set; }

        public LeagueBoostEventArgs(bool _clientRunning)
        {
            ClientRunning = _clientRunning;
        }
    }

    public class ProcessPriorityWatcherEnabledEventArgs : EventArgs
    {
        /// <summary>
        /// True if Enabled.
        /// </summary>
        public bool Enabled { get; private set; }

        public ProcessPriorityWatcherEnabledEventArgs(bool _Enabled)
        {
            Enabled = _Enabled;
        }
    }
    public static class LeaguePriority
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        

        public static event EventHandler<LeagueBoostEventArgs> GameBoostOk = delegate { };
        public static event EventHandler<LeagueBoostErrorEventArgs> GameBoostFail = delegate { };
        public static event EventHandler<LeagueBoostEventArgs> ClientNormalOk = delegate { };
        public static event EventHandler<LeagueBoostErrorEventArgs> ClientNormalFail = delegate { };

        /// <summary>
        /// Fired when ProcessWatcher enabled changes state.
        /// </summary>
        public static event EventHandler<ProcessPriorityWatcherEnabledEventArgs> ProcessPriorityWatcherEnabled = delegate { };


        public static System.Timers.Timer BoostCheckTimer { get; private set; }
        public static readonly int BoostCheckTimerInterval = 1 * 60 * 1000;
        static int boostCheckEventCount;

        private static bool WMIVerifyReceived;
        private static bool WMIVerifyConsistent;

        private static bool WMISalvageReceived;
        private static bool WMISalvageSuccess;

        private static bool WMIResetReceived;
        private static bool WMIResetSuccess;

        public static bool ProcessWatcherEnabled { get; private set; }


        /// <summary>
        /// Returns true if watcher has been started successfully, false otherwise.
        /// </summary>
        /// <returns></returns>
        public static bool InitAndStartWatcher()
        {
            ProcessPriorityWatcherEnabled += LeaguePriority_ProcessPriorityWatcherEnabled;
            InitBoostCheckTimer();
            try
            {
                StartProcessWatcher();
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while trying to start process watcher: " + Environment.NewLine);
                MessageBox.Show($"WMI repository seems to be bad. (WMI is something important in windows for correct functionality) " +
                    $"Program will now preform consistency check on the WMI repository. If WMI repository is not consistent then league process management wont work.", "LeagueFPSBoost: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WMIManagment.WMIVerifyResultReceived += WMIManagment_WMIVerifyResultReceived;
                WMIManagment.VerifyRepository();
                WMIManagment.WMIVerifyResultReceived -= WMIManagment_WMIVerifyResultReceived;

                while (!WMIVerifyReceived) Thread.Sleep(100);
                if(WMIVerifyConsistent)
                {
                    var rslt = MessageBox.Show("WMI repository is consistent but program cannot access wmi. Do you want to set Winmgmt service startup type to automatic?", "LeagueFPSBoost: WMI Result", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if(rslt == DialogResult.Yes)
                    {
                        logger.Debug("Changing WMI service startup type to automatic.");
                        try
                        {
                            var svc = new ServiceController(WinMgmtResources.ServiceName);
                            ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Automatic);
                            logger.Debug("WMI service startup type changed to automatic successfully.");
                            return RetryStartProcessWatcher();
                        }
                        catch(Exception ex2)
                        {
                            logger.Error(ex2, Strings.exceptionThrown + " while trying to change WMI service startup type: " + Environment.NewLine);
                            MessageBox.Show("Could not access WMI. Check logs for more details. Process priority managment will not work until this problem is fixed.", "LeagueFPSBoost: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if(DialogResult.Yes == MessageBox.Show("WMI repository is not consistent. Would you like to try salvaging repository(try to repair it)?"
                        + Environment.NewLine + "If you select No then no changes will be made to your WMI repository but game process priority management will not work.", "LeagueFPSBoost: Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error))
                    {
                        WMIManagment.WMISalvageResultReceived += WMIManagment_WMISalvageResultReceived;
                        WMIManagment.SalvageRepository();
                        WMIManagment.WMISalvageResultReceived -= WMIManagment_WMISalvageResultReceived;
                        while (!WMISalvageReceived) Thread.Sleep(100);
                        if(WMISalvageSuccess)
                        {
                            MessageBox.Show("WMI repository salvaged successfully. Trying to restart process watcher.", "LeagueFPSBoost: WMI Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return RetryStartProcessWatcher();
                        }
                        else
                        {
                            if (DialogResult.Yes == MessageBox.Show("WMI salvaging failed, check logs for more deatils. Would you like to reset wmi repository to initial state when system was installed?", "LeagueFPSBoost: Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error))
                            {
                                WMIManagment.WMIResetResultReceived += WMIManagment_WMIResetResultReceived;
                                WMIManagment.ResetRepository();
                                WMIManagment.WMIResetResultReceived -= WMIManagment_WMIResetResultReceived;
                                while (!WMIResetReceived) Thread.Sleep(100);
                                if(WMIResetSuccess)
                                {
                                    MessageBox.Show("WMI repository has been reset successfully. Trying to restart process watcher.", "LeagueFPSBoost: WMI Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return RetryStartProcessWatcher();
                                }
                                else
                                {
                                    MessageBox.Show("WMI repository resetting failed. See logs for more details. Process priority management will not work until problem is fixed.", "LeagueFPSBoost: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        private static void LeaguePriority_ProcessPriorityWatcherEnabled(object sender, ProcessPriorityWatcherEnabledEventArgs e)
        {
            ProcessWatcherEnabled = e.Enabled;
        }

        private static void WMIManagment_WMIResetResultReceived(object sender, WMIResetResultReceivedEventArgs e)
        {
            WMIResetSuccess = e.Success;
            WMIResetReceived = true;
        }

        private static bool RetryStartProcessWatcher()
        {
            try
            {
                StartProcessWatcher();
                return true;
            }
            catch (Exception ex1)
            {
                logger.Error(ex1, Strings.exceptionThrown + " while trying to start process watcher after wmi repair: " + Environment.NewLine);
                MessageBox.Show("Starting process watcher failed again. Please check logs for more detail or try to restart system.", "LeagueFPSBoost: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static void StartProcessWatcher()
        {
            logger.Debug("Trying to start process watcher.");
            Program.StartWatch.Start();
            Program.StopWatch.Start();
            BoostCheckTimer.Start();
            ProcessWatcherEnabled = true;
            ProcessPriorityWatcherEnabled?.Invoke(null, new ProcessPriorityWatcherEnabledEventArgs(true));
            logger.Debug("Process watcher has been started.");
            LeagueLogger.Okay("Process watcher started.");
        }

        public static void StopProcessWatcher()
        {
            logger.Debug("Trying to stop process watcher.");
            Program.StartWatch.Stop();
            Program.StopWatch.Stop();
            BoostCheckTimer.Stop();
            ProcessWatcherEnabled = false;
            ProcessPriorityWatcherEnabled?.Invoke(null, new ProcessPriorityWatcherEnabledEventArgs(false));
            logger.Debug("Process watcher has been stopped.");
        }

        private static void InitBoostCheckTimer()
        {
            logger.Debug("Initializing BoostCheckTimer.");
            BoostCheckTimer = new System.Timers.Timer
            {
                Interval = BoostCheckTimerInterval
            };
            BoostCheckTimer.Elapsed += BoostCheckTimer_Elapsed;
            logger.Debug("BoostCheckTimer initialized.");
        }

        private static void WMIManagment_WMISalvageResultReceived(object sender, WMISalvageResultReceivedEventArgs e)
        {
            WMISalvageSuccess = e.Success;
            WMISalvageReceived = true;
        }

        private static void WMIManagment_WMIVerifyResultReceived(object sender, WMIVerifyResultReceivedEventArgs e)
        {
            WMIVerifyConsistent = e.Consistent;
            WMIVerifyReceived = true;
        }

        private static void BoostCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Process.GetProcessesByName(Strings.GameProcessName).Length != 0)
            {
                logger.Debug($"Timer is trying to boost game. Interval: {BoostCheckTimer.Interval}ms");
                ProcessManagement.LeaguePriority.CheckAndBoost(Program.NoClient);
            }
            else if (boostCheckEventCount >= 5)
            {
                logger.Debug($"Timer is trying to return client to normal priority. Interval: {BoostCheckTimer.Interval}ms");
                ProcessManagement.LeaguePriority.CheckAndBoost(Program.NoClient);
                boostCheckEventCount = 0;
            }

            boostCheckEventCount++;
        }

        private static void Boost(bool clientRunning)
        {
            try
            {
                if (Process.GetProcessesByName(Strings.GameProcessName).Length != 0)
                {
                    logger.Debug("Trying to boost game. Client running: " + clientRunning);
                    if (clientRunning)
                    {
                        if (!IsGameHigh() || !IsClientBelowNormal())
                        {
                            try
                            {
                                SetClientPriority(ProcessPriorityClass.BelowNormal);
                                SetGamePriority(ProcessPriorityClass.High);
                                logger.Info("Game has been boosted successfully.");
                                GameBoostOk?.Invoke(null, new LeagueBoostEventArgs(clientRunning));

                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, Strings.exceptionThrown + " while boosting game: " + Environment.NewLine);
                                GameBoostFail?.Invoke(null, new LeagueBoostErrorEventArgs(clientRunning, ex));
                            }
                        }
                    }
                    else if (!IsGameHigh())
                    {
                        try
                        {
                            SetGamePriority(ProcessPriorityClass.High);
                            logger.Info("Game has been boosted successfully.");
                            GameBoostOk?.Invoke(null, new LeagueBoostEventArgs(clientRunning));
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, Strings.exceptionThrown + " while boosting game: " + Environment.NewLine);
                            GameBoostFail?.Invoke(null, new LeagueBoostErrorEventArgs(clientRunning, ex));
                        }
                    }
                }
                else if (clientRunning && !IsClientNormal())
                {
                    try
                    {
                        SetClientPriority(ProcessPriorityClass.Normal);
                        logger.Info("Client has been returned to normal successfully.");
                        ClientNormalOk?.Invoke(null, new LeagueBoostEventArgs(clientRunning));
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while returning client to normal: " + Environment.NewLine);
                        ClientNormalFail?.Invoke(null, new LeagueBoostErrorEventArgs(clientRunning, ex));
                    }
                }
                else
                {
                    logger.Debug("Game is not running.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while trying to boost game." + Environment.NewLine);
            }
        }

        public static void CheckAndBoost(bool noClient)
        {
            logger.Debug("Checking processes for changing priority. No Client Arg: " + noClient);
            if (Process.GetProcessesByName(Strings.ClientProcessNames[0]).Length != 0)
            {
                Boost(true);
            }
            else
            {
                if (noClient)
                {
                    Boost(!noClient); // client running false
                }
                else if (Process.GetProcessesByName(Strings.GameProcessName).Length != 0)
                {
                    logger.Info("Game has been started. Process name: " + Strings.GameProcessName);
                    logger.Info("Game was not boosted because client is not running. Checked process name: " + Strings.ClientProcessNames[0]);
                    GameBoostFail?.Invoke(null, new LeagueBoostErrorEventArgs(false, new Exception("Client is not running."), "Game was not boosted because client is not running."));
                }
            }
        }

        public static bool IsClientBelowNormal()
        {
            return Process.GetProcesses().Where(proc => proc.ProcessName.StartsWith(Strings.ClientProcessNames[0], StringComparison.Ordinal)).ToList().All(proc => proc.PriorityClass == ProcessPriorityClass.BelowNormal);
        }

        public static bool IsClientNormal()
        {
            return Process.GetProcesses().Where(proc => proc.ProcessName.StartsWith(Strings.ClientProcessNames[0], StringComparison.Ordinal)).ToList().All(proc => proc.PriorityClass == ProcessPriorityClass.Normal);
        }

        public static bool IsGameHigh()
        {

            return Process.GetProcessesByName(Strings.GameProcessName).ToList().All(proc => proc.PriorityClass == ProcessPriorityClass.High);
        }

        public static void SetClientPriority(ProcessPriorityClass ppclass)
        {
            if (ppclass == ProcessPriorityClass.BelowNormal)
            {
                UpdateManager.StopUpdateCheckTimer();
            }
            else
            {
                UpdateManager.StartUpdateCheckTimer();
            }
            logger.Debug("Changing client priority to: " + ppclass);
            foreach (string clientProcessName in Strings.ClientProcessNames)
            {
                foreach (Process proc in Process.GetProcessesByName(clientProcessName))
                {
                    try
                    {
                        logger.Debug($"Changing client process {proc.Id} priority to {ppclass}.");
                        proc.PriorityClass = ppclass;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + $" while changing client process {proc.Id} priority to {ppclass}. " + Environment.NewLine);
                        logger.Debug($"Process info: {Environment.NewLine}{proc.GetProcessInfoForLogging(Program.PrintProcessModules)}");
                    }
                }
            }
        }

        public static void SetGamePriority(ProcessPriorityClass ppclass)
        {
            if(ppclass == ProcessPriorityClass.High)
            {
                UpdateManager.StopUpdateCheckTimer();
            }
            logger.Debug("Changing game priority to: " + ppclass);
            foreach (Process proc in Process.GetProcessesByName(Strings.GameProcessName))
            {
                try
                {
                    logger.Debug($"Changing game process {proc.Id} priority to {ppclass}.");
                    proc.PriorityClass = ppclass;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, Strings.exceptionThrown + $" while changing game process {proc.Id} priority to {ppclass}. " + Environment.NewLine);
                    logger.Debug($"Process info: {Environment.NewLine}{proc.GetProcessInfoForLogging(Program.PrintProcessModules)}");
                }
            }
        }

        public static bool CheckForFreeMemory(ulong requiredMemoryInBytes = 1073741824)
        {
            try
            {
                logger.Debug("Reading available physical memory...");
                var freemem = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
                logger.Debug("Available physical memory: " + freemem + " Bytes");
                logger.Debug("Specified required memory: " + requiredMemoryInBytes + " Bytes");
                if(freemem > requiredMemoryInBytes)
                {
                    logger.Info("There is enough memory for the game. Remaining: " + (freemem - requiredMemoryInBytes) + " Bytes");
                    return true;
                }
                logger.Warn("There is not enough memory for the game. Missing: " + (requiredMemoryInBytes - freemem) + " Bytes");
                System.Media.SystemSounds.Beep.Play();
            }
            catch (Win32Exception win32ex)
            {
                logger.Error(win32ex, Strings.exceptionThrown + " while trying to read available physical memory:" + Environment.NewLine);
            }
            return false;
        }
    }
}

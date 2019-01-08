using LeagueFPSBoost.Logging;
using LeagueFPSBoost.Text;
using LeagueFPSBoost.Updater;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
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

    public static class LeaguePriority
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        

        public static event EventHandler<LeagueBoostEventArgs> GameBoostOk = delegate { };
        public static event EventHandler<LeagueBoostErrorEventArgs> GameBoostFail = delegate { };
        public static event EventHandler<LeagueBoostEventArgs> ClientNormalOk = delegate { };
        public static event EventHandler<LeagueBoostErrorEventArgs> ClientNormalFail = delegate { };


        public static System.Timers.Timer BoostCheckTimer { get; private set; }
        public static readonly int BoostCheckTimerInterval = 1 * 60 * 1000;
        static int boostCheckEventCount;

        public static void StartWatcher()
        {
            try
            {
                logger.Debug("Trying to start process watcher.");
                Program.StartWatch.Start();
                Program.StopWatch.Start();
                logger.Debug("Process watcher has been started.");
                LeagueLogger.Okay("Process watcher started.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while trying to start process watcher: " + Environment.NewLine);
                MessageBox.Show($"There was an fatal error.{Environment.NewLine}Please restart the program.{Environment.NewLine}Check log for details.{Environment.NewLine}Program will now close.", "LeagueFPSBoost: Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            BoostCheckTimer = new System.Timers.Timer
            {
                Interval = BoostCheckTimerInterval
            };
            BoostCheckTimer.Elapsed += BoostCheckTimer_Elapsed;

            BoostCheckTimer.Start();
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
    }
}

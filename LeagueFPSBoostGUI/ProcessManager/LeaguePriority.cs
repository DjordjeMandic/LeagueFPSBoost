using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Diagnostics;

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

        private static void Boost(bool clientRunning)
        {
            try
            {
                if (Process.GetProcessesByName(Strings.GameProcessName).Length != 0)
                {
                    logger.Debug("Trying to boost game. Client running: " + clientRunning);
                    if (clientRunning)
                    {
                        if (IsClientNormal() || !IsGameHigh())
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
                else if (clientRunning && IsClientBelowNormal())
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
            var status = true;
            foreach (string clientProcessName in Strings.ClientProcessNames)
            {
                if (!status) break;
                foreach (Process proc in Process.GetProcessesByName(clientProcessName))
                {
                    if (status && proc.PriorityClass != ProcessPriorityClass.BelowNormal)
                    {
                        status = false;
                        break;
                    }
                }
            }
            return status;
        }

        public static bool IsClientNormal()
        {
            var status = true;
            foreach (string clientProcessName in Strings.ClientProcessNames)
            {
                if (!status) break;
                foreach (Process proc in Process.GetProcessesByName(clientProcessName))
                {
                    if (status && proc.PriorityClass != ProcessPriorityClass.Normal)
                    {
                        status = false;
                        break;
                    }
                }
            }
            return status;
        }

        public static bool IsGameHigh()
        {
            var game = false;
            foreach (Process proc in Process.GetProcessesByName(Strings.GameProcessName))
            {
                game = proc.PriorityClass == ProcessPriorityClass.High;
            }
            return game;
        }

        public static void SetClientPriority(ProcessPriorityClass ppclass)
        {
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
                        logger.Debug($"Process info: {Environment.NewLine}{proc.GetProcessInfoForLogging(Program.printProcessModules)}");
                    }
                }
            }
        }

        public static void SetGamePriority(ProcessPriorityClass ppclass)
        {
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
                    logger.Debug($"Process info: {Environment.NewLine}{proc.GetProcessInfoForLogging(Program.printProcessModules)}");
                }
            }
        }
    }
}

﻿using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LeagueFPSBoost.ProcessManagement
{
    static class Restart
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static bool RestartNow()
        {
            return RestartNow(Application.ExecutablePath);
        }

        public static bool RestartNow(string path)
        {
            logger.Info("Trying to restart!");
            try
            {
                Process.Start(path);
                logger.Info("Successfully started new instance. Closing this one.");
                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while trying to restart LeagueFPSBoost." + Environment.NewLine);
                return false;
            }
        }

    }
}

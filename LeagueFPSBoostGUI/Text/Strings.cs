using Microsoft.Win32.Interop;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LeagueFPSBoost.Text
{
    public static class Strings
    {
        public static readonly string tab = "\t";
        public static readonly string tabWithLine = tab + "- ";
        public static readonly string doubleTabWithLine = tab + tabWithLine;
        public static readonly string tripleTabWithLine = tab + doubleTabWithLine;

        public static readonly string noClientArg = "-noClient";
        public static readonly string updateCheckArg = "-updateCheck";
        public static readonly string printProcessModulesArg = "-procModules";
        public static readonly string configRestartReasonArg = "--configRestartReason";
        public static readonly string adminRestartReasonArg = "--adminRestartReason";
        public static readonly string createUpdateFolderArg = "-createUpdateFolder";
        public static readonly string updatedArg = "--updated";

        public static readonly string exceptionThrown = "Exception has been thrown";

        public static readonly string LeagueRegistrySubKeyName = @"SOFTWARE\Riot Games, Inc\League of Legends";
        public static readonly string LeagueRegistryLocationStringName = "Location";

        public static readonly string defaultLeagueDirectoryPath = @"C:\Riot Games\League Of Legends";
        public static readonly string logDateTimeFormat = "yyyy-MM-ddTHH-mm-ss";
        public static readonly string startTimeFormat = "dd/MM/yyyy HH:mm:ss.ffff";

        public static readonly string[] ClientProcessNames = { "LeagueClient", "LeagueClientUx", "LeagueClientUxRender" };
        public static readonly string GameProcessName = "League Of Legends";

        public static readonly string ColorStyleSetTo = "Color style set to ";

        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static Dictionary<int, string> _FieldLookup;

        public static bool TryGetErrorName(int result, out string errorName)
        {
            if (_FieldLookup == null)
            {
                Dictionary<int, string> tmpLookup = new Dictionary<int, string>();

                FieldInfo[] fields = typeof(ResultWin32).GetFields();

                foreach (FieldInfo field in fields)
                {
                    int errorCode = (int)field.GetValue(null);

                    try
                    {
                        tmpLookup.Add(errorCode, field.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, exceptionThrown + $" while adding error code ({errorCode}) and name ({field.Name}) to dictionary." + Environment.NewLine);
                    }
                }

                _FieldLookup = tmpLookup;
            }

            return _FieldLookup.TryGetValue(result, out errorName);
        }

    }
}

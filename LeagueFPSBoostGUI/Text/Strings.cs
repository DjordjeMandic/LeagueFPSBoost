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

        public static readonly string noClientArg = "n|noClient";
        public static readonly string printProcessModulesArg = "p|procModules";
        public static readonly string RestartReasonArg = "r|restartReason=";
        public static readonly string UpdateFolderArg = "u|updateFolder=";
        public static readonly string ExitBeforeMainWindow = "e|exitEarly";
        public static readonly string clearLogsArg = "c|clearLogs";
        public static readonly string helpArg = "h|help";

        public static readonly string exceptionThrown = "Exception has been thrown";

        public static readonly string LeagueRegistrySubKeyName = @"SOFTWARE\Riot Games, Inc\League of Legends";
        public static readonly string LeagueRegistryLocationStringName = "Location";

        public static readonly string defaultLeagueDirectoryPath = @"C:\Riot Games\League Of Legends";
        public static readonly string logDateTimeFormat = "yyyy-MM-ddTHH-mm-ss";
        public static readonly string startTimeFormat = "dd/MM/yyyy HH:mm:ss.ffff";

        public static readonly string[] ClientProcessNames = { "LeagueClient", "LeagueClientUx", "LeagueClientUxRender" };
        public static readonly string GameProcessName = "League Of Legends";

        public static readonly string ColorStyleSetTo = "Color style set to ";




        public static readonly string Updater_XML_URL = @"https://raw.githubusercontent.com/DjordjeMandic/LeagueFPSBoost/beta/AutoUpdater/updater.xml";
        public static readonly string BoardsPage_URL = @"https://goo.gl/bpxbGV";
        public static readonly string GitHub_URL = @"https://goo.gl/eww7KH";
        public static readonly string YouTube_URL = @"https://goo.gl/RDpsri";
        public static readonly string Facebook_URL = @"https://goo.gl/MjQp43";
        public static readonly string OPGG_URL = @"https://goo.gl/sEYLbe";


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

using LeagueFPSBoost.Native.Unmanaged;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LeagueFPSBoost.Text
{
    public static class Strings
    {
        public const string tab = "\t";
        public const string tabWithLine = tab + "- ";
        public const string doubleTabWithLine = tab + tabWithLine;
        public const string tripleTabWithLine = tab + doubleTabWithLine;

        public const string noClientArg = "n|noClient";
        public const string printProcessModulesArg = "p|procModules";
        public const string RestartReasonArg = "r|restartReason=";
        public const string UpdateFolderArg = "u|updateFolder=";
        public const string ExitBeforeMainWindow = "e|exitEarly";
        public const string clearLogsArg = "c|clearLogs";
        public const string helpArg = "h|help";

        public const string exceptionThrown = "Exception has been thrown";

        public const string LeagueRegistrySubKeyName = @"SOFTWARE\Riot Games, Inc\League of Legends";
        public const string LeagueRegistryLocationStringName = "Location";

        public const string defaultLeagueDirectoryPath = @"C:\Riot Games\League Of Legends";
        public const string logDateTimeFormat = "yyyy-MM-ddTHH-mm-ss";
        public const string startTimeFormat = "dd/MM/yyyy HH:mm:ss.ffff";

        public static readonly string[] ClientProcessNames = { "LeagueClient", "LeagueClientUx", "LeagueClientUxRender" };
        public const string GameProcessName = "League Of Legends";

        public const string ColorStyleSetTo = "Color style set to ";
        
        public const string Updater_XML_URL = @"https://raw.githubusercontent.com/DjordjeMandic/LeagueFPSBoost/master/AutoUpdater/updater.xml";
        public const string Updater_XML_Download_URL = @"https://github.com/DjordjeMandic/LeagueFPSBoost/raw/master/AutoUpdater/LeagueFPSBoost.zip";
        public const string Updater_XML_Changelog_URL = @"https://boards.eune.leagueoflegends.com/en/c/alpha-client-discussion-en/jkmeEvQe-fps-boost-program-open-source-ask-any-questions-if-you-have";
        public const string BoardsPage_URL = @"https://goo.gl/bpxbGV";
        public const string GitHub_URL = @"https://goo.gl/eww7KH";
        public const string YouTube_URL = @"https://goo.gl/RDpsri";
        public const string Facebook_URL = @"https://goo.gl/MjQp43";
        public const string OPGG_URL = @"https://goo.gl/sEYLbe";


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

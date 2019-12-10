using LeagueFPSBoost.Native.Unmanaged;
using LeagueFPSBoost.Updater;
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
        public const string LeagueClientPathArg = "l|leagueClientFile=";
        public const string ExitBeforeMainWindow = "e|exitEarly";
        public const string clearLogsArg = "c|clearLogs";
        public const string helpArg = "h|help";

        public const string exceptionThrown = "Exception has been thrown";


        public const string POWER_OPTIONS_CPL = "powercfg.cpl";

        public const string LeagueRegistrySubKeyName = @"SOFTWARE\Riot Games, Inc\League of Legends";
        public const string LeagueRegistryLocationStringName = "Location";

        public const string defaultLeagueDirectoryPath = @"C:\Riot Games\League Of Legends";
        public const string logDateTimeFormat = "yyyy-MM-ddTHH-mm-ss";
        public const string startTimeFormat = "dd/MM/yyyy HH:mm:ss.ffff";

        public static readonly string[] ClientProcessNames = { "LeagueClient", "LeagueClientUx", "LeagueClientUxRender" };
        public const string GameProcessName = "League Of Legends";

        public const string ColorStyleSetTo = "Color style set to ";
        
        public const string Updater_XML_URL = @"https://raw.githubusercontent.com/DjordjeMandic/LeagueFPSBoost/master/AutoUpdater/updater.xml";
#if !DEBUG
        public const string Updater_JSON_URL = @"https://github.com/DjordjeMandic/LeagueFPSBoost/raw/master/AutoUpdater/updater.json";
#else
        public const string Updater_JSON_URL = @"file:///F:/Documents/Visual%20Studio%202017/Projects/LeagueFPSBoost/AutoUpdater/updater.json";
#endif
        public const string Updater_Download_URL_ZIP = @"https://github.com/DjordjeMandic/LeagueFPSBoost/raw/master/AutoUpdater/LeagueFPSBoost.zip";
        public const string Updater_Download_URL_ZIP_LATEST_RELEASE = @"https://github.com/DjordjeMandic/LeagueFPSBoost/releases/latest/download/LeagueFPSBoost.zip";
        public const string Updater_Changelog_URL = @"https://github.com/DjordjeMandic/LeagueFPSBoost/releases";

        public const string BoardsPage_URL = @"https://goo.gl/bpxbGV";
        public const string GitHub_URL = @"https://goo.gl/eww7KH";
        public const string YouTube_URL = @"https://goo.gl/RDpsri";
        public const string Facebook_URL = @"https://goo.gl/MjQp43";
        public const string OPGG_URL = @"https://goo.gl/sEYLbe";
        public const string DONATE_URL = @"https://paypal.me/DjordjeMandic";
        public const string DOT_NET_FRAMEWORK_URL = @"https://dotnet.microsoft.com/download/thank-you/net472";

        public const string UserTriggeredExceptionMessage = @"This is user triggered crash used to create and send crash report with logs. Application is working fine, just restart it after crash reporting is done.";

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

using AutoUpdaterDotNET;
using LeagueFPSBoost.GUI;
using LeagueFPSBoost.Text;
using LeagueFPSBoost.Updater.MessageBoxCollection;
using LeagueFPSBoost.Updater.PostUpdateAction;
using Newtonsoft.Json;
using NLog;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace LeagueFPSBoost.Updater
{
    static class UpdateManager
    {

        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static System.Timers.Timer UpdateCheckTimer { get; private set; }
        public static bool UpdateCheckFinished { get; private set; }
        public static readonly bool UseJSONParser = true;
        public static UpdaterData JsonParsedUpdaterData { get; private set; }
        public static bool JsonParsedUpdaterDataReady { get; private set; } = false;

        public static void InitAndCheckForUpdates()
        {
            Init();
            CheckForUpdates();
            Task.Run(() => {
                while (true)
                {
                    Thread.Sleep(100);
                    if(JsonParsedUpdaterDataReady)
                    {
                        if(Program.FirstRun)
                        {
                            while(!MainWindow.FirstDonationMessageBoxClosed)
                            {
                                Thread.Sleep(100);
                            }
                            break;
                        }
                        break;
                    }
                }
                foreach (var msgbox in JsonParsedUpdaterData.MessageBoxes)
                {
                    if(msgbox == MessageBoxList.ManuallyDownloadLeagueFPSBoost)
                    {
                        if(DialogResult.Yes == MessageBoxList.ManuallyDownloadLeagueFPSBoost.ShowMessageBox())
                        {
                            ManuallyDownloadLeagueFPSBoostMessageBox.OpenLatestReleaseURL();
                            continue;
                        }
                    }
                    else if(msgbox == MessageBoxList.FailedUpdateSorry)
                    {
                        MessageBoxList.FailedUpdateSorry.ShowMessageBox();
                    }
                    else if (msgbox == MessageBoxList.GameBarAndFullScrOptim)
                    {
                        MessageBoxList.GameBarAndFullScrOptim.ShowMessageBox();
                    }
                    else if(!msgbox.GetRequiresSpecialCall())
                    {
                        msgbox.ShowMessageBox();
                    }
                }

                foreach(var action in JsonParsedUpdaterData.PostUpdate)
                {
                    if (action == ActionList.RestartPostUpdate_StabilityReason) ActionList.RestartPostUpdate_StabilityReason.Run();
                    if (action == ActionList.RestartPostUpdate_NoReason) ActionList.RestartPostUpdate_NoReason.Run();
                }
            });
        }

        public static void Init()
        {
            UpdateCheckTimer = new System.Timers.Timer
            {
                Interval = 5 * 60 * 1000
            };
            UpdateCheckTimer.Elapsed += UpdateCheckTimer_Elapsed;
            
        }

        public static void CheckForUpdates()
        {
            //AutoUpdater.ReportErrors = Program.DebugBuild;
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.LetUserSelectRemindLater = true;
            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
            AutoUpdater.RemindLaterAt = 1;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            //AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            if (UseJSONParser)
                AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;
            else
                AutoUpdater.ParseUpdateInfoEvent -= AutoUpdaterOnParseUpdateInfoEvent;

            Start();

            logger.Debug("Checking for updates...");
            UpdateCheckFinished = true;
            UpdateCheckTimer.Start();
        }

        public static void Start()
        {
            AutoUpdater.Start(UseJSONParser ? Strings.Updater_JSON_URL : Strings.Updater_XML_URL);
        }

        static void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            logger.Debug("RemoteData(JSON/XML): " + Environment.NewLine + args.RemoteData);
            JsonParsedUpdaterData = JsonConvert.DeserializeObject<UpdaterData>(args.RemoteData);
            JsonParsedUpdaterDataReady = true;
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = JsonParsedUpdaterData.Version,
                ChangelogURL = JsonParsedUpdaterData.ChangelogURL,
                Mandatory = JsonParsedUpdaterData.Mandatory,
#if !DEBUG
                DownloadURL = JsonParsedUpdaterData.DownloadURL,
#else
                DownloadURL = Strings.Updater_Download_URL_ZIP_LOCAL,
#endif
                Checksum = JsonParsedUpdaterData.Checksum.Value,
                HashingAlgorithm = JsonParsedUpdaterData.Checksum.Type.ToString(),
                InstallerArgs = JsonParsedUpdaterData.CommandLineArguments,
                UpdateMode = JsonParsedUpdaterData.UpdateMode
            };
            
            AutoUpdater.Mandatory = args.UpdateInfo.Mandatory;
            AutoUpdater.UpdateMode = args.UpdateInfo.UpdateMode;
        }
        static void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            UpdateCheckFinished = false;
            logger.Debug("Checking for updates...");
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    StopUpdateCheckTimer();
                    logger.Info($@"There is new version { args.CurrentVersion } available. Current installed version is { args.InstalledVersion }.");
                    if (DialogResult.Yes == MessageBox.Show(
                        $@"There is new version {args.CurrentVersion} available. You are using version {
                                args.InstalledVersion
                            }. Do you want to update the application now?", @"Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information))
                    {
                        logger.Info("User agreed to update now.");
                        if (!Application.MessageLoop)
                        {
                            logger.Debug("Message loop doesn't exist on this thread. Enabling visual styles.");
                            Application.EnableVisualStyles();
                        }
                        if (Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA))
                        {
                            logger.Debug("Showing update form.");
                            AutoUpdater.ShowUpdateForm();
                        }
                        else
                        {
                            logger.Debug("Current thread's apartment state is not STA. Creating new thread with apartment state STA.");
                            Thread thread = new Thread(AutoUpdater.ShowUpdateForm);
                            thread.CurrentCulture = thread.CurrentUICulture = CultureInfo.CurrentCulture;
                            thread.SetApartmentState(ApartmentState.STA);
                            thread.Start();
                            thread.Join();
                        }
                    }
                    else
                    {
                        logger.Info("User declined to update now.");
                    }
                }
                else
                {
                    logger.Info("There is no update available please try again later.");
                }
            }
            else
            {
                logger.Warn("There is a problem reaching update server please check your internet connection and try again later.");
            }
            AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;
            UpdateCheckFinished = true;
            StartUpdateCheckTimer();
        }

        public static void StopUpdateCheckTimer()
        {
            logger.Debug("Stopping timer for update checking.");
            UpdateCheckTimer.Stop();
        }

        public static void StartUpdateCheckTimer()
        {
            logger.Debug("Starting timer for update checking.");
            UpdateCheckTimer.Start();
        }

        static void UpdateCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateCheckFinished = false;
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            Start();
        }

        static void AutoUpdater_ApplicationExitEvent()
        {
            logger.Info("Update pending. Closing application.");
            Application.Exit();
        }

        public static void ShowMessageBoxes()
        {

        }
    }
}

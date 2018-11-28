using AutoUpdaterDotNET;
using IniParser;
using IniParser.Model;
using LeagueFPSBoost.Diagnostics.Debugger;
using LeagueFPSBoost.Logging;
using LeagueFPSBoost.Native;
using LeagueFPSBoost.Properties;
using LeagueFPSBoost.Text;
using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Forms;
using NLog;
using PowerManagerAPI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace LeagueFPSBoost.GUI
{
    public partial class MainWindow : MetroForm
    {
        public static bool notification;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static FileIniDataParser configParser = new FileIniDataParser();
        public static IniData GameConfigData { get; private set; }

        public static bool Loaded;

        public static System.Timers.Timer UpdateCheckTimer { get; private set; }
        public static System.Timers.Timer BoostCheckTimer { get; private set; }

        public static Guid currentLastActivePowerPlan;

        static int boostCheckEventCount;

        static string aboutTXT = "";
        static string aboutTXTDebug = "";
        public static bool saving;
        public static bool UpdateCheckFinished { get; private set; }
        public MainWindow()
        {
            logger.Trace("Initializing main window.");
            InitializeComponent();
            aboutTXT = metroLabel9.Text;
            metroTabControl1.SelectedIndex = 0;
            metroLink2.Text = metroLink2.Text + Program.CurrentVersionFull;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            //Debug text update
            if(Program.DebugBuild)
            {
                Text = "LeagueFPSBoost β";
                metroLabel9.Text += Environment.NewLine + "THIS IS BETA BUILD!";
                aboutTXTDebug = metroLabel9.Text;
            }
            Program.DebuggerWatcher.DebuggerChanged += DebuggerChangedGUI;
            Program.DebuggerWatcher.DebuggerChecked += DebuggerChangedGUI;
            Program.DebuggerWatcher.CheckNow();

            //Theme
            metroStyleManager1.Theme = Settings.Default.ThemeStyle;
            metroStyleManager1.Style = Settings.Default.ColorStyle;
            if (metroStyleManager1.Theme == MetroThemeStyle.Dark)
            {
                darkThemeToggle.Checked = true;
            }
            else
            {
                darkThemeToggle.Checked = false;
            }
            logger.Debug("Loaded style settings.");
            LeagueLogger.Okay("Loaded style settings.");

            //High PPP
            try
            {
                logger.Debug("Reading current active power plan.");
                currentLastActivePowerPlan = PowerManager.GetActivePlan();
                logger.Debug($"Current active power plan: {currentLastActivePowerPlan} - {PowerManager.GetPlanName(currentLastActivePowerPlan)}");
                highPerformanceToggle.CheckedChanged -= HighPerformanceToggle1_CheckedChanged;
                highPerformanceToggle.Checked = currentLastActivePowerPlan == NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID;
                highPerformanceToggle.CheckedChanged += HighPerformanceToggle1_CheckedChanged;
                logger.Debug("Loaded current active power plan.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while reading active power plan: " + Environment.NewLine);
            }

            if (currentLastActivePowerPlan != Settings.Default.LastActivePowerPlan)
            {
                logger.Warn("Current active power plan and last active power plan don't match. Maybe program has closed incorrectly or plan was changed outside of program.");
                try
                {
                    logger.Info("Trying to change power plan.");
                    logger.Info($"Changing current power plan to last saved active power plan: {Settings.Default.LastActivePowerPlan} - {PowerManager.GetPlanName(Settings.Default.LastActivePowerPlan)}");
                    PowerManager.SetActivePlan(Settings.Default.LastActivePowerPlan);
                    currentLastActivePowerPlan = PowerManager.GetActivePlan();
                    logger.Debug($"Power plan set to: {currentLastActivePowerPlan} - {PowerManager.GetPlanName(currentLastActivePowerPlan)}");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, Strings.exceptionThrown + " while trying to change power plan to last active one: " + Environment.NewLine);
                }
            }

            SaveLastActivePowerPlan(currentLastActivePowerPlan);

            

            //Notification
            notification = Properties.Settings.Default.Notifications;
            notificationsToggle.Checked = notification;
            Program.PlayNotiAllow = notification;
            logger.Debug("Loaded notification settings.");
            LeagueLogger.Okay("Loaded notification settings.");
            

            //Ini-Praser
            ReadGameConfigData();

            //Watcher
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
            

            Loaded = true;
            logger.Debug("Main window has been loaded.");
            LeagueLogger.Okay("Main window loaded.");
            Program.MainWindowLoaded = true;
            if (Program.FirstRun.Value) new Thread(() => { Thread.Sleep(2000); MessageBox.Show("If you like the program a small donation would be helpful! Check More Information window in about tab for donate button.", "LeagueFPSBoost: Support Developer", MessageBoxButtons.OK, MessageBoxIcon.Information); }).Start();
            UpdateCheckTimer = new System.Timers.Timer
            {
                Interval = 5 * 60 * 1000
            };
            UpdateCheckTimer.Elapsed += UpdateCheckTimer_Elapsed;

            BoostCheckTimer = new System.Timers.Timer
            {
                Interval = 1 * 60 * 1000
            };
            BoostCheckTimer.Elapsed += BoostCheckTimer_Elapsed;

            BoostCheckTimer.Start();

            CheckForUpdates();
            this.BringToFront();
        }

        private void SaveLastActivePowerPlan(Guid currentLastActivePP)
        {
            try
            {
                logger.Debug($"Saving current active power plan to settings: " + currentLastActivePP);
                Properties.Settings.Default.LastActivePowerPlan = currentLastActivePP;
                Properties.Settings.Default.Save();
                logger.Info("Last active power plan has been saved.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while saving current active power plan in settings: " + Environment.NewLine);
                MessageBox.Show("There was an error while opening while saving current active power plan in settings. Check logs for more details.", "LeagueFPSBoost: Power Manager Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BoostCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(Process.GetProcessesByName(Strings.GameProcessName).Length != 0)
            {
                logger.Debug($"Timer is trying to boost game. Interval: {BoostCheckTimer.Interval}ms");
                ProcessManagement.LeaguePriority.CheckAndBoost(Program.NoClient);
            }
            else if(boostCheckEventCount >= 5)
            {
                logger.Debug($"Timer is trying to return client to normal priority. Interval: {BoostCheckTimer.Interval}ms");
                ProcessManagement.LeaguePriority.CheckAndBoost(Program.NoClient);
                boostCheckEventCount = 0;
            }

            boostCheckEventCount++;
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

        private void UpdateCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateCheckFinished = false;
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.Start(Strings.Updater_XML_URL);
        }

        private void CheckForUpdates()
        {
            //fix for #3
            //ServicePointManager.SecurityProtocol = (ServicePointManager.SecurityProtocol & SecurityProtocolType.Ssl3) | (SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


            //AutoUpdater.ReportErrors = Program.DebugBuild;
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.LetUserSelectRemindLater = true;
            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
            AutoUpdater.RemindLaterAt = 1;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            var xmlUrl = Strings.Updater_XML_URL; //Settings.Default.UpdaterXML_URL;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            logger.Debug("Checking for updates...");
            AutoUpdater.Start(xmlUrl);
            UpdateCheckFinished = true;
            UpdateCheckTimer.Start();
        }
        
        private void AutoUpdater_ApplicationExitEvent()
        {
            logger.Info("Update pending. Closing application.");
            Application.Exit();
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            UpdateCheckFinished = false;
            logger.Debug("Checking for updates...");
            if (args != null)
            {
                if(args.IsUpdateAvailable)
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
        
        private void DebuggerChangedGUI(object sender, DebugEventArgs e)
        {
            if(e.Attached)
            {
                if(Program.DebugBuild)
                {
                    Text = "LeagueFPSBoost Dβ";
                    metroLabel9.Text = aboutTXTDebug + Environment.NewLine + "Debugger is attached!";
                }
                else
                {
                    Text = "LeagueFPSBoost D";
                    metroLabel9.Text = aboutTXT + Environment.NewLine + Environment.NewLine + "Debugger is attached!";
                }
            }
            else
            {
                if(Program.DebugBuild)
                {
                    Text = "LeagueFPSBoost β";
                    metroLabel9.Text = aboutTXTDebug;
                }
                else
                {
                    Text = "LeagueFPSBoost";
                    metroLabel9.Text = aboutTXT;
                }
            }
            Refresh();
        }

        private void OpenFolder(string path)
        {
            if(Directory.Exists(path))
            {
                var startInfo = new ProcessStartInfo
                {
                    Arguments = path,
                    FileName = "explorer.exe",
                    Verb = "runas"
                };
                try
                {
                    Process.Start(startInfo);
                    logger.Info("Folder opened: " + path);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, Strings.exceptionThrown + " while opening folder: " + path);
                }
            }
            else
            {
                logger.Warn("Can't open folder because it doesn't exist: " + path);
            }
        }

        private void DarkThemeToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (darkThemeToggle.Checked)
            {
                UpdateAndSaveThemeStyle(metroStyleManager1, MetroThemeStyle.Dark);
                if (Loaded) LeagueLogger.Okay("Theme style set to dark.");
            }
            else
            {
                UpdateAndSaveThemeStyle(metroStyleManager1, MetroThemeStyle.Light);
                if (Loaded) LeagueLogger.Okay("Theme style set to light.");
            }
            Refresh();
        }

        private void BlackColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Black);
            LeagueLogger.Okay("Color style set to black.");
        }

        private void UpdateAndSaveColorStyle(MetroStyleManager stylemanager, MetroColorStyle stylecolor)
        {
            logger.Debug("Changing color style.");
            stylemanager.Style = stylecolor;
            logger.Info(Strings.ColorStyleSetTo + stylecolor + ".");
            Properties.Settings.Default.ColorStyle = stylemanager.Style;
            try
            {
                logger.Debug("Saving color style settings.");
                Properties.Settings.Default.Save();
                logger.Info("Color style settings have been saved.");
                LeagueLogger.Okay("Saving color style succeeded.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while saving color style in settings: " + Environment.NewLine);
                LeagueLogger.Error("Error while saving color style: " + ex.Message);
            }
        }

        private void UpdateAndSaveThemeStyle(MetroStyleManager stylemanager, MetroThemeStyle styletheme)
        {
            logger.Debug("Changing theme style.");
            stylemanager.Theme = styletheme;
            logger.Info("Theme style has been changed to: " + styletheme);
            Properties.Settings.Default.ThemeStyle = stylemanager.Theme;
            try
            {
                logger.Debug("Saving theme style settings.");
                Properties.Settings.Default.Save();
                logger.Info("Theme style settings have been saved.");
                LeagueLogger.Okay("Saving theme style succeeded.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while saving theme style settings: " + Environment.NewLine);
                LeagueLogger.Error("Error while saving theme style: " + ex.Message);
            }
        }

        private void WhiteColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.White);
            LeagueLogger.Okay("Color style set to white.");
        }

        private void DeepSkyBlueColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Blue);
            LeagueLogger.Okay("Color style set to blue.");
        }

        private void TealColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Teal);
            LeagueLogger.Okay("Color style set to teal.");
        }

        private void SilverColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Silver);
            LeagueLogger.Okay("Color style set to silver.");
        }

        private void BrownColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Brown);
            LeagueLogger.Okay("Color style set to brown.");
        }

        private void OrangeColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Orange);
            LeagueLogger.Okay("Color style set to orange.");
        }

        private void RedColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Red);
            LeagueLogger.Okay("Color style set to red.");
        }

        private void YellowColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Yellow);
            LeagueLogger.Okay("Color style set to yellow.");
        }

        private void MagentaColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Magenta);
            LeagueLogger.Okay("Color style set to magenta.");
        }

        private void PinkColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Pink);
            LeagueLogger.Okay("Color style set to pink.");
        }

        private void GreenColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Green);
            LeagueLogger.Okay("Color style set to green.");
        }

        private void LimeGreenColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Lime);
            LeagueLogger.Okay("Color style set to lime.");
        }

        private void PurpleColorButton_Click(object sender, EventArgs e)
        {
            UpdateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Purple);
            LeagueLogger.Okay("Color style set to purple.");
        }

        private void NotificationsToggle_CheckedChanged(object sender, EventArgs e)
        {
            notification = notificationsToggle.Checked;
            Program.PlayNotiAllow = notification;
            Properties.Settings.Default.Notifications = notification;
            try
            {
                logger.Debug("Saving notification settings.");
                Properties.Settings.Default.Save();
                if (notification)
                {
                    if (Loaded)
                    {
                        logger.Info("Notifications are turned on.");
                        LeagueLogger.Okay("Notifications are turned on.");
                    }
                }
                else
                {
                    if (Loaded)
                    {
                        logger.Info("Notifications are turned off.");
                        LeagueLogger.Okay("Notifications are turned off.");
                    }
                }
                logger.Debug("Saving notification settings succeeded.");
                LeagueLogger.Okay("Saving notification settings succeeded.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while saving notification settings: " + Environment.NewLine);
                LeagueLogger.Error("Error while saving notification settings: " + ex.Message);
            }
        }

        private void BackupConfigButton_Click(object sender, EventArgs e)
        {
            saving = true;
            using (var sd = new SaveFileDialog())
            {
                sd.Title = @"Backup Config File";
                sd.Filter = @"Config File|*.cfg";
                sd.FileName = @"gameBACKUP" + DateTime.Now.ToString(Strings.logDateTimeFormat);
                sd.InitialDirectory = Path.Combine(Program.LeagueConfigDirPath);
                sd.OverwritePrompt = true;
                sd.RestoreDirectory = true;

                if (sd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        logger.Debug("Saving game's configuration backup.");
                        File.Copy(Path.Combine(Program.LeagueConfigDirPath, "game.cfg"), sd.FileName, false);
                        LeagueLogger.Okay("Config backup saved at " + sd.FileName.Replace(@"\", @"/"));
                        logger.Debug("Game's configuration backup saved at: " + sd.FileName);
                        try
                        {
                            logger.Debug("Saving new settings to game's configuration file.");
                            configParser.WriteFile(Path.Combine(Program.LeagueConfigDirPath, "game.cfg"), GameConfigData);
                            GameConfigData = configParser.ReadFile(Path.Combine(Program.LeagueConfigDirPath, "game.cfg"));
                            var sb = new StringBuilder();
                            sb.AppendLine(Strings.tabWithLine + "Saved new game's configuration:");
                            sb.AppendLine(Strings.doubleTabWithLine + "File name: " + Path.Combine(Program.LeagueConfigDirPath, "game.cfg"));
                            sb.AppendLine(Strings.tripleTabWithLine + "CharacterInking=" + GameConfigData["Performance"]["CharacterInking"]);
                            sb.AppendLine(Strings.tripleTabWithLine + "EnableHUDAnimations=" + GameConfigData["Performance"]["EnableHUDAnimations"]);
                            sb.AppendLine(Strings.tripleTabWithLine + "ShadowsEnabled=" + GameConfigData["Performance"]["ShadowsEnabled"]);
                            sb.Append(Strings.tripleTabWithLine + "EnableGrassSwaying=" + GameConfigData["Performance"]["EnableGrassSwaying"]);
                            logger.Debug("Finished saving new game's configuration: " + Environment.NewLine + sb);
                            MessageBox.Show("Successfully saved new game's configuration." + Environment.NewLine + "Check log for more information.", "LeagueFPSBoost: Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, Strings.exceptionThrown + " while saving new game's configuration: " + Environment.NewLine);
                            MessageBox.Show("An error occurred while saving new config." + Environment.NewLine + "Check log for more details.");
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving game's configuration backup: " + sd.FileName);
                        MessageBox.Show("An error occurred while saving config backup." + Environment.NewLine + "Check log for more details.");
                        LeagueLogger.Error("Error while saving config backup: " + ex.Message);
                    }
                }
            }
            saving = false;
        }

        private void BoardsLink_Click(object sender, EventArgs e)
        {
            logger.Debug("Opening More Information Window.");
            using (var moreInfoForm = new InformationWindow(metroStyleManager1))
            {
                Hide();
                moreInfoForm.ShowDialog();
                metroTabControl1.SelectedIndex = 0;
                Show();
            }
        }

        private void MetroLink2_Click(object sender, EventArgs e)
        {
            logger.Debug("Opening op.gg link: " + @"https://goo.gl/sEYLbe");
            try
            {
                Process.Start(@"https://goo.gl/sEYLbe");
                logger.Debug("Successfully opened op.gg link.");
                LeagueLogger.Okay(@"Opened op.gg link: https://goo.gl/sEYLbe");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while opening op.gg link " + @"https://goo.gl/sEYLbe :" + Environment.NewLine);
                LeagueLogger.Error("Error while opening op.gg link: " + ex.Message);
            }
        }

        private void SetGameConfigData(string a, string b, string val)
        {
            var method = $"{nameof(GameConfigData)}[{a}][{b}] = {val}.";
            logger.Debug($"Trying: {method}");
            try
            {
                GameConfigData[a][b] = val;
                logger.Debug($"Success: {method}");
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"{Strings.exceptionThrown} while trying: {method} " + Environment.NewLine);
                MessageBox.Show($"Failed changing {b} under {a} to {val}. {Environment.NewLine} Check log for more details.", "LeagueFPSBosot: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CharacterInkingToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (characterInkingToggle.Checked)
            {
                //GameConfigData["Performance"]["CharacterInking"] = "1";
                SetGameConfigData("Performance", "CharacterInking", "1");
            }
            else
            {
                //GameConfigData["Performance"]["CharacterInking"] = "0";
                SetGameConfigData("Performance", "CharacterInking", "0");
            }
        }

        private void HudAnimationsToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (hudAnimationsToggle.Checked)
            {
                //GameConfigData["Performance"]["EnableHUDAnimations"] = "1";
                SetGameConfigData("Performance", "EnableHUDAnimations", "1");
            }
            else
            {
                //GameConfigData["Performance"]["EnableHUDAnimations"] = "0";
                SetGameConfigData("Performance", "EnableHUDAnimations", "0");
            }
        }

        private void ShadowsToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (shadowsToggle.Checked)
            {
                //GameConfigData["Performance"]["ShadowsEnabled"] = "1";
                SetGameConfigData("Performance", "ShadowsEnabled", "1");
            }
            else
            {
                //GameConfigData["Performance"]["ShadowsEnabled"] = "0";
                SetGameConfigData("Performance", "ShadowsEnabled", "0");
            }
        }

        private void GrassSwayingToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (grassSwayingToggle.Checked)
            {
                //GameConfigData["Performance"]["EnableGrassSwaying"] = "1";
                SetGameConfigData("Performance", "EnableGrassSwaying", "1");
            }
            else
            {
                //GameConfigData["Performance"]["EnableGrassSwaying"] = "0";
                SetGameConfigData("Performance", "EnableGrassSwaying", "0");
            }
        }       

        private void ReadGameConfigData()
        {
            try
            {
                if (!saving)
                {
                    if (!File.Exists(Path.Combine(Program.LeagueConfigDirPath, "game.cfg")))
                    {
                        logger.Error("Game's configuration file doesn't exist.");
                        throw new FileNotFoundException("Could not find file '" + Path.Combine(Program.LeagueConfigDirPath, "game.cfg") + "'.", Path.Combine(Program.LeagueConfigDirPath, "game.cfg"));
                    }
                    GameConfigData = configParser.ReadFile(Path.Combine(Program.LeagueConfigDirPath, "game.cfg"));

                    characterInkingToggle.Checked = Convert.ToBoolean(int.Parse(GameConfigData["Performance"]["CharacterInking"]));
                    hudAnimationsToggle.Checked = Convert.ToBoolean(int.Parse(GameConfigData["Performance"]["EnableHUDAnimations"]));
                    shadowsToggle.Checked = Convert.ToBoolean(int.Parse(GameConfigData["Performance"]["ShadowsEnabled"]));
                    grassSwayingToggle.Checked = Convert.ToBoolean(int.Parse(GameConfigData["Performance"]["EnableGrassSwaying"]));
                    logger.Debug("Successfully read game's configuration from file: " + Path.Combine(Program.LeagueConfigDirPath, "game.cfg"));
                }
                else
                {
                    logger.Debug("Skipping reading game's configuration file because its currently getting saved.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while reading game's configuration file: " + Environment.NewLine);
            }
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            ReadGameConfigData();
            highPerformanceToggle.CheckedChanged -= HighPerformanceToggle1_CheckedChanged;
            if(currentLastActivePowerPlan != NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID) currentLastActivePowerPlan = PowerManager.GetActivePlan();
            highPerformanceToggle.Checked = PowerManager.GetActivePlan() == NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID;
            highPerformanceToggle.CheckedChanged += HighPerformanceToggle1_CheckedChanged;
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            OpenFolder(Program.LeagueLogFileDirPath);
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            OpenFolder(Program.LeagueConfigDirPath);
        }

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            OpenFolder(Program.AppConfigDir);
        }

        private void MetroButton4_Click(object sender, EventArgs e)
        {
            BoardsLink_Click(sender, e);
        }

        private void HighPerformanceToggle1_CheckedChanged(object sender, EventArgs e)
        {
            if(!Settings.Default.HighPPPAgreement && currentLastActivePowerPlan != NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID)
            {
                logger.Info("User clicked on High PPP without agreeing.");
                if( DialogResult.OK != MessageBox.Show("By clicking OK you are agreeing that you are aware of possibility of reducing battery life on laptops and increased temperatures" +
                    " while using high performance power plan and that developer is not responsible for any damage.", "LeagueFPSBoost: WARNING", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning))
                {
                    logger.Info("User declined agreement.");
                    try
                    {
                        logger.Debug("Saving agreement status as false in settings.");
                        Settings.Default.HighPPPAgreement = false;
                        Settings.Default.Save();
                        logger.Debug("Saving done.");
                    }
                    catch(Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving agreement status: " + Environment.NewLine);
                    }
                    highPerformanceToggle.CheckedChanged -= HighPerformanceToggle1_CheckedChanged;
                    highPerformanceToggle.Checked = false;
                    highPerformanceToggle.CheckedChanged += HighPerformanceToggle1_CheckedChanged;
                    logger.Info("Aborting High PPP.");
                    return;
                }
                else
                {
                    logger.Info("User accepted agreement.");
                    try
                    {
                        logger.Debug("Saving agreement status as true in settings.");
                        Settings.Default.HighPPPAgreement = true;
                        Settings.Default.Save();
                        logger.Debug("Saving done.");
                    }
                    catch(Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving agreement status: " + Environment.NewLine);
                    }
                }
            }
            
            if (currentLastActivePowerPlan == NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID)
            {
                logger.Info("Old power plan was also high performance.");
                MessageBox.Show("Old power plan was also high performance. Try double clicking on 'High Performance PP' and selecting your default plan and then click on 'High Performance PP' and reset last active power plan.", "LeagueFPSBoost: PowerManager Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (highPerformanceToggle.Checked)
            {
                logger.Debug("High PPP has been checked.");
                try
                {
                    logger.Info($"Changing power plan to: {NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID} - {PowerManager.GetPlanName(NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID)}");
                    PowerManager.SetActivePlan(NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID);
                    logger.Info("Power plan change finished without exceptions.");
                }
                catch(Exception ex)
                {
                    logger.Error(ex, Strings.exceptionThrown + $" while changing power plan to high performance: {NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID}{Environment.NewLine}");
                    MessageBox.Show("There was an error while changing power plan to high performance. Check log for more details.", "LeagueFPSBoost: PowerManager Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                logger.Debug("High PPP has been unchecked.");
                try
                {
                    logger.Info($"Changing power plan to last active one: {currentLastActivePowerPlan} - {PowerManager.GetPlanName(currentLastActivePowerPlan)}");
                    PowerManager.SetActivePlan(currentLastActivePowerPlan);
                    logger.Info("Power plan change finished without exceptions.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, Strings.exceptionThrown + $" while changing power plan to last active power plan: {currentLastActivePowerPlan}");
                    MessageBox.Show("There was an error while changing power plan to last active power plan. Check log for more details.", "LeagueFPSBoost: PowerManager Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            highPerformanceToggle.CheckedChanged -= HighPerformanceToggle1_CheckedChanged;
            highPerformanceToggle.Checked = PowerManager.GetActivePlan() == NativeGUIDs.HIGH_PERFORMANCE_POWER_PLAN_GUID;
            highPerformanceToggle.CheckedChanged += HighPerformanceToggle1_CheckedChanged;
        }

        private void MetroLabel2_DoubleClick(object sender, EventArgs e)
        {
            logger.Info("High Performance PP label has been double clicked. Trying to open Power Options in Control Panel: " + Strings.POWER_OPTIONS_CPL);
            try
            {
                Process.Start(Strings.POWER_OPTIONS_CPL);
                logger.Debug("Successfully opened: " + Strings.POWER_OPTIONS_CPL);
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while trying to start: " + Strings.POWER_OPTIONS_CPL);
                MessageBox.Show("There was an error while opening Power Options in Control Panel. Check logs for more details.", "LeagueFPSBoost: Power Manager Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MetroLabel2_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show("Are you sure that you want to reset last active power plan?", "LeagueFPSBoost: PowerManager", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                SaveLastActivePowerPlan(Guid.Empty);
                currentLastActivePowerPlan = PowerManager.GetActivePlan();
            }            
        }
    }
}

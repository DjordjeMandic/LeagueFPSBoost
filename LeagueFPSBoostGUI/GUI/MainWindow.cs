using AutoUpdaterDotNET;
using IniParser;
using IniParser.Model;
using LeagueFPSBoost.Diagnostics.Debugger;
using LeagueFPSBoost.Logging;
using LeagueFPSBoost.Properties;
using LeagueFPSBoost.Text;
using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Forms;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        private static string aboutTXT = "";
        private static string aboutTXTDebug = "";
        public static bool saving;
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

            //Notification
            notification = Properties.Settings.Default.Notifications;
            notificationsToggle.Checked = notification;
            Program.PlayNotiAllow = notification;
            logger.Debug("Loaded notification settings.");
            LeagueLogger.Okay("Loaded notification settings.");
            

            //Ini-Praser
            ReadGameConfigData();

            //Watcher
            Program.StartWatch.Start();
            Program.StopWatch.Start();
            logger.Debug("Process watcher has been started.");
            LeagueLogger.Okay("Process watcher started.");

            Loaded = true;
            logger.Debug("Main window has been loaded.");
            LeagueLogger.Okay("Main window loaded.");
            Program.MainWindowLoaded = true;
            CheckForUpdates();
        }

        private void CheckForUpdates()
        {
            //AutoUpdater.ReportErrors = Program.DebugBuild;
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.LetUserSelectRemindLater = true;
            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
            AutoUpdater.RemindLaterAt = 1;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            logger.Debug("Checking for updates...");
            var xmlUrl = Strings.Updater_XML_URL; //Settings.Default.UpdaterXML_URL;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.Start(xmlUrl);
        }
        
        private void AutoUpdater_ApplicationExitEvent()
        {
            logger.Info("Update pending. Closing application.");
            Application.Exit();
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

        private void darkThemeToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (darkThemeToggle.Checked)
            {
                updateAndSaveThemeStyle(metroStyleManager1, MetroThemeStyle.Dark);
                if (Loaded) LeagueLogger.Okay("Theme style set to dark.");
            }
            else
            {
                updateAndSaveThemeStyle(metroStyleManager1, MetroThemeStyle.Light);
                if (Loaded) LeagueLogger.Okay("Theme style set to light.");
            }
            Refresh();
        }

        private void blackColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Black);
            LeagueLogger.Okay("Color style set to black.");
        }

        private void updateAndSaveColorStyle(MetroStyleManager stylemanager, MetroColorStyle stylecolor)
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

        private void updateAndSaveThemeStyle(MetroStyleManager stylemanager, MetroThemeStyle styletheme)
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

        private void whiteColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.White);
            LeagueLogger.Okay("Color style set to white.");
        }

        private void deepSkyBlueColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Blue);
            LeagueLogger.Okay("Color style set to blue.");
        }

        private void tealColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Teal);
            LeagueLogger.Okay("Color style set to teal.");
        }

        private void silverColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Silver);
            LeagueLogger.Okay("Color style set to silver.");
        }

        private void brownColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Brown);
            LeagueLogger.Okay("Color style set to brown.");
        }

        private void orangeColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Orange);
            LeagueLogger.Okay("Color style set to orange.");
        }

        private void redColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Red);
            LeagueLogger.Okay("Color style set to red.");
        }

        private void yellowColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Yellow);
            LeagueLogger.Okay("Color style set to yellow.");
        }

        private void magentaColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Magenta);
            LeagueLogger.Okay("Color style set to magenta.");
        }

        private void pinkColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Pink);
            LeagueLogger.Okay("Color style set to pink.");
        }

        private void greenColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Green);
            LeagueLogger.Okay("Color style set to green.");
        }

        private void limeGreenColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Lime);
            LeagueLogger.Okay("Color style set to lime.");
        }

        private void purpleColorButton_Click(object sender, EventArgs e)
        {
            updateAndSaveColorStyle(metroStyleManager1, MetroColorStyle.Purple);
            LeagueLogger.Okay("Color style set to purple.");
        }

        private void notificationsToggle_CheckedChanged(object sender, EventArgs e)
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

        private void backupConfigButton_Click(object sender, EventArgs e)
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
                            sb.AppendLine(Strings.tripleTabWithLine + "EnableGrassSwaying=" + GameConfigData["Performance"]["EnableGrassSwaying"]);
                            sb.Append(Strings.tripleTabWithLine + "PerPixelPointLighting=" + GameConfigData["Performance"]["PerPixelPointLighting"]);
                            logger.Debug("Finished saving new game's configuration: " + sb);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, Strings.exceptionThrown + " while saving new game's configuration: " + Environment.NewLine);
                            MessageBox.Show("An error occurred while saving new confing." + Environment.NewLine + "Check log for more details.");
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving game's configuration backup: " + sd.FileName);
                        MessageBox.Show("An error occurred while saving confing backup." + Environment.NewLine + "Check log for more details.");
                        LeagueLogger.Error("Error while saving config backup: " + ex.Message);
                    }
                }
            }
            saving = false;
        }

        private void boardsLink_Click(object sender, EventArgs e)
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

        private void metroLink2_Click(object sender, EventArgs e)
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

        private void CharacterInkingToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (characterInkingToggle.Checked)
            {
                GameConfigData["Performance"]["CharacterInking"] = "1";
            }
            else
            {
                GameConfigData["Performance"]["CharacterInking"] = "0";
            }
        }

        private void HudAnimationsToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (hudAnimationsToggle.Checked)
            {
                GameConfigData["Performance"]["EnableHUDAnimations"] = "1";
            }
            else
            {
                GameConfigData["Performance"]["EnableHUDAnimations"] = "0";
            }
        }

        private void ShadowsToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (shadowsToggle.Checked)
            {
                GameConfigData["Performance"]["ShadowsEnabled"] = "1";
            }
            else
            {
                GameConfigData["Performance"]["ShadowsEnabled"] = "0";
            }
        }

        private void GrassSwayingToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (grassSwayingToggle.Checked)
            {
                GameConfigData["Performance"]["EnableGrassSwaying"] = "1";
            }
            else
            {
                GameConfigData["Performance"]["EnableGrassSwaying"] = "0";
            }
        }

        private void PerPixelPointLightingToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (perPixelPointLightingToggle.Checked)
            {
                GameConfigData["Performance"]["PerPixelPointLighting"] = "1";
            }
            else
            {
                GameConfigData["Performance"]["PerPixelPointLighting"] = "0";
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
                    perPixelPointLightingToggle.Checked = Convert.ToBoolean(int.Parse(GameConfigData["Performance"]["PerPixelPointLighting"]));
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
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            OpenFolder(Program.LeagueLogFileDirPath);
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            OpenFolder(Program.LeagueConfigDirPath);
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            OpenFolder(Program.AppConfigDir);
        }

        private void metroButton4_Click(object sender, EventArgs e)
        {
            boardsLink_Click(sender, e);
        }
    }
}

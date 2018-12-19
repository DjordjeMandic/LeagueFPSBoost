using LeagueFPSBoost.Properties;
using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LeagueFPSBoost.Configuration
{
    public class AppConfigEventArgs : EventArgs
    {
        public AppConfigEventArgs(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
    }

    public abstract class AppConfig : IDisposable
    {
        public static event EventHandler<AppConfigEventArgs> OnPathChange = delegate { };
        static readonly Logger appConfigLogger = LogManager.GetCurrentClassLogger();
        public static AppConfig Change(string path)
        {
            return new ChangeAppConfig(path);
        }

        static Process currProc = Process.GetCurrentProcess();
        static string exeName = currProc.MainModule.FileName;
        public static readonly ProcessStartInfo restartInfo = new ProcessStartInfo(exeName)
        {
            Verb = "runas",
            Arguments = Program.ArgumentsStr + " -" + Strings.RestartReasonArg.Split('|')[0] + "=" + Program.RestartReason.Configuration.ToString()
        };

        public static void CreateConfigIfNotExists()
        {
            var configFile = $"{Application.ExecutablePath}.config";
            if (!File.Exists(configFile))
            {
                File.WriteAllText(configFile, Resources.App_Config);
                appConfigLogger.Debug("Temporary configuration file doesn't exist. Writing new one: " + configFile);
            }
            var configDir = Path.Combine(Program.LeagueConfigDirPath, @"LeagueFPSBoost\");

            var appConfigPath = Path.Combine(configDir, Path.GetFileName(configFile));
            Program.AppConfigDir = configDir;
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
                appConfigLogger.Debug("Configuration directory doesn't exist. Created new one: " + configDir);
            }

            CorrectRoamingSettingsFileIfNeeded(Assembly.GetEntryAssembly().Location);
            
            if (Settings.Default.UpgradeRequired)
            {
                Program.FirstRun.Value = true;
                appConfigLogger.Debug("Upgrading settings.");
                if (File.Exists(appConfigPath))
                {
                    File.Delete(appConfigPath);
                    appConfigLogger.Debug("Deleted old application settings file: " + appConfigPath);
                }

                appConfigLogger.Debug("Writing new application settings file: " + appConfigPath);
                File.WriteAllText(appConfigPath, Resources.App_Config);

                Change(appConfigPath);

                Settings.Default.Upgrade();
                Settings.Default.Reload();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
                appConfigLogger.Debug("Done upgrading settings.");
            }
            else
            {
                Program.FirstRun.Value = false;
                if (!File.Exists(appConfigPath))
                {
                    appConfigLogger.Debug("Application configuration file doesn't exist. Writing new one: " + appConfigPath);
                    File.WriteAllText(appConfigPath, Resources.App_Config);
                }
                Change(appConfigPath);
                Settings.Default.Reload();
            }

            if (File.Exists(configFile))
            {
                appConfigLogger.Debug("Deleting temporary configuration file: " + configFile);
                File.Delete(configFile);
            }
        }

        /// <summary>
        /// Corrects the roaming settings file if needed because sometimes the node "configSections" is missing in the settings file. 
        /// Correct this by taking this node out of the default config file.
        /// </summary>
        public static void CorrectRoamingSettingsFileIfNeeded(string configExePath)
        {
            appConfigLogger.Debug($"Incrementing {nameof(Settings.Default.UserConfigCorrectionHelperCount)} to {Settings.Default.UserConfigCorrectionHelperCount++}.");
            appConfigLogger.Debug("Saving settings to generate user.config file if it doesn't exist then correcting it.");
            Settings.Default.Save();
            appConfigLogger.Debug("Saving settings done.");

            appConfigLogger.Debug("Correcting user settings file if needed.");
            const string NODE_NAME_CONFIGURATION = "configuration";
            const string NODE_NAME_CONFIGSECTIONS = "configSections";
            const string NODE_NAME_USERSETTINGS = "userSettings";

            //Exit if no romaing config (file) to correct...
            var configRoaming = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            if (configRoaming == null || !configRoaming.HasFile)
            {
                appConfigLogger.Debug("There is no user configuration file to correct.");
                return;
            }
            appConfigLogger.Debug("User configuration file: " + configRoaming.FilePath);
            //Check for the <sectionGroup> with name="userSettings"
            //Note: Used ugly iteration because "configRoaming.GetSectionGroup(sectionGroupName)" throws ArgumentException.
            appConfigLogger.Debug("Checking for the <sectionGroup> with name=\"userSettings\"");
            ConfigurationSectionGroup sectionGroupUserSettings = null;
            foreach (ConfigurationSectionGroup sectionGroup in configRoaming.SectionGroups)
            {
                if (sectionGroup.Name.Equals(NODE_NAME_USERSETTINGS))
                {
                    sectionGroupUserSettings = sectionGroup;
                    break;
                }
            }

            //Exit if the needed section group is found...
            if (sectionGroupUserSettings != null && sectionGroupUserSettings.IsDeclared)
            {
                appConfigLogger.Debug("Needed section group is found.");
                return;
            }

            appConfigLogger.Debug("Needed section group is not found. Correcting user configuration file.");
            //Do correction actions...
            var xDoc = XDocument.Load(configRoaming.FilePath);
            var userSettingsNode = xDoc.Element(NODE_NAME_CONFIGURATION).Element(NODE_NAME_USERSETTINGS);

            //var configExePath = Assembly.GetEntryAssembly().Location;
            var configDefault = ConfigurationManager.OpenExeConfiguration(configExePath);
            var xDocDefault = XDocument.Load(configDefault.FilePath);
            var configSectionsNode = xDocDefault.Element(NODE_NAME_CONFIGURATION).Element(NODE_NAME_CONFIGSECTIONS);

            userSettingsNode.AddBeforeSelf(configSectionsNode);
            xDoc.Save(configRoaming.FilePath);
            appConfigLogger.Debug("User configuration file has been corrected.");

            appConfigLogger.Debug("Trying to read user configuration.");
            try
            {
                appConfigLogger.Debug($"Reading {nameof(Settings.Default.UserConfigCorrectionHelperCount)}: {Settings.Default.UserConfigCorrectionHelperCount}");
                appConfigLogger.Debug("Reading successful.");
            }
            catch (Exception ex)
            {
                appConfigLogger.Error(ex, Strings.exceptionThrown + " while reading: " + Environment.NewLine);
                appConfigLogger.Debug("Restarting: " + Environment.NewLine + Strings.tabWithLine + "File Name: " + restartInfo.FileName + Environment.NewLine + Strings.tabWithLine + "Arguments: " + restartInfo.Arguments);
                Process.Start(restartInfo);
                Environment.Exit(0);
            }            
        }

        public abstract void Dispose();

        class ChangeAppConfig : AppConfig
        {
            readonly string oldConfig =
                AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE").ToString();

            bool disposedValue;

            public ChangeAppConfig(string path)
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", path);
                ResetConfigMechanism();
                appConfigLogger.Debug("Application configuration changed to: " + path);
                OnPathChange?.Invoke(this, new AppConfigEventArgs(path));
            }

            public override void Dispose()
            {
                if (!disposedValue)
                {
                    AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", oldConfig);
                    ResetConfigMechanism();


                    disposedValue = true;
                }
                GC.SuppressFinalize(this);
            }

            static void ResetConfigMechanism()
            {
                typeof(ConfigurationManager)
                    .GetField("s_initState", BindingFlags.NonPublic |
                                             BindingFlags.Static)
                    .SetValue(null, 0);

                typeof(ConfigurationManager)
                    .GetField("s_configSystem", BindingFlags.NonPublic |
                                                BindingFlags.Static)
                    .SetValue(null, null);

                typeof(ConfigurationManager)
                    .Assembly.GetTypes()
                    .Where(x => x.FullName ==
                                "System.Configuration.ClientConfigPaths")
                    .First()
                    .GetField("s_current", BindingFlags.NonPublic |
                                           BindingFlags.Static)
                    .SetValue(null, null);
            }
        }
    }
}

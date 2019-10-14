using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueFPSBoost.Text;
using Microsoft.Win32;
using NLog;

namespace LeagueFPSBoost.RegistryManagment
{

    static class GameBar
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string GameDVRPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
        private static readonly string GameConfigStorePath = @"System\GameConfigStore";
        private static bool EnableLogger = true;

        /// <summary>
        /// Checks if Game Bar is enabled. 
        /// Reference: https://forums.tomshardware.com/faq/how-to-turn-the-game-bar-on-or-off-in-windows-10.2716301/
        /// </summary>
        /// <param name="log">Enables or disables logging for GameBar class.</param>
        /// <returns></returns>
        public static bool IsEnabled(bool log)
        {
            var tmpLogEn = EnableLogger;
            EnableLogger = log;
            var result = IsEnabled();
            EnableLogger = tmpLogEn;
            return result;
        }

        /// <summary>
        /// Enable or Disable Game Bar.
        /// </summary>
        /// <param name="enabled">Enables or disables Game Bar.</param>
        /// <returns>True if operation was successful otherwise false.</returns>
        public static bool Set(bool enabled)
        {
            return Set(enabled, true);
        }

        /// <summary>
        /// Enable or Disable Game Bar.
        /// </summary>
        /// <param name="enabled">Enables or disables Game Bar.</param>
        /// <param name="log">Enables or disables logging for GameBar class.</param>
        /// <returns></returns>
        public static bool Set(bool enabled, bool log)
        {
            var tmpLogEn = EnableLogger;
            EnableLogger = log;
            var result = enabled ? Enable() : Disable();
            EnableLogger = tmpLogEn;
            return result;
        }

        /// <summary>
        /// Disables Game Bar in Windows 10.
        /// </summary>
        /// <returns>False if it fails or OS major version is less than 10, otherwise true.</returns>
        public static bool Disable()
        {
            if(EnableLogger) Logger.Debug("Disabling Game Bar.");

            if(!SetDWORDValues(0))
            {
                Logger.Debug("Disabling Game Bar failed. Returning...");
                return false;
            }

            if (EnableLogger) Logger.Debug("Game Bar disabled.");
            return true;
        }

        /// <summary>
        /// Enables Game Bar in Windows 10.
        /// </summary>
        /// <returns>False if it fails or OS major version is less than 10, otherwise true.</returns>
        public static bool Enable()
        {
            if (EnableLogger) Logger.Debug("Enabling Game Bar.");

            if (!SetDWORDValues(1))
            {
                Logger.Debug("Enabling Game Bar failed. Returning...");
                return false;
            }

            if (EnableLogger) Logger.Debug("Game Bar enabled.");
            return true;
        }

        /// <summary>
        /// Checks for OS major version.
        /// </summary>
        /// <returns>False if it's not 10.</returns>
        private static bool CheckOSVersion()
        {
            if (EnableLogger) Logger.Debug("Checking for OS major version.");

            if (Environment.OSVersion.Version.Major != 10)
            {
                Logger.Warn("OS major version is not 10. Returning...");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if Game Bar is enabled. 
        /// Reference: https://forums.tomshardware.com/faq/how-to-turn-the-game-bar-on-or-off-in-windows-10.2716301/
        /// </summary>
        /// <returns>True if AppCaptureEnabled or GameDVR_Enabled are set to 1, false if os major version is not 10 and both values are 0.</returns>
        public static bool IsEnabled()
        {
            if (EnableLogger) Logger.Debug("Checking is the Game Bar enabled.");
            if (!CheckOSVersion()) return false;
            bool dvrEnabled, appCaptureEnabled = false;

            using (var regKey = Registry.CurrentUser.OpenSubKey(GameDVRPath, false))
            {

                if (regKey == null)
                {
                    Logger.Warn(@"GameDVR registry key does not exist.");
                    return false;
                }

                try
                {
                    if (EnableLogger) Logger.Debug("Reading AppCaptureEnabled.");
                    var appcapt = regKey.GetValue("AppCaptureEnabled", null);
                    if (EnableLogger) Logger.Debug("AppCaptureEnabled: " + appcapt);
                    appCaptureEnabled = (appcapt == null) ? false : appcapt.ToString() == "1";

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + "while reading AppCaptureEnabled: " + Environment.NewLine);
                    appCaptureEnabled = false;
                }
            }

            using (var regKey = Registry.CurrentUser.OpenSubKey(GameConfigStorePath, false))
            {
                if (regKey == null)
                {
                    Logger.Warn(@"GameConfigStore registry key does not exist.");
                    return false;
                }

                try
                {
                    if (EnableLogger) Logger.Debug("Reading GameDVR_Enabled.");
                    var gamedvr = regKey.GetValue("GameDVR_Enabled", null);
                    if (EnableLogger) Logger.Debug("GameDVR_Enabled: " + gamedvr);
                    dvrEnabled = (gamedvr == null) ? false : gamedvr.ToString() == "1";

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + "while reading GameDVR_Enabled: " + Environment.NewLine);
                    dvrEnabled = false;
                }
            }

            if (EnableLogger) Logger.Debug(nameof(dvrEnabled) + " = " + dvrEnabled);
            if (EnableLogger) Logger.Debug(nameof(appCaptureEnabled) + " = " + appCaptureEnabled);

            return dvrEnabled || appCaptureEnabled;
        }

        /// <summary>
        /// Sets DWORD values AppCaptureEnabled and GameDVR_Enabled.
        /// </summary>
        /// <param name="value">Value of DWORD in registry.</param>
        /// <returns>False if it fails, otherwise true.</returns>
        private static bool SetDWORDValues(int value)
        {
            if (!CheckOSVersion()) return false;

            using (var regKey = Registry.CurrentUser.OpenSubKey(GameDVRPath, true))
            {

                if (regKey == null)
                {
                    Logger.Warn(@"GameDVR registry key does not exist.");
                    return false;
                }

                try
                {
                    if (EnableLogger) Logger.Debug("Setting AppCaptureEnabled to " + value);
                    regKey.SetValue("AppCaptureEnabled", value, RegistryValueKind.DWord);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + $" while setting AppCaptureEnabled to {value}: " + Environment.NewLine);
                    return false;
                }
            }

            using (var regKey = Registry.CurrentUser.OpenSubKey(GameConfigStorePath, true))
            {
                if (regKey == null)
                {
                    Logger.Warn(@"GameConfigStore registry key does not exist.");
                    return false;
                }

                try
                {
                    if (EnableLogger) Logger.Debug("Setting GameDVR_Enabled to " + value);
                    regKey.SetValue("GameDVR_Enabled", value, RegistryValueKind.DWord);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + $" while setting GameDVR_Enabled to {value}: " + Environment.NewLine);
                    return false;
                }
            }

            return true;
        }
    }
}

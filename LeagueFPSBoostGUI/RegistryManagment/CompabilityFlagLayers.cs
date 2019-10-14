using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueFPSBoost.Text;
using Microsoft.Win32;
using NLog;

namespace LeagueFPSBoost.RegistryManagment
{

    
    class CompabilityFlagLayers
    {
        private static string LayersRegistryPath = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
        private string ExecutablePath = "";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Gets or sets value indicating whether the logging is enabled.  
        /// </summary>
        public bool LogEnabled = true;

        /// <summary>
        /// Creates new instance of CompabilityFlagLayers for specified program's path.
        /// </summary>
        /// <param name="executablePath">Sets the registry entry name.</param>
        /// <param name="logEnabled">Sets the value indicating whether the logging is enabled or disabled.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when executablePath is null or empty.</exception>
        public CompabilityFlagLayers(string executablePath, bool logEnabled) : this(executablePath)
        {
            LogEnabled = logEnabled;
        }

        /// <summary>
        /// Creates new instance of CompabilityFlagLayers for specified program's path.
        /// </summary>
        /// <param name="executablePath">Sets the registry entry name.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when executablePath is null or empty.</exception>
        public CompabilityFlagLayers(string executablePath)
        {
            Logger.Debug("Creating new instance: " + executablePath);
            if (string.IsNullOrEmpty(executablePath))
            {
                Logger.Fatal(nameof(executablePath) + " is null or empty!");
                throw new ArgumentNullException(nameof(executablePath));
            }
            ExecutablePath = executablePath;
            if (!File.Exists(ExecutablePath)) Logger.Warn("Executable path does not exist: " + ExecutablePath);
            Logger.Debug("Created new instance.");
        }

        /// <summary>
        /// Returns value of layers registry entry.
        /// </summary>
        /// <param name="logEnabled">Sets the value indicating whether the logging is enabled or disabled for this method.</param>
        /// <returns>Value of layers registry entry as string.</returns>
        public string GetValue(bool logEnabled)
        {
            var tmpLogEna = LogEnabled;
            LogEnabled = logEnabled;
            var result = GetValue();
            LogEnabled = tmpLogEna;
            return result;
        }

        /// <summary>
        /// Sets the value of layers registry entry.
        /// </summary>
        /// <param name="value">Value of the registry entry.</param>
        /// <param name="logEnabled">Sets the value indicating whether the logging is enabled or disabled for this method.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetValue(string value, bool logEnabled)
        {
            var tmpLogEna = LogEnabled;
            LogEnabled = logEnabled;
            var result = SetValue(value);
            LogEnabled = tmpLogEna;
            return result;
        }

        /// <summary>
        /// Returns value of layers registry entry.
        /// </summary>
        /// <returns>Value of layers registry entry as string or null if it fails to retrieve a value.</returns>
        public string GetValue()
        {
            if(LogEnabled) Logger.Debug("Opening SubKey..");
            using(var regKey = Registry.CurrentUser.OpenSubKey(LayersRegistryPath, false))
            {
                if(regKey == null)
                {
                    Logger.Warn("LayersRegistryPath could not be found.");
                    return null;
                }

                try
                {
                    if (LogEnabled) Logger.Debug("Trying to read value of: " + ExecutablePath);
                    var value = regKey.GetValue(ExecutablePath, null);
                    if (LogEnabled) Logger.Debug("Successfully retrieved the value.");
                    return value == null ? "" : value.ToString();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + " while reading value of: " + ExecutablePath + Environment.NewLine);
                    return null;
                }
            }
        }

        /// <summary>
        /// Sets the value of layers registry key.
        /// </summary>
        /// <param name="value">Value of registry key.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetValue(string value)
        {
            if (LogEnabled) Logger.Debug("Opening SubKey..");
            using (var regKey = Registry.CurrentUser.OpenSubKey(LayersRegistryPath, true))
            {
                if (regKey == null)
                {
                    Logger.Warn("LayersRegistryPath could not be found.");
                    return false;
                }

                try
                {
                    if (LogEnabled) Logger.Debug("Trying to set value of: " + ExecutablePath + " to: " + value);
                    regKey.SetValue(ExecutablePath, value, RegistryValueKind.String);
                    if (LogEnabled) Logger.Debug("Successfully set the value.");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + " while reading setting of: " + ExecutablePath);
                    return false;
                }
            }
        }
    }
}

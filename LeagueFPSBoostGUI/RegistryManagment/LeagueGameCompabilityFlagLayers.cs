using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LeagueFPSBoost.Text;
using NLog;

namespace LeagueFPSBoost.RegistryManagment
{
    class LeagueGameCompabilityFlagLayers : CompabilityFlagLayers
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly string DisableFullScreenOptimizationsFlagReg = "DISABLEDXMAXIMIZEDWINDOWEDMODE";

        private LeagueGameCompabilityFlagLayers() : base(Program.LeagueGamePath)
        {
            Logger.Debug("Created new instance.");
        }

        /// <summary>
        /// Gets the value indicating whether the FullScreenOptimizationsFlag is enabled.  
        /// </summary>
        /// <returns>True if FullScreenOptimizations is enabled, false otherwise or if it fails.</returns>
        public bool GetFullScreenOptimizationsFlag()
        {
            return GetFullScreenOptimizationsFlag(true);
        }

        /// <summary>
        /// Gets the value indicating whether the FullScreenOptimizationsFlag is enabled.  
        /// </summary>
        /// <param name="logEnabled">Sets the value indicating whether the logging is enabled for this method.</param>
        /// <returns>True if FullScreenOptimizations is enabled or if it fails, false otherwise.</returns>
        public bool GetFullScreenOptimizationsFlag(bool logEnabled)
        {
            if (!CheckOSVersion(logEnabled)) return true;
            try
            {
                if (logEnabled) Logger.Debug("Reading FullScreenOptimizationsFlag.");
                var registryQueryResult = base.GetValue(logEnabled);
                if(string.IsNullOrEmpty(registryQueryResult))
                {
                    if (logEnabled) Logger.Warn("Registry query is null or empty, returning FullScreenOptimizationsFlag as true.");
                    return true;
                }

                return !registryQueryResult.Contains(DisableFullScreenOptimizationsFlagReg);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while reading GetFullScreenOptimizationsFlag: " + Environment.NewLine);
                return true;
            }
        }

        /// <summary>
        /// Enables or disables FullScreenOptimizations.
        /// </summary>
        /// <param name="enabled">Value indicating whether the flag is enabled or disabled.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetFullScreenOptimizationsFlag(bool enabled)
        {
            return SetFullScreenOptimizationsFlag(enabled, true);
        }

        /// <summary>
        /// Enables or disables FullScreenOptimizations.
        /// </summary>
        /// <param name="enabled">Value indicating whether the flag is enabled or disabled.</param>
        /// <param name="logEnabled">Value indicating whether the logging is enabled for this method.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetFullScreenOptimizationsFlag(bool enabled, bool logEnabled)
        {
            if (!CheckOSVersion(logEnabled)) return false;
            try
            {
                if (logEnabled) Logger.Debug("Setting FullScreenOptimizationsFlag to: " + enabled);
                var regQueryValue = base.GetValue(logEnabled);
                var hasData = !string.IsNullOrEmpty(regQueryValue);
                var newRegValue = "";
                if (logEnabled) Logger.Debug("Registry entry value: " + regQueryValue);

                if(hasData)
                {
                    if (logEnabled) Logger.Debug("Registry entry has some data. Splitting it.");
                    var paramList = regQueryValue.Split(' ').ToList();
                    if(enabled)
                    {
                        if (logEnabled) Logger.Debug("Enabling FullScreenOptimizations, removing: " + DisableFullScreenOptimizationsFlagReg);
                        if (paramList.Remove(DisableFullScreenOptimizationsFlagReg))
                        {
                            if (logEnabled) Logger.Debug("Successfully removed.");
                        }
                        else
                        {
                            if (logEnabled) Logger.Warn("Removing failed, maybe it was not present. Returning...");
                            return true;
                        }
                    }
                    else
                    {
                        if (logEnabled) Logger.Debug("Disabling FullScreenOptimizations, adding: " + DisableFullScreenOptimizationsFlagReg);
                        if (!paramList.Contains(DisableFullScreenOptimizationsFlagReg))
                        {
                            paramList.Insert(1, DisableFullScreenOptimizationsFlagReg);
                            if (logEnabled) Logger.Debug($"Inserted {DisableFullScreenOptimizationsFlagReg} at position 1.");
                        }
                        else
                        {
                            if (logEnabled) Logger.Debug($"{DisableFullScreenOptimizationsFlagReg} already present. Returning...");
                            return true;
                        }
                    }

                    newRegValue = string.Join(" ", paramList);
                    if (logEnabled) Logger.Debug("Registry value before applying regex: " + newRegValue);
                    newRegValue = Regex.Replace(newRegValue, @"/\s{2,}/g", " ");
                    if (logEnabled) Logger.Debug("Registry value after applying regex: " + newRegValue);
                }
                else
                {
                    newRegValue = enabled ? "" : ("~ " + DisableFullScreenOptimizationsFlagReg);
                }

                if (logEnabled) Logger.Debug("Saving new registry value...");

                base.SetValue(newRegValue, logEnabled);

                return true;
            }
            catch(Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while setting FullScreenOptimizationsFlag: " + Environment.NewLine);
                return false;
            }
        }

        /// <summary>
        /// Checks for OS major version.
        /// </summary>
        /// <returns>False if it's not 10.</returns>
        private static bool CheckOSVersion(bool logEnabled)
        {
            if (logEnabled) Logger.Debug("Checking for OS major version.");

            if (Environment.OSVersion.Version.Major != 10)
            {
                Logger.Warn("OS major version is not 10. Returning...");
                return false;
            }
            return true;
        }

        private static Lazy<LeagueGameCompabilityFlagLayers> instance = new Lazy<LeagueGameCompabilityFlagLayers>(() => new LeagueGameCompabilityFlagLayers());

        /// <summary>
        /// Gets the static instance of LeagueGameCompabilityFlagLayers.
        /// </summary>
        public static LeagueGameCompabilityFlagLayers Instance => instance.Value;
    }
}

using LeagueFPSBoost.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.IO;

namespace LeagueFPSBoost.Updater.Json
{
    public static class UpdaterJson
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static bool Save(UpdaterData data)
        {
            var jsonText = JsonString(data);
            try
            {
                WriteTextToFile(data.FileName, jsonText);
            }
            catch (Exception ex)
            {
                LogError(ex, data.FileName);
                return false;
            }
            return true;
        }

        public static string SaveAndReturnString(UpdaterData data)
        {
            var jsonText = JsonString(data);
            try
            {
                WriteTextToFile(data.FileName, jsonText);
            }
            catch (Exception ex)
            {
                LogError(ex, data.FileName);
                return "Error";
            }
            return jsonText;
        }

        private static string JsonString(UpdaterData data)
        {
            var jsonUpdaterDataObject = JObject.FromObject(data);
            jsonUpdaterDataObject.Property(nameof(data.FileName)).Remove();
            jsonUpdaterDataObject.Property(nameof(data.UpdaterDataType)).Remove();
            return JsonConvert.SerializeObject(jsonUpdaterDataObject, Formatting.Indented);
        }

        private static void WriteTextToFile(string filename, string text)
        {
            logger.Info("Trying to save json updater data file: " + filename);
            File.WriteAllText(filename, text);
            logger.Info("Successfully saved json updater data file.");
        }

        private static void LogError(Exception ex, string filename)
        {
            logger.Error(ex, Strings.exceptionThrown + " while saving json updater file: " + filename);
        }
    }
}

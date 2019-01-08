using LeagueFPSBoost.Cryptography;
using LeagueFPSBoost.Text;
using LeagueFPSBoost.Updater.Json;
using LeagueFPSBoost.Updater.MessageBoxCollection;
using LeagueFPSBoost.Updater.PostUpdateAction;
using LeagueFPSBoost.Updater.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.Collections.Generic;
namespace LeagueFPSBoost.Updater
{
    public enum UpdaterDataTypeFormat
    {
        XDocument,
        JavaScriptObjectNotation
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct UpdaterData
    {
        [JsonProperty]
        public DateTime CreationTime { get; private set; } // = DateTime.Now;

        [JsonProperty]
        [JsonConverter(typeof(VersionConverter))]
        public Version Version { get; private set; } //= Program.Version;

        [JsonProperty]
        public string DownloadURL { get; private set; } //= Strings.Updater_Download_URL;

        [JsonProperty]
        public string ChangelogURL { get; private set; } //= Strings.Updater_Changelog_URL;

        [JsonProperty]
        public bool Mandatory { get; private set; }// = true;
        
        [JsonProperty]
        public Checksum Checksum { get; private set; } //= new Checksum();

        [JsonProperty]
        public string CommandLineArguments { get; private set; } //= string.Empty;

        [JsonProperty]
        public List<PostUpdateActionData> PostUpdate { get; private set; }  //= new Dictionary<string, string>();

        [JsonProperty]
        public List<MessageBoxData> MessageBoxes { get; private set; }
        
        public UpdaterDataTypeFormat UpdaterDataType { get; private set; } //= UpdaterDataTypeFormat.XDocument;

        public string FileName { get; private set; }// = "";

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [JsonConstructor]
        public UpdaterData(DateTime creationTime, Version version, string downloadURL, string changelogURL, bool mandatory, Checksum checksum, string commandLineArguments, List<PostUpdateActionData> postUpdate, List<MessageBoxData> messageBoxes)
            : this("WEB", UpdaterDataTypeFormat.JavaScriptObjectNotation, checksum, mandatory, downloadURL, version, changelogURL, commandLineArguments)
        {
            /*Mandatory = mandatory;
            Version = version;
            DownloadURL = downloadURL;
            ChangelogURL = changelogURL;
            CommandLineArguments = commandlineArgs;
            Checksum = checksum;*/
            CreationTime = creationTime;
            PostUpdate = postUpdate;
            MessageBoxes = messageBoxes;
        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum, bool mandatory, string downloadURL, Version version, string changelogURL, string commandlineArguments)
        {
            FileName = fileName;
            UpdaterDataType = updaterDataType;
            Version = version;
            DownloadURL = downloadURL;
            ChangelogURL = changelogURL;
            Mandatory = mandatory;
            Checksum = checksum;
            CommandLineArguments = commandlineArguments;
            CreationTime = DateTime.Now;
            PostUpdate = new List<PostUpdateActionData>();
            MessageBoxes = new List<MessageBoxData>();

            logger.Debug("Created new instance: " + Environment.NewLine + 
                Strings.tabWithLine + "FileName: " + FileName + Environment.NewLine +
                Strings.tabWithLine + "UpdaterDataType: " + UpdaterDataType + Environment.NewLine +
                Strings.tabWithLine + "Creation Time: " + CreationTime + Environment.NewLine +
                Strings.tabWithLine + "Version: " + Version + Environment.NewLine +
                Strings.tabWithLine + "DownloadURL: " + DownloadURL + Environment.NewLine +
                Strings.tabWithLine + "ChangelogURL: " + ChangelogURL + Environment.NewLine +
                Strings.tabWithLine + "Mandatory: " + Mandatory + Environment.NewLine +
                Strings.tabWithLine + "CommandLineArguments: " + CommandLineArguments + Environment.NewLine +
                Strings.tabWithLine + "Checksum: " + Checksum.Value + " - " + Checksum.Type);
        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum, bool mandatory, string downloadURL, Version version, string changelogURL)
            : this(fileName, updaterDataType, checksum, mandatory, downloadURL, version, changelogURL, string.Empty)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum, bool mandatory, string downloadURL, Version version)
            : this(fileName, updaterDataType, checksum, mandatory, downloadURL, version, Strings.Updater_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum, bool mandatory, string downloadURL)
            : this(fileName, updaterDataType, checksum, mandatory, downloadURL, Program.Version, Strings.Updater_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum, bool mandatory)
            : this(fileName, updaterDataType, checksum, mandatory, Strings.Updater_Download_URL, Program.Version, Strings.Updater_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum)
            : this(fileName, updaterDataType, checksum, true, Strings.Updater_Download_URL, Program.Version, Strings.Updater_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType)
            : this(fileName, updaterDataType, new Checksum(), true, Strings.Updater_Download_URL, Program.Version, Strings.Updater_Changelog_URL)
        {

        }

        public UpdaterData(string fileName)
            : this(fileName, UpdaterDataTypeFormat.XDocument, new Checksum(), true, Strings.Updater_Download_URL, Program.Version, Strings.Updater_Changelog_URL)
        {

        }

        public void AddPostUpdateAction(PostUpdateActionData postUpdateActionData)
        {
            PostUpdate.Add(postUpdateActionData);
            logger.Debug($"Added new entry to {nameof(PostUpdate)}: {postUpdateActionData.ToStringTabbed()}.");
        }

        public void AddMessageBox(MessageBoxData messageBoxData)
        {
            MessageBoxes.Add(messageBoxData);
            logger.Debug($"Added new entry to {nameof(MessageBoxes)}: {messageBoxData.ToStringTabbed()}");
        }

        public string SaveAndReturnString()
        {
            switch (UpdaterDataType)
            {
                case UpdaterDataTypeFormat.XDocument:
                    logger.Info("Saving XML: " + FileName);
                    string xml = "";
                    try
                    {
                        xml = UpdaterXDocument.Save(this).ToString();
                        logger.Info("Successfully saved xml file.");
                        return xml;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving xml file." + Environment.NewLine);
                        return xml;
                    }
                case UpdaterDataTypeFormat.JavaScriptObjectNotation:
                    logger.Info("Saving JSON: " + FileName);
                    string json = "";
                    try
                    {
                        json = UpdaterJson.SaveAndReturnString(this);
                        logger.Info("Successfully saved json file.");
                        return json;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving json file." + Environment.NewLine);
                        return json;
                    }
                default:
                    return "Error";
            }
        }

        public bool Save()
        {
            switch (UpdaterDataType)
            {
                case UpdaterDataTypeFormat.XDocument:
                    logger.Info("Saving XML: " + FileName);
                    try
                    {
                        UpdaterXDocument.Save(this);
                        logger.Info("Successfully saved xml file.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving xml file." + Environment.NewLine);
                        return false;
                    }
                case UpdaterDataTypeFormat.JavaScriptObjectNotation:
                    logger.Info("Saving JSON: " + FileName);
                    try
                    {
                        UpdaterJson.Save(this);
                        logger.Info("Successfully saved json file.");
                        return true;
                    }
                    catch(Exception ex)
                    {
                        logger.Error(ex, Strings.exceptionThrown + " while saving json file." + Environment.NewLine);
                        return false;
                    }
                default:
                    return false;
            }
        }

        public override string ToString()
        {
            return "FileName: " + FileName + Environment.NewLine +
                "UpdaterDataType: " + UpdaterDataType + Environment.NewLine +
                "Creation Time: " + CreationTime + Environment.NewLine +
                "Version: " + Version + Environment.NewLine +
                "DownloadURL: " + DownloadURL + Environment.NewLine +
                "ChangelogURL: " + ChangelogURL + Environment.NewLine +
                "Mandatory: " + Mandatory + Environment.NewLine +
                "CommandLineArguments: " + CommandLineArguments + Environment.NewLine +
                "Checksum: " + Checksum.Value + " - " + Checksum.Type;
        }

        public string ToStringTabbed()
        {
            return Strings.tabWithLine + "FileName: " + FileName + Environment.NewLine +
                Strings.tabWithLine + "UpdaterDataType: " + UpdaterDataType + Environment.NewLine +
                Strings.tabWithLine + "Creation Time: " + CreationTime + Environment.NewLine +
                Strings.tabWithLine + "Version: " + Version + Environment.NewLine +
                Strings.tabWithLine + "DownloadURL: " + DownloadURL + Environment.NewLine +
                Strings.tabWithLine + "ChangelogURL: " + ChangelogURL + Environment.NewLine +
                Strings.tabWithLine + "Mandatory: " + Mandatory + Environment.NewLine +
                Strings.tabWithLine + "CommandLineArguments: " + CommandLineArguments + Environment.NewLine +
                Strings.tabWithLine + "Checksum: " + Checksum.Value + " - " + Checksum.Type;
        }
        
    }
}

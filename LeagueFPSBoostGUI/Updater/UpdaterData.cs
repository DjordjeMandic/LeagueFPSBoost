using LeagueFPSBoost.Cryptography;
using LeagueFPSBoost.Text;
using LeagueFPSBoost.Updater.Json;
using LeagueFPSBoost.Updater.Xml;
using NLog;
using System;

namespace LeagueFPSBoost.Updater
{
    public enum UpdaterDataTypeFormat
    {
        XDocument,
        JavaScriptObjectNotation
    }

    public class UpdaterData
    {
        public bool Mandatory { get; private set; } = true;
        public Version Version { get; private set; } = Program.Version;
        public string DownloadURL { get; private set; } = Strings.Updater_XML_Download_URL;
        public string ChangelogURL { get; private set; } = Strings.Updater_XML_Changelog_URL;
        public string CommandLineArguments { get; private set; } = string.Empty;

        public Checksum Checksum { get; private set; } = new Checksum();
        public UpdaterDataTypeFormat UpdaterDataType { get; private set; } = UpdaterDataTypeFormat.XDocument;
        public string FileName { get; private set; } = "";

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

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

            logger.Debug("Created new instance: " + Environment.NewLine + 
                Strings.tabWithLine + "FileName: " + FileName + Environment.NewLine +
                Strings.tabWithLine + "UpdaterDataType: " + UpdaterDataType + Environment.NewLine +
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
            : this(fileName, updaterDataType, checksum, mandatory, downloadURL, version, Strings.Updater_XML_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum, bool mandatory, string downloadURL)
            : this(fileName, updaterDataType, checksum, mandatory, downloadURL, Program.Version, Strings.Updater_XML_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum, bool mandatory)
            : this(fileName, updaterDataType, checksum, mandatory, Strings.Updater_XML_Download_URL, Program.Version, Strings.Updater_XML_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType, Checksum checksum)
            : this(fileName, updaterDataType, checksum, true, Strings.Updater_XML_Download_URL, Program.Version, Strings.Updater_XML_Changelog_URL)
        {

        }

        public UpdaterData(string fileName, UpdaterDataTypeFormat updaterDataType)
            : this(fileName, updaterDataType, new Checksum(), true, Strings.Updater_XML_Download_URL, Program.Version, Strings.Updater_XML_Changelog_URL)
        {

        }

        public UpdaterData(string fileName)
            : this(fileName, UpdaterDataTypeFormat.XDocument, new Checksum(), true, Strings.Updater_XML_Download_URL, Program.Version, Strings.Updater_XML_Changelog_URL)
        {

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
    }
}

using LeagueFPSBoost.Cryptography;
using NLog;
using System;
using System.Xml.Linq;

namespace LeagueFPSBoost.Updater.Xml
{
    public static class UpdaterXDocument
    {
        /*public static bool Mandatory = true;
        public static Version Version = Program.Version;
        public static string DownloadURL = Strings.Updater_XML_Download_URL;
        public static string ChangelogURL = Strings.Updater_XML_Changelog_URL;
        public static string CommandLineArguments = string.Empty;

        public static Checksum Checksum = new Checksum();*/

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static XDocument Save(string fileName, Version version, string downloadURL, string changelogURL, bool mandatory, string commandLineArguments, Checksum checksum)
        {
            if (string.IsNullOrEmpty(downloadURL)) { logger.Error("DownloadURL is null or empty."); throw new ArgumentException("DownloadURL is null or empty."); }
            
            var xmlItem = new XElement("item");
            xmlItem.Add(new XElement("version", version.ToString()));
            xmlItem.Add(new XElement("url", downloadURL));

            if (!string.IsNullOrEmpty(changelogURL)) { logger.Debug("Changelog url exists. Adding: " + changelogURL); xmlItem.Add(new XElement("changelog", changelogURL)); }

            xmlItem.Add(new XElement("mandatory", mandatory));

            if (!string.IsNullOrEmpty(commandLineArguments)) { logger.Debug("Command line arguments exists. Adding.."); xmlItem.Add(new XElement("args", commandLineArguments)); }

            if (!string.IsNullOrEmpty(checksum.Value))
            {
                logger.Debug("Checksum exists. Adding: " + checksum.Type + " - " + checksum.Value);
                var xmlChecksum = new XElement("checksum", checksum.Value);
                xmlChecksum.SetAttributeValue("algorithm", checksum.Type);
                xmlItem.Add(xmlChecksum);
            }
            var xmlDoc = new XDocument(xmlItem);
            xmlDoc.Save(fileName);
            return xmlDoc;
        }

        /*public static XDocument Save(string fileName)
        {
            return Save(fileName, Version, DownloadURL, ChangelogURL, Mandatory, CommandLineArguments, Checksum);
        }*/

        public static XDocument Save(UpdaterData data)
        {
            return Save(data.FileName, data.Version, data.DownloadURL, data.ChangelogURL, data.Mandatory, data.CommandLineArguments, data.Checksum);
        }
    }
}

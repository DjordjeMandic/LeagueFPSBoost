using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Xml.Linq;

namespace LeagueFPSBoost.Updater.Xml
{
    public static class UpdaterXDocument
    {
        public static bool Mandatory = true;
        public static Version Version = Program.Version;
        public static string DownloadURL = Strings.Updater_XML_Download_URL;
        public static string ChangelogURL = Strings.Updater_XML_Changelog_URL;
        public static string CommandLineArguments = string.Empty;

        public static Checksum Checksum = new Checksum();

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static XDocument Save(string fileName)
        {
            if (string.IsNullOrEmpty(DownloadURL)) { logger.Error("DownloadURL is null or empty."); throw new ArgumentException("DownloadURL is null or empty."); }
            
            var xmlItem = new XElement("item");
            xmlItem.Add(new XElement("version", Version.ToString()));
            xmlItem.Add(new XElement("url", DownloadURL));

            if (!string.IsNullOrEmpty(ChangelogURL)) { logger.Debug("Changelog url exists. Adding.."); xmlItem.Add(new XElement("changelog", ChangelogURL)); }

            xmlItem.Add(new XElement("mandatory", Mandatory));

            if (!string.IsNullOrEmpty(CommandLineArguments)) { logger.Debug("Command line arguments exists. Adding.."); xmlItem.Add(new XElement("args", CommandLineArguments)); }

            if (!string.IsNullOrEmpty(Checksum.Value))
            {
                logger.Debug("Checksum url exists. Adding: " + Checksum.Type + " - " + Checksum.Value);
                var xmlChecksum = new XElement("checksum", Checksum.Value);
                xmlChecksum.SetAttributeValue("algorithm", Checksum.Type);
                xmlItem.Add(xmlChecksum);
            }
            var xmlDoc = new XDocument(xmlItem);
            xmlDoc.Save(fileName);
            return xmlDoc;
        }
    }
}

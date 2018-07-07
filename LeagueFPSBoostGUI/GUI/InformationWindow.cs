using AutoUpdaterDotNET;
using LeagueFPSBoost.ProcessManagement;
using LeagueFPSBoost.Text;
using MetroFramework.Components;
using MetroFramework.Forms;
using NLog;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LeagueFPSBoost.GUI
{
    public partial class InformationWindow : MetroForm
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        bool console = true;

        public InformationWindow(MetroStyleManager styleManager)
        {
            logger.Debug("Initializing information window.");
            InitializeComponent();
            metroStyleManager1.Style = styleManager.Style;
            metroStyleManager1.Theme = styleManager.Theme;
            metroStyleManager1.Update();
            metroButton2.Visible = Program.HasConsole();
        }

        private void InformationWindow_Load(object sender, EventArgs e)
        {
            noClientCheckBox1.Checked = Program.NoClient;
            clearLogsCheckBox1.Checked = Program.ClearLogs;
            procModulesCheckBox1.Checked|= Program.PrintProcessModules;
            exitEarlyCheckBox1.Checked = Program.ExitBeforeMainWindow;
            configRstRsnCheckBox1.Checked = Program.RestartReasonParsed == Program.RestartReason.Configuration;
            adminRstRsnCheckBox1.Checked = Program.RestartReasonParsed == Program.RestartReason.SelfElevation;
            logger.Debug("Information window loaded.");
        }

        private void GitLink3_Click(object sender, EventArgs e)
        {
            OpenUrl.Open(Strings.GitHub_URL);
        }

        private void BoardsLink4_Click(object sender, EventArgs e)
        {
            OpenUrl.Open(Strings.BoardsPage_URL);
        }

        private void YtLink1_Click(object sender, EventArgs e)
        {
            OpenUrl.Open(Strings.YouTube_URL);
        }

        private void FbLink2_Click(object sender, EventArgs e)
        {
            OpenUrl.Open(Strings.Facebook_URL);
        }

        private void CleanRstButton2_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Debug("Trying clean restart.");
                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                logger.Debug("Success.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while trying clean restart: " + Environment.NewLine);
                MessageBox.Show(ex.ToString(), "An error occurred while trying clean restart.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void rstButton1_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Debug("Trying to restart.");
                var startInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName);
                if (noClientCheckBox1.Checked) startInfo.Arguments += " -" + Strings.noClientArg.Split('|')[0] + " ";
                if (clearLogsCheckBox1.Checked) startInfo.Arguments += " -" + Strings.clearLogsArg.Split('|')[0] + " ";
                if (procModulesCheckBox1.Checked) startInfo.Arguments += " -" + Strings.printProcessModulesArg.Split('|')[0] + " ";
                if (exitEarlyCheckBox1.Checked) startInfo.Arguments += " -" + Strings.ExitBeforeMainWindow.Split('|')[0] + " ";
                if(cleanCheckBox1.Checked)
                {
                    CleanRstButton2_Click(this, EventArgs.Empty);
                }
                else
                {
                    logger.Debug("Restarting: " + Environment.NewLine + Strings.tabWithLine + "File Name: " + startInfo.FileName + Environment.NewLine + Strings.tabWithLine + "Arguments: " + startInfo.Arguments);
                    Process.Start(startInfo);
                }
                logger.Debug("Success");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while trying to restart: " + Environment.NewLine);
                MessageBox.Show(ex.ToString(), "An error occurred while trying to restart.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InformationWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            logger.Debug("Information window closed.");
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            logger.Debug("Checking for updates.");
            AutoUpdater.ReportErrors = true;
            AutoUpdater.Start(Strings.Updater_XML_URL);
        }

        private void metroCheckBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cleanCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            noClientCheckBox1.Enabled = !cleanCheckBox1.Checked;
            clearLogsCheckBox1.Enabled = !cleanCheckBox1.Checked;
            procModulesCheckBox1.Enabled = !cleanCheckBox1.Checked;
            exitEarlyCheckBox1.Enabled = !cleanCheckBox1.Checked;
            configRstRsnCheckBox1.Enabled = !cleanCheckBox1.Checked;
            adminRstRsnCheckBox1.Enabled = !cleanCheckBox1.Checked;
            logger.Debug("Clean checkbox checked: " + cleanCheckBox1.Checked);
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            console = !Program.ConsoleState(console);
        }
    }
}

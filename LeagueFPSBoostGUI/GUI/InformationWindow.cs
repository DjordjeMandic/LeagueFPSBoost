using AutoUpdaterDotNET;
using LeagueFPSBoost.ProcessManagement;
using LeagueFPSBoost.Text;
using MetroFramework.Components;
using MetroFramework.Forms;
using NLog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace LeagueFPSBoost.GUI
{
    public partial class InformationWindow : MetroForm
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        bool console = true;

        bool checkingForUpdate;

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

        private void RstButton1_Click(object sender, EventArgs e)
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

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            logger.Debug("Update button clicked, checking for updates.");
            if (!checkingForUpdate)
            {
                logger.Debug("Update check thread not running. Starting it now.");
                try
                {

                    ThreadPool.QueueUserWorkItem(UpdateCheckMethod);
                }
                catch(Exception ex)
                {
                    logger.Error(ex, Strings.exceptionThrown + " while trying to queue update check thread: " + Environment.NewLine);
                    MessageBox.Show("There was an error while trying to check for updates. Check log for more info.", "LeagueFPSBoost: Update check fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                logger.Debug("Update check thread is already running.");
            }
        }

        private void UpdateCheckMethod(object StateInfo)
        {
            checkingForUpdate = true;
            while (!MainWindow.updateCheckFinished)
            {
                logger.Debug("Checking for updates.");
                logger.Warn("Automatic update check is currently in progress. Sleeping this thread for 500ms.");
                Thread.Sleep(500);
            }
            AutoUpdater.ReportErrors = true;
            logger.Debug("Starting auto updater.");
            AutoUpdater.Start(Strings.Updater_XML_URL);
            Thread.Sleep(500);
            checkingForUpdate = false;
        }

        private void MetroCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            logger.Debug("Exit early checkbox checked: " + exitEarlyCheckBox1.Checked);
        }

        private void CleanCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            logger.Debug("Clean checkbox checked: " + cleanCheckBox1.Checked);

            noClientCheckBox1.Enabled = !cleanCheckBox1.Checked;
            clearLogsCheckBox1.Enabled = !cleanCheckBox1.Checked;
            procModulesCheckBox1.Enabled = !cleanCheckBox1.Checked;
            exitEarlyCheckBox1.Enabled = !cleanCheckBox1.Checked;
            configRstRsnCheckBox1.Enabled = !cleanCheckBox1.Checked;
            adminRstRsnCheckBox1.Enabled = !cleanCheckBox1.Checked;
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            logger.Debug("Console button clicked.");
            console = !Program.ConsoleState(console);
        }

        private void NoClientCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            logger.Debug("No client checkbox checked: " + noClientCheckBox1.Checked);
        }

        private void ClearLogsCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            logger.Debug("Clear logs checkbox checked: " + clearLogsCheckBox1.Checked);
        }

        private void ProcModulesCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            logger.Debug("Process modules checkbox checked: " + procModulesCheckBox1.Checked);
        }

        private void GroupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void ConfigRstRsnCheckBox1_CheckedChanged(object sender, EventArgs e)
        {

            logger.Debug("Restart reason config checkbox checked: " +configRstRsnCheckBox1.Checked);
        }

        private void AdminRstRsnCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            logger.Debug("Restart reason admin checkbox checked: " + adminRstRsnCheckBox1.Checked);
        }

        private void ExitEarlyCheckBox1_CheckStateChanged(object sender, EventArgs e)
        {

        }

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            OpenUrl.Open(Strings.DONATE_URL);
        }
    }
}

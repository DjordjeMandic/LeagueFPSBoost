using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeagueFPSBoost.GUI
{
    public partial class ProgressBarWindow : Form
    {
        public bool Loaded { get; private set; } = false;

        public ProgressBarWindow()
        {
            InitializeComponent();
            listBox1.Items.Add("Event history:");
            listBox1.Items.Add("");
            Program.OnCrashUploadReport += Program_OnCrashUploadReport;
        }

        private void Program_OnCrashUploadReport(object sender, CrashUploadReportEventArgs e)
        {
            progressBar1.Value = e.Percentage;
            label2.Text = e.Percentage.ToString() + "%";
            label1.Text = e.Status;
            listBox1.Items.Add(label1.Text);
            this.Refresh();
        }

        private void ProgressBarWindow_Load(object sender, EventArgs e)
        {
            Loaded = true;
        }
    }
}

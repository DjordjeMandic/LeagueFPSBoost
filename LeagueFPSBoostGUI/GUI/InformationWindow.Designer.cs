namespace LeagueFPSBoost.GUI
{
    partial class InformationWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InformationWindow));
            this.ytLink1 = new MetroFramework.Controls.MetroLink();
            this.fbLink2 = new MetroFramework.Controls.MetroLink();
            this.gitLink3 = new MetroFramework.Controls.MetroLink();
            this.boardsLink4 = new MetroFramework.Controls.MetroLink();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cleanCheckBox1 = new MetroFramework.Controls.MetroCheckBox();
            this.exitEarlyCheckBox1 = new MetroFramework.Controls.MetroCheckBox();
            this.metroLabel1 = new MetroFramework.Controls.MetroLabel();
            this.adminRstRsnCheckBox1 = new MetroFramework.Controls.MetroCheckBox();
            this.configRstRsnCheckBox1 = new MetroFramework.Controls.MetroCheckBox();
            this.procModulesCheckBox1 = new MetroFramework.Controls.MetroCheckBox();
            this.clearLogsCheckBox1 = new MetroFramework.Controls.MetroCheckBox();
            this.noClientCheckBox1 = new MetroFramework.Controls.MetroCheckBox();
            this.rstButton1 = new MetroFramework.Controls.MetroButton();
            this.metroStyleManager1 = new MetroFramework.Components.MetroStyleManager(this.components);
            this.metroButton1 = new MetroFramework.Controls.MetroButton();
            this.metroButton2 = new MetroFramework.Controls.MetroButton();
            this.metroButton3 = new MetroFramework.Controls.MetroButton();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.metroStyleManager1)).BeginInit();
            this.SuspendLayout();
            // 
            // ytLink1
            // 
            this.ytLink1.Location = new System.Drawing.Point(23, 254);
            this.ytLink1.Name = "ytLink1";
            this.ytLink1.Size = new System.Drawing.Size(241, 23);
            this.ytLink1.TabIndex = 0;
            this.ytLink1.Text = "Developer\'s YouTube Channel: N!cky";
            this.ytLink1.UseStyleColors = true;
            this.ytLink1.Click += new System.EventHandler(this.YtLink1_Click);
            // 
            // fbLink2
            // 
            this.fbLink2.Location = new System.Drawing.Point(23, 283);
            this.fbLink2.Name = "fbLink2";
            this.fbLink2.Size = new System.Drawing.Size(241, 23);
            this.fbLink2.TabIndex = 1;
            this.fbLink2.Text = "Katarina Mains Global Facebook Group";
            this.fbLink2.UseStyleColors = true;
            this.fbLink2.Click += new System.EventHandler(this.FbLink2_Click);
            // 
            // gitLink3
            // 
            this.gitLink3.Location = new System.Drawing.Point(151, 63);
            this.gitLink3.Name = "gitLink3";
            this.gitLink3.Size = new System.Drawing.Size(113, 23);
            this.gitLink3.TabIndex = 2;
            this.gitLink3.Text = "GitHub Repository";
            this.gitLink3.UseStyleColors = true;
            this.gitLink3.Click += new System.EventHandler(this.GitLink3_Click);
            // 
            // boardsLink4
            // 
            this.boardsLink4.Location = new System.Drawing.Point(23, 63);
            this.boardsLink4.Name = "boardsLink4";
            this.boardsLink4.Size = new System.Drawing.Size(113, 23);
            this.boardsLink4.TabIndex = 3;
            this.boardsLink4.Text = "Boards Page";
            this.boardsLink4.UseStyleColors = true;
            this.boardsLink4.Click += new System.EventHandler(this.BoardsLink4_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cleanCheckBox1);
            this.groupBox1.Controls.Add(this.exitEarlyCheckBox1);
            this.groupBox1.Controls.Add(this.metroLabel1);
            this.groupBox1.Controls.Add(this.adminRstRsnCheckBox1);
            this.groupBox1.Controls.Add(this.configRstRsnCheckBox1);
            this.groupBox1.Controls.Add(this.procModulesCheckBox1);
            this.groupBox1.Controls.Add(this.clearLogsCheckBox1);
            this.groupBox1.Controls.Add(this.noClientCheckBox1);
            this.groupBox1.Location = new System.Drawing.Point(23, 92);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(162, 144);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Startup Arguments";
            this.groupBox1.Enter += new System.EventHandler(this.GroupBox1_Enter);
            // 
            // cleanCheckBox1
            // 
            this.cleanCheckBox1.AutoSize = true;
            this.cleanCheckBox1.Location = new System.Drawing.Point(103, 18);
            this.cleanCheckBox1.Name = "cleanCheckBox1";
            this.cleanCheckBox1.Size = new System.Drawing.Size(53, 15);
            this.cleanCheckBox1.TabIndex = 10;
            this.cleanCheckBox1.Text = "Clean";
            this.cleanCheckBox1.UseStyleColors = true;
            this.cleanCheckBox1.UseVisualStyleBackColor = true;
            this.cleanCheckBox1.CheckedChanged += new System.EventHandler(this.CleanCheckBox1_CheckedChanged);
            // 
            // exitEarlyCheckBox1
            // 
            this.exitEarlyCheckBox1.AutoSize = true;
            this.exitEarlyCheckBox1.Location = new System.Drawing.Point(6, 18);
            this.exitEarlyCheckBox1.Name = "exitEarlyCheckBox1";
            this.exitEarlyCheckBox1.Size = new System.Drawing.Size(69, 15);
            this.exitEarlyCheckBox1.TabIndex = 9;
            this.exitEarlyCheckBox1.Text = "Exit Early";
            this.exitEarlyCheckBox1.UseStyleColors = true;
            this.exitEarlyCheckBox1.UseVisualStyleBackColor = true;
            this.exitEarlyCheckBox1.CheckedChanged += new System.EventHandler(this.MetroCheckBox1_CheckedChanged);
            this.exitEarlyCheckBox1.CheckStateChanged += new System.EventHandler(this.ExitEarlyCheckBox1_CheckStateChanged);
            // 
            // metroLabel1
            // 
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.FontSize = MetroFramework.MetroLabelSize.Small;
            this.metroLabel1.FontWeight = MetroFramework.MetroLabelWeight.Bold;
            this.metroLabel1.Location = new System.Drawing.Point(6, 0);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(114, 15);
            this.metroLabel1.TabIndex = 8;
            this.metroLabel1.Text = "Startup Arguments";
            this.metroLabel1.UseStyleColors = true;
            // 
            // adminRstRsnCheckBox1
            // 
            this.adminRstRsnCheckBox1.AutoCheck = false;
            this.adminRstRsnCheckBox1.AutoSize = true;
            this.adminRstRsnCheckBox1.Location = new System.Drawing.Point(6, 123);
            this.adminRstRsnCheckBox1.Name = "adminRstRsnCheckBox1";
            this.adminRstRsnCheckBox1.Size = new System.Drawing.Size(151, 15);
            this.adminRstRsnCheckBox1.TabIndex = 5;
            this.adminRstRsnCheckBox1.Text = "Restart Reason Elevation";
            this.adminRstRsnCheckBox1.UseStyleColors = true;
            this.adminRstRsnCheckBox1.UseVisualStyleBackColor = true;
            this.adminRstRsnCheckBox1.CheckedChanged += new System.EventHandler(this.AdminRstRsnCheckBox1_CheckedChanged);
            // 
            // configRstRsnCheckBox1
            // 
            this.configRstRsnCheckBox1.AutoCheck = false;
            this.configRstRsnCheckBox1.AutoSize = true;
            this.configRstRsnCheckBox1.Location = new System.Drawing.Point(6, 102);
            this.configRstRsnCheckBox1.Name = "configRstRsnCheckBox1";
            this.configRstRsnCheckBox1.Size = new System.Drawing.Size(139, 15);
            this.configRstRsnCheckBox1.TabIndex = 4;
            this.configRstRsnCheckBox1.Text = "Restart Reason Config";
            this.configRstRsnCheckBox1.UseStyleColors = true;
            this.configRstRsnCheckBox1.UseVisualStyleBackColor = true;
            this.configRstRsnCheckBox1.CheckedChanged += new System.EventHandler(this.ConfigRstRsnCheckBox1_CheckedChanged);
            // 
            // procModulesCheckBox1
            // 
            this.procModulesCheckBox1.AutoSize = true;
            this.procModulesCheckBox1.Location = new System.Drawing.Point(6, 81);
            this.procModulesCheckBox1.Name = "procModulesCheckBox1";
            this.procModulesCheckBox1.Size = new System.Drawing.Size(112, 15);
            this.procModulesCheckBox1.TabIndex = 3;
            this.procModulesCheckBox1.Text = "Process Modules";
            this.procModulesCheckBox1.UseStyleColors = true;
            this.procModulesCheckBox1.UseVisualStyleBackColor = true;
            this.procModulesCheckBox1.CheckedChanged += new System.EventHandler(this.ProcModulesCheckBox1_CheckedChanged);
            // 
            // clearLogsCheckBox1
            // 
            this.clearLogsCheckBox1.AutoSize = true;
            this.clearLogsCheckBox1.Location = new System.Drawing.Point(6, 60);
            this.clearLogsCheckBox1.Name = "clearLogsCheckBox1";
            this.clearLogsCheckBox1.Size = new System.Drawing.Size(78, 15);
            this.clearLogsCheckBox1.TabIndex = 2;
            this.clearLogsCheckBox1.Text = "Clear Logs";
            this.clearLogsCheckBox1.UseStyleColors = true;
            this.clearLogsCheckBox1.UseVisualStyleBackColor = true;
            this.clearLogsCheckBox1.CheckedChanged += new System.EventHandler(this.ClearLogsCheckBox1_CheckedChanged);
            // 
            // noClientCheckBox1
            // 
            this.noClientCheckBox1.AutoSize = true;
            this.noClientCheckBox1.Location = new System.Drawing.Point(6, 39);
            this.noClientCheckBox1.Name = "noClientCheckBox1";
            this.noClientCheckBox1.Size = new System.Drawing.Size(73, 15);
            this.noClientCheckBox1.TabIndex = 1;
            this.noClientCheckBox1.Text = "No Client";
            this.noClientCheckBox1.UseStyleColors = true;
            this.noClientCheckBox1.UseVisualStyleBackColor = true;
            this.noClientCheckBox1.CheckedChanged += new System.EventHandler(this.NoClientCheckBox1_CheckedChanged);
            // 
            // rstButton1
            // 
            this.rstButton1.Location = new System.Drawing.Point(191, 102);
            this.rstButton1.Name = "rstButton1";
            this.rstButton1.Size = new System.Drawing.Size(73, 23);
            this.rstButton1.TabIndex = 7;
            this.rstButton1.Text = "Restart";
            this.rstButton1.Click += new System.EventHandler(this.RstButton1_Click);
            // 
            // metroStyleManager1
            // 
            this.metroStyleManager1.Owner = this;
            // 
            // metroButton1
            // 
            this.metroButton1.Location = new System.Drawing.Point(191, 131);
            this.metroButton1.Name = "metroButton1";
            this.metroButton1.Size = new System.Drawing.Size(73, 23);
            this.metroButton1.TabIndex = 5;
            this.metroButton1.Text = "Update";
            this.metroButton1.Click += new System.EventHandler(this.MetroButton1_Click);
            // 
            // metroButton2
            // 
            this.metroButton2.Location = new System.Drawing.Point(191, 207);
            this.metroButton2.Name = "metroButton2";
            this.metroButton2.Size = new System.Drawing.Size(75, 23);
            this.metroButton2.TabIndex = 8;
            this.metroButton2.Text = "Console";
            this.metroButton2.Click += new System.EventHandler(this.MetroButton2_Click);
            // 
            // metroButton3
            // 
            this.metroButton3.Location = new System.Drawing.Point(191, 160);
            this.metroButton3.Name = "metroButton3";
            this.metroButton3.Size = new System.Drawing.Size(73, 23);
            this.metroButton3.TabIndex = 9;
            this.metroButton3.Text = "Donate";
            this.metroButton3.Click += new System.EventHandler(this.MetroButton3_Click);
            // 
            // InformationWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(287, 329);
            this.Controls.Add(this.metroButton3);
            this.Controls.Add(this.metroButton2);
            this.Controls.Add(this.rstButton1);
            this.Controls.Add(this.metroButton1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.boardsLink4);
            this.Controls.Add(this.gitLink3);
            this.Controls.Add(this.fbLink2);
            this.Controls.Add(this.ytLink1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InformationWindow";
            this.Resizable = false;
            this.ShowIcon = false;
            this.Text = "More Information";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.InformationWindow_FormClosed);
            this.Load += new System.EventHandler(this.InformationWindow_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.metroStyleManager1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private MetroFramework.Controls.MetroLink ytLink1;
        private MetroFramework.Controls.MetroLink fbLink2;
        private MetroFramework.Controls.MetroLink gitLink3;
        private MetroFramework.Controls.MetroLink boardsLink4;
        private System.Windows.Forms.GroupBox groupBox1;
        private MetroFramework.Controls.MetroCheckBox noClientCheckBox1;
        private MetroFramework.Controls.MetroCheckBox adminRstRsnCheckBox1;
        private MetroFramework.Controls.MetroCheckBox configRstRsnCheckBox1;
        private MetroFramework.Controls.MetroCheckBox procModulesCheckBox1;
        private MetroFramework.Controls.MetroCheckBox clearLogsCheckBox1;
        private MetroFramework.Controls.MetroButton rstButton1;
        private MetroFramework.Components.MetroStyleManager metroStyleManager1;
        private MetroFramework.Controls.MetroLabel metroLabel1;
        private MetroFramework.Controls.MetroButton metroButton1;
        private MetroFramework.Controls.MetroCheckBox exitEarlyCheckBox1;
        private MetroFramework.Controls.MetroCheckBox cleanCheckBox1;
        private MetroFramework.Controls.MetroButton metroButton2;
        private MetroFramework.Controls.MetroButton metroButton3;
    }
}
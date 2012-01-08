using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SSP4
{
    public partial class frmMain : Form
    {
        // #### Datamembers #### //
        private cSSP4Base moSSP4;
        private string mstrFilter = ""; // Stores the previous filter when radDark is selected.
        public bool mbIsClosing = false;


        public frmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Create an SSP4 Object
            moSSP4 = new cSSP4Base();
            moSSP4.SetParentForm(this);

            // Place limits or load the values for the form:

            // Limit the integration Up/Down Box
            nudIntegration.DecimalPlaces = 2;
            nudIntegration.Increment = (decimal)moSSP4.IntegrationTimeStepSize;
            nudIntegration.Minimum = (decimal) moSSP4.IntegrationTimeMin;
            nudIntegration.Maximum = (decimal) moSSP4.IntegrationTimeMax;

            // Now, setup the Gain Combo box:
            // Load the gain values
            foreach (int iValue in moSSP4.GainValues)
                cboGain.Items.Add(iValue);

            ReloadSettingsBoxes();
            txtStatus.Text = "Offline";
            menuConnect.Enabled = false;

            // Init the Filter Combo box (TODO: Pull the filters from the database)
            cboFilter.Items.Add("Clear");
            cboFilter.Items.Add("Dark");
            cboFilter.Items.Add("H");
            cboFilter.Items.Add("J");
            cboFilter.SelectedIndex = 0;
        }

       

        private void setupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Load the form, setup a few items.
            SettingsForm frmSettings = new SettingsForm();
            frmSettings.CommPort = moSSP4.COMMPort;
            frmSettings.Temperature = moSSP4.Temperature;
            frmSettings.ShowDialog();
            
            // If the user clicks "OK" on the window, pull out the setting changes.
            if (frmSettings.DialogResult == DialogResult.OK)
            {
                moSSP4.COMMPort = frmSettings.CommPort;
                moSSP4.Temperature = frmSettings.Temperature;

                // Reload the settings boxes
                ReloadSettingsBoxes();

                UpdateConnectOption();
            }
        }

        private void UpdateConnectOption()
        {
            if (moSSP4.COMMPort.Length > 0 && moSSP4.Savefile.Length > 0)
                menuConnect.Enabled = true;
            else
                menuConnect.Enabled = false;
        }

        public void UpdateTemperature(double dTemp)
        {
            txtTemp.Text = dTemp.ToString();
        }

        public void UpdateSSP4Status()
        {
            // If the unit is online, make a few changes to the interface.
            if (moSSP4.Online)
            {
                txtStatus.Text = "Online";
                menuConnect.Text = "Disconnect";
                newToolStripMenuItem.Enabled = false;
                closeToolStripMenuItem.Enabled = false;
                openToolStripMenuItem1.Enabled = false;
            }
            else
            {
                txtStatus.Text = "Offline";
                menuConnect.Text = "Connect";
                closeToolStripMenuItem.Enabled = true;
            }

            if (!(moSSP4.Scanning))
                ToggleControls(true);
            else
                ToggleControls(false);
        }

        public void ReloadSettingsBoxes()
        {
            cboGain.SelectedItem = moSSP4.Gain;
            txtCommPort.Text = moSSP4.COMMPort;

        }

        /////////////////////////////////
        // Delegate Functions:
        /////////////////////////////////

        // A method and delegate for setting the window text.
        //delegate void CommandWindowDelegate(string strText);
        public void SetCommandWindowText(string strText)
        {
            lstCommandWindow.Items.Add(strText);
            lstCommandWindow.SelectedIndex = (lstCommandWindow.Items.Count - 1);
        }

        // A method to signal the scan status bar
        public void IncrementProgressBar()
        {
            progbarScanStatus.PerformStep();
        }

        // A method to reset/clear the status bar
        public void ResetProgressBar()
        {
            progbarScanStatus.Value = 0;
        }


        public void ToggleControls(bool bEnableUI)
        {
            // Only enable the controls if the SSP-4 is online.
            bool bEnable = moSSP4.Online & bEnableUI;

            cboGain.Enabled = bEnable;

            // Only enable the target name box if neither the dark or sky box are checked.
            if (!(radDark.Checked) || !(radSky.Checked))
            {
                txtTargetName.Enabled = bEnable;
            }

            // Only enable the filter combo box if the dark checkbox is not selected.s
            if(!(radDark.Checked))
                cboFilter.Enabled = bEnable;
            
            nudExposures.Enabled = bEnable;
            nudIntegration.Enabled = bEnable;
            radCalibrator.Enabled = bEnable;
            radTarget.Enabled = bEnable;
            radSky.Enabled = bEnable;
            radDark.Enabled = bEnable;

            // The comment box and button are always avaliable as long as the SSP-4 is online.
            txtComment.Enabled = moSSP4.Online;
            btnComment.Enabled = moSSP4.Online;

            // The Scan and Stop buttons are always enabled when the SSP-4 is online.
            btnScan.Enabled = moSSP4.Online;
            btnStop.Enabled = moSSP4.Online;
            if (moSSP4.Online)
            {
                if (bEnable)
                {
                    btnScan.Enabled = true;
                    btnScan.Text = "Scan";
                }
                else
                    btnScan.Enabled = false;
            }
        }

        private void menuConnect_Click(object sender, EventArgs e)
        {
            if (!(moSSP4.Online))
            {
                moSSP4.Connect();
                closeToolStripMenuItem.Enabled = false;
            }
            else
            {
                moSSP4.Disconnect();
                closeToolStripMenuItem.Enabled = true;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Close the connection
            mbIsClosing = true;
            moSSP4.Disconnect();
            moSSP4.CloseFile();
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            // Determine the number of exposures
            int iExposures = Convert.ToInt32(nudExposures.Value);
            progbarScanStatus.Step = Convert.ToInt32(100 / iExposures);

            ToggleControls(false);
            moSSP4.Scan(Convert.ToInt32(nudExposures.Value));
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (moSSP4.Scanning)
            {
                moSSP4.Stop();
                btnScan.Text = "Waiting...";
                btnScan.Enabled = false;
                btnStop.Enabled = false;
            }
        }

        private void nudIntegration_ValueChanged(object sender, EventArgs e)
        {
            moSSP4.IntegrationTime = Convert.ToDouble(nudIntegration.Value);
        }

        private void cboGain_SelectedIndexChanged(object sender, EventArgs e)
        {
            moSSP4.Gain = Convert.ToInt32(cboGain.SelectedItem);
        }


        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog FileUI = new OpenFileDialog();
            FileUI.Filter = "L0 files (*.L0)|*.L0|All files (*.*)|*.*";
            FileUI.FilterIndex = 1;


            if (FileUI.ShowDialog() == DialogResult.OK)
            {
                moSSP4.OpenFile(FileUI.FileName, true);

                // Now, open the observer information UI to prompt the user for additional information
                frmObsInfo ObsUI = new frmObsInfo();
                if (ObsUI.ShowDialog() == DialogResult.OK)
                {
                    moSSP4.ObserverName = ObsUI.Observer;
                    moSSP4.TelescopeName = ObsUI.TelescopeName;
                    moSSP4.Conditions = ObsUI.Conditions;

                    moSSP4.WriteObservingInfo();
                }

                btnScan.Enabled = true;
                closeToolStripMenuItem.Enabled = true;

                UpdateConnectOption();
                ToggleControls(true);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog FileUI = new SaveFileDialog();
            FileUI.Filter = "L0 files (*.L0)|*.L0|All files (*.*)|*.*";

            if (FileUI.ShowDialog() == DialogResult.OK)
            {
                // The file has been selected, tell the software to open the file
                moSSP4.OpenFile(FileUI.FileName, false);

                // Now, open the observer information UI to prompt the user for additional information
                frmObsInfo ObsUI = new frmObsInfo();
                if (ObsUI.ShowDialog() == DialogResult.OK)
                {
                    moSSP4.ObserverName = ObsUI.Observer;
                    moSSP4.TelescopeName = ObsUI.TelescopeName;
                    moSSP4.Conditions = ObsUI.Conditions;

                    moSSP4.WriteObservingInfo();
                }

                btnScan.Enabled = true;
                closeToolStripMenuItem.Enabled = true;

                UpdateConnectOption();
                ToggleControls(true);
            }
            
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            moSSP4.CloseFile();
            closeToolStripMenuItem.Enabled = false;
            newToolStripMenuItem.Enabled = true;
            openToolStripMenuItem1.Enabled = true;
            ToggleControls(false);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void txtTargetName_TextChanged(object sender, EventArgs e)
        {
            moSSP4.SetTargetName(txtTargetName.Text);
        }

        private void cboFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboFilter.SelectedItem != null)
                moSSP4.Filter = cboFilter.SelectedItem.ToString();
        }

        private void btnComment_Click(object sender, EventArgs e)
        {
            if (txtComment.Text.Length > 0)
            {
                moSSP4.SendComment(txtComment.Text);
                txtComment.Clear();
            }
        }

        private void radTarget_CheckedChanged(object sender, EventArgs e)
        {
            if (radTarget.Checked)
            {
                moSSP4.ObjectRole = ObjectRoles.Target;
                moSSP4.SetTargetName(txtTargetName.Text);
            }
        }

        private void radCalibrator_CheckedChanged(object sender, EventArgs e)
        {
            if (radCalibrator.Checked)
            {
                moSSP4.ObjectRole = ObjectRoles.Calibrator;
                moSSP4.SetTargetName(txtTargetName.Text);
            }
        }

        private void radSky_CheckedChanged(object sender, EventArgs e)
        {
            if (radSky.Checked)
            {
                EnableObjectDialog(false);
                moSSP4.ObjectRole = ObjectRoles.Sky;
                moSSP4.SetTargetName("Sky");
            }
            else
            {
                EnableObjectDialog(true);
            }
            
        }

        private void radDark_CheckedChanged(object sender, EventArgs e)
        {
            if (radDark.Checked)
            {
                EnableObjectDialog(false);
                moSSP4.ObjectRole = ObjectRoles.Dark;
                moSSP4.SetTargetName("Dark");
                mstrFilter = cboFilter.Text;
                cboFilter.Text = "Dark";
                cboFilter.Enabled = false;
            }
            else
            {
                cboFilter.Text = mstrFilter;
                cboFilter.Enabled = true;
                EnableObjectDialog(true);
            }

        }

        // Enable/Disable the Target Object textbox. 
        private void EnableObjectDialog(bool bEnable)
        {
            txtTargetName.Enabled = bEnable;

            // If the box is to be enabled, inform the SSP-4 of the target object's name.
            if(bEnable)
                moSSP4.SetTargetName(txtTargetName.Text);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAboutBox frmAbout = new frmAboutBox();
            frmAbout.ShowDialog();
        }

    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SSP4
{
    public partial class SettingsForm : Form
    {
        private string mstrCommPort;
        private int miTemperature;

        public SettingsForm()
        {
            mstrCommPort = "";
            miTemperature = 0;
            InitializeComponent();
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
            // Setup form items:

            // Populate the Combo box with valid Ports
            foreach (string strCommPort in System.IO.Ports.SerialPort.GetPortNames())
                cboCommPorts.Items.Add(strCommPort);

            if (cboCommPorts.Items.Count > 0)
            {
                if (mstrCommPort.Length > 0)
                    cboCommPorts.SelectedItem = mstrCommPort;
                else
                {
                    cboCommPorts.SelectedIndex = 0;
                    mstrCommPort = cboCommPorts.SelectedItem.ToString();
                }
            }
            else  // No comm ports were detected, notify the user and prohibit them from continuing.
            {
                System.Windows.Forms.MessageBox.Show("No COMM ports were detected on your computer.  Please verify that a COMM port exists on your system.");
                btnOK.Enabled = false;
            }

            // Set the limits on the numeric up-down box
            nudTemperature.Minimum = -40;
            nudTemperature.Maximum = 0;
            nudTemperature.Increment = 1;
            nudTemperature.Value = miTemperature;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }


        private void cboCommPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            mstrCommPort = cboCommPorts.SelectedItem.ToString();
        }

        private void nudTemperature_ValueChanged(object sender, EventArgs e)
        {
            miTemperature = Convert.ToInt32(nudTemperature.Value);
        }

        // #### Methods #### //

        public string CommPort
        {
            get
            {
                return mstrCommPort;
            }
            set
            {
                mstrCommPort = value;
            }
        }

        public int Temperature
        {
            get
            {
                return miTemperature;
            }
            set
            {
                miTemperature = value;
            }
        }

    }
}
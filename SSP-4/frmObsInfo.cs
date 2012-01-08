using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SSP4
{
    public partial class frmObsInfo : Form
    {
        public frmObsInfo()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public string TelescopeName
        {
            get
            {
                return txtTelescope.Text;
            }
        }

        public string Observer
        {
            get
            {
                return txtObserver.Text;
            }
        }

        public string Conditions
        {
            get
            {
                return txtConditions.Text;
            }
        }
    }
}
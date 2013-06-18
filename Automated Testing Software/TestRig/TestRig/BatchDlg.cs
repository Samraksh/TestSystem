using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestRig
{
    public partial class BatchDlg : Form
    {
        public string batchName { get; private set; }
        public string batchDesc { get; private set; }

        public BatchDlg()
        {
            InitializeComponent();
            
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            batchName = this.tbName.Text;
            batchDesc = this.tbDesc.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}

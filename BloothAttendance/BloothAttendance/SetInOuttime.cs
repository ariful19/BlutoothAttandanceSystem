using System;
using System.Windows.Forms;

namespace BloothAttendance
{
    public partial class SetInOuttime : Form
    {
        public SetInOuttime()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SetInOuttime_Load(object sender, EventArgs e)
        {
            dtpIn.Value = dtpOut.Value = DateTime.Now;
        }

        private void dtpIn_ValueChanged(object sender, EventArgs e)
        {
            if (dtpOut.Enabled==false)
            {
                dtpOut.Value = dtpIn.Value;
            }
        }
    }
}

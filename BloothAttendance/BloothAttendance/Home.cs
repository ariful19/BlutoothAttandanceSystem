using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BloothAttendance
{
    public partial class Home : Form
    {
        public Home()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            new Register().Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            new Form1().Show();
        }

        private void Home_Load(object sender, EventArgs e)
        {
            AcceptButton = button3;
        }
    }
}

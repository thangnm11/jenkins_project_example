using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication.Net40
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Text = String.Format("Today is [{0}]", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        }

        private void btnCheckNetFramework_Click(object sender, EventArgs e)
        {
            MessageBox.Show(String.Format("WindowsFormsApplication is [{0}]", "Net40"));
        }
    }
}

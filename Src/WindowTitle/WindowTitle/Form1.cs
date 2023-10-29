using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace WindowTitle
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            textBox2.Text = GetWindowTitle(Int32.Parse(textBox1.Text));
        }
        public string GetWindowTitle(int processId)
        {
            return Process.GetProcessById(processId).MainWindowTitle;
        }
    }
}
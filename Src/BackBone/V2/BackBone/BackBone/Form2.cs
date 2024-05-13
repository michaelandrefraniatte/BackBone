using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PromptHandle
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        private static Form2 prompt = new Form2();
        private void Form2_Load(object sender, EventArgs e)
        {
        }
        private void Form2_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        private static void OnKeyDown(Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                const string message = "• Author: Michaël André Franiatte.\n\r\n\r• Contact: michael.franiatte@gmail.com.\n\r\n\r• Publisher: https://github.com/michaelandrefraniatte.\n\r\n\r• Copyrights: All rights reserved, no permissions granted.\n\r\n\r• License: Not open source, not free of charge to use.";
                const string caption = "About";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        public static async Task<String> ShowDialog(string caption, string text, string record, List<string> listrecords)
        {
            return await Task.Run(() =>
            {
                prompt.Width = 360;
                prompt.Height = 140;
                prompt.Text = caption;
                Label textLabel = new Label() { Left = 16, Top = 20, Width = 316, Text = text };
                ComboBox cmbx = new ComboBox() { Left = 16, Top = 44, Width = 316, Text = record };
                foreach (string listrecord in listrecords)
                {
                    cmbx.Items.Add(listrecord);
                }
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(cmbx);
                prompt.StartPosition = FormStartPosition.CenterScreen;
                prompt.ShowDialog();
                return string.Format(cmbx.Text);
            });
        }
    }
}
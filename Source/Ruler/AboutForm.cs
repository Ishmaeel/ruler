using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ruler
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            richTextBox.Text = richTextBox.Text.Replace("{VERSION}", Application.ProductVersion);

            pictureBox.Image = SystemIcons.Information.ToBitmap();
        }

        private void RichTextBox_Enter(object sender, EventArgs e)
        {
            //HACK: Prevent the I-Beam cursor from appearing.
            ActiveControl = null;
        }

        private void RichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }
    }
}

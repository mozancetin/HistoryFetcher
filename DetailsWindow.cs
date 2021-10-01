using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HistoryFetcher
{
    public partial class DetailsWindow : Form
    {
        private string title;
        private string url;

        public DetailsWindow(string title, string url)
        {
            InitializeComponent();
            this.title = title;
            this.url = url;
            label2.Text = this.title;
            linkLabel1.Text = this.url;
            linkLabel1.Links.Add(0, this.url.Length, this.url);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.url);
            MessageBox.Show("Url copied!");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(this.url);
        }
    }
}

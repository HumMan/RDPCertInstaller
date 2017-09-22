using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RDPCertInstaller
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            textBox3.Text = "Please select the \"Key file\" and \"Cert file\" ..."+Environment.NewLine;
        }        

        private void button3_Click(object sender, EventArgs e)
        {
            var certPath = Path.GetTempFileName();
            var pfxMaker = new PfxMaker(UpdateLog);
            pfxMaker.MakePfx(textBox1.Text, textBox2.Text, certPath);
            bool registryThumbprintChanged;
            var rdpInstaller = new RDPCertInstaller(UpdateLog);
            rdpInstaller.SetupRdpWithCert(certPath, out registryThumbprintChanged);
        }

        private void UpdateLog(string text)
        {
            textBox3.AppendText(text + Environment.NewLine);
            textBox3.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = openFileDialog2.FileName;
            }
        }
    }
}

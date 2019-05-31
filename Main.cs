using System;
using System.IO;
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
            try
            {
                var pfxMaker = new PfxMaker(UpdateLog);
                pfxMaker.MakePfx(textBox1.Text, textBox2.Text, certPath);
                var rdpInstaller = new RDPCertInstaller(UpdateLog);
                rdpInstaller.SetupRdpWithCert(certPath, out bool registryThumbprintChanged);
            }
            catch(Exception ex)
            {
                UpdateLog(ex.ToString());
            }
            finally
            {
                File.Delete(certPath);
            }
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

        private void button4_Click(object sender, EventArgs e)
        {
            var certPath = "cert.pfx";
            try
            {
                var pfxMaker = new PfxMaker(UpdateLog);
                pfxMaker.MakePfx(textBox1.Text, textBox2.Text, certPath);
            }
            catch (Exception ex)
            {
                UpdateLog(ex.ToString());
            }
        }
    }
}

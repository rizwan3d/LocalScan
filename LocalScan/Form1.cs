using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalScan
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        public string GetMacAddress(string ipAddress)
        {
            string macAddress = string.Empty;
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = "arp";
            pProcess.StartInfo.Arguments = "-a " + ipAddress;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();
            string strOutput = pProcess.StandardOutput.ReadToEnd();
            string[] substrings = strOutput.Split('-');
            if (substrings.Length >= 8)
            {
                macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2))
                         + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6]
                         + "-" + substrings[7] + "-"
                         + substrings[8].Substring(0, 2);
                return macAddress;
            }

            else
            {
                return "not found";
            }
        }

        public string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch (Exception)
            {
                //unknown host or
                //not every IP has a name
                //log exception (manage it)
            }

            return null;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }

        private void startScanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startScanToolStripMenuItem.Enabled = false;
            dataGridView1.Rows.Clear();
            new Task(() =>
            {

                string hostName = Dns.GetHostName();
                var v = Dns.GetHostEntry(hostName);
                var ips = /*Dns.GetHostByName(hostName)*/v.AddressList;
                this.BeginInvoke((Action)(() =>
                {
                    toolStripProgressBar1.Maximum = ips.Length * 255;
                }));
                Parallel.ForEach(ips, myIP => {  
                //string myIP = ips[0].ToString();
                var ii = myIP.MapToIPv4().ToString().Split('.');
                string ip = $"{ii[0]}.{ii[1]}.{ii[2]}.";
                //string ip = "192.168.10.";//
                bool net = CheckForInternetConnection();
                Parallel.For(0, 255,
                    i =>
                    {
                        string ss = $"{ip}{i}";
                        bool b = PingHost(ss);
                        if (b)
                        {
                            string mac = GetMacAddress(ss);
                            string name = GetHostName(ss);
                            string vendor = string.Empty;
                            if (net)
                            {
                                using (WebClient client = new WebClient())
                                {
                                    string s = client.DownloadString($"https://macvendors.co/api/vendorname/{mac}");
                                    if (!s.Contains("No"))
                                    {
                                        vendor = s;
                                    }
                                }
                            }
                            this.BeginInvoke((Action)(() =>
                            {
                               this.dataGridView1.Rows.Add($"{ss}", $"{mac}", $"{name}", $"{vendor}");
                            }));
                        }
                        this.BeginInvoke((Action)(() =>
                        {
                            this.toolStripProgressBar1.PerformStep();
                        }));
                        if (this.toolStripProgressBar1.Value >= this.toolStripProgressBar1.Maximum - 1)
                        {
                            this.BeginInvoke((Action)(() =>
                            {
                                startScanToolStripMenuItem.Enabled = true;
                                this.toolStripProgressBar1.Value = 0;
                            }));
                        }
                    });
                });
            }).Start();
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://macvendors.co/"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().Show();
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}

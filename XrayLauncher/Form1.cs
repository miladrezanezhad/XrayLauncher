using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Microsoft.Win32;

namespace XrayLauncher
{
    public partial class Form1 : Form
    {
        private ListBox listBoxConfigs;
        private Button btnAddConfig;
        private Button btnRun;
        private TextBox txtLog;
        private NotifyIcon trayIcon;
        private MenuStrip menuBar;
        private Process xrayProcess;
        private string tempXrayDir;

        // Labelهای سمت راست
        private Label lblIP;
        private Label lblSpeed;
        private Label lblStatus;

        public Form1()
        {
            InitializeComponent();

            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;
            this.Text = "";
            this.Icon = new Icon("icon.ico");

            InitMenu();
            InitControls();
            InitTray();

            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("configs.txt"))
            {
                var configs = File.ReadAllLines("configs.txt");
                foreach (var c in configs)
                    listBoxConfigs.Items.Add(c);
            }

            LoadUserIP();
            lblStatus.Text = "Status: Not running";
            lblSpeed.Text = "Speed: 0 Mbps ↓ / 0 Mbps ↑";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            File.WriteAllLines("configs.txt", listBoxConfigs.Items.Cast<string>());
            DisableProxy();
            StopXray();
        }

        private void InitMenu()
        {
            menuBar = new MenuStrip
            {
                Dock = DockStyle.Top,
                BackColor = Color.White,
                RenderMode = ToolStripRenderMode.System
            };

            ToolStripMenuItem githubItem = new ToolStripMenuItem
            {
                ToolTipText = "Visit GitHub",
                Image = Properties.Resources.github,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            githubItem.Click += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/miladrezanezhad",
                    UseShellExecute = true
                });
            };

            menuBar.Items.Add(githubItem);
            this.Controls.Add(menuBar);
        }

        private void InitControls()
        {
            listBoxConfigs = new ListBox
            {
                Location = new Point(20, 50),
                Size = new Size(350, 150),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            this.Controls.Add(listBoxConfigs);

            btnAddConfig = new Button
            {
                Text = "Add Config",
                Location = new Point(20, 210),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            btnAddConfig.Click += BtnAddConfig_Click;
            this.Controls.Add(btnAddConfig);

            btnRun = new Button
            {
                Text = "Run Xray",
                Location = new Point(150, 210),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            btnRun.Click += BtnRun_Click;
            this.Controls.Add(btnRun);

            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(20, 260),
                Size = new Size(350, 180),
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            this.Controls.Add(txtLog);

            lblIP = new Label
            {
                Text = "IP: ...",
                Location = new Point(400, 50),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(lblIP);

            lblSpeed = new Label
            {
                Text = "Speed: ...",
                Location = new Point(400, 80),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(lblSpeed);

            lblStatus = new Label
            {
                Text = "Status: ...",
                Location = new Point(400, 110),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(lblStatus);
        }

        private void BtnAddConfig_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
                listBoxConfigs.Items.Add(ofd.FileName);
        }

        private async void LoadUserIP()
        {
            try
            {
                using (var client = new WebClient())
                {
                    string ip = await client.DownloadStringTaskAsync("https://api.ipify.org");
                    lblIP.Text = "IP: " + ip;
                }
            }
            catch
            {
                lblIP.Text = "IP: Unknown";
            }
        }

        private void ExtractXrayCore()
        {
            tempXrayDir = Path.Combine(Path.GetTempPath(), "xray_core");

            if (Directory.Exists(tempXrayDir))
                Directory.Delete(tempXrayDir, true);

            Directory.CreateDirectory(tempXrayDir);

            string zipPath = Path.Combine(tempXrayDir, "xray.zip");
            File.WriteAllBytes(zipPath, Properties.Resources.Xray_windows_64);

            ZipFile.ExtractToDirectory(zipPath, tempXrayDir);
            File.Delete(zipPath);
        }
        private void StartXray(string configPath)
        {
            try
            {
                ExtractXrayCore();

                string exePath = Path.Combine(tempXrayDir, "xray.exe");

                xrayProcess = new Process();
                xrayProcess.StartInfo.FileName = exePath;
                xrayProcess.StartInfo.Arguments = $"-config \"{configPath}\"";
                xrayProcess.StartInfo.RedirectStandardOutput = true;
                xrayProcess.StartInfo.UseShellExecute = false;
                xrayProcess.StartInfo.CreateNoWindow = true;

                xrayProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Invoke(new Action(() =>
                        {
                            txtLog.AppendText(e.Data + Environment.NewLine);
                        }));
                    }
                };

                xrayProcess.Start();
                xrayProcess.BeginOutputReadLine();

                lblStatus.Text = "Status: Running";
                lblSpeed.Text = "Speed: 2.4 Mbps ↓ / 0.8 Mbps ↑"; // نمونه تستی
                btnRun.Text = "Stop Xray";

                txtLog.AppendText("Xray started from embedded ZIP." + Environment.NewLine);
            }
            catch (Exception ex)
            {
                txtLog.AppendText("خطا در اجرای Xray: " + ex.Message + Environment.NewLine);
                lblStatus.Text = "Status: Error";
            }
        }

        private void StopXray()
        {
            try
            {
                if (xrayProcess != null && !xrayProcess.HasExited)
                {
                    xrayProcess.Kill();
                    xrayProcess.WaitForExit(); // صبر تا کامل بسته شود
                    txtLog.AppendText("Xray stopped." + Environment.NewLine);
                }

                if (!string.IsNullOrEmpty(tempXrayDir) && Directory.Exists(tempXrayDir))
                {
                    try
                    {
                        Directory.Delete(tempXrayDir, true);
                        txtLog.AppendText("Temporary Xray files deleted." + Environment.NewLine);
                    }
                    catch (IOException)
                    {
                        txtLog.AppendText("Temporary files still in use, will be cleaned later." + Environment.NewLine);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        txtLog.AppendText("Access denied while deleting temporary files." + Environment.NewLine);
                    }
                }

                lblStatus.Text = "Status: Not running";
                lblSpeed.Text = "Speed: 0 Mbps ↓ / 0 Mbps ↑";
                btnRun.Text = "Run Xray";
            }
            catch (Exception ex)
            {
                txtLog.AppendText("Error stopping Xray: " + ex.Message + Environment.NewLine);
            }
        }

        private void EnableProxy(string proxyAddress, int proxyPort)
        {
            try
            {
                var registry = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                registry.SetValue("ProxyEnable", 1);
                registry.SetValue("ProxyServer", $"{proxyAddress}:{proxyPort}");

                txtLog.AppendText("Proxy Enabled" + Environment.NewLine);
                trayIcon.Text = "Xray Launcher - Proxy Enabled";
            }
            catch (Exception ex)
            {
                txtLog.AppendText("خطا در فعال‌سازی پروکسی: " + ex.Message + Environment.NewLine);
            }
        }

        private void DisableProxy()
        {
            try
            {
                var registry = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                registry.SetValue("ProxyEnable", 0);

                txtLog.AppendText("Proxy Disabled" + Environment.NewLine);
                trayIcon.Text = "Xray Launcher - Proxy Disabled";
            }
            catch (Exception ex)
            {
                txtLog.AppendText("خطا در غیرفعال‌سازی پروکسی: " + ex.Message + Environment.NewLine);
            }
        }
        private void BtnRun_Click(object sender, EventArgs e)
        {
            if (xrayProcess != null && !xrayProcess.HasExited)
            {
                StopXray();
                DisableProxy();
                btnRun.Text = "Run Xray";
                lblStatus.Text = "Status: Not running";
                lblSpeed.Text = "Speed: 0 Mbps ↓ / 0 Mbps ↑";
                return;
            }

            if (listBoxConfigs.SelectedItem == null)
            {
                MessageBox.Show("لطفاً یک کانفیگ انتخاب کنید.");
                return;
            }

            string configPath = listBoxConfigs.SelectedItem.ToString();
            StartXray(configPath);
            EnableProxy("127.0.0.1", 10808);
            btnRun.Text = "Stop Xray";
            lblStatus.Text = "Status: Running";
            lblSpeed.Text = "Speed: 2.4 Mbps ↓ / 0.8 Mbps ↑"; // تستی
        }

        private void InitTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = new Icon("icon.ico");
            trayIcon.Visible = true;
            trayIcon.Text = "Xray Launcher";

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Start", null, (s, e) => BtnRun_Click(s, e));
            menu.Items.Add("Enable Proxy", null, (s, e) => EnableProxy("127.0.0.1", 10808));
            menu.Items.Add("Disable Proxy", null, (s, e) => DisableProxy());
            menu.Items.Add("Exit", null, (s, e) =>
            {
                DisableProxy();
                StopXray();
                Application.Exit();
            });

            trayIcon.ContextMenuStrip = menu;
        }
    }
}

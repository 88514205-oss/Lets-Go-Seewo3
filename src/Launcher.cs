using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LetsGoSeewo3
{
    public class MainForm : Form
    {
        const string TITLE = "Let's_Go!Seewo3!";
        const string OFFICIAL_PAGE = "https://e.seewo.com/product/EasiNote3";

        // 希沃官方 OSS 直链（2019-08-30 发布版），必须带 Referer 否则 403
        const string OFFICIAL_URL = "https://oss1.seewo.com/software/SeewoYiJiaSoftWares/Easinote3/EasiNoteSetup_3.1.2.3606.exe";
        const string REFERER = "https://e.seewo.com/";
        const long EXPECTED_SIZE = 384671976L;

        private Label lblStatus;
        private Button btnMain;
        private Label lblHint;

        private string baseDir;
        private string easiNoteExe;
        private bool busy;

        public MainForm()
        {
            baseDir = Application.StartupPath;
            BuildUI();
            this.Load += new EventHandler(OnLoad);
        }

        void BuildUI()
        {
            this.Text = TITLE;
            this.ClientSize = new Size(500, 330);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 27, 33);
            this.Font = new Font("微软雅黑", 10f);

            Label t = new Label();
            t.Text = TITLE;
            t.Font = new Font("微软雅黑", 20f, FontStyle.Bold);
            t.ForeColor = Color.White;
            t.Location = new Point(20, 18);
            t.Size = new Size(460, 40);
            this.Controls.Add(t);

            lblStatus = new Label();
            lblStatus.Text = "正在检测...";
            lblStatus.Font = new Font("微软雅黑", 12f);
            lblStatus.ForeColor = Color.Silver;
            lblStatus.Location = new Point(22, 78);
            lblStatus.Size = new Size(456, 30);
            this.Controls.Add(lblStatus);

            btnMain = new Button();
            btnMain.Text = "启 动";
            btnMain.Location = new Point(20, 122);
            btnMain.Size = new Size(460, 90);
            btnMain.FlatStyle = FlatStyle.Flat;
            btnMain.FlatAppearance.BorderSize = 0;
            btnMain.BackColor = Color.FromArgb(0, 150, 100);
            btnMain.ForeColor = Color.White;
            btnMain.Font = new Font("微软雅黑", 20f, FontStyle.Bold);
            btnMain.Cursor = Cursors.Hand;
            btnMain.Click += new EventHandler(OnMainClick);
            this.Controls.Add(btnMain);

            lblHint = new Label();
            lblHint.Font = new Font("微软雅黑", 8.5f);
            lblHint.ForeColor = Color.FromArgb(130, 135, 145);
            lblHint.Location = new Point(22, 224);
            lblHint.Size = new Size(458, 88);
            this.Controls.Add(lblHint);
        }

        void Log(string m)
        {
            try { File.AppendAllText(Path.Combine(baseDir, "seewo3.log"), DateTime.Now.ToString("MM-dd HH:mm:ss  ") + m + Environment.NewLine, Encoding.UTF8); }
            catch { }
        }

        string FindEasiNote()
        {
            string[] c = new string[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), @"Seewo\EasiNote\Main\EasiNote.exe"),
                @"D:\Seewo\EasiNote\Main\EasiNote.exe",
                @"C:\Seewo\EasiNote\Main\EasiNote.exe",
                Path.Combine(baseDir, @"EasiNote\Main\EasiNote.exe")
            };
            foreach (string p in c) { try { if (File.Exists(p)) return p; } catch { } }
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Seewo\EasiNote"))
                {
                    if (k != null)
                    {
                        object o = k.GetValue("path");
                        if (o != null)
                        {
                            string p = Path.Combine(o.ToString(), @"Main\EasiNote.exe");
                            if (File.Exists(p)) return p;
                        }
                    }
                }
            }
            catch { }
            try
            {
                string[] roots = new string[] {
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
                };
                foreach (string root in roots)
                {
                    using (RegistryKey k = Registry.LocalMachine.OpenSubKey(root))
                    {
                        if (k == null) continue;
                        foreach (string n in k.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey s = k.OpenSubKey(n))
                                {
                                    if (s == null) continue;
                                    object o = s.GetValue("DisplayIcon");
                                    if (o == null) continue;
                                    string icon = o.ToString().Trim().Trim('"');
                                    int cm = icon.LastIndexOf(',');
                                    if (cm > 0) icon = icon.Substring(0, cm);
                                    if (icon.IndexOf("EasiNote.exe", StringComparison.OrdinalIgnoreCase) >= 0 && File.Exists(icon)) return icon;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        string FindLocalSetup()
        {
            try
            {
                foreach (string f in Directory.GetFiles(baseDir, "*.exe"))
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (n.IndexOf("easinote") >= 0 && n.IndexOf("setup") >= 0) return f;
                }
                // 缓存目录里下载过的
                string cached = Path.Combine(baseDir, "EasiNoteSetup_3.1.2.3606.exe");
                if (File.Exists(cached) && new FileInfo(cached).Length == EXPECTED_SIZE) return cached;
            }
            catch { }
            return null;
        }

        void ApplyFix()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Avalon.Graphics"))
                    if (k != null) k.SetValue("DisableHWAcceleration", 1, RegistryValueKind.DWord);
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(easiNoteExe))
                    using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
                        if (k != null) k.SetValue(easiNoteExe, "~ WIN7RTM DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String);
            }
            catch { }
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Seewo\EasiNote"))
                    if (k != null && k.GetValue("MachineID") == null)
                        k.SetValue("MachineID", Guid.NewGuid().ToString("N").ToUpper(), RegistryValueKind.String);
            }
            catch { }
        }

        void EnsureShortcut()
        {
            try
            {
                string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Let's_Go!Seewo3!.lnk");
                if (File.Exists(lnk)) return;
                Type ty = Type.GetTypeFromProgID("WScript.Shell");
                if (ty == null) return;
                object ws = Activator.CreateInstance(ty);
                object sc = ty.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, ws, new object[] { lnk });
                Type st = sc.GetType();
                st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { Application.ExecutablePath });
                st.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { Application.StartupPath });
                st.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { Application.ExecutablePath + ",0" });
                st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, sc, null);
            }
            catch { }
        }

        void RefreshState()
        {
            easiNoteExe = FindEasiNote();
            if (easiNoteExe != null)
            {
                lblStatus.Text = "● 白板3 已就绪";
                lblStatus.ForeColor = Color.FromArgb(85, 210, 130);
                btnMain.Text = "启 动";
                btnMain.BackColor = Color.FromArgb(0, 150, 100);
                lblHint.Text = "点上面按钮：自动打兼容配置 → 打开希沃白板3\n\n" + easiNoteExe;
                return;
            }
            lblStatus.Text = "● 没找到白板3";
            lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
            if (FindLocalSetup() != null)
            {
                btnMain.Text = "安 装 并 启 动";
                btnMain.BackColor = Color.FromArgb(150, 100, 0);
                lblHint.Text = "同目录已有安装包，点按钮静默安装并自动配置。";
            }
            else
            {
                btnMain.Text = "下 载 并 安 装";
                btnMain.BackColor = Color.FromArgb(55, 100, 155);
                lblHint.Text = "将从希沃官方服务器下载白板3（约 384 MB），\n装好会自动打兼容配置并启动。\n\n第一次用请耐心等下载完。";
            }
        }

        void OnLoad(object sender, EventArgs e)
        {
            EnsureShortcut();
            RefreshState();
        }

        void OnMainClick(object sender, EventArgs e)
        {
            if (busy) return;
            RefreshState();
            if (easiNoteExe != null) { Launch(); return; }
            string local = FindLocalSetup();
            if (local != null) { Install(local); return; }
            Download();
        }

        void Launch()
        {
            ApplyFix();
            Log("修复+启动: " + easiNoteExe);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(easiNoteExe);
                psi.UseShellExecute = true;
                psi.WorkingDirectory = Path.GetDirectoryName(easiNoteExe);
                Process.Start(psi);
                Application.Exit();
            }
            catch (Exception ex) { MessageBox.Show(this, "启动失败：" + ex.Message, TITLE); }
        }

        void Install(string setupPath)
        {
            busy = true;
            btnMain.Enabled = false;
            lblStatus.Text = "● 正在静默安装，请稍候...";
            lblStatus.ForeColor = Color.Silver;
            Application.DoEvents();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(setupPath, "/S");
                psi.UseShellExecute = true;
                psi.WorkingDirectory = Path.GetDirectoryName(setupPath);
                Process p = Process.Start(psi);
                if (p != null) p.WaitForExit(900000);
            }
            catch (Exception ex) { MessageBox.Show(this, "安装启动失败：" + ex.Message, TITLE); }
            System.Threading.Thread.Sleep(3000);
            busy = false;
            btnMain.Enabled = true;
            RefreshState();
            if (easiNoteExe != null) Launch();
            else
            {
                lblStatus.Text = "● 装完仍没找到";
                lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
                lblHint.Text = "可能被安全软件拦了。装好后重开本程序即可。";
            }
        }

        void Download()
        {
            string target = Path.Combine(baseDir, "EasiNoteSetup_3.1.2.3606.exe");
            busy = true;
            btnMain.Enabled = false;
            lblStatus.Text = "● 正在连接官方服务器...";
            lblStatus.ForeColor = Color.Silver;

            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            wc.Headers.Add("Referer", REFERER);

            wc.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs ev)
            {
                double gotMb = ev.BytesReceived / 1048576.0;
                double totMb = ev.TotalBytesToReceive / 1048576.0;
                lblStatus.Text = string.Format("● 下载中 {0}%   {1:F0}/{2:F0} MB", ev.ProgressPercentage, gotMb, totMb);
            };

            wc.DownloadFileCompleted += delegate(object s, AsyncCompletedEventArgs ev)
            {
                busy = false;
                btnMain.Enabled = true;
                bool ok = (ev.Error == null) && File.Exists(target);
                long len = 0;
                if (ok) { try { len = new FileInfo(target).Length; } catch { } }
                if (ok && len != EXPECTED_SIZE)
                {
                    ok = false;
                    try { File.Delete(target); } catch { }
                    MessageBox.Show(this, "下载的文件大小不对（" + len + " 字节），可能被拦截了。", TITLE);
                }
                if (!ok)
                {
                    if (ev.Error != null) Log("下载失败: " + ev.Error.Message);
                    lblStatus.Text = "● 下载失败";
                    lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
                    DialogResult r = MessageBox.Show(this,
                        "从官方服务器下载失败。\n\n可以试试：\n1. 打开官方产品页自己下（点是）\n2. 或者把 EasiNoteSetup_*.exe 放到本程序同目录\n\n原因：" + (ev.Error != null ? ev.Error.Message : "文件校验失败"),
                        TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r == DialogResult.Yes) { try { Process.Start(OFFICIAL_PAGE); } catch { } }
                    RefreshState();
                    return;
                }
                Log("下载完成: " + target + " (" + len + " 字节)");
                Install(target);
            };

            try { wc.DownloadFileAsync(new Uri(OFFICIAL_URL), target); }
            catch (Exception ex)
            {
                busy = false;
                btnMain.Enabled = true;
                MessageBox.Show(this, "下载启动失败：" + ex.Message, TITLE);
                RefreshState();
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
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

        // 下载源：按顺序尝试。官方已下架，保留做兜底；也可放同目录本地包
        static readonly string[] SOURCES = new string[] {
            "https://e.seewo.com/download/file?code=EasiNote3&&version=3.1.2.3606"
        };

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
            lblHint.Text = "";
            lblHint.Font = new Font("微软雅黑", 8.5f);
            lblHint.ForeColor = Color.FromArgb(130, 135, 145);
            lblHint.Location = new Point(22, 224);
            lblHint.Size = new Size(458, 88);
            this.Controls.Add(lblHint);
        }

        void Log(string m)
        {
            try { File.AppendAllText(Path.Combine(baseDir, "seewo3.log"), DateTime.Now.ToString("HH:mm:ss ") + m + Environment.NewLine, Encoding.UTF8); }
            catch { }
        }

        // ---------- 查找白板3 ----------
        string FindEasiNote()
        {
            string[] c = new string[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), @"Seewo\EasiNote\Main\EasiNote.exe"),
                @"D:\Seewo\EasiNote\Main\EasiNote.exe",
                @"C:\Seewo\EasiNote\Main\EasiNote.exe"
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
                string[] a = Directory.GetFiles(baseDir, "*.exe");
                foreach (string f in a)
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (n.IndexOf("easinote") >= 0 && n.IndexOf("setup") >= 0) return f;
                }
            }
            catch { }
            return null;
        }

        // ---------- 打兼容配置 ----------
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

        // ---------- 桌面快捷方式 ----------
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
                lblHint.Text = "点上面按钮：自动打兼容配置 → 打开希沃白板3\n\n程序位置：" + easiNoteExe;
            }
            else
            {
                lblStatus.Text = "● 没找到白板3";
                lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
                string local = FindLocalSetup();
                if (local != null)
                {
                    btnMain.Text = "安 装 并 启 动";
                    btnMain.BackColor = Color.FromArgb(150, 100, 0);
                    lblHint.Text = "同目录发现安装包：\n" + Path.GetFileName(local) + "\n点上面按钮静默安装并自动配置。";
                }
                else
                {
                    btnMain.Text = "下 载 并 安 装";
                    btnMain.BackColor = Color.FromArgb(55, 100, 155);
                    lblHint.Text = "希沃官方已下架白板3 的下载（产品页还在但没有安装包）。\n\n最简单：把 EasiNoteSetup_*.exe 放到本目录，\n再点上面的按钮，它就会自动装好并配置。";
                }
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
            TryDownload();
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
                psi.WorkingDirectory = baseDir;
                Process p = Process.Start(psi);
                if (p != null)
                {
                    if (!p.WaitForExit(600000)) { MessageBox.Show(this, "安装超时。", TITLE); }
                }
            }
            catch (Exception ex) { MessageBox.Show(this, "安装启动失败：" + ex.Message, TITLE); }
            System.Threading.Thread.Sleep(2500);
            busy = false;
            btnMain.Enabled = true;
            RefreshState();
            if (easiNoteExe != null) { Launch(); }
            else
            {
                lblStatus.Text = "● 安装后仍未找到";
                lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
                lblHint.Text = "可能安装被取消了，或被安全软件拦住。\n装好后重新打开本程序即可。";
            }
        }

        void TryDownload()
        {
            string target = Path.Combine(Path.GetTempPath(), "EasiNoteSetup_download.exe");
            busy = true;
            btnMain.Enabled = false;
            lblStatus.Text = "● 正在连接官方下载源...";
            lblStatus.ForeColor = Color.Silver;

            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            wc.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs ev)
            {
                lblStatus.Text = "● 下载中 " + ev.ProgressPercentage + "%";
            };
            wc.DownloadFileCompleted += delegate(object s, AsyncCompletedEventArgs ev)
            {
                busy = false;
                btnMain.Enabled = true;
                if (ev.Error != null || !File.Exists(target) || new FileInfo(target).Length < 1000000)
                {
                    lblStatus.Text = "● 官方源不可用";
                    lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
                    if (File.Exists(target)) { try { File.Delete(target); } catch { } }
                    DialogResult r = MessageBox.Show(this,
                        "希沃官方已经不再提供白板3 的下载了。\n\n" +
                        "要不要打开官方产品页看看？\n" +
                        "（更快的办法：把安装包 exe 放到本程序同目录，再点按钮）",
                        TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (r == DialogResult.Yes)
                    {
                        try { Process.Start(OFFICIAL_PAGE); } catch { }
                    }
                    RefreshState();
                    return;
                }
                Log("下载完成: " + target);
                Install(target);
            };
            try
            {
                wc.DownloadFileAsync(new Uri(SOURCES[0]), target);
            }
            catch (Exception ex)
            {
                busy = false;
                btnMain.Enabled = true;
                MessageBox.Show(this, "下载失败：" + ex.Message, TITLE);
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
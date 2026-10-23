using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LetsGoSeewo3
{
    public class MainForm : Form
    {
        const string TITLE = "Let's_Go!Seewo3!";
        const string OFFICIAL_PAGE = "https://e.seewo.com/product/EasiNote3";
        const string OFFICIAL_URL = "https://oss1.seewo.com/software/SeewoYiJiaSoftWares/Easinote3/EasiNoteSetup_3.1.2.3606.exe";
        const string REFERER = "https://e.seewo.com/";
        const long EXPECTED_SIZE = 384671976L;
        const string REPORT_URL = "http://38.244.14.65:17881/report";
        const string APP_VERSION = "1.0.0";

        private Label lblStatus;
        private Label lblPath;
        private Button btnFind, btnDownload, btnStart, btnTest, btnFeedback;
        private TextBox txtLog;

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
            this.ClientSize = new Size(480, 470);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(13, 13, 13);
            this.Font = new Font("微软雅黑", 10f);

            Label t = new Label();
            t.Text = TITLE;
            t.Font = new Font("微软雅黑", 18f, FontStyle.Bold);
            t.ForeColor = Color.White;
            t.Location = new Point(20, 16);
            t.Size = new Size(440, 36);
            this.Controls.Add(t);

            lblStatus = new Label();
            lblStatus.Text = "正在检测...";
            lblStatus.Font = new Font("微软雅黑", 11f);
            lblStatus.ForeColor = Color.FromArgb(190, 190, 190);
            lblStatus.Location = new Point(22, 58);
            lblStatus.Size = new Size(440, 24);
            this.Controls.Add(lblStatus);

            lblPath = new Label();
            lblPath.Text = "";
            lblPath.Font = new Font("微软雅黑", 7.5f);
            lblPath.ForeColor = Color.FromArgb(105, 105, 105);
            lblPath.Location = new Point(24, 82);
            lblPath.Size = new Size(438, 18);
            this.Controls.Add(lblPath);

            // 唯一带颜色的：启动
            btnStart = MakeBtn("启 动", 20, 110, 440, 64, Color.FromArgb(27, 160, 92), 18f);
            btnStart.Click += new EventHandler(OnStart);

            // 其余全灰
            btnFind = MakeBtn("寻 找", 20, 184, 214, 46, Color.FromArgb(38, 38, 38), 12f);
            btnFind.Click += new EventHandler(OnFind);

            btnDownload = MakeBtn("获 取 下 载 器", 246, 184, 214, 46, Color.FromArgb(38, 38, 38), 12f);
            btnDownload.Click += new EventHandler(OnDownload);

            btnTest = MakeBtn("测 试 环 境", 20, 240, 214, 46, Color.FromArgb(38, 38, 38), 12f);
            btnTest.Click += new EventHandler(OnTest);

            btnFeedback = MakeBtn("反 馈 问 题", 246, 240, 214, 46, Color.FromArgb(38, 38, 38), 12f);
            btnFeedback.Click += new EventHandler(OnFeedback);
            btnTest.Click += new EventHandler(OnTest);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Location = new Point(20, 302);
            txtLog.Size = new Size(440, 152);
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.FromArgb(125, 125, 125);
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Font = new Font("Consolas", 8.5f);
            this.Controls.Add(txtLog);
        }
        Button MakeBtn(string text, int x, int y, int w, int h, Color c) { return MakeBtn(text, x, y, w, h, c, 12f); }

        Button MakeBtn(string text, int x, int y, int w, int h, Color c, float fs)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = c;
            b.ForeColor = Color.White;
            b.Font = new Font("微软雅黑", fs, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            this.Controls.Add(b);
            return b;
        }

        void Log(string m)
        {
            txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss  ") + m + Environment.NewLine);
            try { File.AppendAllText(Path.Combine(baseDir, "seewo3.log"), DateTime.Now.ToString("MM-dd HH:mm:ss  ") + m + Environment.NewLine, Encoding.UTF8); }
            catch { }
        }

        bool IsAdmin()
        {
            try
            {
                using (WindowsIdentity id = WindowsIdentity.GetCurrent())
                    return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
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

        string ReadHWAccel()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Avalon.Graphics"))
                {
                    if (k == null) return "未设置";
                    object v = k.GetValue("DisableHWAcceleration");
                    if (v == null) return "未设置";
                    return (Convert.ToInt32(v) == 1) ? "已关闭（正确）" : "未关闭（会导致卡100%）";
                }
            }
            catch { return "读取失败"; }
        }

        string ReadCompat()
        {
            try
            {
                if (string.IsNullOrEmpty(easiNoteExe)) return "无程序，跳过";
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
                {
                    if (k == null) return "未设置";
                    object v = k.GetValue(easiNoteExe);
                    return v == null ? "未设置" : v.ToString();
                }
            }
            catch { return "读取失败"; }
        }

        string GetOSInfo()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k == null) return Environment.OSVersion.VersionString;
                    string build = k.GetValue("CurrentBuild") as string;
                    string disp = k.GetValue("DisplayVersion") as string;
                    string prod = k.GetValue("ProductName") as string;
                    int b = 0;
                    int.TryParse(build, out b);
                    string name = (b >= 22000) ? "Windows 11" : ((b >= 10240) ? "Windows 10" : "Windows " + build);
                    string s = name + "  Build " + build;
                    if (!string.IsNullOrEmpty(disp)) s += "  " + disp;
                    if (!string.IsNullOrEmpty(prod) && prod.IndexOf("Windows 1") < 0) s += "  (" + prod + ")";
                    return s;
                }
            }
            catch { return Environment.OSVersion.VersionString; }
        }

        string GetGPU()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (k == null) return "";
                    foreach (string sub in k.GetSubKeyNames())
                    {
                        int num;
                        if (!int.TryParse(sub, out num)) continue;
                        using (RegistryKey s = k.OpenSubKey(sub))
                        {
                            if (s == null) continue;
                            object desc = s.GetValue("DriverDesc");
                            if (desc == null) continue;
                            object ver = s.GetValue("DriverVersion");
                            sb.AppendLine("  " + desc + "  驱动 " + (ver == null ? "?" : ver.ToString()));
                        }
                    }
                }
            }
            catch { }
            return sb.ToString();
        }
        string JsonStr(string s)
        {
            if (s == null) s = "";
            StringBuilder b = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (c < 0x20) b.Append(string.Format("\\u{0:X4}", (int)c));
                        else b.Append(c);
                        break;
                }
            }
            return b.ToString();
        }

        string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            try
            {
                string u = Environment.UserName;
                if (!string.IsNullOrEmpty(u) && u.Length > 2) s = s.Replace(u, "<user>");
                string m = Environment.MachineName;
                if (!string.IsNullOrEmpty(m) && m.Length > 2) s = s.Replace(m, "<pc>");
            }
            catch { }
            return s;
        }

        string BuildReport(string evt, string err)
        {
            StringBuilder tail = new StringBuilder();
            try
            {
                string lp = Path.Combine(baseDir, "seewo3.log");
                if (File.Exists(lp))
                {
                    string[] lines = File.ReadAllLines(lp);
                    int start = Math.Max(0, lines.Length - 20);
                    for (int i = start; i < lines.Length; i++) tail.AppendLine(lines[i]);
                }
            }
            catch { }

            string gpu = "";
            try { gpu = GetGPU().Trim().Replace(Environment.NewLine, " / "); } catch { }

            StringBuilder j = new StringBuilder();
            j.Append("{");
            j.Append("\"event\":\"").Append(JsonStr(evt)).Append("\",");
            j.Append("\"error\":\"").Append(JsonStr(Sanitize(err))).Append("\",");
            j.Append("\"version\":\"").Append(APP_VERSION).Append("\",");
            j.Append("\"env\":{");
            j.Append("\"os\":\"").Append(JsonStr(GetOSInfo())).Append("\",");
            j.Append("\"x64\":").Append(Environment.Is64BitOperatingSystem ? "true" : "false").Append(",");
            j.Append("\"admin\":").Append(IsAdmin() ? "true" : "false").Append(",");
            j.Append("\"dotnet\":\"").Append(JsonStr(Environment.Version.ToString())).Append("\",");
            j.Append("\"baseDir\":\"").Append(JsonStr(Sanitize(baseDir))).Append("\",");
            j.Append("\"easinote\":\"").Append(JsonStr(Sanitize(easiNoteExe == null ? "(not found)" : easiNoteExe))).Append("\",");
            j.Append("\"hwaccel\":\"").Append(JsonStr(ReadHWAccel())).Append("\",");
            j.Append("\"compat\":\"").Append(JsonStr(ReadCompat())).Append("\",");
            j.Append("\"gpu\":\"").Append(JsonStr(Sanitize(gpu))).Append("\"");
            j.Append("},");
            j.Append("\"log\":\"").Append(JsonStr(Sanitize(tail.ToString()))).Append("\"");
            j.Append("}");
            return j.ToString();
        }

        void ReportAsync(string evt, string err)
        {
            string json = BuildReport(evt, err);
            System.Threading.Thread t = new System.Threading.Thread(delegate()
            {
                bool ok = false;
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(REPORT_URL);
                    req.Method = "POST";
                    req.ContentType = "application/json; charset=utf-8";
                    req.UserAgent = "LetsGoSeewo3/" + APP_VERSION;
                    req.Timeout = 10000;
                    req.ReadWriteTimeout = 10000;
                    req.ContentLength = data.Length;
                    using (Stream s = req.GetRequestStream()) { s.Write(data, 0, data.Length); }
                    using (WebResponse resp = req.GetResponse()) { }
                    ok = true;
                }
                catch { ok = false; }
                try
                {
                    Invoke(new MethodInvoker(delegate()
                    {
                        Log(ok ? "已上报错误报告 ✓" : "上报失败（不影响使用）");
                    }));
                }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        void OnFeedback(object sender, EventArgs e)
        {
            if (busy) return;
            DialogResult r = MessageBox.Show(this, "把当前环境信息和最近日志发给作者吗？\n（会自动去掉用户名和电脑名）", TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            ReportAsync("手动反馈", "老师手动点击了反馈按钮");
            MessageBox.Show(this, "已发送，谢谢！\n作者可以在报告页看到。", TITLE);
        }
        string CountCoreButtons()
        {
            try
            {
                int n = 0;
                foreach (string f in Directory.GetFiles(baseDir, "*.exe"))
                {
                    string n2 = Path.GetFileName(f).ToLowerInvariant();
                    if (n2.IndexOf("easinote") >= 0 && n2.IndexOf("setup") >= 0) n++;
                }
                return n.ToString();
            }
            catch { return "?"; }
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
                lblPath.Text = easiNoteExe;
                btnStart.BackColor = Color.FromArgb(0, 150, 100);
            }
            else
            {
                lblStatus.Text = "● 没找到白板3";
                lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
                string local = FindLocalSetup();
                lblPath.Text = local != null ? ("同目录有安装包：" + Path.GetFileName(local)) : "点「寻找」重试，或点「获取下载器」下载安装包";
                btnStart.BackColor = Color.FromArgb(60, 70, 80);
            }
        }

        void OnLoad(object sender, EventArgs e)
        {
            EnsureShortcut();
            RefreshState();
            Log("程序启动，位置=" + baseDir);
            Log("管理员权限: " + (IsAdmin() ? "是" : "否（不影响常规使用）"));
        }

        void OnFind(object sender, EventArgs e)
        {
            if (busy) return;
            Log("--- 正在寻找白板3 ---");
            RefreshState();
            if (easiNoteExe != null)
            {
                Log("找到: " + easiNoteExe);
                FileInfo fi = new FileInfo(easiNoteExe);
                Log("版本: " + fi.LastWriteTime.ToString("yyyy-MM-dd") + "  " + (fi.Length / 1024) + " KB");
            }
            else
            {
                Log("常见路径和注册表里都没找到。");
                if (FindLocalSetup() != null) Log("但同目录发现了安装包，可以点「获取下载器」旁边的安装流程。");
            }
        }

        void OnStart(object sender, EventArgs e)
        {
            if (busy) return;
            RefreshState();
            if (easiNoteExe == null || !File.Exists(easiNoteExe))
            {
                Log("启动失败：还没找到白板3。先点「寻找」或「获取下载器」。");
                return;
            }
            ApplyFix();
            Log("已打兼容配置：关闭硬件加速 + Win7 兼容模式");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(easiNoteExe);
                psi.UseShellExecute = true;
                psi.WorkingDirectory = Path.GetDirectoryName(easiNoteExe);
                Process.Start(psi);
                Log("已启动: " + easiNoteExe);
                Application.Exit();
            }
            catch (Exception ex) { Log("启动失败: " + ex.Message); ReportAsync("启动失败", ex.ToString()); }
        }

        void OnDownload(object sender, EventArgs e)
        {
            if (busy) return;
            string local = FindLocalSetup();
            if (local != null)
            {
                Log("同目录已有安装包: " + Path.GetFileName(local));
                DialogResult r = MessageBox.Show(this, "同目录已经有一份安装包了：\n" + Path.GetFileName(local) + "\n\n要直接安装它吗？\n（选否 = 重新下载最新官方版）",
                    TITLE, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) return;
                if (r == DialogResult.Yes) { Install(local); return; }
            }
            Download();
        }

        void Install(string setupPath)
        {
            busy = true;
            btnDownload.Enabled = false; btnStart.Enabled = false; btnFind.Enabled = false; btnTest.Enabled = false;
            lblStatus.Text = "● 正在静默安装，请稍候...";
            lblStatus.ForeColor = Color.Silver;
            Log("开始静默安装: " + setupPath);
            Application.DoEvents();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(setupPath, "/S");
                psi.UseShellExecute = true;
                psi.WorkingDirectory = Path.GetDirectoryName(setupPath);
                Process p = Process.Start(psi);
                if (p != null) p.WaitForExit(900000);
                Log("安装程序已退出");
            }
            catch (Exception ex) { Log("安装失败: " + ex.Message); ReportAsync("安装失败", ex.ToString()); }
            System.Threading.Thread.Sleep(3000);
            busy = false;
            btnDownload.Enabled = true; btnStart.Enabled = true; btnFind.Enabled = true; btnTest.Enabled = true;
            RefreshState();
            if (easiNoteExe != null)
            {
                Log("安装成功: " + easiNoteExe);
                ApplyFix();
                Log("已自动打好兼容配置，点「启动」即可使用。");
            }
            else
            {
                Log("装完仍未找到程序，可能被安全软件拦了。");
            }
        }

        void Download()
        {
            string target = Path.Combine(baseDir, "EasiNoteSetup_3.1.2.3606.exe");
            busy = true;
            btnDownload.Enabled = false; btnStart.Enabled = false; btnFind.Enabled = false; btnTest.Enabled = false;
            lblStatus.Text = "● 正在连接希沃官方服务器...";
            lblStatus.ForeColor = Color.Silver;
            Log("--- 开始从官方 OSS 下载 ---");
            Log("URL: " + OFFICIAL_URL);
            Log("Referer: " + REFERER);

            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            wc.Headers.Add("Referer", REFERER);

            wc.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs ev)
            {
                double got = ev.BytesReceived / 1048576.0;
                double tot = ev.TotalBytesToReceive / 1048576.0;
                lblStatus.Text = string.Format("● 下载中 {0}%    {1:F0}/{2:F0} MB", ev.ProgressPercentage, got, tot);
            };

            wc.DownloadFileCompleted += delegate(object s, AsyncCompletedEventArgs ev)
            {
                busy = false;
                btnDownload.Enabled = true; btnStart.Enabled = true; btnFind.Enabled = true; btnTest.Enabled = true;
                bool ok = (ev.Error == null) && File.Exists(target);
                long len = 0;
                if (ok) { try { len = new FileInfo(target).Length; } catch { } }
                if (ok && len != EXPECTED_SIZE)
                {
                    Log("大小校验失败: " + len + " 字节（期望 " + EXPECTED_SIZE + "）"); ReportAsync("下载校验失败", "got=" + len);
                    ok = false;
                    try { File.Delete(target); } catch { }
                }
                if (!ok)
                {
                    if (ev.Error != null) { Log("下载失败: " + ev.Error.Message); ReportAsync("下载失败", ev.Error.ToString()); }
                    lblStatus.Text = "● 下载失败";
                    lblStatus.ForeColor = Color.FromArgb(235, 125, 105);
                    DialogResult r = MessageBox.Show(this,
                        "从官方服务器下载失败。\n\n可选：\n1. 打开官方产品页自己下载（点「是」）\n2. 手动把 EasiNoteSetup_*.exe 放到本程序同目录\n\n详情看下方日志。",
                        TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r == DialogResult.Yes) { try { Process.Start(OFFICIAL_PAGE); } catch { } }
                    RefreshState();
                    return;
                }
                Log("下载完成: " + len + " 字节，校验通过");
                lblStatus.Text = "● 下载完成";
                lblStatus.ForeColor = Color.FromArgb(85, 210, 130);
                DialogResult rr = MessageBox.Show(this, "安装包下载完成（384 MB）。\n\n现在静默安装吗？", TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (rr == DialogResult.Yes) Install(target);
                else { Log("已取消安装，安装包保存在: " + target); RefreshState(); }
            };

            try { wc.DownloadFileAsync(new Uri(OFFICIAL_URL), target); }
            catch (Exception ex)
            {
                busy = false;
                btnDownload.Enabled = true; btnStart.Enabled = true; btnFind.Enabled = true; btnTest.Enabled = true;
                Log("下载启动失败: " + ex.Message);
                RefreshState();
            }
        }

        void OnTest(object sender, EventArgs e)
        {
            if (busy) return;
            Log("");
            Log("=== 环境检测 ===");
            Log("系统: " + GetOSInfo());
            Log("64位: " + (Environment.Is64BitOperatingSystem ? "是" : "否"));
            Log("管理员: " + (IsAdmin() ? "是" : "否"));
            Log(".NET: " + Environment.Version);
            Log("程序目录: " + baseDir);

            try
            {
                DriveInfo di = new DriveInfo(Path.GetPathRoot(baseDir));
                Log("所在盘剩余: " + (di.AvailableFreeSpace / 1073741824.0).ToString("F1") + " GB");
            }
            catch { }

            Log("白板3: " + (easiNoteExe != null ? easiNoteExe : "未找到"));
            Log("硬件加速: " + ReadHWAccel());
            Log("兼容模式: " + ReadCompat());

            // 显卡（读注册表，比 WMI 稳）
            try
            {
                string gpu = GetGPU();
                if (string.IsNullOrEmpty(gpu)) Log("显卡: 未读到");
                else Log("显卡:" + Environment.NewLine + gpu.TrimEnd());
            }
            catch (Exception gex) { Log("显卡: 查询失败 (" + gex.Message + ")"); }

            // 判断
            Log("--- 结论 ---");
            if (easiNoteExe == null)
                Log("❌ 白板3 未安装，先点「获取下载器」。");
            else
            {
                bool hw = ReadHWAccel().IndexOf("已关闭") >= 0;
                if (hw) Log("✅ 关键设置到位，点「启动」应该能正常进入。");
                else Log("⚠️ 硬件加速还没关，直接点「启动」会自动补上。");
            }
            Log("（这个面板的信息可以直接截图发给白糖）");
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
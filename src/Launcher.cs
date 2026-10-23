using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EasiNoteLauncher
{
    public class MainForm : Form
    {
        const string APP_TITLE = "希沃白板3 启动器";
        private Label lblStatus, lblPath, lblTip;
        private Button btnStart, btnFix, btnInstall;
        private CheckBox chkAuto;

        private string baseDir;
        private string easiNoteExe;

        public MainForm()
        {
            baseDir = Application.StartupPath;
            BuildUI();
            this.Load += new EventHandler(OnLoad);
        }

        void BuildUI()
        {
            this.Text = APP_TITLE;
            this.ClientSize = new Size(560, 424);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 34, 42);
            this.Font = new Font("微软雅黑", 10.5f);

            Label t = new Label();
            t.Text = APP_TITLE;
            t.Font = new Font("微软雅黑", 17f, FontStyle.Bold);
            t.ForeColor = Color.White;
            t.Location = new Point(22, 16);
            t.Size = new Size(510, 38);
            this.Controls.Add(t);

            lblStatus = new Label();
            lblStatus.Text = "正在查找白板3...";
            lblStatus.Font = new Font("微软雅黑", 11f);
            lblStatus.ForeColor = Color.Silver;
            lblStatus.Location = new Point(24, 62);
            lblStatus.Size = new Size(510, 26);
            this.Controls.Add(lblStatus);

            lblPath = new Label();
            lblPath.Text = "";
            lblPath.Font = new Font("微软雅黑", 8.5f);
            lblPath.ForeColor = Color.FromArgb(110, 150, 205);
            lblPath.Location = new Point(24, 88);
            lblPath.Size = new Size(512, 34);
            this.Controls.Add(lblPath);

            btnStart = MakeBtn("启 动 白 板 3", 24, 128, 512, 66, Color.FromArgb(0, 145, 95), 17f);
            btnStart.Click += new EventHandler(OnStart);

            btnFix = MakeBtn("修 复 设 置", 24, 206, 248, 54, Color.FromArgb(45, 88, 140), 13f);
            btnFix.Click += new EventHandler(OnFix);

            btnInstall = MakeBtn("重 新 查 找", 288, 206, 248, 54, Color.FromArgb(82, 74, 120), 13f);
            btnInstall.Click += new EventHandler(OnRescan);

            chkAuto = new CheckBox();
            chkAuto.Text = "启动前自动打一遍设置（强烈建议勾选）";
            chkAuto.ForeColor = Color.Silver;
            chkAuto.Font = new Font("微软雅黑", 10f);
            chkAuto.Location = new Point(26, 274);
            chkAuto.Size = new Size(500, 28);
            chkAuto.Checked = true;
            this.Controls.Add(chkAuto);

            lblTip = new Label();
            lblTip.Text = "";
            lblTip.Font = new Font("微软雅黑", 8.8f);
            lblTip.ForeColor = Color.FromArgb(145, 145, 155);
            lblTip.Location = new Point(26, 310);
            lblTip.Size = new Size(508, 100);
            this.Controls.Add(lblTip);
        }

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

        void Log(string msg)
        {
            try
            {
                File.AppendAllText(Path.Combine(baseDir, "launcher.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }

        string FindEasiNote()
        {
            string[] cands = new string[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Seewo\EasiNote\Main\EasiNote.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), @"Seewo\EasiNote\Main\EasiNote.exe"),
                @"D:\Seewo\EasiNote\Main\EasiNote.exe",
                @"C:\Seewo\EasiNote\Main\EasiNote.exe",
                Path.Combine(baseDir, @"EasiNote\Main\EasiNote.exe"),
                Path.Combine(baseDir, @"Seewo\EasiNote\Main\EasiNote.exe")
            };
            foreach (string c in cands) { try { if (File.Exists(c)) return c; } catch { } }

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
                        foreach (string name in k.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey sub = k.OpenSubKey(name))
                                {
                                    if (sub == null) continue;
                                    object o = sub.GetValue("DisplayIcon");
                                    if (o == null) continue;
                                    string icon = o.ToString().Trim().Trim('"');
                                    int comma = icon.LastIndexOf(',');
                                    if (comma > 0) icon = icon.Substring(0, comma);
                                    if (icon.IndexOf("EasiNote.exe", StringComparison.OrdinalIgnoreCase) >= 0 && File.Exists(icon))
                                        return icon;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

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
            return null;
        }

        void RefreshStatus()
        {
            easiNoteExe = FindEasiNote();
            if (easiNoteExe != null)
            {
                lblStatus.Text = "● 已找到白板3";
                lblStatus.ForeColor = Color.FromArgb(80, 205, 125);
                lblPath.Text = easiNoteExe;
                lblTip.Text = "程序在 C 盘没被还原，所以不用重装。\n每次点「启动」都会先把设置打一遍，再打开白板3。\n还原只影响设置，这一下就补回来了。";
                btnStart.Enabled = true;
                btnFix.Enabled = true;
            }
            else
            {
                lblStatus.Text = "● 没找到白板3";
                lblStatus.ForeColor = Color.FromArgb(235, 120, 100);
                lblPath.Text = "常见位置都没找到 EasiNote.exe";
                lblTip.Text = "如果白板3 装在别的路径，先手动打开一次确认能启动，\n再点「重新查找」。";
                btnStart.Enabled = false;
                btnFix.Enabled = false;
            }
        }

        void EnsureDesktopShortcut()
        {
            try
            {
                string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "启动 希沃白板3.lnk");
                if (File.Exists(lnk)) return;
                Type t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null) return;
                object ws = Activator.CreateInstance(t);
                object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, ws, new object[] { lnk });
                Type st = sc.GetType();
                st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { Application.ExecutablePath });
                st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { Application.StartupPath });
                st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { Application.ExecutablePath + ",0" });
                st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { "点这个启动白板3（会自动修复设置）" });
                st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
                Log("已重建桌面快捷方式");
            }
            catch { }
        }

        void OnLoad(object sender, EventArgs e)
        {
            Log("启动器打开，位置=" + baseDir);
            EnsureDesktopShortcut();
            RefreshStatus();
        }

        int ApplyFix()
        {
            int ok = 0;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Avalon.Graphics"))
                    if (k != null) { k.SetValue("DisableHWAcceleration", 1, RegistryValueKind.DWord); ok++; }
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(easiNoteExe))
                {
                    using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
                        if (k != null) { k.SetValue(easiNoteExe, "~ WIN7RTM DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String); ok++; }
                }
            }
            catch { }

            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Seewo\EasiNote"))
                    if (k != null && k.GetValue("MachineID") == null)
                        k.SetValue("MachineID", Guid.NewGuid().ToString("N").ToUpper(), RegistryValueKind.String);
            }
            catch { }

            Log("打设置: " + ok + "/2");
            return ok;
        }

        void OnFix(object sender, EventArgs e)
        {
            RefreshStatus();
            if (easiNoteExe == null) { MessageBox.Show(this, "还没找到白板3。", APP_TITLE); return; }
            int ok = ApplyFix();
            if (ok == 2) MessageBox.Show(this, "设置已打好。\n\n现在点「启动白板3」就行。", APP_TITLE);
            else MessageBox.Show(this, "设置有 " + (2 - ok) + " 项没成功，可能被安全软件拦了。\n可以试试右键「以管理员身份运行」本程序。", APP_TITLE);
        }

        void OnStart(object sender, EventArgs e)
        {
            RefreshStatus();
            if (easiNoteExe == null)
            {
                MessageBox.Show(this, "没找到白板3。\n请确认它已经装在这台电脑上。", APP_TITLE);
                return;
            }
            if (chkAuto.Checked) ApplyFix();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(easiNoteExe);
                psi.UseShellExecute = true;
                psi.WorkingDirectory = Path.GetDirectoryName(easiNoteExe);
                Process.Start(psi);
                Log("已启动: " + easiNoteExe);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "启动失败：" + ex.Message, APP_TITLE);
            }
        }

        void OnRescan(object sender, EventArgs e)
        {
            RefreshStatus();
            if (easiNoteExe != null) MessageBox.Show(this, "找到了：\n" + easiNoteExe, APP_TITLE);
            else MessageBox.Show(this, "还是没找到。\n\n可以试试右键「以管理员身份运行」本程序，\n或者告诉白糖白板3 装在哪里。", APP_TITLE);
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
# 希沃白板3 启动器 (EasiNote 3 Launcher)

一个 13 KB 的小工具，解决 **希沃白板3 在 Windows 11 上启动卡在"加载 100%"** 的问题。

## 这是什么问题

希沃白板3（EasiNote 3.1.x，2017 年的程序）是 WPF 应用，界面靠 DirectX 渲染。
在 Win11 的某些显卡/驱动组合上（尤其是核显、一体机），主窗口会创建失败，
表现就是**进度条读到 100% 后没有任何反应**。

解法有两个关键设置：

1. 关闭 WPF 硬件加速 → `HKCU\Software\Microsoft\Avalon.Graphics\DisableHWAcceleration = 1`
2. 给 `EasiNote.exe` 设置 Windows 7 兼容模式 + 禁用全屏优化

## 为什么要做成"启动器"

在带还原系统（还原卡 / 冰点还原）的机器上，C 盘的程序文件会保留，
但**注册表会被还原** —— 上面那两个设置每次重启都会消失，于是又卡住。

这个启动器每次启动前会把设置重新打一遍，再打开白板3，
就不用每次手动修了。

## 用法

1. 确保本机已安装希沃白板3（本工具**不包含**、也**不分发**希沃的任何程序）
2. 把 `希沃白板3启动器.exe` 放到不被还原的盘（比如 D 盘）
3. 双击 → 点「启动白板3」

启动器会自动查找白板3 的安装位置，第一次运行还会在桌面建一个快捷方式。

## 它改了什么

只写当前用户（HKCU），不需要管理员权限：

| 注册表项 | 作用 |
|---|---|
| `Software\Microsoft\Avalon.Graphics\DisableHWAcceleration` | 关闭 WPF 硬件加速 |
| `Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers` | Win7 兼容模式 + 禁用全屏优化 |
| `Software\Seewo\EasiNote\MachineID` | 补一个用户标识（仅在该值缺失时） |

不修改系统文件、不安装服务、不联网。

## 自己编译

需要 Windows 自带的 .NET Framework 编译器：

```
build.bat
```

本质就一行：

```
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:希沃白板3启动器.exe src\Launcher.cs
```

## 说明

- 本工具与希沃（Seewo / 广州视睿电子科技有限公司）无关，是第三方兼容性补丁
- 不包含、不分发希沃白板3 的任何文件，请通过正规渠道获取
- exe 没有数字签名，首次运行 Windows SmartScreen 可能提示，属于正常现象

## License

MIT
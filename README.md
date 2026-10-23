# Let's_Go!Seewo3!

一个 **22 KB** 的小工具，让 **希沃白板3** 在 Windows 11 一体机上正常启动，并且能从希沃官方服务器**自动下载安装**。

## 四个按钮，各管一件事

| 按钮 | 作用 |
|---|---|
| **寻找** | 扫描注册表 + 常见路径，定位白板3 装在哪 |
| **获取下载器** | 从希沃官方 OSS 下载安装包（384 MB）→ 校验 → 询问是否静默安装 |
| **启动** | 打兼容配置 → 打开白板3 |
| **测试环境** | 一键体检：系统版本、.NET、显卡、白板3、注册表配置、磁盘、结论 |

「测试环境」的输出可以直接截图发人，排查问题最快。

## 解决什么问题

希沃白板3（EasiNote 3.1.x，2017 年的 WPF 程序）在 Win11 的某些核显 / 驱动组合上，
主窗口创建失败，表现是 **进度条读到 100% 后卡死不动**。

根因：WPF 靠 DirectX 渲染，老程序 + 新驱动不兼容。

## 为什么必须"每次启动都打一遍"

教室电脑有还原系统。重启后：

- C 盘的程序文件 —— **保留**（不用重装）
- 注册表里的设置 —— **被还原**

所以每次重启后那两个关键设置会消失，白板3 又卡回 100%。
本工具每次点「启动」都会重新打一遍，就绕过了还原。

## 官方下载直链（重点）

希沃下载页 `https://e.seewo.com/product/EasiNote3` 已把白板3 的条目清空
（页面还在，但 `downloadUrl` 只剩白板5 的）。文件本身还在 OSS：

```
https://oss1.seewo.com/software/SeewoYiJiaSoftWares/Easinote3/EasiNoteSetup_3.1.2.3606.exe
```

**必须带 Referer，否则 403：**

| 请求 | 结果 |
|---|---|
| 不带 Referer | `403 Forbidden`（denied by Referer ACL） |
| 带 `Referer: https://e.seewo.com/` | `200`，`Content-Length: 384671976`，`Last-Modified: 2019-08-30` |

本工具在 `WebClient` 里设了 Referer，下载后还会校验文件大小必须等于 `384671976` 字节。

> 小坑：某些 CDN 节点的 TLS 配置会让 `curl` 握手失败（exit 35），
> 但 .NET 的 TLS 栈正常，不受影响。

## 改了哪些注册表

只写当前用户（HKCU），**不需要管理员权限**：

| 注册表项 | 作用 |
|---|---|
| `Software\Microsoft\Avalon.Graphics\DisableHWAcceleration` | 关闭 WPF 硬件加速（解决卡 100% 的关键） |
| `Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers` | Win7 兼容模式 + 禁用全屏优化 |
| `Software\Seewo\EasiNote\MachineID` | 补用户标识（仅在该值缺失时） |

## 编译

```
build.bat
```

只要系统自带的 .NET Framework 4.x。

## 免责说明

- 与希沃（Seewo / 广州视睿电子科技有限公司）无关，是第三方兼容性补丁
- 安装包从希沃官方 OSS 下载，**本仓库不包含、不分发任何希沃文件**
- exe 无数字签名，首次运行 SmartScreen 可能提示，属正常现象

## License

MIT
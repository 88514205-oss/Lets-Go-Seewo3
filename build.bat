@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [!] csc.exe not found. Please install .NET Framework 4.x first.
  pause
  exit /b 1
)
echo [*] Compiling with: %CSC%
"%CSC%" /nologo /target:winexe /codepage:65001 /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:"EasiNote3Launcher.exe" "src\Launcher.cs"
if errorlevel 1 (
  echo [!] Build failed.
  pause
  exit /b 1
)
echo [OK] Built: EasiNote3Launcher.exe
echo      ^(可以重命名为任意中文名，不影响运行^)
pause
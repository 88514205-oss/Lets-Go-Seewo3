@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [!] csc.exe not found. Install .NET Framework 4.x first.
  pause
  exit /b 1
)
echo [*] Compiling...
"%CSC%" /nologo /target:winexe /codepage:65001 /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:"Lets_GoSeewo3.exe" "src\Launcher.cs"
if errorlevel 1 ( echo [!] Build failed. & pause & exit /b 1 )
echo [OK] Built: Lets_GoSeewo3.exe
echo      Rename it to "Let's_Go!Seewo3!.exe" if you like.
pause
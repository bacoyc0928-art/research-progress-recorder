$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '找不到 .NET Framework C# 编译器 csc.exe' }
$sources = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
$outputPath = Join-Path $root '研究进展记录器.exe'
$iconPath = Join-Path $root 'app.ico'
$manifestPath = Join-Path $root 'app.manifest'
& $compiler /nologo /target:winexe "/out:$outputPath" "/win32icon:$iconPath" "/win32manifest:$manifestPath" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.VisualBasic.dll $sources
if ($LASTEXITCODE -ne 0) { throw "编译失败，退出代码 $LASTEXITCODE" }
Write-Host '编译完成：研究进展记录器.exe'

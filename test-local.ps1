param([string]$TestDirectory)
$ErrorActionPreference = 'Stop'
if (-not $TestDirectory) { throw 'Specify a new isolated -TestDirectory.' }
if (Test-Path -LiteralPath $TestDirectory) { throw 'Choose a fresh directory for isolated tests.' }
New-Item -ItemType Directory -Path $TestDirectory -Force | Out-Null
$TestDirectory = [IO.Path]::GetFullPath($TestDirectory)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:exe /main:Verify002 "/win32manifest:$PSScriptRoot\app.manifest" "/out:$TestDirectory\verify.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.VisualBasic.dll /r:System.Xml.dll $sources (Join-Path $PSScriptRoot 'verify-local.cs') (Join-Path $PSScriptRoot 'test-local.cs')
if ($LASTEXITCODE -ne 0) { throw 'Offline test compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'app.config') -Destination (Join-Path $TestDirectory 'verify.exe.config')
& (Join-Path $TestDirectory 'verify.exe')
if ($LASTEXITCODE -ne 0) { throw 'Offline verification failed.' }

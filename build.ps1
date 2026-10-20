param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = $sourceRoot }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Missing .NET Framework C# compiler.' }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
$buildDirectory = Join-Path $OutputDirectory ('.build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $buildDirectory | Out-Null
$appTemporary = Join-Path $buildDirectory 'SRPRecorder2027.exe'
$uninstallTemporary = Join-Path $buildDirectory '卸载SRPRecorder2027.exe'
& $compiler /nologo /target:winexe /main:SRLogbook.Program "/out:$appTemporary" "/win32icon:$sourceRoot\app.ico" "/win32manifest:$sourceRoot\app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.VisualBasic.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Main application compilation failed.' }
& $compiler /nologo /target:winexe /main:SRUninstall.Program "/out:$uninstallTemporary" /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $sourceRoot 'src\Uninstaller.cs') (Join-Path $sourceRoot 'src\Version.cs')
if ($LASTEXITCODE -ne 0) { throw 'Uninstaller compilation failed.' }
Copy-Item -LiteralPath $appTemporary -Destination (Join-Path $OutputDirectory 'SRPRecorder2027.exe') -Force
Copy-Item -LiteralPath $uninstallTemporary -Destination (Join-Path $OutputDirectory '卸载SRPRecorder2027.exe') -Force
foreach ($name in @('SRPRecorder2027.exe.config','卸载SRPRecorder2027.exe.config')) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'app.config') -Destination (Join-Path $OutputDirectory $name) -Force
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory '.srprec-install'), 'SRPRecorder2027 portable v1', [Text.UTF8Encoding]::new($false))
Write-Host "Built local candidate in $OutputDirectory"

param([string]$Destination, [switch]$IncludePrivateDefault)
$ErrorActionPreference = 'Stop'
if (-not $Destination) { throw 'Specify a new local output folder with -Destination.' }
$destinationRoot = [IO.Path]::GetFullPath($Destination)
$sourceRoot = [IO.Path]::GetFullPath($PSScriptRoot)
if ($destinationRoot.Equals($sourceRoot, [StringComparison]::OrdinalIgnoreCase) -or $destinationRoot.StartsWith($sourceRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be outside the live SRCRec installation.' }
if (Test-Path -LiteralPath $destinationRoot) { throw 'Choose a new output directory to avoid mixing old files.' }
$bundleRoot = Join-Path $destinationRoot 'SRCRec'
New-Item -ItemType Directory -Path $bundleRoot -Force | Out-Null
foreach ($file in @('app.ico','app.manifest','app.config','README.md','build.ps1','package-local.ps1','说明.md','说明.txt','分发与使用说明.md')) {
    if (Test-Path -LiteralPath (Join-Path $sourceRoot $file)) { Copy-Item -LiteralPath (Join-Path $sourceRoot $file) -Destination $bundleRoot }
}
Copy-Item -LiteralPath (Join-Path $sourceRoot 'src') -Destination (Join-Path $bundleRoot 'src') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot '卸载工具') -Destination (Join-Path $bundleRoot '卸载工具') -Recurse
New-Item -ItemType Directory -Path (Join-Path $bundleRoot 'References') -Force | Out-Null
if ($IncludePrivateDefault) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'References\Default') -Destination (Join-Path $bundleRoot 'References\Default') -Recurse
} else {
    New-Item -ItemType Directory -Path (Join-Path $bundleRoot 'References\Default') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $bundleRoot 'References\Default\README.txt'), 'Private team library: configure your own GitHub token, then Download Default Library. Research documents are not distributed in the public application package.', [Text.UTF8Encoding]::new($false))
}
New-Item -ItemType Directory -Path (Join-Path $bundleRoot 'PersonalWorkspaceSRRec') -Force | Out-Null
# No personal JSON, tokens, member downloads, logs or live progress files enter the candidate.
& (Join-Path $bundleRoot 'build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Candidate build failed.' }
$checksums = @(Get-FileHash -LiteralPath (Join-Path $bundleRoot 'SRPRecorder2027.exe'),(Join-Path $bundleRoot '卸载SRPRecorder2027.exe') -Algorithm SHA256 | ForEach-Object { $_.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_.Path) })
[IO.File]::WriteAllLines((Join-Path $bundleRoot 'SHA256SUMS.txt'), $checksums, [Text.UTF8Encoding]::new($false))
$archive = Join-Path $destinationRoot 'research-progress-recorder-0.1.0-Windows.zip'
# Explicit relative entry names preserve the single SRCRec root, the marker, and Unicode filenames.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($archive, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in (Get-ChildItem -LiteralPath $bundleRoot -File -Recurse -Force)) {
        $entryName = $file.FullName.Substring($destinationRoot.TrimEnd('\').Length + 1).Replace('\','/')
        if ($entryName -match '/\.build-[^/]+/') { continue }
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose() }
Get-FileHash -LiteralPath $archive -Algorithm SHA256

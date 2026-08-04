param(
    [string]$Runtime = "win-x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$artifactsDirectory = Join-Path $repositoryRoot "artifacts"
$releaseDirectory = Join-Path $artifactsDirectory "release"
$archivePath = Join-Path $artifactsDirectory "BaiduShareTool.zip"
$setupPath = Join-Path $artifactsDirectory "百度网盘分享链接助手安装程序.exe"
$legacySetupPath = Join-Path $artifactsDirectory "BaiduShareTool-Setup.exe"
$appProject = Join-Path $repositoryRoot "src\BaiduShareTool.App\BaiduShareTool.App.csproj"
$installerProject = Join-Path $PSScriptRoot "BaiduShareTool.Installer.csproj"
$iconScript = Join-Path $repositoryRoot "tools\Generate-AppIcon.ps1"
$dotnetInfo = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetCommand = if ($null -ne $dotnetInfo) { $dotnetInfo.Source } else { $null }

if (-not $dotnetCommand) {
    $dotnetCommand = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
}
if (-not (Test-Path $dotnetCommand)) {
    throw "The .NET SDK was not found."
}

Remove-Item -LiteralPath $releaseDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $archivePath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $setupPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $legacySetupPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null

& $iconScript
& $dotnetCommand publish $appProject --configuration Release --runtime $Runtime --self-contained true --output $releaseDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Compress-Archive -Path (Join-Path $releaseDirectory "*") -DestinationPath $archivePath -CompressionLevel Optimal -Force
& $dotnetCommand publish $installerProject --configuration Release --runtime $Runtime --self-contained true --output $artifactsDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Remove-Item -LiteralPath $archivePath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath ([System.IO.Path]::ChangeExtension($setupPath, ".pdb")) -Force -ErrorAction SilentlyContinue

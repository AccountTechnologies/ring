$IsCiBuild = $null -ne $env:TF_BUILD
$SrcPath = $IsCiBuild ? $env:SRC_PATH : "src/ATech.Ring.DotNet.Cli"
$NuGetSource = $IsCiBuild ? $env:BUILD_ARTIFACTSTAGINGDIRECTORY : "$SrcPath/bin/Release/"
$PkgVer = $IsCiBuild ? $env:PKGVER : ((dotnet run --project $SrcPath -- version 2>&1) -split ' ')[1]

function Get-ManifestDir
{
    param($Global = $false)
    $Global ? '.' : "$TestDrive"
}
function Get-ManifestPath
{
    param($Global = $false)
    "$(Get-ManifestDir -Global $Global)\.config\dotnet-tools.json"
}


function Install-Ring {

  param(
    [switch]$Global = $false
  )
  $ErrorActionPreference = "Stop"
  $ToolType = $Global ? '--global' : '--local'
  $ToolManifestPath = Get-ManifestDir -Global $Global

  if (!$IsCiBuild) {
    Write-Host "Running dotnet pack due to a local dev build"
    $NuGetPath = "($env:USERPROFILE)\.nuget\packages\atech.ring.dotnet.cli\$PkgVer"
    if (Test-Path  $NuGetPath) { Remove-Item $NuGetPath -r -force }
    dotnet pack $SrcPath -c Release
  }

  Write-Host "Installing ring (version: $PkgVer, $ToolType) from $NuGetSource"
  if (!$Global) {
      dotnet new tool-manifest --output "$ToolManifestPath"
  }
  Write-Host "dotnet tool install $ToolType ATech.Ring.DotNet.Cli --version $PkgVer --add-source $NuGetSource  --tool-manifest "$(Get-ManifestPath -Global $Global)" --configfile `"$PSScriptRoot/NuGet.config`""
  dotnet tool install $ToolType ATech.Ring.DotNet.Cli --version $PkgVer --add-source $NuGetSource --tool-manifest "$(Get-ManifestPath -Global $Global)" --configfile "$PSScriptRoot/NuGet.config"
  if ($LASTEXITCODE -ne 0) {
    throw "Cannot install ring"
  }
}

function Uninstall-Ring {
  param(
    [switch]$Global = $false
  )
  $ToolType = $Global ? '--global' : '--local'
  
  dotnet tool uninstall $ToolType ATech.Ring.DotNet.Cli --tool-manifest "$(Get-ManifestPath -Global $Global)"
}
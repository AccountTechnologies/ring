$IsCiBuild = $null -ne $env:TF_BUILD
$SrcPath = $IsCiBuild ? $env:SRC_PATH : "src/ATech.Ring.DotNet.Cli"
$NuGetSource = $IsCiBuild ? $env:BUILD_ARTIFACTSTAGINGDIRECTORY : "$SrcPath/bin/Release/"
$PkgVer = $IsCiBuild ? $env:PKGVER : ((dotnet run --project $SrcPath -- version 2>&1) -split ' ')[1]
$ManifestDir = $TestDrive
$ManifestFilePath = "$ManifestDir/.config/dotnet-tools.json"

function Install-Ring {

  param(
    [switch]$Global = $false
  )
  $ErrorActionPreference = "Stop"
  $ToolType = $Global ? '--global' : '--local'

  if (!$IsCiBuild) {
    Write-Host "Running dotnet pack due to a local dev build"
    $NuGetPath = "($env:USERPROFILE)\.nuget\packages\atech.ring.dotnet.cli\$PkgVer"
    if (Test-Path  $NuGetPath) { Remove-Item $NuGetPath -r -force }
    dotnet pack $SrcPath -c Release
  }

  Write-Host "Installing ring (version: $PkgVer, $ToolType) from $NuGetSource"
  if ($Global) {
    dotnet tool install --global ATech.Ring.DotNet.Cli --version $PkgVer --add-source $NuGetSource --configfile "$PSScriptRoot/NuGet.config"
  } 
  else 
  {
    dotnet new tool-manifest --output "$ManifestDir"
    dotnet tool install ATech.Ring.DotNet.Cli --version $PkgVer --add-source $NuGetSource --tool-manifest $ManifestFilePath --configfile "$PSScriptRoot/NuGet.config"
  }
  
  if ($LASTEXITCODE -ne 0) {
    throw "Cannot install ring"
  }
}

function Uninstall-Ring {
  param(
    [switch]$Global = $false
  )
  
  if ($Global) {
    dotnet tool uninstall --global ATech.Ring.DotNet.Cli
  }
  else {
    dotnet tool uninstall ATech.Ring.DotNet.Cli --tool-manifest $ManifestFilePath
  }
}

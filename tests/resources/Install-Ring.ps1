$IsCiBuild = $null -ne $env:TF_BUILD
$SrcPath = $IsCiBuild ? $env:SRC_PATH : "src/ATech.Ring.DotNet.Cli"
$NuGetSource = $IsCiBuild ? $env:BUILD_ARTIFACTSTAGINGDIRECTORY : "$SrcPath/bin/Release/"
$PkgVer = $IsCiBuild ? $env:PKGVER : ((dotnet run --project $SrcPath -- version 2>&1) -split ' ')[1]

function Install-Ring {

  param(
    [switch]$Global = $false
  )

  $ToolType = $Global ? "--global" : "--local"

  if (!$IsCiBuild) {
    Write-Host "Running dotnet pack due to a local dev build"
    dotnet pack $SrcPath -c Release
  }
  Write-Host "Installing ring (version: $PkgVer) from $NuGetSource"
  dotnet tool install $ToolType ATech.Ring.DotNet.Cli --version $PkgVer --add-source $NuGetSource --configfile "$PSScriptRoot/NuGet.config"
}

function Uninstall-Ring {
  param(
    [switch]$Global = $false
  )
  $ToolType = $Global ? "--global" : "--local"
  dotnet tool uninstall $ToolType ATech.Ring.DotNet.Cli
}
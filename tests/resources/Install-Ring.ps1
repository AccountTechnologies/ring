$IsCiBuild = $null -ne $env:TF_BUILD
$SrcPath = $IsCiBuild ? $env:SRC_PATH : "src/ATech.Ring.DotNet.Cli"
$NuGetSource = $IsCiBuild ? $env:BUILD_ARTIFACTSTAGINGDIRECTORY : "$SrcPath/bin/Release/"
$PkgVer = $IsCiBuild ? $env:PKGVER : ((dotnet run --project $SrcPath -- version 2>&1) -split ' ')[1]
$ManifestDir = $TestDrive

function Get-LocalManifestPath ($Global) {
  
  $Global ? '.' : "$ManifestDir\.config\dotnet-tools.json" 
}

function Install-Ring {

  param(
    [switch]$Global = $false
  )
  $ErrorActionPreference = "Stop"
  $ToolType = $Global ? '--global' : '--local'
  $ToolManifestPath = Get-LocalManifestPath $Global

  if (!$IsCiBuild) {
    Write-Host "Running dotnet pack due to a local dev build"
    dotnet pack $SrcPath -c Release
  }
  Write-Host "Installing ring (version: $PkgVer, $ToolType) from $NuGetSource"
  if (!$Global) {
    dotnet new tool-manifest --output "$ManifestDir"
  }
  Write-Host "dotnet tool install $ToolType ATech.Ring.DotNet.Cli --version $PkgVer --add-source $NuGetSource  --tool-manifest $ToolManifestPath --configfile `"$PSScriptRoot/NuGet.config`""
  dotnet tool install $ToolType ATech.Ring.DotNet.Cli --version $PkgVer --add-source $NuGetSource --tool-manifest $ToolManifestPath --configfile "$PSScriptRoot/NuGet.config"
  if ($LASTEXITCODE -ne 0) {
    throw "Cannot install ring"
  }
}

function Uninstall-Ring {
  param(
    [switch]$Global = $false
  )
  $ToolType = $Global ? '--global' : '--local'
  $ToolManifestPath = Get-LocalManifestPath $Global
  dotnet tool uninstall $ToolType ATech.Ring.DotNet.Cli --tool-manifest $ToolManifestPath
}
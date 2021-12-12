Install-Module -Name Pester -Force
Import-Module -Name Pester
$Container = New-PesterContainer -Path "$PSScriptRoot/All.Tests.ps1"

switch ($true) {
  $env:TF_BUILD {
    Invoke-Pester -Container $Container -OutputFile ./test-results.xml -OutputFormat 'NUnitXML'
  }
  default {
    Invoke-Pester -Container $Container -Output Detailed
  }
}

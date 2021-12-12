Install-Module -Name Pester -Force
Import-Module -Name Pester


switch ($true) {
  $env:TF_BUILD {
    Invoke-Pester -Script "./Ring.test.ps1" -OutputFile "./Test-Pester.XML" -OutputFormat NUnitXML
  }
  default {
    $Container = New-PesterContainer -Path "$PSScriptRoot/Ring.test.ps1"
    Invoke-Pester -Container $Container -Output Detailed
  }
}

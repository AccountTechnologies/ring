Install-Module -Name Pester -Force
Import-Module -Name Pester


switch ($true) {
  $env:TF_BUILD {
    Invoke-Pester -Script "$(System.DefaultWorkingDirectory)\Ring.test.ps1" -OutputFile "$(System.DefaultWorkingDirectory)\Test-Pester.XML" -OutputFormat NUnitXML
  }
  default {
    $Container = New-PesterContainer -Path "$PSScriptRoot/Ring.test.ps1"
    Invoke-Pester -Container $Container -Output Detailed
  }
}

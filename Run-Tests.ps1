Install-Module -Name Pester -Force
Import-Module -Name Pester


switch ($true) {
  $env:TF_BUILD {
    Invoke-Pester -Path "./Ring.test.ps1" -CI -OutputFile "./Test-Pester.XML"
  }
  default {
    $Container = New-PesterContainer -Path "$PSScriptRoot/Ring.test.ps1"
    Invoke-Pester -Container $Container -Output Detailed
  }
}

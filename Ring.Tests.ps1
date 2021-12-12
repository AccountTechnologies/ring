Describe 'Ring' {

  BeforeAll { . "./tests/resources/Install-Ring.ps1" }

  Context '-- as a global tool' -Tag 'global' {
  
    BeforeAll { Install-Ring -Global }

    Describe 'show-config' {
      It 'should return a valid path' {

        $ExpectedPath = switch ($true) {
          $IsWindows {
            "$($env:USERPROFILE)\.dotnet\tools\.store\atech.ring.dotnet.cli\$PkgVer\atech.ring.dotnet.cli\$PkgVer\tools\net6.0\any\appsettings.json"
          }
          default {
            "$($env:HOME)/.dotnet/tools/.store/atech.ring.dotnet.cli/$PkgVer/atech.ring.dotnet.cli/$PkgVer/tools/net6.0/any/appsettings.json"
          }
        }

        ring show-config | Should -be $ExpectedPath
      }
    }

    AfterAll { Uninstall-Ring -Global }
  }

  Context '-- as a local tool' -Tag 'local' {

  }
}

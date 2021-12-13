Describe 'Ring' {

  BeforeAll { . "$PSScriptRoot/tests/resources/Install-Ring.ps1" }

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

  # TODO: add ring console client so it can receive messages and determine if ring behaves correctly
  # also it would be able to terminate the server
  Context '-- as a local tool' -Tag 'local' {
    BeforeEach { 
      Install-Ring
      Push-Location
      Set-Location $TestDrive
    }
    Describe 'run' {
      It 'should be able run a workspace' {
        dotnet ring run -w "$PSScriptRoot/test/resources/basic/n2etcore.toml" | Should -Be "YEah"
        $LASTEXITCODE | Should -Be 0
      }
    }
    AfterEach {
      Uninstall-Ring
      Pop-Location
    }
  }
}

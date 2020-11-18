# Configuration

Ring as a .NET Core application is configured via a standard `appsettings.json` file. It is shipped with a defualt configuration. You can override the defaults by copying the default config file to the right location in AppData:

```powershell
cat "$(ring show-config -n)" > "$($env:APPDATA)\ATech\ring\appsettings.json"
```

## Configuration keys

* `ring:gitCloneRootPath` - the path where ring clones the repos of runnables that support it
* `ring:kustomizeCacheRootPath` - the path of kustomize cache. Defaults to: `/tmp/kustomize-cache`
* `ring:hooks:init:command` - a command to be run on the workspace init hook event
* `ring:hooks:init:args` - arguments to be passed to the above command

## Example configuration

```json
{
  "Ring": {
    "GitCloneRootPath" : "%TEMP%\\ring\\repos",
    "KustomizeCacheRootPath" : "/tmp/my-cache/kustomize",
    "Hooks": {
      "Init": {
        "Command": "C:\\Program Files\\PowerShell\\7-preview\\pwsh.exe",
        "Args": ["-C", "./Run-MyScript.ps1"]
      }
    }
  }
}
```

# ASP.NET Core runnable

It runs ASP.NET Core and other .NET Core apps. 

## Syntax

```toml
[[aspnetcore]]
sshRepoUrl = "git@your.git.server:path/to/repo.git"
csproj = "path/to/your/project.name.csproj"
urls = ["http://localhost:6201/"]
```
## Config keys

* `sshRepoUrl` (optional `string`) - if set Ring clones the default branch (usually `master`) of the specified repo and attempts to build and execute the project specified by the `csproj` key.
Ring clones are located at `%TEMP%/ring/repos/path/to/repo`. If the clone already exists Ring preforms `git pull` instead.

* `csproj` (mandatory `string`) - if `sshRepoUrl` is used then `csproj` must be a relative path and the project is loaded from `%TEMP%/ring/repos/path/to/repo/${csProj}`. If `sshRepoUrl` is not set then
`csproj` may be either absolute or relative.

* `urls` (optional `string[]`) - one or more URLs that are passed to the `ASPNETCORE_URLS` env variable

## How it works

Given project name is `project.name`
Ring scans project's build ouput for either a `project.name.exe` file (.NET Core 3.1) or `project.name.dll`. Exes are run directly whereas dlls
are executed using `dotnet exec`. 

## Environment variables

Ring passes the following env variables to the spawned process:

* `ASPNETCORE_ENVIRONMENT` = `Development`
* `ASPNETCORE_URLS` = the value of `urls` from runnable configuration (values joined by `;`)

## Health check

Ring does a simple *"is the process alive"* check. 

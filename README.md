# Ring [![Build Status](https://dev.azure.com/AccountTechnologies/Ring/_apis/build/status/AccountTechnologies.ring?branchName=master)](https://dev.azure.com/AccountTechnologies/Ring/_build/latest?definitionId=2&branchName=master) [![NuGet Badge](https://buildstats.info/nuget/ATech.Ring.Dotnet.Cli?includePreReleases=true)](https://www.nuget.org/packages/ATech.Ring.Dotnet.Cli)


## Meta-orchestrator for developers

Ring brings order into the messy world of developing and debugging a cloud-ready microservice system side by side with maintaining and migrating legacy ones where you may have many different types of services (ASP.NET Core, Topshelf, WCF, ...) hosted in many different ways (Kubernetes, Docker, IIS Express, WindowsService, Exe) and scattered across many solutions and repositories. 

# What is it?

Ring consists of the following part:

* process launcher and monitor (a dotnet CLI tool)
* [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vsix) (2022, also versions pre 4.0 support 2017, and 2019)
* [Visual Studio Code Extension](https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vscode) (WIP)

# How it works

Ring groups *runnables* (processes/services) into *workspaces*. Workspaces are defined in [TOML](https://github.com/toml-lang/toml) files. Workspaces are composed from runnables and other workspaces. A workspace can be loaded and started. Ring periodically runs a health check for every runnable, tries restarting the unhealthy ones, and reports the dead ones. Ring also exposes a web socket interface. Visual Studio extensions use it mainly for visualizing workspace/runnables states, turning services off/on for build/debugging if they're a part of the currently loaded project/solution.

# Basic facts

* You can run multiple instances of ring (serving different independent workspaces) 
* There can be multiple clients (VS/VS Code extensions) interacting with a Ring instance at a time although mostly you'd have just one
* Ring is meant to keep your workspace running even if you quit Visual Studio
* You can also run Ring in a stand-alone mode which just keeps your workspace running
* Ring exposes a web socket interface on port 7999

# Supported runnables

* [kustomize](docs/runnables/kustomize.md) - Kubernetes apps managed by [Kustomize](https://kustomize.io/)
* `dockercompose` - docker-compose files
* [aspnetcore](docs/runnables/aspnetcore.md) - .NET Core apps running in console (like ASP.NET Core in Kestrel)
* `iisxcore` - ASP.NET Core apps in IIS Express
* [iisexpress](docs/runnables/iisexpress.md) - WCF and other .NET Framework services hosted in IIS Express
* `netexe` - full .NET Framework console apps (like TopShelf)

# Installation 

## Ring dotnet tool
```
dotnet tool install --global ATech.Ring.DotNet.Cli --version '4.0.0-alpha.*'
```

## Visual Studio Extension

*Make sure you installed the dotnet tool first.*

Download here [ring! for Visual Studio](https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vsix)

## Visual Studio Code Extension

Download an [early preview](https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vscode)

## Troubleshooting 

If ring does not work as expected you can use `--debug` or `-d` switch to enable a debug level output.

```
ring run -w .\path\to\your\workspace.toml -d
```

# CLI commands

* `run` - runs a specified workspace in a stand-alone mode.
* `headless` - starts and awaits clients (VS Code / VS extension) connections. Once connected a client can load a workspace and interact with it.
* `clone` - loads a workspace and clones configured repos for each runnable. The runnables must have the `sshRepoUrl` parameter configured otherwise they'll be skipped.
* `config-*` commands - more info here - [configuration files](./docs/configuration.md).

# Vocabulary

* *runnable* - a service/process ring manages.
* *workspace* - a logical grouping of runnables defined in TOML file(s). Workspaces can be composed of other workspaces using the `import` tag. Ring can only run a single workspace at a time. Example workspace:
```toml
# your workspace.toml
[[kustomize]]
path = "your/app"

[[dockercompose]]
path = "app/2"

[[import]]
path = "relative/path/to/your/workspace.toml"
```

# Authoring workspaces

Workspaces are written in [TOML](https://github.com/toml-lang/toml) and they mostly use the [arrays of tables](https://github.com/toml-lang/toml#array-of-tables) to define workspace's components. The following syntax is supported:

## Kustomize app

Requirements: 
* kubectl (configured with a local cluster access - like Docker Desktop or minikube)
* kustomize

```toml
[[kustomize]]
path = "path/to/app"
```

## Docker Compose app

Requirements:
* Docker Desktop

```toml
[[dockercompose]]
path = "path/to/docker-compose.yml"
```

## Dotnet app 

Requirements:
* dotnet SDK

```toml
[[aspnetcore]]
csproj = "/path/to/your/project.csproj"
```

## Legacy formats

### ASP.NET Core (in IIS Express)

Requirements:
* dotnet SDK

```toml
[[iisxcore]]
csproj = "path/to/your/project.csproj"
```

### IIS Express hosted .NET Framework service (e.g. AspNet MVC or WCF)

Requirements:

* .NET Framework (4.*)

```toml
[[iisexpress]]
csproj = "path/to/your/project.csproj"
```

### .NET Framework executable project

Requirements:

* .NET Framework (4.*)

```toml
[[netexe]]
csproj = "path/to/your/project.csproj"
```

## Import another workspace

Simplified syntax:

```toml
imports = [
  "path/to/workspace/a.toml",
  "path/to/workspace/b.toml",
  "path/to/yet/another/workspace/c.toml"
]
```

Classic syntax:

```toml
[[import]]
path = "path/to/workspace/a.toml"

[[import]]
path = "path/to/workspace/b.toml"

[[import]]
path = "path/to/yet/another/workspace/c.toml"
```

## Comment

```toml
# This is a comment
# [[aspnetcore]]
# csproj = "/path/to/your/project.csproj"
```

## Workspace flavours

:information_source: A new feature in v4

Sometimes the user may have multiple workspaces that significantly overlap. Stopping one workspace and starting another may
be quite slow if there are tens of runnables. *Flavours* help to solve that problem with only stopping runnables that are not
included in the new workspace and only starting the ones that were not running in the previous one. All the runnables existing in both
keep happily running.

Example:

Flavours are specified with `tags` and each runnable can have multiple.
The below workspace has 3 flavours: `a`, `b`, and `backend`.

Given we run flavour `a`:

- `app-x`
- `app-common-1`
- `app-common-2`
- `app-common-3`
- `ui-a`

When we apply flavour `b`:

It stops:

- `app-x`
- `ui-a`

It starts:

- `app-y`
- `ui-b`

All the 3 common apps keep running.

```toml
[[kustomize]]
path = "app-x"
tags = ["a", "backend"]

[[kustomize]]
path = "app-y"
tags = ["b", "backend"]

[[kustomize]]
path = "app-common-1"
tags = ["a", "b", "backend"]

[[kustomize]]
path = "app-common-2"
tags = ["a", "b", "backend"]

[[kustomize]]
path = "app-common-3"
tags = ["a", "b", "backend"]

[[kustomize]]
path = "ui-a"
tags = ["a"]

[[kustomize]]
path = "ui-b"
tags = ["b"]

```

If the same service is declared multiple times in imported workspaces they will be deduplicated and only one instance of a service (based on the project path) will be launched.

# How to contribute
Coming soon

# Release notes

[Here](RELEASENOTES.md)

# Known Issues
## Permission Issue on Apple Silicon
Running Ring! on Apple Silicone with .NET 6 (x64) installed can result in permission errors such as this one:

```
[‘/etc/dotnet/install_location_x64’] failed to open: Permission denied.
```

To resolve this issue add your user to the `dotnet` directory (and its encolsed items) with `Read & Write` permissions.

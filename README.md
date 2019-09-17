# ring! - one *ring* to rule them all...

Ring brings order into the messy world of developing and debugging a cloud-ready microservice system side by side with maintaining and migrating legacy ones where you may have many different types of services (ASP.NET Core, Topshelf, WCF) hosted in many different ways (IIS Express, WindowsService, Exe) and scattered across many solutions and repositories. 

# What is it?

Ring consists of the following part:

* Service launcher and monitor (a global dotnet tool)
* [Visual Studio Extension]((https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vsix)) (2017, 2019)
* [Visual Studio Code Extension]((https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vscode)) (early preview)

# How it works

Ring groups *runnables* (mostly services but not only) into *workspaces*. Workspaces are defined in [TOML](https://github.com/toml-lang/toml) files. Workspaces are composed from runnables and other workspaces. A workspace can be loaded and started. Ring periodically runs a health check for every runnable, tries restarting the unhealthy ones, and reports the dead ones. Ring also exposes a web socket interface. Visual Studio extensions use it mainly for visualizing workspace/runnables states, turning services off/on for build/debugging if they're a part of the currently loaded project/solution.

# Basic facts

* There is only one Ring instance on your dev machine running at a time 
* There can be multiple clients (VS/VS Code extensions) interacting with Ring at a time although mostly you'd have just one
* Ring is meant to keep your workspace running even if you quit Visual Studio
* You can also run Ring in a stand-alone mode which just keeps your workspace running
* Ring exposes a web socket interface on port 7999

# Installation 

## Ring dotnet tool
```
dotnet tool install --global ATech.Ring.DotNet.Cli --version 1.1.6
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

# Vocabulary

* *runnable* - usually a service. Currently the following types are supported:
    * `iisexpress` - WCF and other services hosted in IIS Express
    * `aspnetcore` - .NET Core apps running in console
    * `netexe` - full .NET Framework console apps (like TopShelf)
    * `dockercompose` - docker-compose files

* *workspace* - a logical grouping of runnables defined in TOML file(s). Workspaces can be composed of other workspaces using the `import` tag. Ring can only run a single workspace at a time. Example workspace:
```toml
# your workspace.toml
[[iisexpress]]
csproj = "path/to/your/amazing.name.cool.csproj"

[[iisexpress]]
csproj = "path/to/your/another.name.csproj"

[[import]]
path = "../a/relative/path/to/your/workspace.toml"
```

# Authoring workspaces

Workspaces are written in [TOML](https://github.com/toml-lang/toml) and they mostly use the [arrays of tables](https://github.com/toml-lang/toml#array-of-tables) to define workspace's components. The following syntax is supported:

*Imports another workspace*

```toml
[[import]]
path = "path/to/another/workspace.toml"
```

*Runs IIS Express hosted full .NET Framework service (e.g. AspNet MVC or WCF)*

```toml
[[iisexpress]]
csproj = "path/to/your/project.csproj"
```

*Runs full .NET Framework executable project*

```toml
[[netexe]]
csproj = "path/to/your/project.csproj"
```

*Runs NET Core web project (e.g. AspNet Core MVC)*

```toml
[[aspnetcore]]
csproj = "/path/to/your/project.csproj"
```

*Runs Docker Compose file*
```toml
[[dockercompose]]
path = "path/to/docker-compose.yml"
```

*Comments*

```toml
# This is a comment
# [[aspnetcore]]
# csproj = "/path/to/your/project.csproj"
```

If the same service is declared multiple times in imported workspaces they will be deduplicated and only one instance of a service (based on the project path) will be launched.

# How to contribute
Coming soon

# Release notes

## 1.1.6

* Added support for docker-compose files
* Fixed a bug on capturing output of exited processes
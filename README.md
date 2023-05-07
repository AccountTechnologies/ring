# This repository is no longer maintained. All further development moves to [queil/ring](https://github.com/queil/ring).

# Ring [![Build Status](https://dev.azure.com/AccountTechnologies/Ring/_apis/build/status/AccountTechnologies.ring?branchName=master)](https://dev.azure.com/AccountTechnologies/Ring/_build/latest?definitionId=2&branchName=master) [![NuGet Badge](https://buildstats.info/nuget/ATech.Ring.Dotnet.Cli?includePreReleases=true)](https://www.nuget.org/packages/ATech.Ring.Dotnet.Cli)

## Meta-orchestrator for developers (cross-platform)

Ring brings order into the messy world of developing and debugging a cloud-ready microservice system side by side with maintaining and migrating legacy ones where you may have many different types of services (ASP.NET Core, Topshelf, WCF, ...) hosted in many different ways (Kubernetes, Docker, IIS Express, WindowsService, Exe) and scattered across many solutions and repositories. 

# What is it?

Ring consists of the following parts:

* the meta-orchestrator (a dotnet CLI tool)
* [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vsix) (2022, also versions pre 4.0 support 2017, and 2019)
* [Visual Studio Code Extension](https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vscode) (WIP)

# How it works

Ring groups *apps* into *workspaces*. Workspaces are defined in [TOML](https://github.com/toml-lang/toml) files. 
Workspaces are composed from apps and other workspaces. A workspace can be loaded and started. 
Ring periodically runs a health check for every app, tries restarting the unhealthy ones, and reports the dead ones.
Ring also exposes a web socket interface. Visual Studio extensions use it mainly for visualizing workspace/apps states,
turning services off/on for build/debugging if they're a part of the currently loaded project/solution.

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
* `proc` - arbitrary native processes

Windows-only:

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

* *app* (aka *runnable* - an application/service/process ring manages.
* *workspace* - a logical grouping of apps defined in TOML file(s). 
  Workspaces can be composed of other workspaces using the `import` tag. 
  Ring can only run a single workspace at a time. 

## Example workspace

```toml
# your workspace.toml
[[kustomize]]
path = "your/app"

[[dockercompose]]
path = "app/2"

[[import]]
path = "relative/path/to/your/workspace.toml"
```

## Authoring workspaces

[Authoring workspaces docs](docs/authoring-workspaces.md)

# Release notes

[Here](RELEASENOTES.md)

# Working with the docs

Serve locally:

```bash
docker run -p 8089:8089 --rm -it -v ~/.ssh:/root/.ssh -v ${PWD}:/docs squidfunk/mkdocs-material serve -a 0.0.0.0:8089
```

Publish

```bash
docker run --rm -it -v ~/.ssh:/root/.ssh -v ${PWD}:/docs squidfunk/mkdocs-material gh-deploy 
```

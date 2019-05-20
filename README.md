# ring! - one *ring* to rule them all...

Ring brings order into the messy world of developing a cloud-ready microservice system side by side with maintaining and migrating legacy ones where you may have many different types of services (ASP.NET Core, Topshelf, WCF) hosted in many different ways (IIS Express, WindowsService, Exe) and scattered across many solutions and repositories. 

# Purpose

It lets you focus on the actual development/debugging quickly showing when one or more of your services failed to start or just died in the middle of debugging.

# What is it?

Ring consists of the following part:

* Service launcher and monitor (global dotnet tool)
* Visual Studio Extension (2017, 2019)
* Visual Studio Code Extension (coming soon)

# How it works

Ring groups *runnables* (mostly services but not only) into *workspaces*. Workspaces are defined in [TOML](https://github.com/toml-lang/toml) files. Workspaces can be imported by other workspaces letting you compose them. Once a workspace gets loaded into Ring it can be started. Ring periodically runs a health check for every runnable, tries restarting the unhealthy ones, and reports the dead ones. Ring also exposes a web socket interface. Visual Studio extensions use it mainly for visualizing workspace/runnables status, turning services off/on for build/debugging if they're a part of the currently loaded project/solution.

# Installation 

## Ring dotnet tool
```
dotnet tool install --global ATech.Ring.DotNet.Cli --version 1.0.0-beta
```

## Visual Studio Extension

Donwload here [ring! for Visual Studio](https://marketplace.visualstudio.com/items?itemName=account-technologies.ring-vsix)

Please note the extension will not work without the dotnet tool.

## Visual Studio Code Extension

Coming soon


# How to contribute

Coming soon

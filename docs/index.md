---
title: "Ring - cross-platform meta-orchestrator for developers"
---

!!! warning

    Work in progress. The docs are not yet complete.

Ring is a cross-platform command-line tool that can run and monitor workspaces built of apps (aka *runnables*) - e.g. processes, Docker Compose files,
Kustomize apps, and more). Workspaces are declarative configuration files (using [TOML](https://github.com/toml-lang/toml) format).
Ring exposes a web-socket server. Clients (like the VS Code extension) can use it to control Ring and subscribe to real-time apps status notifications.

## Requirements

Ring is a dotnet cli tool which means it requires Dotnet SDK. Various app types may require additional components.
Read more in the [authoring workspaces](authoring-workspaces.md) section.

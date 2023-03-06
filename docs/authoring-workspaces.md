---
title: "Authoring workspaces"
---

Workspaces are [TOML](https://github.com/toml-lang/toml) files and they mostly use the [arrays of tables](https://github.com/toml-lang/toml#array-of-tables) to define workspace components.

## Apps

### Kustomize app

Requirements:

* local Kubernetes cluster
* kubectl
* kustomize

```toml
[[kustomize]]
path = "path/to/app"
```

### Docker Compose app

Requirements:

* Docker Desktop

```toml
[[dockercompose]]
path = "path/to/docker-compose.yml"
```

### Dotnet app

Requirements:

* Dotnet SDK

```toml
[[aspnetcore]]
csproj = "/path/to/your/project.csproj"
```

### Process

```toml
[[proc]]
command = "sleep"
args = ["30"]

[proc.vars]
    MY_TEST_ENV_VAR = "NONSENSE"
    YET_ANOTHER = "QUIT"
```

### Legacy formats

#### ASP.NET Core (IIS Express)

Requirements:

* Dotnet SDK

```toml
[[iisxcore]]
csproj = "path/to/your/project.csproj"
```

#### .NET Framework web service (IIS Express)

Legacy Windows web services like AspNet MVC or WCF

Requirements:

* .NET Framework (4.*)

```toml
[[iisexpress]]
csproj = "path/to/your/project.csproj"
```

#### .NET Framework executable project

Legacy Windows services like TopShelf

Requirements:

* .NET Framework (4.*)

```toml
[[netexe]]
csproj = "path/to/your/project.csproj"
```

## Imports

Ring workspaces can be composed from other workspaces. If the same app is declared multiple times in imported 
workspaces they will be deduplicated and only one instance of an app will be launched.

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

## Running multiple instances of an app

:warning: v4 only

If there are two (or more) apps with the same identifier Ring
de-duplicates them and only runs a single instance. Sometimes the user
may want run multiple instances of a single app. Each instance can be 
configured with an `id` key as follows:

```toml
[[proc]]
command = "sleep"
args = ["30"]
id = "sleep-1"

[[proc]]
command = "sleep"
args = ["60"]
id = "sleep-2"
```
Now ring runs two `sleep` processes.

## Workspace flavours

:warning: v4 only

Sometimes the user may have multiple workspaces that significantly overlap. Stopping one workspace and starting another may
be quite slow if there are tens of apps. *Flavours* help to solve that problem with only stopping apps that are not
included in the new workspace and only starting the ones that were not running in the previous one. All the apps existing in both
keep happily running.

:bulb: Example

Flavours are specified with `tags` and each app can have multiple.
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

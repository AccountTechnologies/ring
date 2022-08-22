# Configuration

This section is about configuring the tool itself rather than authoring workspaces.

Ring can be configured using `appsettings.toml` and/or environment variables.

Toml files may exist in 3 scopes:

- default - the base config shipped with the tool that should not be modified (as it gets overwritten when installing new version of ring) 
- user - user-scoped config
- local - directory-scoped config

The most specific scope takes precedence. Environment variables take precedence over `appsettings.toml`.

## Tasks

### Display config paths
 
You can display paths of the above configs using `ring config-path` with an appropriate scope flag.

### Create configs

You can also generate `appsettings.toml` using `ring config-create` (also with the right scope flag).

### Dump configuration

You can verify how the fully-built configuration store looks like via `ring config-dump`.

## Configuration keys

:information_source: When configuring the below via environment variables use `RING_` prefix and replace `.` by `__` (double underscore). For example:
`hooks.init.command` becomes `RING_HOOKS__INIT__COMMAND`.

* `gitCloneRootPath` - the path where ring clones the repos of runnables that support it. Default: `$HOME/.ring/repos`
* `kustomizeCacheRootPath` - the path of kustomize cache. Default: `$HOME/.ring/kustomize-cache`
* `hooks.init.command` - a command to be run on the workspace init hook event. Default: N/A
* `hooks.init.args` - arguments to be passed to the above command. Default: N/A

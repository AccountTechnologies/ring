# Release notes

## 3.0.0

Migrate to .NET 6

## 2.5.1

* Clone the default branch rather than hard-coded "master".

## 2.5.0

* Make core Ring cross-platform

## 2.4.4

* Optimisations
* Kustomize runnable: replace kubectl calls for direct API access
* IISExpress runnable: de-parallelise health checks
* Only add Uri detail if there are URLs
* Kustomize runnable: expose pod details
* RingVsix: added runnable context menu
* Use DetailsKeys
* Support for friendly name
* Fixed VSToolsPath
* Bump Vsix version

## 2.4.3

* KustomizeRunnable: improve init/start indication and other improvements

## 2.4.2

* git-supporting runnables: use shallow single-branch (master) clones
* kustomize runnables: enable built manifests caching 

## 2.4.1

* Awaiting runnables to terminate on server shutdown
* Deleting corrupted git repos no longer fails with access denied errors
* Kustomize runnable health check correctly reacts on pods deletion/recreation

## 2.4.0

* Support for Kustomize apps
* Ring clone command
* Added support for user configuration

## 2.3.4

* no changes vs 2.3.4-pre. Releasing as the stable release before staring work on new features.

## 2.3.4-pre

* Fixed https://github.com/AccountTechnologies/ring/issues/1

## 2.3.2-pre

* bug fixes

## 2.3.1-pre

* bug fixes

## 2.3.0-pre

* Enabled cloning aspnetcore runnables source code from git.

## 2.2.1-pre

* Fixed CsProj-based runnables trying to read from paths relative to the root working dir rather than the absolute ones. It was happening only if imported workspaces were in different folders.

## 2.2.0-pre

* Added IISXCore runnable (ASP.NET Core on IIS Express)

## 2.1.0-pre

* Upgraded to .NET Core 3.1
* Fixed iisexpress temp dir creation if does not exist
* Fixed pulling re-tagged Docker images (like latest)

## 2.0.0

Upgraded to NET Core 3

## 1.1.10

* IISExpress Runnable: enable retrieval of custom binding information from csproj files

Ring uses `/Project/ProjectExtensions/VisualStudio/FlavorProperties/WebProjectProperties` to determine how to run a particular project.
So far it used `DevelopmentServerPort` and assume `localhost`. From now on it will use the URL specified at `IISUrl` instead.

* Bugfixes

## 1.1.9

* Logging improvements.

## 1.1.8

* Fixed System.ArgumentOutOfRangeException if runnable is dead.

## 1.1.7

* Fixed swallowing exceptions if one of the base tasks fails. 
* Fixed Microsoft.AspNetCore.WebSockets reference.

## 1.1.6

* Added support for docker-compose files
* Fixed a bug on capturing output of exited processes

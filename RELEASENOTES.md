# Release notes

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
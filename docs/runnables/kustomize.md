# Kustomize runnable

It runs Kustomize apps in a Kubernetes cluster.

## Warning

Ring executes `kubectl` in WSL2 and uses the current `kubectl` context. At present it doesn't have any restrictions in place to limit the context it executes on.

## Requirements

* A local K8s cluster
* WSL2 with installed `kubectl` and `kustomize` 

## Syntax

```toml
[[kustomize]]
path = "ssh://git@your.domain/repo.git/path/to/app?ref=branch-name"
```

# Kustomize runnable

It runs Kustomize apps in a Kubernetes cluster.

## Requirements

* A local K8s cluster
* Installed `kubectl` and `kustomize` 

## Supported paths

Ring supports every path supported by Kustomize ([go-getter](https://github.com/hashicorp/go-getter#url-format)) 

## Caching

Ring caches generated manifests to speed up subsequent executions. The default cache location is `/tmp/kustomize-cache` and can be [configured](../configuration.md#configuration-keys) 

## Syntax

```toml
[[kustomize]]
path = "ssh://git@your.domain/repo.git/path/to/app?ref=branch-name"

[[kustomize]]
path = "local/app"
```

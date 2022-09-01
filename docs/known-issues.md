---
title: "Known issues"
---

## Permission Issue on Apple Silicon

Running Ring! on Apple Silicone with .NET 6 (x64) installed can result in permission errors such as this one:

```
['/etc/dotnet/install_location_x64'] failed to open: Permission denied.
```

To resolve this issue add your user to the `dotnet` directory (and its enclosed items) with `Read & Write` permissions.

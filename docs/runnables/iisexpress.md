# IIS Express runnable

It runs .NET Framework WCF services and ASP.NET MVC websites.

## Syntax

```toml
[[iisexpress]]
csproj = "path/to/your/project.csproj"
```
## How it works

IIS Express runnable expects the following XML elements exist in csproj file:
```xml
<Project>
    <ProjectExtensions>
        <VisualStudio>
            <FlavorProperties GUID="{349c5851-65df-11da-9384-00065b846f21}">
            <WebProjectProperties>
                <DevelopmentServerPort>90</DevelopmentServerPort>
                <IISUrl>http://domain.name:90/</IISUrl>
            </WebProjectProperties>
            </FlavorProperties>
        </VisualStudio>
    </ProjectExtensions>
</Project>
```

Ring before 1.1.10 only uses `DevelopmentServerPort` and assumes `localhost` when running IIS Express.
Version 1.1.10 and later versions support non-localhost bindings by utilising `IISUrl` first and falling back to `DevelopmentServerPort` if `IISUrl` is not found.

## Health check

Ring does a simple *"is IIS Express process alive"* check. Additionally for WCF services it detects all the `.svc` files in the project directory and checks whether they return HTTP 200.  
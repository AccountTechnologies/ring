namespace ATech.Ring.DotNet.Cli.Infrastructure;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public sealed class WsClientEqualityComparer : EqualityComparer<WsClient>
{
    public override bool Equals(WsClient? x, WsClient? y)
    {
        return x is not null && y is not null && x.Id == y.Id;
    }

    public override int GetHashCode([DisallowNull] WsClient obj) => obj.GetHashCode();
}

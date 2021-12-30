namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static class HttpContextExtensions
{
    internal static T Get<T>(this HttpContext ctx) where T : notnull => ctx.RequestServices.GetRequiredService<T>();

    internal static ILogger<RingMiddleware> Logger(this HttpContext ctx) => ctx.Get<ILogger<RingMiddleware>>();

    internal static Task BadRequest(this HttpResponse rs, string errorMessage)
    {
        rs.StatusCode = 400;
        return rs.WriteAsync(errorMessage);
    }

    internal static async Task<bool> ShouldHandle(this HttpContext ctx, RequestDelegate next)
    {
        if (ctx.Request.Path.StartsWithSegments("/ws"))
        {
            if (ctx.WebSockets.IsWebSocketRequest)
                return true;
            await ctx.Response.BadRequest("This is not a web socket request");
            return false;
        }
        await next(ctx);
        return false;
    }
}

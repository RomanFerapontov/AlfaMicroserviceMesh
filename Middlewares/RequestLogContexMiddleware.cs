using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace AlfaMicroserviceMesh.Middlewares;

public class RequestLogContexMiddleware(RequestDelegate next) {
    private readonly RequestDelegate _next = next;

    public Task InvokeAsync(HttpContext context) {
        using(LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        {
            return _next(context);
        }
    }
}

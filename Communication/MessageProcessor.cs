using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Helpers;
using AlfaMicroserviceMesh.Models.Service;
using AlfaMicroserviceMesh.Models.ReqRes;
using AlfaMicroserviceMesh.Registry;
using Serilog;

namespace AlfaMicroserviceMesh.Communication;

public class MessageProcessor {
    private readonly Context _context = Services.selfContext;

    public async Task ProcessMessage(string message) {
        var ctx = await message.DeserializeAsync<Context>();
        var eventName = ctx.Event;

        if (eventName is "discover") UpdateNodeRegistry(ctx);
        if (eventName is "dispose") UpdateNodeRegistry(ctx, false);

        if (eventName is "request") await HandleRequest(ctx);
        if (eventName is "event") await HandleEvent(ctx);

        if (eventName is "response" || eventName is "error") SaveNewResponse(ctx);
    }

    private static void UpdateNodeRegistry(Context ctx, bool addNode = true) {
        if (addNode) Services.AddNode(ctx);
        else Services.DeleteNode(ctx);
    }

    private async Task HandleRequest(Context ctx) {
        string caller = ctx.InstanceID;
        string action = ctx.Action;

        _context.RequestID = ctx.RequestID;

        try {
            int handlerTimeout = Handlers.Timeouts.TryGetValue(action, out int timeout) ? timeout :
                ServiceBroker.Service.RequestTimeout;

            Response handlerResult = await Timers
                .ExecuteWithTimeout(Handlers.Call[action], ctx, handlerTimeout);

            _context.Event = "response";
            _context.Response = handlerResult;

            var message = await _context.SerializeAsync();

            RabbitMQService.PublishMessage(message, caller);
        }
        catch (Exception exception) {
            _context.Event = "error";
            _context.Response.Error = exception is MicroserviceException MSException ?
                MSException.Info :
                new() { Error = exception.Message };

            var message = await _context.SerializeAsync();

            RabbitMQService.PublishMessage(message, caller);
        }
    }

    private static async Task HandleEvent(Context ctx) {
        try {
            await Handlers.Emit[ctx.Action](ctx);
        }
        catch (Exception exception) {
            Log.Error(exception.Message);
        }
    }

    private static void SaveNewResponse(Context ctx) =>
        Responses.SaveResponse(ctx.RequestID, ctx.Response);
}
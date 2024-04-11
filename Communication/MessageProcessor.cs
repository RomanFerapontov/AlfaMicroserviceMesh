using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Helpers;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.ReqRes;
using AlfaMicroserviceMesh.Services;
using Serilog;

namespace AlfaMicroserviceMesh.Communication;

public class MessageProcessor {
    private readonly Context _context = Nodes.selfContext;

    public async Task ProcessMessage(string message) {
        var ctx = await message.DeserializeAsync<Context>();

        if (ctx?.Event == "discover") UpdateNodeRegistry(ctx);
        if (ctx?.Event == "dispose") UpdateNodeRegistry(ctx, false);

        if (ctx?.Event == "request") await HandleRequest(ctx);
        if (ctx?.Event == "response") SaveNewResponse(ctx);

        if (ctx?.Event == "event") await HandleEvent(ctx);

        if (ctx?.Event == "error") SaveNewResponse(ctx);
    }

    private void UpdateNodeRegistry(Context ctx, bool addNode = true) {
        if (addNode) Nodes.AddNode(ctx);
        else Nodes.DeleteNode(ctx);
    }

    private async Task HandleRequest(Context ctx) {
        string caller = ctx.InstanceID;
        string action = ctx.Action;

        _context.RequestID = ctx.RequestID;

        try {

            int handlerTimeout = Handlers.Timeouts.TryGetValue(action, out int timeout) ? timeout :
                ServiceBroker.Service.RequestTimeout;

            Response handlerResult = await Timers
                .ExecuteWithTimeout(Handlers.Call[action], ctx ,handlerTimeout);

            _context.Event = "response";
            _context.Response = handlerResult;

            var message = await _context.SerializeAsync();

            RabbitMQService.PublishMessage(message, caller);
        }
        catch (Exception exception) {
            _context.Event = "error";
            _context.Response.Error = exception is MicroserviceException MSException ?
                MSException.Info :
                new() { Errors = [exception.Message] };

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

    private void SaveNewResponse(Context ctx) {
        Responses.SaveResponse(ctx.RequestID, ctx.Response);
    }
}
using AlfaMicroserviceMesh.Communication;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Models;

namespace AlfaMicroserviceMesh.Utils;

public class MessageProcessor {
    private readonly Context _context = NodesRegistry.selfContext;

    public async Task ProcessMessage(string message) {
        var ctx = await message.DeserializeAsync<Context>();

        if (ctx?.Event == "discover") UpdateNodeRegistry(ctx);
        if (ctx?.Event == "dispose") UpdateNodeRegistry(ctx, false);

        if (ctx?.Event == "request") await HandleRequestEvent(ctx);
        if (ctx?.Event == "response") SaveNewResponse(ctx);

        if (ctx?.Event == "error") SaveNewResponse(ctx);
    }

    private void UpdateNodeRegistry(Context ctx, bool addNode = true) {
        if (addNode) NodesRegistry.AddNode(ctx);
        else NodesRegistry.DeleteNode(ctx);
    }

    private async Task HandleRequestEvent(Context ctx) {
        string caller = ctx.InstanceID;
        string action = ctx.Action;

        _context.RequestID = ctx.RequestID;

        try {
            var actionResult = await HandlersRegistry.Call[action](ctx);

            _context.Event = "response";
            _context.Response.Data = actionResult;
            _context.Response.Error = null;

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

    private void SaveNewResponse(Context ctx) {
        ResponsesRegistry.SaveResponse(ctx.RequestID, ctx.Response.Data, ctx.Response.Error);
    }
}
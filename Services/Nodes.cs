using AlfaMicroserviceMesh.Communication;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Helpers;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.Action;
using AlfaMicroserviceMesh.Models.Node;
using AlfaMicroserviceMesh.Models.ReqRes;
using AlfaMicroserviceMesh.Validator;
using Serilog;

namespace AlfaMicroserviceMesh.Services;

public static class Nodes {
    public readonly static Dictionary<string, Node> _nodes = [];
    private readonly static Dictionary<string, string> _timers = [];

    public static readonly Context selfContext = new() {
        NodeName = ServiceBroker.Service.Name,
        InstanceID = ServiceBroker.Service.InstanceID
    };

    public static void AddNode(Context ctx) {
        string node = ctx.NodeName;
        string InstanceID = ctx.InstanceID;

        if (!_nodes.ContainsKey(node)) {
            RegisterNode(ctx);
            Log.Information($"Node '{node}' added");
        }

        if (!_nodes[node].Instances.ContainsKey(InstanceID)) {
            RegisterNodeInstance(ctx);
            Log.Information($"instance '{InstanceID}' added");
        }
        else {
            RemoveNodeInstanceTimer(InstanceID);
        }
        SetInstanceLiveTimeInterval(ctx, 15000);
    }

    private static void RegisterNode(Context ctx) => _nodes[ctx.NodeName] = new Node();

    public static void DeleteNode(Context ctx) {
        if (_nodes.TryGetValue(ctx.NodeName, out Node? value)) {
            if (value.Instances.ContainsKey(ctx.InstanceID)) {
                RemoveNodeInstance(ctx);
            }
        }
    }

    private static void RegisterNodeInstance(Context ctx) {
        _nodes[ctx.NodeName].Instances[ctx.InstanceID] = new InstanceMetadata {
            Actions = ctx.Metadata.Actions,
            Events = ctx.Metadata.Events
        };
    }

    private static void RemoveNodeInstance(Context ctx) {
        RemoveNodeInstanceTimer(ctx.InstanceID);

        _nodes[ctx.NodeName].Instances.Remove(ctx.InstanceID);

        if (_nodes[ctx.NodeName].Instances.Count == 0) _nodes.Remove(ctx.NodeName);
        Log.Warning($"Node '{ctx.InstanceID}' deleted");
    }

    private static void SetInstanceLiveTimeInterval(Context ctx, int liveInterval) {
        var timerID = Timers.SetTimeout(() => RemoveNodeInstance(ctx), liveInterval);
        _timers[ctx.InstanceID] = timerID;
    }

    private static void RemoveNodeInstanceTimer(string InstanceID) =>
        Timers.ClearTimer(_timers[InstanceID]);

    public static string GetNodeInstanceUid(string nodeName) {
        if (!_nodes.TryGetValue(nodeName, out Node? node))
            throw new MicroserviceException([$"Service '{nodeName}' is not available"]);

        string lastRequestedInstanceUid = node.LastRequest;

        List<string> instansesUids = [.. node.Instances.Keys];

        if (string.IsNullOrEmpty(lastRequestedInstanceUid) || !node.Instances.ContainsKey(lastRequestedInstanceUid)) {
            node.LastRequest = instansesUids[0];
            return node.LastRequest;
        }

        string requestedInstanceUid = Helper.GetNextCyclicItem(instansesUids, lastRequestedInstanceUid);
        node.LastRequest = requestedInstanceUid;

        return requestedInstanceUid;
    }

    public static bool IsActionExists(string node, string instanceId, string action) =>
        _nodes[node].Instances[instanceId].Actions.ContainsKey(action);

    public static async Task<NewAction> GetActionData(string node, string instanceId, string action) {
        if (!IsActionExists(node, instanceId, action))
            throw new MicroserviceException([$"Invalid action name: '{action}'"]);

        var actionData = await _nodes[node].Instances[instanceId].Actions[action].SerializeAsync();

        return await actionData.DeserializeAsync<NewAction>();
    }

    public static async Task<Response?> Call(string node, string action, object parameters = null!, string instanceId = null!) {
        instanceId ??= GetNodeInstanceUid(node);
        
        var actionData = await GetActionData(node, instanceId, action);
        var exception = new MicroserviceException();
        var retries = actionData.RetryPolicy?.MaxAttempts ??
            ServiceBroker.Service.RetryPolicy.MaxAttempts;

        while (retries != 0) {
            var requestTimeout = actionData?.RequestTimeout ?? ServiceBroker.Service.RequestTimeout;
            var requestID = Guid.NewGuid().ToString();

            if (parameters != null && actionData!.Params != null) {
                var paramsSchema = await actionData.Params.ConvertToModel<Dictionary<string, ActionParams>>();
                parameters = await CustomValidator.ValidateParams(parameters, paramsSchema!);
            }

            Context context = new() {
                NodeName = selfContext.NodeName,
                InstanceID = selfContext.InstanceID,
                Action = action,
                Event = "request",
                RequestID = requestID,
                Request = new Request {
                    Parameters = parameters ?? new { },
                },
            };

            var message = await context.SerializeAsync();

            RabbitMQService.PublishMessage(message, instanceId);
            
            Response result = await Responses
                .GetResponse(requestID, TimeSpan.FromMilliseconds(requestTimeout));

            if (result?.Error != null) {
                exception.Info.Errors = result.Error.Errors;
                retries--;
                await Task.Delay(actionData?.RetryPolicy?.Delay ?? 0);
            }
            else return result;
        }

        throw exception;
    }

    public static async Task Broadcast(string action, object? parameters = null!) {
        foreach (var node in _nodes) {
            Context context = new() {
                NodeName = selfContext.NodeName,
                InstanceID = selfContext.InstanceID,
                Action = action,
                Event = "event",
                RequestID = Guid.NewGuid().ToString(),
                Request = new Request {
                    Parameters = parameters ?? new { },
                },
            };

            var instanceId = GetNodeInstanceUid(node.Key);

            if (_nodes[node.Key].Instances[instanceId].Events.Contains(action)) {
                var message = await context.SerializeAsync();
                RabbitMQService.PublishMessage(message, instanceId);
            }
        }
    }
}

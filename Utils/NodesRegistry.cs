using AlfaMicroserviceMesh.Communication;
using AlfaMicroserviceMesh.Dtos;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Helpers;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.Action;
using AlfaMicroserviceMesh.Models.Node;
using AlfaMicroserviceMesh.Models.ReqRes;
using AlfaMicroserviceMesh.Validator;
using Serilog;

namespace AlfaMicroserviceMesh.Utils;

public static class NodesRegistry {
    public readonly static Dictionary<string, Node> _nodes = [];
    private readonly static Dictionary<string, string> _timers = [];

    public static readonly Context selfContext = new() {
        NodeName = ServiceBroker.ServiceName,
        InstanceID = ServiceBroker.ServiceInstance,
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
            Actions = ctx.Metadata.Actions
        };
    }

    private static void RemoveNodeInstance(Context ctx) {
        RemoveNodeInstanceTimer(ctx.InstanceID);

        _nodes[ctx.NodeName].Instances.Remove(ctx.InstanceID);

        if (_nodes[ctx.NodeName].Instances.Count == 0) _nodes.Remove(ctx.NodeName);
        Log.Warning($"Node '{ctx.InstanceID}' deleted");
    }

    private static void SetInstanceLiveTimeInterval(Context ctx, int liveInterval) {
        var timerID = IntervalTimer.SetTimeout(() => RemoveNodeInstance(ctx), liveInterval);
        _timers[ctx.InstanceID] = timerID;
    }

    private static void RemoveNodeInstanceTimer(string InstanceID) =>
        IntervalTimer.ClearTimer(_timers[InstanceID]);

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

    public static ActionDTO GetActionData(string node, string instanceId, string action) {
        if (!IsActionExists(node, instanceId, action))
            throw new MicroserviceException([$"Invalid action name: '{action}'"]);

        return _nodes[node].Instances[instanceId].Actions[action];
    }

    public static async Task<object?> Call(string node, string action, object parameters, string instanceId = null!) {
        instanceId ??= GetNodeInstanceUid(node);

        var actionData = GetActionData(node, instanceId, action);

        var paramsSchema = await actionData.Params.ConvertToModel<Dictionary<string, ActionParams>>();

        var validRequestParams = await CustomValidator.ValidateParams(parameters, paramsSchema!);

        string requestID = Guid.NewGuid().ToString();

        Context context = new() {
            NodeName = selfContext.NodeName,
            InstanceID = selfContext.InstanceID,
            Action = action,
            Event = "request",
            RequestID = requestID,
            Request = new Request {
                Parameters = validRequestParams,
            },
        };

        var message = await context.SerializeAsync();

        RabbitMQService.PublishMessage(message, instanceId);

        var result = await ResponsesRegistry.GetResponse(requestID, TimeSpan.FromSeconds(5));

        if (result?.Error != null) {
            throw new MicroserviceException {
                Info = result.Error
            };
        }

        return result?.Data;
    }
}

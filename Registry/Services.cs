using AlfaMicroserviceMesh.Communication;
using AlfaMicroserviceMesh.Constants;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Helpers;
using AlfaMicroserviceMesh.Models.ReqRes;
using AlfaMicroserviceMesh.Models.Service;
using AlfaMicroserviceMesh.Models.Service.Handler;
using AlfaMicroserviceMesh.Validator;
using Serilog;

namespace AlfaMicroserviceMesh.Registry;

public static class Services {
    public readonly static Dictionary<string, Service> _services = [];
    private readonly static Dictionary<string, string> _timers = [];

    public static readonly Context selfContext = new() {
        ServiceName = ServiceBroker.Service.Name,
        InstanceID = ServiceBroker.Service.InstanceID
    };

    public static void AddNode(Context ctx) {
        string node = ctx.ServiceName;
        string InstanceID = ctx.InstanceID;

        if (!_services.ContainsKey(node)) {
            RegisterNode(ctx);
            Log.Information($"Node '{node}' added");
        }

        if (!_services[node].Instances.ContainsKey(InstanceID)) {
            RegisterNodeInstance(ctx);
            Log.Information($"instance '{InstanceID}' added");
        }
        else {
            RemoveNodeInstanceTimer(InstanceID);
        }
        SetInstanceLiveTimeInterval(ctx, 15000);
    }

    private static void RegisterNode(Context ctx) => _services[ctx.ServiceName] = new Service();

    public static void DeleteNode(Context ctx) {
        if (_services.TryGetValue(ctx.ServiceName, out Service? value)) {
            if (value.Instances.ContainsKey(ctx.InstanceID)) {
                RemoveNodeInstance(ctx);
            }
        }
    }

    private static void RegisterNodeInstance(Context ctx) {
        _services[ctx.ServiceName].Instances[ctx.InstanceID] = new ServiceData {
            Actions = ctx.Metadata.Actions,
            Events = ctx.Metadata.Events
        };
    }

    private static void RemoveNodeInstance(Context ctx) {
        RemoveNodeInstanceTimer(ctx.InstanceID);

        _services[ctx.ServiceName].Instances.Remove(ctx.InstanceID);

        if (_services[ctx.ServiceName].Instances.Count is 0) {
            _services.Remove(ctx.ServiceName);
        }

        Log.Warning($"Node '{ctx.InstanceID}' deleted");
    }

    private static void SetInstanceLiveTimeInterval(Context ctx, int liveInterval) {
        var timerID = Timers.SetTimeout(() => RemoveNodeInstance(ctx), liveInterval);

        _timers[ctx.InstanceID] = timerID;
    }

    private static void RemoveNodeInstanceTimer(string InstanceID) =>
        Timers.ClearTimer(_timers[InstanceID]);

    public static string GetNodeInstanceUid(string ServiceName) {
        if (!_services.TryGetValue(ServiceName, out Service? node)) {
            throw new MicroserviceException($"Service '{ServiceName}' not found", ErrorTypes.ServiceNotFound);
        }

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
        _services[node].Instances[instanceId].Actions.ContainsKey(action);

    public static async Task<NewAction> GetActionData(string node, string instanceId, string action) {
        if (!IsActionExists(node, instanceId, action)) {
            throw new MicroserviceException($"Action '{action}' not found", ErrorTypes.ActionNotFound);
        }

        var actionData = await _services[node].Instances[instanceId].Actions[action].SerializeAsync();

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

            if (parameters != null && actionData!.Params is not null) {
                var paramsSchema = await actionData.Params.ConvertToModel<Dictionary<string, ActionParams>>();
                parameters = await CustomValidator.ValidateParams(parameters, paramsSchema!);
            }

            Context context = new() {
                ServiceName = selfContext.ServiceName,
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

            if (result?.Error is not null) {
                
                exception.Info = result.Error;
                retries--;
                await Task.Delay(actionData?.RetryPolicy?.Delay ??
                    ServiceBroker.Service.RetryPolicy.Delay);
            }
            else return result;
        }
        throw exception;
    }

    public static async Task Broadcast(string action, object? parameters = null!) {
        foreach (var node in _services) {
            Context context = new() {
                ServiceName = selfContext.ServiceName,
                InstanceID = selfContext.InstanceID,
                Action = action,
                Event = "event",
                RequestID = Guid.NewGuid().ToString(),
                Request = new Request {
                    Parameters = parameters ?? new { },
                },
            };

            var instanceId = GetNodeInstanceUid(node.Key);

            if (_services[node.Key].Instances[instanceId].Events.Contains(action)) {
                var message = await context.SerializeAsync();
                RabbitMQService.PublishMessage(message, instanceId);
            }
        }
    }
}

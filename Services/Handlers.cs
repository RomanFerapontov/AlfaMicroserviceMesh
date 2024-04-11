using System.Reflection;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.Action;
using AlfaMicroserviceMesh.Models.Node;
using AlfaMicroserviceMesh.Models.ReqRes;

namespace AlfaMicroserviceMesh.Services;

public class Handlers {
    public static readonly Dictionary<string, Func<Context, Task<Response>>> Call = [];
    public static readonly Dictionary<string, int> Timeouts = [];
    public static readonly Dictionary<string, Func<Context, Task>> Emit = [];
    private static readonly InstanceMetadata instancesMetadata = new();

    public static void Add(List<object> handlersList) {
        foreach (var handlers in handlersList) {

            var type = handlers.GetType();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields) {
                if (field.FieldType == typeof(NewAction)) {
                    var action = (NewAction)field.GetValue(handlers)!;

                    Dictionary<string, object> args = [];

                    if (action.Route != null) args.Add("Route", action.Route);
                    if (action.Params != null) args.Add("Params", action.Params);
                    if (action.Access != null) args.Add("Access", action.Access);
                    if (action.RequestTimeout != null) {
                        Timeouts[field.Name] = (int)action.RequestTimeout;
                        args.Add("RequestTimeout", action.RequestTimeout);
                    }
                    if (action.RetryPolicy != null) args.Add("RetryPolicy", action.RetryPolicy);
                    if (action.Caching != null) args.Add("Caching", action.Caching);


                    instancesMetadata.Actions[field.Name] = args;

                    var handler = action?.Handler;

                    if (handler != null) Call[field.Name] = handler;
                }

                if (field.FieldType == typeof(NewEvent)) {
                    instancesMetadata.Events.Add(field.Name);

                    var newEvent = (NewEvent)field.GetValue(handlers)!;
                    var listener = newEvent?.Handler;

                    if (listener != null) Emit[field.Name] = listener;
                }
            }
        };
        Nodes.selfContext.Metadata = instancesMetadata;
    }
}
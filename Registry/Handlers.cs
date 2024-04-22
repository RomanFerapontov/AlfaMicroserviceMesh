using System.Reflection;
using AlfaMicroserviceMesh.Models.ReqRes;
using AlfaMicroserviceMesh.Models.Service;
using AlfaMicroserviceMesh.Models.Service.Handler;

namespace AlfaMicroserviceMesh.Registry;

public class Handlers {
    public static readonly Dictionary<string, Func<Context, Task<Response>>> Call = [];
    public static readonly Dictionary<string, Func<Context, Task>> Emit = [];
    public static readonly Dictionary<string, int> Timeouts = [];
    private static readonly ServiceData instancesMetadata = new();

    public static void Add(List<object> handlersList) {
        foreach (var handlers in handlersList) {
            var type = handlers.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields) {
                if (field.FieldType == typeof(NewAction)) {
                    var action = (NewAction)field.GetValue(handlers)!;

                    Dictionary<string, object> args = [];

                    if (action.Route is not null) args.Add("Route", action.Route);
                    if (action.Params is not null) args.Add("Params", action.Params);
                    if (action.Access is not null) args.Add("Access", action.Access);
                    if (action.RequestTimeout is not null) {
                        Timeouts[field.Name] = (int)action.RequestTimeout;
                        args.Add("RequestTimeout", action.RequestTimeout);
                    }
                    if (action.RetryPolicy is not null) args.Add("RetryPolicy", action.RetryPolicy);
                    if (action.Caching is not null) args.Add("Caching", action.Caching);

                    instancesMetadata.Actions[field.Name] = args;

                    var handler = action?.Handler;

                    if (handler is not null) Call[field.Name] = handler;
                }

                if (field.FieldType == typeof(NewEvent)) {
                    instancesMetadata.Events.Add(field.Name);

                    var newEvent = (NewEvent)field.GetValue(handlers)!;
                    var listener = newEvent?.Handler;

                    if (listener is not null) Emit[field.Name] = listener;
                }
            }
        };
        Services.selfContext.Metadata = instancesMetadata;
    }
}
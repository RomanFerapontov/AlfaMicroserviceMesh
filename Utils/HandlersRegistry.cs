using System.Reflection;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.Action;
using AlfaMicroserviceMesh.Models.Node;

namespace AlfaMicroserviceMesh.Utils;

public class HandlersRegistry {
    public static readonly Dictionary<string, Func<Context, Task<object>>> Call = [];
    public static readonly Dictionary<string, Func<Context, Task>> Emit = [];
    public static readonly InstanceMetadata instancesMetadata = new();

    public static void AddHandlers(List<object> handlersList) {
        foreach (var handlers in handlersList) {

            var type = handlers.GetType();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields) {
                if (field.FieldType == typeof(NewAction)) {
                    var action = (NewAction)field.GetValue(handlers)!;

                    Dictionary<string, object> args = [];

                    if (action.Route != null) args.Add("route", action.Route);
                    if (action.Params != null) args.Add("params", action.Params);
                    if (action.Access != null) args.Add("access", action.Access);
                    if (action?.Caching != null) args.Add("caching", action.Caching);

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
        NodesRegistry.selfContext.Metadata = instancesMetadata;
    }
}
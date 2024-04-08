using System.Reflection;
using AlfaMicroserviceMesh.Mappers;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.Action;
using AlfaMicroserviceMesh.Models.Node;

namespace AlfaMicroserviceMesh.Utils;

public class HandlersRegistry {
    public static readonly Dictionary<string, Func<Context, Task<object>>> Call = [];
    public static readonly InstanceMetadata instancesMetadata = new();

    public static void AddHandlers(List<object> handlersList) {
        foreach (var handlers in handlersList) {

            var type = handlers.GetType();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields) {
                if (field.FieldType == typeof(NewAction)) {
                    var action = (NewAction)field.GetValue(handlers)!;

                    var newAction = new NewAction {
                        Route = action.Route,
                        Params = action.Params,
                        Access = action.Access,
                        Caching = action.Caching,
                    };

                    instancesMetadata.Actions[field.Name] = newAction.ToRegistryDTO();

                    var handler = action.Handler;

                    if (handler != null) Call[field.Name] = handler;
                }
            }
        };
        NodesRegistry.selfContext.Metadata = instancesMetadata;
    }
}
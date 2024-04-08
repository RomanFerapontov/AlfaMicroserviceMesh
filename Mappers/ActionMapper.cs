using AlfaMicroserviceMesh.Dtos;
using AlfaMicroserviceMesh.Models.Action;

namespace AlfaMicroserviceMesh.Mappers;

public static class ActionMapper {
    public static ActionDTO ToRegistryDTO (this NewAction newAction) {
        return new ActionDTO {
            Route = newAction.Route,
            Params = newAction.Params,
            Access = newAction.Access,
            Caching = newAction.Caching,
        };
    }
}

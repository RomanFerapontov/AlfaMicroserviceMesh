namespace AlfaMicroserviceMesh.Models.Service.Handler;

public class NewEvent {
    public Func<Context, Task>? Handler { get; set; }
}

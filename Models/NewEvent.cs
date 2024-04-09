namespace AlfaMicroserviceMesh.Models;

public class NewEvent {
    public Func<Context, Task>? Handler { get; set; }
}

namespace AlfaMicroserviceMesh.Models.Service.Handler;

public class RetryPolicy {
    public int MaxAttempts { get; set; } = 1;
    public int Delay { get; set; } = 0;
}
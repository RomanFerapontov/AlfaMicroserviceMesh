namespace AlfaMicroserviceMesh.Utils;

public static class IntervalTimer {
    private static readonly Dictionary<string, CancellationTokenSource> Timers = [];

    public static string SetTimeout(Action Callback, int interval) {
        string timerID = Guid.NewGuid().ToString();

        CancellationTokenSource cts = new();

        Timers[timerID] = cts;

        var token = cts.Token;

        Task.Run(async () => {
            await Task.Delay(interval);

            while (!token.IsCancellationRequested) {
                Callback();

                await Task.Delay(interval, token);

                ClearTimer(timerID);
            }
        }, token);

        return timerID;
    }


    public static void ClearTimer(string timerID) {
        if (Timers.TryGetValue(timerID, out CancellationTokenSource? cts)) {
            cts.Cancel();
            cts.Dispose();

            Timers.Remove(timerID);
        }
    }
}


using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.ReqRes;

namespace AlfaMicroserviceMesh.Helpers;

public static class Timers {
    private static readonly Dictionary<string, CancellationTokenSource> Tokens = [];

    public static string SetTimeout(Action Callback, int interval) {
        string timerID = Guid.NewGuid().ToString();

        CancellationTokenSource cts = new();

        Tokens[timerID] = cts;

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
        if (Tokens.TryGetValue(timerID, out CancellationTokenSource? cts)) {
            cts.Cancel();
            cts.Dispose();

            Tokens.Remove(timerID);
        }
    }

    public static async Task<Response> ExecuteWithTimeout(Func<Context, Task<Response>> func, Context ctx, int ms) {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var task = func.Invoke(ctx);
        var delayTask = Task.Delay(ms, cancellationToken);

        await Task.WhenAny(task, delayTask);

        if (delayTask.IsCompleted) {
            cancellationTokenSource.Cancel();
            return null!;
        }

        return await task;
    }
}


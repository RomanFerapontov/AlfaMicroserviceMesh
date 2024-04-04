using System.Collections.Concurrent;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.ReqRes;

namespace AlfaMicroserviceMesh.Utils;

public static class ResponsesRegistry {
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<Response>> _responses = new();

    public static Task<Response?> GetResponse(string requestID, TimeSpan interval) {
        var timeoutTask = Task.Delay(interval);
        var responseTcs = new TaskCompletionSource<Response>();
        _responses[requestID] = responseTcs;

        return Task.WhenAny(responseTcs.Task, timeoutTask)
            .ContinueWith(task => {
                _responses.TryRemove(requestID, out _);
                return task.Result == timeoutTask ? null : responseTcs.Task.Result;
            });
    }

    public static void SaveResponse(string requestID, object? data, ErrorData? error) {
        if (_responses.TryGetValue(requestID, out var responseTcs)) {
            Response response = new() { Data = data };

            if (error != null) {
                response.Error = error;
            }

            responseTcs.SetResult(response);
        }
    }
}
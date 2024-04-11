using System.Collections.Concurrent;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Models.ReqRes;

namespace AlfaMicroserviceMesh.Services;

public static class Responses {
    public static readonly ConcurrentDictionary<string, TaskCompletionSource<Response>> _responses = new();

    public static Task<Response> GetResponse(string requestID, TimeSpan interval) {
        var timeoutTask = Task.Delay(interval);
        var responseTcs = new TaskCompletionSource<Response>();
        _responses[requestID] = responseTcs;

        return Task.WhenAny(responseTcs.Task, timeoutTask)
            .ContinueWith(task => {
                _responses.TryRemove(requestID, out _);
                return task.Result == timeoutTask ?
                    new Response { Error = new ErrorData { Errors = ["Request Timeout"] } } : 
                    responseTcs.Task.Result;
            });
    }

    public static void SaveResponse(string requestID, Response? response) {
        if (_responses.TryGetValue(requestID, out var responseTcs)) {
            if (response != null) responseTcs.SetResult(response);
        }
    }
}
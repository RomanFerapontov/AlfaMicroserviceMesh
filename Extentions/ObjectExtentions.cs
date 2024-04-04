using System.Text.Json;

namespace AlfaMicroserviceMesh.Extentions;

public static class ObjectExtentions {
    public static async Task<string> SerializeAsync(this object obj) {
        using MemoryStream stream = new();
        await JsonSerializer.SerializeAsync(stream, obj);
        stream.Position = 0;
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync();
    }

    public static async Task<T> ConvertToModel<T>(this object obj) {
        var modelString = await obj.SerializeAsync();
        return await modelString.DeserializeAsync<T>();
    }
}

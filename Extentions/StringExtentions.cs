using System.Text;
using System.Text.Json;

namespace AlfaMicroserviceMesh.Extentions;

public static class StringExtentions {
    public static async Task<T> DeserializeAsync<T>(this string parameters) {
        byte[] bytes = Encoding.UTF8.GetBytes(parameters);

        using MemoryStream stream = new(bytes);
        {
            var model = await JsonSerializer.DeserializeAsync<T>(stream);
            return model!;
        }
    }
}

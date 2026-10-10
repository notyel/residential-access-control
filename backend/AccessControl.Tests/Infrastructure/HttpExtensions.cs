using AccessControl.Common.DTOs;
using System.Net.Http.Json;

namespace AccessControl.Tests.Infrastructure
{
    public static class HttpExtensions
    {
        /// <summary>Lee el cuerpo como ResponseModel y devuelve Data; falla si la respuesta no fue 2xx.</summary>
        public static async Task<T> ReadDataAsync<T>(this HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {text}");
            var body = await response.Content.ReadFromJsonAsync<ResponseModel<T>>();
            return body!.Data!;
        }

        public static async Task<ResponseModel<object>> ReadErrorAsync(this HttpResponseMessage response)
        {
            var body = await response.Content.ReadFromJsonAsync<ResponseModel<object>>();
            return body!;
        }

        public static string UniqueDocument() => Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
    }
}

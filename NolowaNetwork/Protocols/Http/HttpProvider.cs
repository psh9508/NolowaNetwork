using System.Net.Http.Json;
using System.Text.Json;

namespace NolowaNetwork.Protocols.Http
{
    public interface IHttpHeader
    {
        bool HasHeader(string name);
        bool RemoveHeader(string name);
        bool TryGetHeaderValue(string name, out string value);
        void AddHeader(string name, string value, bool isOverried = false);
    }

    public interface IHttpProvider : IHttpHeader
    {
        Task<(bool IsSuccess, TResult Body)> PostAsync<TResult, TRequest>(string uri, TRequest body, string contentType = "application/json");
        Task<(bool IsSuccess, TResult Body)> GetAsync<TResult>(string uri);
    }

    public class HttpProvider : IHttpProvider
    {
        protected static readonly HttpClient _httpClient = new HttpClient();

        public HttpProvider()
        {
        }

        public bool HasHeader(string name)
        {
            return _httpClient.DefaultRequestHeaders.Contains(name);
        }

        public bool RemoveHeader(string name)
        {
            if (HasHeader(name))
            {
                _httpClient.DefaultRequestHeaders.Remove(name);
                return true;
            }

            return false;
        }

        public bool TryGetHeaderValue(string name, out string value)
        {
            value = string.Empty;

            if (HasHeader(name))
            {
                value = _httpClient.DefaultRequestHeaders.GetValues(name).Single();
                return true;
            }

            return false;
        }

        public void AddHeader(string name, string value, bool isOverried = false)
        {
            if (isOverried)
            {
                if (_httpClient.DefaultRequestHeaders.Contains(name))
                    _httpClient.DefaultRequestHeaders.Remove(name);

                _httpClient.DefaultRequestHeaders.Add(name, value);
            }
            else
            {
                if (_httpClient.DefaultRequestHeaders.Contains(name))
                    return;

                _httpClient.DefaultRequestHeaders.Add(name, value);
            }
        }

        public async Task<(bool IsSuccess, TResult Body)> GetAsync<TResult>(string uri)
        {
            try
            {
                var result = await _httpClient.GetAsync(uri);

                var debug = await result.Content.ReadAsStringAsync();

                return (true, await result.Content.ReadFromJsonAsync<TResult>());
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<(bool IsSuccess, TResult Body)> PostAsync<TResult, TRequest>(string uri, TRequest body)
        {
            return await DoPostBodyAsync<TResult>(async () =>
            {
                var debugBody = JsonSerializer.Serialize(body);

                return await _httpClient.PostAsJsonAsync(uri, body);
            });
        }

        public async Task<(bool IsSuccess, TResult Body)> PostAsync<TResult, TRequest>(string uri, TRequest body, string contentType = "application/json")
        {
            if (contentType == "application/x-www-form-urlencoded")
            {
                return await PostWithURLEncoding<TResult, TRequest>(uri, body);
            }
            else
            {
                return await PostWithJsonEncoding<TResult, TRequest>(uri, body);
            }
        }

        private async Task<(bool IsSuccess, TResult Body)> PostWithJsonEncoding<TResult, TRequest>(string uri, TRequest body)
        {
            return await DoPostBodyAsync<TResult>(async () =>
            {
                var debug = JsonSerializer.Serialize(body);

                return await _httpClient.PostAsJsonAsync(uri, body);
            });
        }

        private async Task<(bool IsSuccess, TResult Body)> PostWithURLEncoding<TResult, TRequest>(string uri, TRequest body)
        {
            return await DoPostBodyAsync<TResult>(async () =>
            {
                var values = ToDictionary(body);

                using (var content = new FormUrlEncodedContent(values))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    return await _httpClient.PostAsync(uri, content);
                }
            });
        }

        private async Task<(bool IsSuccess, TResult Body)> DoPostBodyAsync<TResult>(Func<Task<HttpResponseMessage>> postAsync)
        {
            try
            {
                var response = await postAsync();

                var debug = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode == false)
                    return (false, default(TResult));

                return (true, await response.Content.ReadFromJsonAsync<TResult>());
            }
            catch (NotSupportedException) // When content type is not valid
            {
                return (false, default(TResult));
            }
            catch (JsonException ex) // Invalid JSON
            {
                return (false, default(TResult));
            }
        }

        //public static Dictionary<string, string> ToDictionary<T>(this T src)
        public Dictionary<string, string> ToDictionary<T>(T src)
        {
            var jsonBody = JsonSerializer.Serialize(src);

            return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonBody);
        }
    }
}

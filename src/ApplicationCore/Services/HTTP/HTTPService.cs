using ApplicationCore.Exceptions;
using ApplicationCore.Interfaces.Services;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Services.HTTP
{
    public class HTTPService : IHTTPService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HTTPService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<HttpResponseMessage> RequestRaw(HttpMethod method, string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(host),
            };

            if (data is not null)
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint, UriKind.Relative),
                Method = method,
            };

            string token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
            if (token is not null)
                client.DefaultRequestHeaders.Add("Authorization", token);

            if (headers is not null)
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);

            if (data is not null)
                request.Content = data;

            return await client.SendAsync(request);
        }

        public async Task<ResultObject> Request(HttpMethod method, string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null)
        {
            string baseAddress = host ?? _configuration["Gateway:Host"];

            ArgumentNullException.ThrowIfNull(baseAddress);

            headers ??= new Dictionary<string, string>();
            headers.Add("Accept", "application/json");

            var response = await RequestRaw(method, endpoint, baseAddress, data, headers);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultObject>(json);

            if (result is null || !result.Status)
                throw new GatewayException(result?.Message ?? response.StatusCode.ToString());

            return result;
        }

        public async Task<ResultObject> Get(string endpoint, string host = null, IDictionary<string, string> headers = null)
            => await Request(method: HttpMethod.Get, endpoint: endpoint, host: host, headers: headers);

        public async Task<T> Get<T>(string endpoint, string host = null, IDictionary<string, string> headers = null) where T : class
        {
            var response = await Get(endpoint, host, headers);
            return ((JToken)response.Data).ToObject<T>();
        }

        public async Task<PagedResult<object>> GetAsPagedResult(string endpoint, string host = null, IDictionary<string, string> headers = null)
            => await GetAsPagedResult<object>(endpoint, host, headers);

        public async Task<PagedResult<T>> GetAsPagedResult<T>(string endpoint, string host = null, IDictionary<string, string> headers = null) where T : class
        {
            var response = await Get(endpoint, host, headers);
            return ((JToken)response.Data).ToObject<PagedResult<T>>();
        }

        public async Task<ResultObject> Post(string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null)
            => await Request(method: HttpMethod.Post, endpoint: endpoint, host: host, data: data, headers: headers);

        public async Task<ResultObject> Put(string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null)
            => await Request(method: HttpMethod.Put, endpoint: endpoint, host: host, data: data, headers: headers);

        public async Task<ResultObject> Delete(string endpoint, string host = null, IDictionary<string, string> headers = null)
            => await Request(method: HttpMethod.Delete, endpoint: endpoint, host: host, headers: headers);
    }
}
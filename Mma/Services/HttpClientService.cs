using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using Microsoft.Extensions.Http;

namespace Mma.Services;


public class HttpClientService : IRestHelper, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public string BaseUrl
    {
        get => _httpClient.BaseAddress?.ToString() ?? string.Empty;
        set => _httpClient.BaseAddress = new Uri(value);
    }

    public string Token
    {
        get => _httpClient.DefaultRequestHeaders.Authorization?.Parameter ?? string.Empty;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", value);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
    }

    public HttpClientService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<T>(HttpMethod.Get, resource, body, urlSegments, parameters, headers, cancellationToken);
    }

    public async Task<T?> PostAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<T>(HttpMethod.Post, resource, body, urlSegments, parameters, headers, cancellationToken);
    }

    public async Task<T?> PutAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<T>(HttpMethod.Put, resource, body, urlSegments, parameters, headers, cancellationToken);
    }

    public async Task<T?> DeleteAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<T>(HttpMethod.Delete, resource, body, urlSegments, parameters, headers, cancellationToken);
    }

    private async Task<T?> SendRequestAsync<T>(HttpMethod method, string resource, object? body,
        Dictionary<string, object>? urlSegments, Dictionary<string, object>? parameters,
        Dictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var uri = BuildUri(resource, urlSegments, parameters);
            using var request = new HttpRequestMessage(method, uri);

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add body for methods that support it
            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                var jsonContent = JsonSerializer.Serialize(body, _jsonOptions);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, uri.ToString());

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrEmpty(responseContent))
            {
                return default(T);
            }

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new RestHelperException($"HTTP request failed for {method} {resource}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new RestHelperException($"Request timeout for {method} {resource}", ex);
        }
        catch (JsonException ex)
        {
            throw new RestHelperException($"Failed to deserialize response for {method} {resource}", ex);
        }
    }

    private string BuildUri(string resource, Dictionary<string, object>? urlSegments, Dictionary<string, object>? parameters)
    {
        var uri = resource;

        // Replace URL segments
        if (urlSegments != null)
        {
            foreach (var segment in urlSegments)
            {
                uri = uri.Replace($"{{{segment.Key}}}", segment.Value.ToString());
            }
        }

        // Add query parameters
        if (parameters != null && parameters.Any())
        {
            var queryString = string.Join("&",
                parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value.ToString() ?? string.Empty)}"));
            uri += uri.Contains("?") ? "&" + queryString : "?" + queryString;
        }

        return uri;
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, string requestUri)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        var errorMessage = $"Request to {requestUri} failed with status {response.StatusCode} ({(int)response.StatusCode}). Response: {errorContent}";

        throw new RestHelperException(errorMessage)
        {
            StatusCode = response.StatusCode,
            ResponseContent = errorContent
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

public class RestHelperException : Exception
{
    public HttpStatusCode? StatusCode { get; init; }
    public string? ResponseContent { get; init; }

    public RestHelperException(string message) : base(message) { }
    public RestHelperException(string message, Exception innerException) : base(message, innerException) { }
}

public static class HttpClientHelperExtensions
{
    public static IServiceCollection AddRestHelper(this IServiceCollection services,
        Action<HttpClientService>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.TryAddSingleton<IHttpClientFactory, DefaultHttpClientFactory>();

        services.Add(new ServiceDescriptor(typeof(IRestHelper), provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(HttpClientService));
            var clientHelper = new HttpClientService(httpClient);
            configure?.Invoke(clientHelper);
            return clientHelper;
        }, lifetime));

        return services;
    }

    public static IServiceCollection AddRestHelper(this IServiceCollection services,
        string baseUrl,
        string? token = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddRestHelper(helper =>
        {
            helper.BaseUrl = baseUrl;
            if (!string.IsNullOrEmpty(token))
            {
                helper.Token = token;
            }
        }, lifetime);
    }
}

// Minimal HttpClientFactory implementation for cases where it's not available
internal class DefaultHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}
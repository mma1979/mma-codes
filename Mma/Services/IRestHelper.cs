using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mma.Services;

public interface IRestHelper
{
    Task<T?> GetAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<T?> PostAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<T?> PutAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<T?> DeleteAsync<T>(string resource, object? body = null,
        Dictionary<string, object>? urlSegments = null,
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}
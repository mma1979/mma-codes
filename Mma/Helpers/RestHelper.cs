using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Newtonsoft.Json;

using RestSharp;

using System.Net;

namespace Mma.Helpers;

public class RestHelper
{
    public string BaseUrl { get; set; } = "";
    public  string Token { get; set; } = "";


    public Task<T> APICaller<T>(string resource, Method method, object? body, List<Tuple<string, object>>? urlSegment = null, List<Tuple<string, object>>? parameters = null, List<Tuple<string, string>>? headers = null, List<string>? files = null)
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

        var client = new RestClient(BaseUrl);


        var request = new RestRequest(resource, method);

        if (!string.IsNullOrEmpty(Token))
        {
            request.AddHeader("Authorization", Token);
        }

        if (parameters != null && parameters.Any())
        {
            parameters.ForEach(p => request.AddParameter(p.Item1, p.Item2.ToString()));
        }

        if (urlSegment != null && urlSegment.Any())
        {
            urlSegment.ForEach(s => request.AddUrlSegment(s.Item1, s.Item2.ToString()));
        }

        if (headers != null && headers.Any())
        {
            headers.ForEach(h => request.AddHeader(h.Item1, h.Item2));
        }

        if (files != null && files.Any())
        {
            files.ForEach(f => request.AddFile("content", f));
        }

        if (body != null)
        {
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(body);
        }
        var response = client.Execute<T>(request);


        if (response.StatusCode != System.Net.HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
        {
            string message = $"Error retrieving response from {BaseUrl}{resource}.  Check inner details for more info.";
            var exception = new Exception(message, response.ErrorException);
            throw exception;
        }

        var result = JsonConvert.DeserializeObject<T>(response.Content);
        return Task.FromResult(result);
    }

    public async Task<T> Delete<T>(string resource, object? body = null, List<Tuple<string, object>>? urlSegment = null, List<Tuple<string, object>>? parameters = null, List<Tuple<string, string>>? headers = null, List<string>? files = null)
    {
        var res = await APICaller<T>(resource, Method.Delete, body, urlSegment, parameters, headers, files).ConfigureAwait(false);
        return res;
    }

    public async Task<T> Get<T>(string resource, object? body = null,    List<Tuple<string, object>>? urlSegment = null, List<Tuple<string, object>>? parameters = null, List<Tuple<string, string>>? headers = null, List<string>? files = null)
    {
        var res = await APICaller<T>(resource, Method.Get, body, urlSegment, parameters, headers, files).ConfigureAwait(false);
        return res;
    }



    public async Task<T> Post<T>(string resource, object? body, List<Tuple<string, object>>? urlSegment = null, List<Tuple<string, object>>? parameters = null, List<Tuple<string, string>>? headers = null, List<string>? files = null)
    {
        var res = await APICaller<T>(resource, Method.Post, body, urlSegment, parameters, headers, files).ConfigureAwait(false);
        return res;
    }

    public async Task<T> Put<T>(string resource, object? body, List<Tuple<string, object>>? urlSegment = null, List<Tuple<string, object>>? parameters = null, List<Tuple<string, string>>? headers = null, List<string>? files = null)
    {
        var res = await APICaller<T>(resource, Method.Put, body, urlSegment, parameters, headers, files).ConfigureAwait(false);
        return res;
    }



}

public static class RestHelperExtensions
{
    public static IServiceCollection AddRestHelper(this IServiceCollection serviceCollection, Action<RestHelper>? optionsAction = null, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
    {

        var instance = new RestHelper();
        if (optionsAction != null)
            optionsAction(instance);
        serviceCollection.TryAdd(
            new ServiceDescriptor(typeof(RestHelper),
            provider => instance,
            optionsLifetime));

        return serviceCollection;
    }


}
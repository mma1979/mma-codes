using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mma.Constants;

namespace Mma.Helpers;

public class Translator
{

    public string BaseUrl { get; set; }
    public string EndPoint { get; set; }
    public string DefaultLanguage { get; set; }

    private readonly RestHelper _restHelper;

    public Translator()
    {
        var config = GetConfiguration();
        BaseUrl ??= config.GetValue<string>("BaseUrl")!;
        EndPoint ??= config.GetValue<string>("EndPoint")!;
        DefaultLanguage ??= config.GetValue<string>("DefaultLanguage")!;
        _restHelper = new RestHelper { BaseUrl = BaseUrl };
    }

    public Translator(IConfigurationSection config)
    {
        BaseUrl ??= config.GetValue<string>("BaseUrl")!;
        EndPoint ??= config.GetValue<string>("EndPoint")!;
        DefaultLanguage ??= config.GetValue<string>("DefaultLanguage")!;
        _restHelper = new RestHelper { BaseUrl = BaseUrl };
    }

    private static IConfigurationSection GetConfiguration()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

        var Configuration = builder.Build();

        var config = Configuration.GetSection("Translation");

        return config;
    }

    public string Translate(string key, string? lang = null)
    {
        try
        {
            lang ??= DefaultLanguage;

            var res = _restHelper.Get<string>(
                $"{EndPoint}?key={key}&language={lang}").Result;
            return res;
        }
        catch (Exception)
        {

            return key;
        }
    }

    public async Task<string> TranslateAsync(string key, string lang)
    {
        try
        {
            lang ??= DefaultLanguage;

            var res = await _restHelper.Get<string>(
                $"{EndPoint}-async?key={key}&language={lang}");
            return res;
        }
        catch (Exception)
        {

            return key;
        }
    }

    public string[] Translate(string[] keys, string? lang = null)
    {
        try
        {
            lang ??= DefaultLanguage;

            var query = keys.Select(k => $"keys={k}").ToList();
            query.Add($"lang={lang}");
            var queryString = string.Join('&', query);

            var res = _restHelper.Get<string[]>($"{EndPoint}-array?{queryString}").Result;

            return res;
        }
        catch (Exception)
        {
            return keys;
        }
    }

    public async Task<string[]> TranslateAsync(string[] keys, string? lang = null)
    {
        try
        {
            lang ??= DefaultLanguage;
            var query = keys.Select(k => $"keys={k}").ToList();
            query.Add($"lang={lang}");
            var queryString = string.Join('&', query);

            var res = await _restHelper.Get<string[]>($"{EndPoint}-array?{queryString}");

            return res;
        }
        catch (Exception)
        {
            return keys;
        }
    }

    public Dictionary<string, string> TranslateDict(string[] keys, string? lang = null)
    {
        try
        {
            lang ??= DefaultLanguage;
            var query = keys.Select(k => $"keys={k}").ToList();
            query.Add($"lang={lang}");
            var queryString = string.Join('&', query);

            var res = _restHelper.Get<Dictionary<string, string>>($"{EndPoint}-dict?{queryString}").Result;

            return res;
        }
        catch (Exception)
        {
            return keys.ToDictionary(e => e, e => e);
        }
    }

    public async Task<Dictionary<string, string>> TranslateDictAsync(string[] keys, string? lang = null)
    {
        try
        {
            lang ??= DefaultLanguage;
            var query = keys.Select(k => $"keys={k}").ToList();
            query.Add($"lang={lang}");
            var queryString = string.Join('&', query);

            var res = await _restHelper.Get<Dictionary<string, string>>($"{EndPoint}-dict?{queryString}");

            return res;
        }
        catch (Exception)
        {
            return keys.ToDictionary(e => e, e => e);
        }
    }
    public HtmlString TranslateHtml(string key, string? lang = null)
    {
        try
        {
            lang ??= DefaultLanguage;
            var res = _restHelper.Get<string>(
             $"{EndPoint}?key={key}&language={lang}").Result;
            return new HtmlString(res.ToString());
        }
        catch (Exception)
        {

            return new HtmlString(key);
        }
    }

    public async Task<HtmlString> TranslateHtmlAsync(string key, string? lang = null)
    {
        try
        {
            lang ??= DefaultLanguage;
            var res = await _restHelper.Get<string>(
             $"{EndPoint}-async?key={key}&language={lang}");
            return new HtmlString(res.ToString());
        }
        catch (Exception)
        {

            return new HtmlString(key);
        }
    }
}

public static class TranslatorExtensions
{
    public static IServiceCollection AddTranslator(this IServiceCollection serviceCollection, Action<Translator>? optionsAction = null, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
    {
        var httpContext = serviceCollection.BuildServiceProvider().GetRequiredService<IHttpContextAccessor>().HttpContext;
        var lang = httpContext?.Request.Cookies[CookiesNames.Language] ?? "en";
        var instance = new Translator
        {
            DefaultLanguage = lang
        };
        if (optionsAction != null)
            optionsAction(instance);
        serviceCollection.TryAdd(
            new ServiceDescriptor(typeof(Translator),
            provider => instance,
            optionsLifetime));

        return serviceCollection;
    }
}
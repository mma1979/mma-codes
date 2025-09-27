using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

using Mma.Constants;
using Mma.Extensions;
using Mma.Helpers;

using System.Globalization;

namespace Mma.Components;

public class IMmaViewLocalizer : IViewLocalizer
{
    private readonly Translator _translator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public IMmaViewLocalizer(Translator translator, IHttpContextAccessor httpContextAccessor)
    {
        _translator = translator;
        _httpContextAccessor = httpContextAccessor;
    }

    public LocalizedHtmlString this[string key]
    {
        get
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var lang = _httpContextAccessor.HttpContext.Request.Cookies[CookiesNames.Language] ?? "en";

            var value = _translator.Translate(key, lang);
            return new LocalizedHtmlString(key, value);
        }
    }

    public LocalizedHtmlString this[string key, params object[] arguments]
    {
        get
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var lang = _httpContextAccessor.HttpContext.Request.Cookies[CookiesNames.Language];
            var args = !string.IsNullOrEmpty(lang) ? lang :
               arguments.HasAny() ? arguments[0].ToString() : "en";

            var value = _translator.Translate(key, args);
            return new LocalizedHtmlString(key, value);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        throw new NotImplementedException();
    }

    public LocalizedString GetString(string key)
    {
        var value = _translator.Translate(key);
        return new LocalizedString(key, value);
    }

    public LocalizedString GetString(string key, params object[] arguments)
    {
        var value = _translator.Translate(key);
        return new LocalizedString(key, value);
    }

    public IHtmlLocalizer WithCulture(CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
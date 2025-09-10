using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

using Mma.Extensions;
using Mma.Helpers;

using System.Globalization;

namespace Mma.Components;

public class IOPTStringLocalaizer : IStringLocalizer
{
    private readonly Translator _translator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string language = "ar";
    public IOPTStringLocalaizer(Translator translator, IHttpContextAccessor httpContextAccessor)
    {
        _translator = translator;
        _httpContextAccessor = httpContextAccessor;
        language = _httpContextAccessor.HttpContext.Request.Cookies["lang"] ?? "ar";
    }

    public LocalizedString this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            var value = _translator.Translate(key, language);
            return new LocalizedString(key, value);
        }
    }

    public LocalizedString this[string key, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            var args = !string.IsNullOrEmpty(language) ? language :
               arguments.HasAny() ? arguments[0].ToString() : "ar";

            var value = _translator.Translate(key, args);
            return new LocalizedString(key, value);
        }
    }
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        throw new NotImplementedException();
    }

    public IStringLocalizer WithCulture(CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
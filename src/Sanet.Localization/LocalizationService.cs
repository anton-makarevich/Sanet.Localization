using System.Globalization;
using Sanet.Localization.Providers;

namespace Sanet.Localization;

/// <summary>
/// Culture-invariant localization service managing the active language and resolving
/// localized strings through an <see cref="ILocalizationResourcesProvider"/>, without relying
/// on <see cref="CultureInfo"/>/ICU, which keeps it usable on WebAssembly and
/// invariant-globalization builds.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILocalizationResourcesProvider _resourcesProvider;
    private Dictionary<string, string> _localizedStrings = new();
    private Dictionary<string, string> _defaultLocalizedStrings = new();

    public Language ActiveLanguage { get; private set; } = null!;

    public List<Language> Languages { get; }

    /// <summary>
    /// Raised when the active language changes, so consumers can re-resolve localized strings.
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <param name="resourcesProvider">Provider supplying available languages and localized strings.</param>
    public LocalizationService(ILocalizationResourcesProvider resourcesProvider)
    {
        _resourcesProvider = resourcesProvider;
        Languages = _resourcesProvider.GetAvailableLanguages();
        SetActiveLanguage(Languages.First(l => l.IsDefault));
    }

    public void SetActiveLanguage(Language language)
    {
        if (language.IsDefault && _defaultLocalizedStrings.Count == 0)
        {
            _defaultLocalizedStrings = _resourcesProvider.GetStrings(language);
        }

        if (language == ActiveLanguage && _localizedStrings.Count != 0)
        {
            return;
        }

        _localizedStrings = _resourcesProvider.GetStrings(language);
        ActiveLanguage = language;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetActiveLanguage(string languageCode)
    {
        var language = Languages.FirstOrDefault(l => l.Code == languageCode);
        if (language == null)
        {
            throw new FileNotFoundException($"Resource file not found for language: {languageCode}");
        }

        SetActiveLanguage(language);
    }

    public void SetActiveLanguage(CultureInfo cultureInfo)
    {
        // Not supported in this implementation since it requires CultureInfo
        throw new NotSupportedException("Setting culture using CultureInfo is not supported.");
    }

    public string GetString(string key)
    {
        if (_localizedStrings.TryGetValue(key, out var localizedString))
        {
            return localizedString;
        }

        return _defaultLocalizedStrings.TryGetValue(key, out localizedString) ? localizedString : key;
    }
}

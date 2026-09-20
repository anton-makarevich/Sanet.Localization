using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Sanet.Localization;

/// <summary>
/// Culture-invariant localization service reading embedded <c>.resources</c> compiled from
/// <c>.resx</c> files of the resource assembly supplied at construction.
/// It discovers available languages from the assembly manifest resource names,
/// resolves strings for the active language, falls back to the default language
/// and finally to the raw key, without relying on <see cref="CultureInfo"/>/ICU,
/// which keeps it usable on WebAssembly and invariant-globalization builds.
/// </summary>
public class ResourceLocalizationService : ILocalizationService
{
    private readonly Assembly _resourceAssembly;
    private readonly string _resourceBaseName;
    private readonly string _defaultResourceName;
    private readonly string[] _resourceNames;
    private Dictionary<string, string> _localizedStrings = new();
    private Dictionary<string, string> _defaultLocalizedStrings = new();

    public Language ActiveLanguage { get; private set; } = null!;

    public List<Language> Languages { get; }

    /// <summary>
    /// Raised when the active language changes, so consumers can re-resolve localized strings.
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <param name="resourceAssembly">Assembly embedding the <c>.resources</c> files.</param>
    /// <param name="resourceBaseName">Base name of the resources, e.g. <c>Sanet.MagicalYatzy.Resources.Strings</c>.
    /// The default language must be embedded as <c>{resourceBaseName}.resources</c> and every
    /// additional language as <c>{resourceBaseName}-{code}[-{name}].resources</c>.</param>
    public ResourceLocalizationService(Assembly resourceAssembly, string resourceBaseName)
    {
        _resourceAssembly = resourceAssembly;
        _resourceBaseName = resourceBaseName;
        _defaultResourceName = $"{resourceBaseName}.resources";
        _resourceNames =
        [
            .. _resourceAssembly.GetManifestResourceNames()
                .Where(resourceName => resourceName.StartsWith(_resourceBaseName, StringComparison.OrdinalIgnoreCase))
        ];
        Languages = GetAvailableLanguages();
        SetActiveLanguage(Languages.First(l => l.IsDefault));
    }

    public void SetActiveLanguage(Language language)
    {
        if (language.IsDefault && _defaultLocalizedStrings.Count == 0)
        {
            _defaultLocalizedStrings = LoadLocalizedStrings(language);
        }

        if (language == ActiveLanguage && _localizedStrings.Count != 0)
        {
            return;
        }

        _localizedStrings = LoadLocalizedStrings(language);
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

    private Dictionary<string, string> LoadLocalizedStrings(Language language)
    {
        var resourceFileName = language.IsDefault
            ? _defaultResourceName
            : $"{_resourceBaseName}-{language.Code}.resources";

        var matchingResourceName = _resourceNames.FirstOrDefault(resourceName =>
            resourceName.StartsWith(resourceFileName, StringComparison.OrdinalIgnoreCase));

        if (matchingResourceName == null)
        {
            throw new FileNotFoundException($"Resource file not found for language: {language.Code}");
        }

        using var resourceStream = _resourceAssembly.GetManifestResourceStream(matchingResourceName);
        if (resourceStream == null)
        {
            throw new MissingManifestResourceException($"Resource not found for language: {language.Code}");
        }

        using var resourceReader = new ResourceReader(resourceStream);
        var localizedStrings = new Dictionary<string, string>();

        foreach (DictionaryEntry entry in resourceReader)
        {
            var key = entry.Key.ToString();
            if (key == null) continue;
            var value = entry.Value?.ToString() ?? key;
            localizedStrings[key] = value;
        }

        return localizedStrings;
    }

    private List<Language> GetAvailableLanguages()
    {
        var resourceNames = _resourceNames;

        if (resourceNames.Length == 0)
        {
            throw new ApplicationException($"Missing localization resources for base name {_resourceBaseName}.");
        }

        var defaultLanguageResource = resourceNames.FirstOrDefault(lrn =>
            lrn.StartsWith(_defaultResourceName, StringComparison.OrdinalIgnoreCase));
        if (defaultLanguageResource == null)
        {
            throw new ApplicationException("Missing default language resource.");
        }

        var languages = new List<Language> { new("en", true, "english") };

        languages.AddRange(resourceNames
            .Where(l => l.StartsWith($"{_resourceBaseName}-", StringComparison.OrdinalIgnoreCase))
            .Select(l =>
            {
                var languageAttributes = l.Split('.').First(p => p.Contains('-')).Split('-');
                var code = languageAttributes[1];
                var name = languageAttributes.Length > 2 ? string.Join('-', languageAttributes[2..]) : null;
                return new Language(code, false, name);
            }));

        return languages;
    }
}

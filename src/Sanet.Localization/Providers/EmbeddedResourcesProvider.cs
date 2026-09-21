using System.Collections;
using System.Reflection;
using System.Resources;

namespace Sanet.Localization.Providers;

/// <summary>
/// Culture-invariant <see cref="ILocalizationResourcesProvider"/> reading embedded <c>.resources</c>
/// compiled from <c>.resx</c> files of the resource assembly supplied at construction.
/// Languages are discovered via <see cref="Assembly.GetManifestResourceNames"/> and values are read
/// with <see cref="ResourceReader"/>, without relying on <see cref="System.Globalization.CultureInfo"/>,
/// which keeps it usable on WebAssembly and invariant-globalization builds.
/// </summary>
/// <remarks>
/// Naming convention: the default language must be embedded as <c>{resourceBaseName}.resources</c>
/// and every additional language as <c>{resourceBaseName}-{code}.resources</c>.
/// The optional display name of a language is a regular key/value pair with the
/// <c>language_name</c> key in the resource file itself.
/// </remarks>
public class EmbeddedResourcesProvider : ILocalizationResourcesProvider
{
    private const string LanguageNameKey = "language_name";

    private readonly Assembly _resourceAssembly;
    private readonly string _resourceBaseName;
    private readonly string _defaultResourceName;
    private readonly string[] _resourceNames;

    /// <param name="resourceAssembly">Assembly embedding the <c>.resources</c> files.</param>
    /// <param name="resourceBaseName">Base name of the resources, e.g. <c>Sanet.MagicalYatzy.Resources.Strings</c>.</param>
    public EmbeddedResourcesProvider(Assembly resourceAssembly, string resourceBaseName)
    {
        _resourceAssembly = resourceAssembly;
        _resourceBaseName = resourceBaseName;
        _defaultResourceName = $"{resourceBaseName}.resources";
        _resourceNames =
        [
            .. _resourceAssembly.GetManifestResourceNames()
                .Where(resourceName => resourceName.StartsWith(_resourceBaseName, StringComparison.OrdinalIgnoreCase))
        ];
    }

    public List<Language> GetAvailableLanguages()
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

        var languages = new List<Language> { new("en", true, ReadLanguageName(_defaultResourceName)) };

        languages.AddRange(resourceNames
            .Where(l => l.StartsWith($"{_resourceBaseName}-", StringComparison.OrdinalIgnoreCase))
            .Select(l =>
            {
                var languageAttributes = l.Split('.').First(p => p.Contains('-')).Split('-');
                var code = languageAttributes[1];
                return new Language(code, false, ReadLanguageName(l));
            }));

        return languages;
    }

    public Dictionary<string, string> GetStrings(Language language)
    {
        var resourceFileName = language.IsDefault
            ? _defaultResourceName
            : $"{_resourceBaseName}-{language.Code}.resources";

        var matchingResourceName = _resourceNames.FirstOrDefault(resourceName =>
            string.Equals(resourceName, resourceFileName, StringComparison.OrdinalIgnoreCase));

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

    private string? ReadLanguageName(string resourceName)
    {
        using var resourceStream = _resourceAssembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
        {
            throw new MissingManifestResourceException($"Resource not found: {resourceName}");
        }

        using var resourceReader = new ResourceReader(resourceStream);
        foreach (DictionaryEntry entry in resourceReader)
        {
            if (string.Equals(entry.Key.ToString(), LanguageNameKey, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value?.ToString();
            }
        }

        return null;
    }
}

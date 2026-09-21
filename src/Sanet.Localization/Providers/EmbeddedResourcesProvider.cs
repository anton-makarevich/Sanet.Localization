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
/// and every additional language as <c>{resourceBaseName}-{code}[-{name}].resources</c>.
/// </remarks>
public class EmbeddedResourcesProvider : ILocalizationResourcesProvider
{
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

    public Dictionary<string, string> GetStrings(Language language)
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
}

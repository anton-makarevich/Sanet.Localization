namespace Sanet.Localization.Providers;

/// <summary>
/// Provides discovery of available languages and loading of their localized strings.
/// Implementations define where resources come from — embedded assembly resources,
/// the file system, the network or any other source — and which format they use.
/// </summary>
public interface ILocalizationResourcesProvider
{
    /// <summary>
    /// Discovers the languages available from the resource source.
    /// Exactly one language must be marked as default.
    /// </summary>
    /// <exception cref="ApplicationException">Thrown when no resources are available at all or the default language resource is missing.</exception>
    List<Language> GetAvailableLanguages();

    /// <summary>
    /// Loads the localized key/value strings for <paramref name="language"/>.
    /// </summary>
    /// <param name="language">Language to load strings for; must be one reported by <see cref="GetAvailableLanguages"/>.</param>
    /// <exception cref="FileNotFoundException">Thrown when no resources exist for the given language.</exception>
    Dictionary<string, string> GetStrings(Language language);
}

using System.Globalization;

namespace Sanet.Localization;

public interface ILocalizationService
{
    Language ActiveLanguage { get; }

    List<Language> Languages { get; }

    void SetActiveLanguage(Language language);

    void SetActiveLanguage(string languageCode);

    /// <summary>
    /// Not supported by culture-invariant implementations: language selection is driven
    /// by the embedded resource names, not by <see cref="CultureInfo"/>.
    /// </summary>
    void SetActiveLanguage(CultureInfo cultureInfo);

    /// <summary>
    /// Resolves the localized value for <paramref name="key"/>, falling back to the default
    /// language and then to the raw key.
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Raised when the active language changes, so consumers can re-resolve localized strings.
    /// </summary>
    event EventHandler? LanguageChanged;
}

namespace Sanet.Localization.Providers;

/// <summary>
/// Controls which languages a <see cref="CompositeResourcesProvider"/> exposes
/// when the wrapped providers do not all supply the same set of languages.
/// </summary>
/// <remarks>
/// The policy only decides which language codes are exposed by
/// <see cref="ILocalizationResourcesProvider.GetAvailableLanguages"/> and accepted by
/// <see cref="ILocalizationResourcesProvider.GetStrings"/>. For every exposed code,
/// strings are always merged from every provider that advertises that code.
/// </remarks>
public enum LanguageDiscoveryPolicy
{
    /// <summary>
    /// Exposes every language code advertised by at least one wrapped provider. This is the default policy.
    /// </summary>
    Union,

    /// <summary>
    /// Exposes only the language codes advertised by every wrapped provider.
    /// </summary>
    Intersection,

    /// <summary>
    /// Exposes only the language codes of the first provider in input order.
    /// </summary>
    PrimaryOnly
}

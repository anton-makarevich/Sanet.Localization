namespace Sanet.Localization.Providers;

/// <summary>
/// Culture-invariant <see cref="ILocalizationResourcesProvider"/> combining an ordered list of
/// other providers into a single source, so that strings for one language can come from
/// several resource sets (e.g. an application's and a shared library's embedded resources).
/// </summary>
/// <remarks>
/// <para>
/// The input order defines the priority: providers are listed from lowest to highest priority,
/// and for key conflicts the later provider wins, so an extending application can override
/// strings of an earlier source. The display name of a language is the first non-null
/// <c>language_name</c> taken in priority order (that is, from the last provider that reports one).
/// </para>
/// <para>
/// Which languages are exposed is controlled by <see cref="LanguageDiscoveryPolicy"/>
/// (<see cref="LanguageDiscoveryPolicy.Union"/> by default). Languages are matched by
/// <see cref="Language.Code"/> with an ordinal, case-sensitive comparison — never by
/// <see cref="Language"/> record equality. A wrapped provider that does not advertise
/// a code is skipped when strings for that code are requested.
/// </para>
/// <para>
/// All wrapped providers must agree on the default language code; otherwise
/// an <see cref="ApplicationException"/> is thrown, matching the error handling of
/// <see cref="EmbeddedResourcesProvider"/>. Each provider receives <see cref="Language"/> records
/// carrying its own <see cref="Language.IsDefault"/> value, which providers such as
/// <see cref="EmbeddedResourcesProvider"/> use to select their resource.
/// </para>
/// </remarks>
public class CompositeResourcesProvider : ILocalizationResourcesProvider
{
    private readonly ILocalizationResourcesProvider[] _providers;
    private readonly LanguageDiscoveryPolicy _policy;
    private readonly Lazy<DiscoverySnapshot> _snapshot;

    /// <param name="providers">Wrapped providers ordered from lowest to highest priority.</param>
    public CompositeResourcesProvider(params ILocalizationResourcesProvider[] providers)
        : this(LanguageDiscoveryPolicy.Union, providers)
    {
    }

    /// <param name="policy">Policy selecting which languages the composite exposes.</param>
    /// <param name="providers">Wrapped providers ordered from lowest to highest priority.</param>
    public CompositeResourcesProvider(LanguageDiscoveryPolicy policy, params ILocalizationResourcesProvider[] providers)
    {
        if (providers == null)
        {
            throw new ArgumentNullException(nameof(providers));
        }

        if (providers.Length == 0)
        {
            throw new ArgumentException("At least one provider must be supplied.", nameof(providers));
        }

        if (providers.Contains(null))
        {
            throw new ArgumentException("Providers must not contain null entries.", nameof(providers));
        }

        _policy = policy;
        _providers = [.. providers];
        _snapshot = new Lazy<DiscoverySnapshot>(Discover);
    }

    /// <inheritdoc/>
    public List<Language> GetAvailableLanguages() => [.. _snapshot.Value.ExposedLanguages];

    /// <inheritdoc/>
    public Dictionary<string, string> GetStrings(Language language)
    {
        var snapshot = _snapshot.Value;
        if (!snapshot.IsExposed(language.Code))
        {
            throw new FileNotFoundException($"Resource file not found for language: {language.Code}");
        }

        var mergedStrings = new Dictionary<string, string>();
        for (var i = 0; i < _providers.Length; i++)
        {
            var childLanguage = snapshot.GetAdvertisedLanguage(i, language.Code);
            if (childLanguage == null)
            {
                continue;
            }

            foreach (var pair in _providers[i].GetStrings(childLanguage))
            {
                mergedStrings[pair.Key] = pair.Value;
            }
        }

        return mergedStrings;
    }

    private DiscoverySnapshot Discover()
    {
        var advertisedLanguages = new List<Language>[_providers.Length];
        string? defaultCode = null;

        for (var i = 0; i < _providers.Length; i++)
        {
            var languages = _providers[i].GetAvailableLanguages();
            advertisedLanguages[i] = languages;

            var defaults = languages.Where(l => l.IsDefault).ToList();
            if (defaults.Count != 1)
            {
                throw new ApplicationException(
                    $"Provider {_providers[i].GetType().Name} must report exactly one default language, but reported {defaults.Count}.");
            }

            if (defaultCode == null)
            {
                defaultCode = defaults[0].Code;
            }
            else if (!string.Equals(defaultCode, defaults[0].Code, StringComparison.Ordinal))
            {
                throw new ApplicationException(
                    $"Providers must agree on the default language code, but '{defaultCode}' and '{defaults[0].Code}' were reported.");
            }
        }

        return new DiscoverySnapshot(advertisedLanguages, BuildExposedLanguages(advertisedLanguages, defaultCode!));
    }

    private List<Language> BuildExposedLanguages(List<Language>[] advertisedLanguages, string defaultCode)
    {
        var exposedLanguages = new List<Language> { new(defaultCode, true, GetDisplayName(advertisedLanguages, defaultCode)) };
        exposedLanguages.AddRange(GetExposedCodes(advertisedLanguages, defaultCode)
            .Select(code => new Language(code, false, GetDisplayName(advertisedLanguages, code))));
        return exposedLanguages;
    }

    private IEnumerable<string> GetExposedCodes(List<Language>[] advertisedLanguages, string defaultCode)
    {
        switch (_policy)
        {
            case LanguageDiscoveryPolicy.Intersection:
                return advertisedLanguages[0]
                    .Select(l => l.Code)
                    .Distinct(StringComparer.Ordinal)
                    .Where(code => code != defaultCode && advertisedLanguages.All(languages => languages.Any(l => l.Code == code)));
            case LanguageDiscoveryPolicy.PrimaryOnly:
                return advertisedLanguages[0]
                    .Select(l => l.Code)
                    .Where(code => code != defaultCode)
                    .Distinct(StringComparer.Ordinal);
            default: // Union
                return advertisedLanguages
                    .SelectMany((languages, index) => languages.Select(l => (l.Code, index)))
                    .Where(pair => pair.Code != defaultCode)
                    .GroupBy(pair => pair.Code, StringComparer.Ordinal)
                    .OrderBy(group => group.Min(pair => pair.index))
                    .Select(group => group.Key);
        }
    }

    private string? GetDisplayName(List<Language>[] advertisedLanguages, string code)
    {
        for (var i = advertisedLanguages.Length - 1; i >= 0; i--)
        {
            var language = advertisedLanguages[i].FirstOrDefault(l => l.Code == code);
            if (language?.Name != null)
            {
                return language.Name;
            }
        }

        return null;
    }

    private sealed class DiscoverySnapshot
    {
        private readonly List<Language>[] _advertisedLanguages;

        public DiscoverySnapshot(List<Language>[] advertisedLanguages, List<Language> exposedLanguages)
        {
            _advertisedLanguages = advertisedLanguages;
            ExposedLanguages = exposedLanguages;
        }

        public List<Language> ExposedLanguages { get; }

        public bool IsExposed(string code) => ExposedLanguages.Any(l => l.Code == code);

        /// <summary>
        /// Returns the <see cref="Language"/> record the wrapped provider itself reported for
        /// <paramref name="code"/>, preserving its own <see cref="Language.IsDefault"/> value,
        /// or null when the provider does not advertise the code.
        /// </summary>
        public Language? GetAdvertisedLanguage(int providerIndex, string code)
        {
            return _advertisedLanguages[providerIndex].FirstOrDefault(l => l.Code == code);
        }
    }
}

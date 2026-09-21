using Microsoft.Extensions.DependencyInjection;
using Sanet.Localization.Providers;

namespace Sanet.Localization;

public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILocalizationService"/> as a singleton, backed by the supplied
    /// <see cref="ILocalizationResourcesProvider"/>, which is also registered as a singleton.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="resourcesProvider">Provider supplying available languages and localized strings,
    /// e.g. an <see cref="EmbeddedResourcesProvider"/> reading the application's embedded resources.</param>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services,
        ILocalizationResourcesProvider resourcesProvider)
    {
        services.AddSingleton(resourcesProvider);
        return services.AddSingleton<ILocalizationService>(new LocalizationService(resourcesProvider));
    }
}

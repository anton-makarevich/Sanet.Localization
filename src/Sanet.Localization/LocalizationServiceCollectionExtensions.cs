using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Sanet.Localization;

public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILocalizationService"/> as a singleton, configured to read
    /// the embedded resources of the consuming application.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="resourceAssembly">Assembly embedding the application's <c>.resx</c> resources.</param>
    /// <param name="resourceBaseName">Base name of the resources, e.g. <c>Sanet.MagicalYatzy.Resources.Strings</c>.</param>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services,
        Assembly resourceAssembly,
        string resourceBaseName)
    {
        return services.AddSingleton<ILocalizationService>(
            new ResourceLocalizationService(resourceAssembly, resourceBaseName));
    }
}

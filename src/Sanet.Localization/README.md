# Sanet.Localization

Culture-invariant localization engine for .NET. Consuming applications provide their own embedded `.resx` resource sets; the engine reads them without relying on `CultureInfo` or `IStringLocalizer`, which is the only approach proven to work on WebAssembly and invariant-globalization builds.

## Design

- `ILocalizationService` exposes the active `Language`, the list of available languages, language switching and `GetString(key)`, plus a `LanguageChanged` event (contract aligned with `Sanet.MakaMek.Localization`).
- `LocalizationService` implements the contract and is responsible only for active-language management: it tracks the active language, caches its strings (with a separate cache for the default language), and resolves values falling back from the active language to the default language to the raw key.
- `ILocalizationResourcesProvider` (in `Providers/`) is the resource-management seam: it discovers the available languages and loads the key/value strings for a given language. Languages available to the service come entirely from the provider.
- `EmbeddedResourcesProvider` is the first implementation of the provider, reading embedded `.resources` (compiled from the app's `.resx` files) from a resource assembly and base name supplied at construction. Languages are discovered via `Assembly.GetManifestResourceNames()`, values are read with `System.Resources.ResourceReader`. The default language is embedded as `{resourceBaseName}.resources` and every additional language as `{resourceBaseName}-{code}.resources`. Each resource file may contain a `language_name` key — a regular key/value pair used as the language's display name (omit it when not needed). Other providers (file system, network, non-`resx` formats, in-memory dictionaries for tests) can be added by implementing `ILocalizationResourcesProvider` and passing it to `AddLocalization` or `new LocalizationService(provider)`.
- No UI-framework dependency; the only external dependency is `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Usage

Given resources embedded in `MyApp` as `MyApp.Resources.Strings.resources` (default/English) and `MyApp.Resources.Strings-be.resources` (Belarusian):

```csharp
services.AddLocalization(new EmbeddedResourcesProvider(typeof(SomeAppType).Assembly, "MyApp.Resources.Strings"));
```

Then resolve strings through constructor-injected `ILocalizationService.GetString(key)`.

Resource naming convention:
- default language: `Strings.resx` → embedded as `{resourceBaseName}.resources`
- additional languages: `Strings-be.resx` → embedded as `{resourceBaseName}-{code}.resources`

Each file may define a `language_name` key holding the language's display name, e.g. `<data name="language_name"><value>беларуская</value></data>`.

To supply resources from a custom source, implement `ILocalizationResourcesProvider` and pass it to `AddLocalization`.

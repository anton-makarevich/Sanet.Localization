# Sanet.Localization

Culture-invariant localization engine for .NET. Consuming applications provide their own embedded `.resx` resource sets; the engine reads them without relying on `CultureInfo` or `IStringLocalizer`, which is the only approach proven to work on WebAssembly and invariant-globalization builds.

## Design

- `ILocalizationService` exposes the active `Language`, the list of available languages, language switching and `GetString(key)`, plus a `LanguageChanged` event (contract aligned with `Sanet.MakaMek.Localization`).
- `ResourceLocalizationService` implements the contract by reading embedded `.resources` (compiled from the app's `.resx` files) from a resource assembly and base name supplied at construction. Languages are discovered via `Assembly.GetManifestResourceNames()`, values are read with `System.Resources.ResourceReader`, and resolution falls back from the active language to the default language to the raw key.
- No UI-framework dependency; the only external dependency is `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Usage

Given resources embedded in `MyApp.Core` as `MyApp.Resources.Strings.resources` (default/English) and `MyApp.Resources.Strings-be.resources` (Belarusian):

```csharp
services.AddLocalization(typeof(SomeAppType).Assembly, "MyApp.Resources.Strings");
```

Then resolve strings through constructor-injected `ILocalizationService.GetString(key)`.

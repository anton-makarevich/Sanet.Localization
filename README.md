# Sanet.Localization

Culture-invariant localization for .NET.

[![NuGet](https://img.shields.io/nuget/v/Sanet.Localization?label=Sanet.Localization)](https://www.nuget.org/packages/Sanet.Localization)
[![License](https://img.shields.io/github/license/anton-makarevich/Sanet.Localization)](https://github.com/anton-makarevich/Sanet.Localization/blob/main/LICENSE)

## Packages

| Package | Description |
|---|---|
| [Sanet.Localization](https://www.nuget.org/packages/Sanet.Localization) | Culture-invariant localization engine: `ILocalizationService`, `Language` model, resource-agnostic reader. |

## Overview

Sanet.Localization is a small, dependency-free localization engine extracted from
[Sanet.MagicalYatzyXF](https://github.com/anton-makarevich/MagicalYatzyXF). Its contract is
aligned with the localization service already used by [Sanet.MakaMek](https://github.com/anton-makarevich/MakaMek).

Consuming applications provide their own embedded `.resx` resource sets and register them with
`AddLocalization` — the engine reads translated strings keyed by an invariant key, without relying
on `CultureInfo` or `IStringLocalizer`. This culture-invariant approach is the only mechanism
proven to work on WebAssembly and with invariant-globalization builds.

## How it works

- Applications own their localization resources (`.resx` embedded at runtime, or any other source, including dynamic).
- `ILocalizationResourcesProvider` abstracts resource management: it discovers available languages and loads their key/value strings. `EmbeddedResourcesProvider` (embedded `.resources`) is the first implementation; others (file system, network, non-`resx` formats) can be added later. It supports a reserved `language_name` key in resource files for the language display name.
- `ILocalizationService.GetString(string key)` resolves the value for the active language, falling
  back to the default language and then to the raw key.
- The active language is application-managed; listeners react to the `LanguageChanged` event.

## Structure

```
src/Sanet.Localization/          # the engine (ILocalizationService, LocalizationService,
                                 # Language, AddLocalization)
src/Sanet.Localization/Providers # ILocalizationResourcesProvider, EmbeddedResourcesProvider
tests/Sanet.Localization.Tests/  # xUnit v3 test suite
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (pinned in `global.json`)
- [mise](https://mise.jdx.dev) for repo tooling and task management

## Common tasks

All repo tasks are centralized through mise:

```sh
mise run build   # build the solution
mise run test    # run the test suite with code coverage (cobertura)
mise run pack    # create the NuGet package (set VERSION_SUFFIX for preview builds)
mise run install-skills   # install agent skills to .agents/skills
mise run install-serena   # install the Serena MCP server
```

## License

[MIT](LICENSE) © 2026 Anton Makarevich
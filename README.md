# Sanet.Localization

Culture-invariant localization for .NET.

[![NuGet](https://img.shields.io/nuget/v/Sanet.Localization.Core?label=Sanet.Localization.Core)](https://www.nuget.org/packages/Sanet.Localization.Core)
[![License](https://img.shields.io/github/license/anton-makarevich/Sanet.Localization)](https://github.com/anton-makarevich/Sanet.Localization/blob/main/LICENSE)

## Packages

| Package | Description |
|---|---|
| [Sanet.Localization.Core](https://www.nuget.org/packages/Sanet.Localization.Core) | Culture-invariant localization engine: `ILocalizationService`, `Language` model, resource-agnostic reader. |

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
- `ILocalizationService.GetString(string key)` resolves the value for the active language, falling
  back to the default language and then to the raw key.
- The active language is application-managed; listeners react to the `LanguageChanged` event.

## Structure

```
src/Sanet.Localization.Core/     # the engine (ILocalizationService, Language, reader, AddLocalization)
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
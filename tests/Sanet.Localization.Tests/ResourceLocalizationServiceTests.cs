using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Sanet.Localization.Tests;

public class ResourceLocalizationServiceTests
{
    private const string ResourceBaseName = "Sanet.Localization.Tests.Resources.Strings";
    private static readonly Assembly ResourceAssembly = typeof(ResourceLocalizationServiceTests).Assembly;

    private readonly ResourceLocalizationService _sut = new(ResourceAssembly, ResourceBaseName);

    [Fact]
    public void DefaultLanguage_IsSet_When_ServiceIsCreated()
    {
        var defaultLanguage = _sut.Languages.FirstOrDefault(l => l.IsDefault);
        _sut.ActiveLanguage.ShouldBe(defaultLanguage);
    }

    [Fact]
    public void DefaultLanguage_IsEnglish()
    {
        _sut.ActiveLanguage.Code.ShouldBe("en");
    }

    [Fact]
    public void SetActiveLanguage_WithLanguage_Should_SetLanguage()
    {
        var language = _sut.Languages.FirstOrDefault(l => l.Code == "be");
        _sut.SetActiveLanguage(language!);
        _sut.ActiveLanguage.ShouldBe(language);
    }

    [Fact]
    public void SetActiveLanguage_WithLanguageCode_Should_SetLanguage()
    {
        const string languageCode = "be";
        _sut.SetActiveLanguage(languageCode);
        _sut.ActiveLanguage.Code.ShouldBe(languageCode);
    }

    [Fact]
    public void SetActiveLanguage_WithUnknownLanguageCode_ShouldThrowException()
    {
        Should.Throw<FileNotFoundException>(() => _sut.SetActiveLanguage("agr"));
    }

    [Fact]
    public void SetActiveLanguage_WithUnknownLanguageCode_ShouldKeepActiveLanguageAndStrings()
    {
        _sut.SetActiveLanguage("be");
        var localizedValue = _sut.GetString("TestKey");

        Should.Throw<FileNotFoundException>(() => _sut.SetActiveLanguage("agr"));

        _sut.ActiveLanguage.Code.ShouldBe("be");
        _sut.GetString("TestKey").ShouldBe(localizedValue);
    }

    [Fact]
    public void SetActiveLanguage_WithUnknownLanguageCode_ShouldNotRaiseLanguageChanged()
    {
        var raised = 0;
        _sut.LanguageChanged += (_, _) => raised++;

        Should.Throw<FileNotFoundException>(() => _sut.SetActiveLanguage("agr"));

        raised.ShouldBe(0);
    }

    [Fact]
    public void SetActiveLanguage_WithUnavailableLanguage_ShouldThrowException()
    {
        var language = new Language("agr", false);
        Should.Throw<FileNotFoundException>(() => _sut.SetActiveLanguage(language));
    }

    [Fact]
    public void SetActiveLanguage_WithCulture_ShouldThrowException()
    {
        Should.Throw<NotSupportedException>(() => _sut.SetActiveLanguage(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void GetString_Should_ReturnLocalizedValue_When_ItExistsForSelectedLanguage()
    {
        _sut.SetActiveLanguage(new Language("be", false));
        var localizedString = _sut.GetString("TestKey");
        localizedString.ShouldBe("Прывітанне");
    }

    [Fact]
    public void GetString_Should_ReturnDefaultLocalizedValue_When_ItDoesNotExistForSelectedLanguage()
    {
        _sut.SetActiveLanguage(new Language("be", false));
        var localizedString = _sut.GetString("DefaultOnlyKey");
        localizedString.ShouldBe("Only in default");
    }

    [Fact]
    public void GetString_Should_ReturnRawKey_When_ItDoesNotExistInAnyLanguage()
    {
        _sut.SetActiveLanguage(new Language("be", false));
        var localizedString = _sut.GetString("UnknownKey");
        localizedString.ShouldBe("UnknownKey");
    }

    [Fact]
    public void GetString_Should_ReturnDefaultValue_When_DefaultLanguageIsActive()
    {
        _sut.SetActiveLanguage(new Language("en", true));
        _sut.GetString("TestKey").ShouldBe("Hello");
    }

    [Fact]
    public void Languages_ReturnsAllAvailableLanguages()
    {
        _sut.Languages.Count.ShouldBe(2);
        _sut.Languages.ShouldContain(l => l.Code == "en" && l.IsDefault);
        _sut.Languages.ShouldContain(l => l.Code == "be" && !l.IsDefault);
    }

    [Fact]
    public void LanguageChanged_IsRaised_When_ActiveLanguageChanges()
    {
        var raised = 0;
        _sut.LanguageChanged += (_, _) => raised++;

        _sut.SetActiveLanguage("be");

        raised.ShouldBe(1);
    }

    [Fact]
    public void LanguageChanged_IsNotRaised_When_SameLanguageIsSetAgain()
    {
        var raised = 0;
        _sut.LanguageChanged += (_, _) => raised++;

        _sut.SetActiveLanguage("en");

        raised.ShouldBe(0);
    }

    [Fact]
    public void AddLocalization_RegistersSingletonService()
    {
        var services = new ServiceCollection();
        services.AddLocalization(ResourceAssembly, ResourceBaseName);

        var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<ILocalizationService>();
        var second = provider.GetRequiredService<ILocalizationService>();

        first.ShouldBeSameAs(second);
        first.ShouldBeOfType<ResourceLocalizationService>();
        first.ActiveLanguage.Code.ShouldBe("en");
    }
}

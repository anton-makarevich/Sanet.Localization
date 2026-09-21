using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sanet.Localization.Providers;
using Shouldly;

namespace Sanet.Localization.Tests;

public class LocalizationServiceTests
{
    private readonly ILocalizationResourcesProvider _provider = Substitute.For<ILocalizationResourcesProvider>();

    private LocalizationService CreateSut()
    {
        return new LocalizationService(_provider);
    }

    private void SetupProvider()
    {
        var languages = new List<Language>
        {
            new("en", true, "english"),
            new("be", false)
        };
        _provider.GetAvailableLanguages().Returns(languages);
        _provider.GetStrings(Arg.Is<Language>(l => l.Code == "en" && l.IsDefault))
            .Returns(new Dictionary<string, string> { { "TestKey", "Hello" }, { "DefaultOnlyKey", "Only in default" } });
        _provider.GetStrings(Arg.Is<Language>(l => l.Code == "be" && !l.IsDefault))
            .Returns(new Dictionary<string, string> { { "TestKey", "Прывітанне" } });
    }

    [Fact]
    public void Constructor_SetsDefaultLanguageAsActive()
    {
        SetupProvider();
        var sut = CreateSut();

        sut.ActiveLanguage.ShouldBe(sut.Languages.First(l => l.IsDefault));
        sut.ActiveLanguage.Code.ShouldBe("en");
    }

    [Fact]
    public void Constructor_Throws_WhenNoDefaultLanguage()
    {
        _provider.GetAvailableLanguages().Returns([new Language("be", false)]);

        Should.Throw<InvalidOperationException>(() => CreateSut());
    }

    [Fact]
    public void Languages_ReturnsLanguagesFromProvider()
    {
        SetupProvider();
        var sut = CreateSut();

        sut.Languages.Count.ShouldBe(2);
        sut.Languages.ShouldContain(l => l.Code == "en" && l.IsDefault);
        sut.Languages.ShouldContain(l => l.Code == "be" && !l.IsDefault);
    }

    [Fact]
    public void SetActiveLanguage_WithLanguage_Should_SetLanguage()
    {
        SetupProvider();
        var sut = CreateSut();
        var language = sut.Languages.First(l => l.Code == "be");

        sut.SetActiveLanguage(language);

        sut.ActiveLanguage.ShouldBe(language);
    }

    [Fact]
    public void SetActiveLanguage_WithLanguageCode_Should_SetLanguage()
    {
        SetupProvider();
        var sut = CreateSut();

        sut.SetActiveLanguage("be");

        sut.ActiveLanguage.Code.ShouldBe("be");
    }

    [Fact]
    public void SetActiveLanguage_WithUnknownLanguageCode_ShouldThrowException()
    {
        SetupProvider();
        var sut = CreateSut();

        Should.Throw<FileNotFoundException>(() => sut.SetActiveLanguage("agr"));
    }

    [Fact]
    public void SetActiveLanguage_WithUnknownLanguageCode_ShouldKeepActiveLanguageAndStrings()
    {
        SetupProvider();
        var sut = CreateSut();
        sut.SetActiveLanguage("be");
        var localizedValue = sut.GetString("TestKey");

        Should.Throw<FileNotFoundException>(() => sut.SetActiveLanguage("agr"));

        sut.ActiveLanguage.Code.ShouldBe("be");
        sut.GetString("TestKey").ShouldBe(localizedValue);
    }

    [Fact]
    public void SetActiveLanguage_WithUnknownLanguageCode_ShouldNotRaiseLanguageChanged()
    {
        SetupProvider();
        var sut = CreateSut();
        var raised = 0;
        sut.LanguageChanged += (_, _) => raised++;

        Should.Throw<FileNotFoundException>(() => sut.SetActiveLanguage("agr"));

        raised.ShouldBe(0);
    }

    [Fact]
    public void SetActiveLanguage_WithCulture_ShouldThrowException()
    {
        SetupProvider();
        var sut = CreateSut();

        Should.Throw<NotSupportedException>(() => sut.SetActiveLanguage(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void GetString_Should_ReturnLocalizedValue_When_ItExistsForSelectedLanguage()
    {
        SetupProvider();
        var sut = CreateSut();
        sut.SetActiveLanguage(new Language("be", false));

        sut.GetString("TestKey").ShouldBe("Прывітанне");
    }

    [Fact]
    public void GetString_Should_ReturnDefaultLocalizedValue_When_ItDoesNotExistForSelectedLanguage()
    {
        SetupProvider();
        var sut = CreateSut();
        sut.SetActiveLanguage(new Language("be", false));

        sut.GetString("DefaultOnlyKey").ShouldBe("Only in default");
    }

    [Fact]
    public void GetString_Should_ReturnRawKey_When_ItDoesNotExistInAnyLanguage()
    {
        SetupProvider();
        var sut = CreateSut();
        sut.SetActiveLanguage(new Language("be", false));

        sut.GetString("UnknownKey").ShouldBe("UnknownKey");
    }

    [Fact]
    public void GetString_Should_ReturnDefaultValue_When_DefaultLanguageIsActive()
    {
        SetupProvider();
        var sut = CreateSut();

        sut.GetString("TestKey").ShouldBe("Hello");
    }

    [Fact]
    public void LanguageChanged_IsRaised_When_ActiveLanguageChanges()
    {
        SetupProvider();
        var sut = CreateSut();
        var raised = 0;
        sut.LanguageChanged += (_, _) => raised++;

        sut.SetActiveLanguage("be");

        raised.ShouldBe(1);
    }

    [Fact]
    public void LanguageChanged_IsNotRaised_When_SameLanguageIsSetAgain()
    {
        SetupProvider();
        var sut = CreateSut();
        var raised = 0;
        sut.LanguageChanged += (_, _) => raised++;

        sut.SetActiveLanguage("en");

        raised.ShouldBe(0);
    }

    [Fact]
    public void AddLocalization_RegistersSingletonService()
    {
        SetupProvider();
        var services = new ServiceCollection();
        services.AddLocalization(_provider);

        var serviceProvider = services.BuildServiceProvider();
        var first = serviceProvider.GetRequiredService<ILocalizationService>();
        var second = serviceProvider.GetRequiredService<ILocalizationService>();
        var resolvedProvider = serviceProvider.GetRequiredService<ILocalizationResourcesProvider>();

        first.ShouldBeSameAs(second);
        first.ShouldBeOfType<LocalizationService>();
        resolvedProvider.ShouldBeSameAs(_provider);
        first.ActiveLanguage.Code.ShouldBe("en");
    }
}

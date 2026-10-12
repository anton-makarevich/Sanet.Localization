using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sanet.Localization.Providers;
using Shouldly;

namespace Sanet.Localization.Tests;

public class CompositeResourcesProviderTests
{
    private static readonly Assembly ResourceAssembly = typeof(CompositeResourcesProviderTests).Assembly;

    private static Language DefaultEn() => new("en", true, "english");
    private static Language Be() => new("be", false);
    private static Language Fr() => new("fr", false);

    private static ILocalizationResourcesProvider CreateSubstitute(List<Language> languages)
    {
        var provider = Substitute.For<ILocalizationResourcesProvider>();
        provider.GetAvailableLanguages().Returns(languages);
        return provider;
    }

    private static ILocalizationResourcesProvider CreateProviderA() => CreateSubstitute(
    [
        DefaultEn(),
        Be()
    ]);

    private static ILocalizationResourcesProvider CreateProviderB() => CreateSubstitute(
    [
        new Language("en", true),
        Fr()
    ]);

    // --- Constructor validation ---

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenProvidersArrayIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new CompositeResourcesProvider(null!));
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenProvidersArrayIsEmpty()
    {
        Should.Throw<ArgumentException>(() => new CompositeResourcesProvider());
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenProvidersContainNullEntry()
    {
        Should.Throw<ArgumentException>(() => new CompositeResourcesProvider(CreateProviderA(), null!));
    }

    [Fact]
    public void Constructor_DoesNotCallChildProviders()
    {
        var providerA = CreateProviderA();

        _ = new CompositeResourcesProvider(providerA);

        providerA.DidNotReceive().GetAvailableLanguages();
    }

    // --- Language discovery ---

    [Fact]
    public void GetAvailableLanguages_Union_ExposesAllCodesInFirstAppearanceOrder()
    {
        var sut = new CompositeResourcesProvider(CreateProviderA(), CreateProviderB());

        var languages = sut.GetAvailableLanguages();

        languages.Count.ShouldBe(3);
        languages[0].Code.ShouldBe("en");
        languages[0].IsDefault.ShouldBeTrue();
        languages[1].Code.ShouldBe("be");
        languages[2].Code.ShouldBe("fr");
        languages.Count(l => l.IsDefault).ShouldBe(1);
    }

    [Fact]
    public void GetAvailableLanguages_Intersection_ExposesOnlySharedCodes()
    {
        var sut = new CompositeResourcesProvider(
            LanguageDiscoveryPolicy.Intersection,
            CreateProviderA(),
            CreateProviderB());

        var languages = sut.GetAvailableLanguages();

        languages.Count.ShouldBe(1);
        languages[0].Code.ShouldBe("en");
        languages[0].IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void GetAvailableLanguages_PrimaryOnly_ExposesOnlyPrimaryCodes()
    {
        var sut = new CompositeResourcesProvider(
            LanguageDiscoveryPolicy.PrimaryOnly,
            CreateProviderA(),
            CreateProviderB());

        var languages = sut.GetAvailableLanguages();

        languages.Count.ShouldBe(2);
        languages[0].Code.ShouldBe("en");
        languages[0].IsDefault.ShouldBeTrue();
        languages[1].Code.ShouldBe("be");
    }

    // --- Default-language agreement ---

    [Fact]
    public void GetAvailableLanguages_ThrowsApplicationException_WhenDefaultCodesDiffer()
    {
        var providerWithDifferentDefault = CreateSubstitute([new Language("de", true, "deutsch")]);

        var sut = new CompositeResourcesProvider(CreateProviderA(), providerWithDifferentDefault);

        var ex = Should.Throw<ApplicationException>(() => sut.GetAvailableLanguages());
        ex.Message.ShouldContain("en");
        ex.Message.ShouldContain("de");
    }

    [Fact]
    public void GetAvailableLanguages_ThrowsApplicationException_WhenChildHasNoDefaultLanguage()
    {
        var providerWithoutDefault = CreateSubstitute([Be()]);

        var sut = new CompositeResourcesProvider(CreateProviderA(), providerWithoutDefault);

        Should.Throw<ApplicationException>(() => sut.GetAvailableLanguages());
    }

    [Fact]
    public void GetAvailableLanguages_ThrowsApplicationException_WhenChildHasTwoDefaults()
    {
        var providerWithTwoDefaults = CreateSubstitute(
        [
            DefaultEn(),
            new Language("de", true)
        ]);

        var sut = new CompositeResourcesProvider(providerWithTwoDefaults);

        Should.Throw<ApplicationException>(() => sut.GetAvailableLanguages());
    }

    // --- Display names ---

    [Fact]
    public void GetAvailableLanguages_UsesDisplayNameFromLastProvider()
    {
        var providerA = CreateSubstitute([new Language("en", true, "english A"), Be()]);
        var providerB = CreateSubstitute([new Language("en", true, "english B"), Fr()]);

        var sut = new CompositeResourcesProvider(providerA, providerB);

        sut.GetAvailableLanguages().ShouldContain(l => l.Code == "en" && l.Name == "english B");
    }

    [Fact]
    public void GetAvailableLanguages_FallsBackToEarlierName_WhenLastNameIsNull()
    {
        var providerA = CreateSubstitute([new Language("en", true, "english A"), Be()]);
        var providerB = CreateSubstitute([new Language("en", true), Fr()]);

        var sut = new CompositeResourcesProvider(providerA, providerB);

        sut.GetAvailableLanguages().ShouldContain(l => l.Code == "en" && l.Name == "english A");
    }

    // --- String merging ---

    [Fact]
    public void GetStrings_LaterProviderOverridesDuplicateKey()
    {
        var providerA = CreateSubstitute([DefaultEn()]);
        providerA.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "TestKey", "A value" } });
        var providerB = CreateSubstitute([new Language("en", true)]);
        providerB.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "TestKey", "B value" } });

        var sut = new CompositeResourcesProvider(providerA, providerB);

        sut.GetStrings(new Language("en", true))["TestKey"].ShouldBe("B value");
    }

    [Fact]
    public void GetStrings_EarlierProviderOverridesDuplicateKey_WhenOrderReversed()
    {
        var providerA = CreateSubstitute([DefaultEn()]);
        providerA.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "TestKey", "A value" } });
        var providerB = CreateSubstitute([new Language("en", true)]);
        providerB.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "TestKey", "B value" } });

        var sut = new CompositeResourcesProvider(providerB, providerA);

        sut.GetStrings(new Language("en", true))["TestKey"].ShouldBe("A value");
    }

    [Fact]
    public void GetStrings_Union_SkipsProvidersNotAdvertisingTheCode()
    {
        var providerA = CreateProviderA();
        var providerB = CreateProviderB();
        providerA.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "KeyA", "A" } });
        providerB.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "KeyB", "B" } });

        var sut = new CompositeResourcesProvider(providerA, providerB);

        sut.GetStrings(new Language("be", false));

        providerA.Received(1).GetStrings(Arg.Is<Language>(l => l.Code == "be"));
        providerB.DidNotReceive().GetStrings(Arg.Any<Language>());
    }

    [Fact]
    public void GetStrings_Union_DoesNotCallProviderAForFrench()
    {
        var providerA = CreateProviderA();
        var providerB = CreateProviderB();
        providerB.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "KeyB", "B" } });

        var sut = new CompositeResourcesProvider(providerA, providerB);

        sut.GetStrings(new Language("fr", false));

        providerA.DidNotReceive().GetStrings(Arg.Any<Language>());
        providerB.Received(1).GetStrings(Arg.Is<Language>(l => l.Code == "fr"));
    }

    [Fact]
    public void GetStrings_PrimaryOnly_MergesStringsFromSecondaryProviderAdvertisingTheCode()
    {
        var providerA = CreateSubstitute([DefaultEn(), Be()]);
        providerA.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "KeyA", "A" } });
        var providerB = CreateSubstitute([new Language("en", true), Be()]);
        providerB.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "KeyB", "B" } });

        var sut = new CompositeResourcesProvider(
            LanguageDiscoveryPolicy.PrimaryOnly,
            providerA,
            providerB);

        var strings = sut.GetStrings(new Language("be", false));

        strings["KeyA"].ShouldBe("A");
        strings["KeyB"].ShouldBe("B");
    }

    [Fact]
    public void GetStrings_PrimaryOnly_ThrowsFileNotFoundException_ForNonExposedCode()
    {
        var sut = new CompositeResourcesProvider(
            LanguageDiscoveryPolicy.PrimaryOnly,
            CreateProviderA(),
            CreateProviderB());

        Should.Throw<FileNotFoundException>(() => sut.GetStrings(new Language("fr", false)));
    }

    [Fact]
    public void GetStrings_ThrowsFileNotFoundException_ForUnknownCode()
    {
        var sut = new CompositeResourcesProvider(CreateProviderA(), CreateProviderB());

        Should.Throw<FileNotFoundException>(() => sut.GetStrings(new Language("agr", false)));
    }

    [Fact]
    public void GetStrings_ForwardsChildsOwnLanguageRecord()
    {
        var beLanguage = new Language("be", false, "беларуская");
        var provider = CreateSubstitute([DefaultEn(), beLanguage]);
        provider.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string>());

        var sut = new CompositeResourcesProvider(provider);

        sut.GetStrings(new Language("be", false));

        provider.Received(1).GetStrings(Arg.Is<Language>(l => ReferenceEquals(l, beLanguage)));
    }

    // --- Failure propagation ---

    [Fact]
    public void GetAvailableLanguages_PropagatesChildDiscoveryException()
    {
        var failingProvider = Substitute.For<ILocalizationResourcesProvider>();
        failingProvider.GetAvailableLanguages()
            .Throws(new ApplicationException("child failure"));

        var sut = new CompositeResourcesProvider(CreateProviderA(), failingProvider);

        var ex = Should.Throw<ApplicationException>(() => sut.GetAvailableLanguages());
        ex.Message.ShouldBe("child failure");
    }

    [Fact]
    public void GetStrings_PropagatesChildLoadException()
    {
        var provider = CreateProviderA();
        provider.GetStrings(Arg.Any<Language>()).Throws(new ApplicationException("load failure"));

        var sut = new CompositeResourcesProvider(provider);

        var ex = Should.Throw<ApplicationException>(() => sut.GetStrings(new Language("en", true)));
        ex.Message.ShouldBe("load failure");
    }

    // --- Snapshot ---

    [Fact]
    public void GetAvailableLanguages_DiscoveryRunsOncePerChild()
    {
        var providerA = CreateProviderA();
        var providerB = CreateProviderB();

        var sut = new CompositeResourcesProvider(providerA, providerB);

        sut.GetAvailableLanguages();
        sut.GetAvailableLanguages();

        providerA.Received(1).GetAvailableLanguages();
        providerB.Received(1).GetAvailableLanguages();
    }

    // --- Integration with real embedded resources ---

    [Fact]
    public void GetAvailableLanguages_WithEmbeddedProviders_MergesLanguageFamilies()
    {
        var sut = new CompositeResourcesProvider(
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Strings"),
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Errors"));

        var languages = sut.GetAvailableLanguages();

        languages.ShouldContain(l => l.Code == "en" && l.IsDefault && l.Name == "english");
        languages.ShouldContain(l => l.Code == "be" && !l.IsDefault && l.Name == "беларуская");
        languages.ShouldContain(l => l.Code == "fr" && !l.IsDefault && l.Name == null);
        languages.ShouldContain(l => l.Code == "de" && !l.IsDefault && l.Name == "deutsch");
    }

    [Fact]
    public void GetStrings_WithEmbeddedProviders_MergesStringsFromBothFamilies()
    {
        var sut = new CompositeResourcesProvider(
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Strings"),
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Errors"));

        var defaultStrings = sut.GetStrings(new Language("en", true));
        var beStrings = sut.GetStrings(new Language("be", false));
        var frStrings = sut.GetStrings(new Language("fr", false));
        var deStrings = sut.GetStrings(new Language("de", false));

        defaultStrings["DefaultOnlyKey"].ShouldBe("Only in default");
        defaultStrings["ErrorKey"].ShouldBe("English error");
        defaultStrings["TestKey"].ShouldBe("English test");
        beStrings["TestKey"].ShouldBe("Прывітанне");
        beStrings["ErrorKey"].ShouldBe("Памылка");
        frStrings["TestKey"].ShouldBe("Bonjour");
        frStrings.ContainsKey("ErrorKey").ShouldBeFalse();
        deStrings["ErrorKey"].ShouldBe("Fehler");
        deStrings.ContainsKey("TestKey").ShouldBeFalse();
    }

    [Fact]
    public void GetStrings_MixedSources_CustomProviderOverridesEmbedded()
    {
        var embedded = new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Strings");
        var custom = CreateSubstitute([DefaultEn()]);
        custom.GetStrings(Arg.Any<Language>())
            .Returns(new Dictionary<string, string> { { "TestKey", "Custom" } });

        var sut = new CompositeResourcesProvider(embedded, custom);

        sut.GetStrings(new Language("en", true))["TestKey"].ShouldBe("Custom");
    }

    // --- Service integration ---

    [Fact]
    public void LocalizationService_WithComposite_FallsBackToMergedDefaultStrings()
    {
        var sut = new CompositeResourcesProvider(
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Strings"),
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Errors"));
        var service = new LocalizationService(sut);

        service.SetActiveLanguage("fr");

        service.GetString("TestKey").ShouldBe("Bonjour");
        service.GetString("ErrorKey").ShouldBe("English error");
        service.GetString("UnknownKey").ShouldBe("UnknownKey");
    }

    // --- DI registration ---

    [Fact]
    public void AddLocalization_RegistersCompositeAsTheResolvedProvider()
    {
        var composite = new CompositeResourcesProvider(
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Strings"),
            new EmbeddedResourcesProvider(ResourceAssembly, "Sanet.Localization.Tests.Resources.Errors"));
        var services = new ServiceCollection();
        services.AddLocalization(composite);

        var serviceProvider = services.BuildServiceProvider();
        var resolvedProvider = serviceProvider.GetRequiredService<ILocalizationResourcesProvider>();
        var service = serviceProvider.GetRequiredService<ILocalizationService>();

        resolvedProvider.ShouldBeSameAs(composite);
        service.ActiveLanguage.Code.ShouldBe("en");
    }
}

using System.Reflection;
using Sanet.Localization.Providers;
using Shouldly;

namespace Sanet.Localization.Tests;

public class EmbeddedResourcesProviderTests
{
    private const string ResourceBaseName = "Sanet.Localization.Tests.Resources.Strings";
    private static readonly Assembly ResourceAssembly = typeof(EmbeddedResourcesProviderTests).Assembly;

    private readonly EmbeddedResourcesProvider _sut = new(ResourceAssembly, ResourceBaseName);

    [Fact]
    public void GetAvailableLanguages_ReturnsAllAvailableLanguages()
    {
        var languages = _sut.GetAvailableLanguages();

        languages.Count.ShouldBe(2);
        languages.ShouldContain(l => l.Code == "en" && l.IsDefault);
        languages.ShouldContain(l => l.Code == "be" && !l.IsDefault);
    }

    [Fact]
    public void GetStrings_ReturnsLocalizedValues_ForDefaultLanguage()
    {
        var strings = _sut.GetStrings(new Language("en", true));

        strings["TestKey"].ShouldBe("Hello");
        strings["DefaultOnlyKey"].ShouldBe("Only in default");
    }

    [Fact]
    public void GetStrings_ReturnsLocalizedValues_ForNonDefaultLanguage()
    {
        var strings = _sut.GetStrings(new Language("be", false));

        strings["TestKey"].ShouldBe("Прывітанне");
    }

    [Fact]
    public void GetStrings_ThrowsFileNotFoundException_ForUnknownLanguage()
    {
        Should.Throw<FileNotFoundException>(() => _sut.GetStrings(new Language("agr", false)));
    }

    [Fact]
    public void GetAvailableLanguages_ThrowsApplicationException_WhenNoResourcesExist()
    {
        var sut = new EmbeddedResourcesProvider(ResourceAssembly, "Missing.Base.Name");

        Should.Throw<ApplicationException>(() => sut.GetAvailableLanguages());
    }
}

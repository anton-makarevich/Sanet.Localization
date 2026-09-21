using System.Reflection;
using System.Resources;
using NSubstitute;
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

        languages.Count.ShouldBe(3);
        languages.ShouldContain(l => l.Code == "en" && l.IsDefault && l.Name == "english");
        languages.ShouldContain(l => l.Code == "be" && !l.IsDefault && l.Name == "беларуская");
        languages.ShouldContain(l => l.Code == "fr" && !l.IsDefault && l.Name == null);
    }

    [Fact]
    public void GetStrings_ReturnsLocalizedValues_ForLanguageWithoutName()
    {
        var strings = _sut.GetStrings(new Language("fr", false));

        strings["TestKey"].ShouldBe("Bonjour");
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

    [Fact]
    public void GetAvailableLanguages_ThrowsApplicationException_WhenDefaultResourceMissing()
    {
        var assembly = CreateResourceAssembly(
            ("Sanet.Test.Dynamic.Resources.Strings-be.resources", ResourceStream(new Dictionary<string, string> { { "TestKey", "Test" } })));

        var sut = new EmbeddedResourcesProvider(assembly, "Sanet.Test.Dynamic.Resources.Strings");

        var ex = Should.Throw<ApplicationException>(() => sut.GetAvailableLanguages());
        ex.Message.ShouldBe("Missing default language resource.");
    }

    [Fact]
    public void GetStrings_ThrowsMissingManifestResourceException_WhenStreamIsNull()
    {
        var assembly = CreateResourceAssembly(
            ("Sanet.Test.Dynamic.Resources.Strings.resources", ResourceStream(new Dictionary<string, string> { { "TestKey", "Hello" } })),
            ("Sanet.Test.Dynamic.Resources.Strings-agr.resources", null));

        var sut = new EmbeddedResourcesProvider(assembly, "Sanet.Test.Dynamic.Resources.Strings");

        Should.Throw<MissingManifestResourceException>(() => sut.GetStrings(new Language("agr", false)));
    }

    [Fact]
    public void GetAvailableLanguages_ThrowsMissingManifestResourceException_WhenLanguageNameStreamIsNull()
    {
        var assembly = CreateResourceAssembly(
            ("Sanet.Test.Dynamic.Resources.Strings.resources", ResourceStream(new Dictionary<string, string> { { "TestKey", "Hello" } })),
            ("Sanet.Test.Dynamic.Resources.Strings-agr.resources", null));

        var sut = new EmbeddedResourcesProvider(assembly, "Sanet.Test.Dynamic.Resources.Strings");

        Should.Throw<MissingManifestResourceException>(() => sut.GetAvailableLanguages());
    }

    private static Assembly CreateResourceAssembly(params (string Name, Func<Stream?>? StreamFactory)[] resources)
    {
        var assembly = Substitute.For<Assembly>();
        assembly.GetManifestResourceNames().Returns(resources.Select(r => r.Name).ToArray());
        assembly
            .GetManifestResourceStream(Arg.Any<string>())
            .Returns(callInfo => resources.FirstOrDefault(r => r.Name == callInfo.Arg<string>()).StreamFactory?.Invoke());
        return assembly;
    }

    private static Func<Stream?>? ResourceStream(Dictionary<string, string> entries)
    {
        return () =>
        {
            using var tempStream = new MemoryStream();
            using (var writer = new ResourceWriter(tempStream))
            {
                foreach (var (key, value) in entries)
                {
                    writer.AddResource(key, value);
                }
            }

            var stream = new MemoryStream(tempStream.ToArray());
            stream.Position = 0;
            return stream;
        };
    }
}

using DraftPuck.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Tests.Application;

/// <summary>MediatR and AutoMapper share one Lucky Penny bundle key, set as DRAFTPUCK_LUCKYPENNY__LICENSEKEY.</summary>
public class LicenseKeyTests
{
    [Fact]
    public void MediatRGetsTheLuckyPennyKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LuckyPenny:LicenseKey"] = "the-bundle-key" })
            .Build();

        using var services = new ServiceCollection().AddLogging().AddApplication(configuration).BuildServiceProvider();

        Assert.Equal("the-bundle-key", services.GetRequiredService<MediatRServiceConfiguration>().LicenseKey);
    }
}

using DraftPuck.Application.Features.System;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Settings;

/// <summary>
/// Which event logo the site settings return. Each test runs the app's clock (via test mode) on its own far-future
/// date, so rows from other tests in the shared database can't match.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class EventLogoTests(DatabaseFixture fixture)
{
    private static int _testDay;
    private readonly DateTime _now = new DateTime(2100, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(Interlocked.Increment(ref _testDay) * 30);

    private async Task AddEventAsync(string name, DateTime startsUtc, DateTime endsUtc)
    {
        await using var db = fixture.CreateContext();
        db.EventLogos.Add(new EventLogoEntity
        {
            Name = name,
            StartsUtc = startsUtc,
            EndsUtc = endsUtc,
            LogoUrl = $"/img/logo-{name}.png",
            WideLogoUrl = $"/img/logo-wide-{name}.png"
        });
        await db.SaveChangesAsync();
    }

    private async Task<EventLogoDto?> CurrentEventLogoAsync()
    {
        await using var db = fixture.CreateContext();
        var options = new ApplicationOptions { IsTestMode = true, TestModeStartDateTimeUtc = _now.Subtract(ApplicationOptions.TimeSinceStartup) };
        var handler = new GetSystemSettingsQueryHandler(Options.Create(options), Options.Create(new SystemOptions()), db);
        return (await handler.Handle(new GetSystemSettingsQuery(), default)).EventLogo;
    }

    [Fact]
    public async Task ReturnsTheEventOnNow()
    {
        await AddEventAsync("tournament", _now.AddDays(-2), _now.AddDays(2));

        var logo = await CurrentEventLogoAsync();

        Assert.Equal("tournament", logo?.Name);
        Assert.Equal("/img/logo-tournament.png", logo?.LogoUrl);
        Assert.Equal("/img/logo-wide-tournament.png", logo?.WideLogoUrl);
    }

    [Fact]
    public async Task ReturnsNothingWhenNoEventIsOn()
    {
        await AddEventAsync("past", _now.AddDays(-5), _now.AddDays(-1));
        await AddEventAsync("future", _now.AddDays(1), _now.AddDays(5));

        Assert.Null(await CurrentEventLogoAsync());
    }

    [Fact]
    public async Task WhenEventsOverlapTheLatestStartingWins()
    {
        await AddEventAsync("tournament", _now.AddDays(-5), _now.AddDays(5));
        await AddEventAsync("rivalry-night", _now.AddHours(-6), _now.AddHours(6));

        Assert.Equal("rivalry-night", (await CurrentEventLogoAsync())?.Name);
    }

    [Fact]
    public async Task AnEventEndsAtItsEndTime()
    {
        // Starts are inclusive and ends exclusive, so back-to-back events never both apply.
        await AddEventAsync("ending", _now.AddDays(-1), _now.AddMinutes(-1));
        await AddEventAsync("starting", _now.AddMinutes(-1), _now.AddDays(1));

        Assert.Equal("starting", (await CurrentEventLogoAsync())?.Name);
    }
}

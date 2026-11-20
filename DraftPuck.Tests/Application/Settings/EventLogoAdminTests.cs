using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Admin.EventLogos;
using DraftPuck.Application.Features.System;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Settings;

/// <summary>
/// Adding, listing and removing event logos from the admin page. Each test uses its own far-future dates, so rows from
/// other tests in the shared database don't get in the way.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class EventLogoAdminTests(DatabaseFixture fixture)
{
    private static int _testDay;
    private readonly DateTime _start = new DateTime(2200, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(Interlocked.Increment(ref _testDay) * 30);

    private CreateEventLogoCommand Event(string name = "Opening Night", int days = 3, string logo = "/img/logo-event.png", string wide = "https://example.com/wide.png")
        => new(name, _start, _start.AddDays(days), logo, wide);

    private async Task<EventLogoAdminDto> CreateAsync(CreateEventLogoCommand command)
    {
        await using var db = fixture.CreateContext();
        return await new CreateEventLogoCommandHandler(db).Handle(command, default);
    }

    private async Task<List<EventLogoAdminDto>> ListAsync()
    {
        await using var db = fixture.CreateContext();
        return await new GetEventLogosQueryHandler(db).Handle(new GetEventLogosQuery(), default);
    }

    [Fact]
    public async Task AnAddedEventLogoIsShownDuringTheEvent()
    {
        var created = await CreateAsync(Event("  Opening Night  "));
        Assert.Equal("Opening Night", created.Name);

        await using var db = fixture.CreateContext();
        var duringEvent = _start.AddDays(1);
        var options = new ApplicationOptions { IsTestMode = true, TestModeStartDateTimeUtc = duringEvent.Subtract(ApplicationOptions.TimeSinceStartup) };
        var settings = await new GetSystemSettingsQueryHandler(Options.Create(options), Options.Create(new SystemOptions()), db)
            .Handle(new GetSystemSettingsQuery(), default);

        Assert.Equal("/img/logo-event.png", settings.EventLogo?.LogoUrl);
    }

    [Fact]
    public async Task TheListHasTheLatestStartingFirst()
    {
        var earlier = await CreateAsync(Event("Earlier"));
        var later = await CreateAsync(Event("Later") with { StartsUtc = _start.AddDays(1) });

        var ours = (await ListAsync()).Where(e => e.Id == earlier.Id || e.Id == later.Id).Select(e => e.Name);

        Assert.Equal(["Later", "Earlier"], ours);
    }

    [Fact]
    public async Task TimesWithoutAZoneAreUtc()
    {
        var zoneless = DateTime.SpecifyKind(_start, DateTimeKind.Unspecified);

        var created = await CreateAsync(Event() with { StartsUtc = zoneless, EndsUtc = zoneless.AddDays(1) });

        Assert.Equal(_start, created.StartsUtc);
        Assert.Equal(DateTimeKind.Utc, created.StartsUtc.Kind);
    }

    [Theory]
    [InlineData("", "/img/a.png", "/img/b.png", 3)]
    [InlineData("Event", "/img/a.png", "/img/b.png", 0)]
    [InlineData("Event", "/img/a.png", "/img/b.png", -1)]
    [InlineData("Event", "logo.png", "/img/b.png", 3)]
    [InlineData("Event", "//elsewhere.com/a.png", "/img/b.png", 3)]
    [InlineData("Event", "javascript:alert(1)", "/img/b.png", 3)]
    [InlineData("Event", "/img/a.png", "", 3)]
    public async Task BadInputIsRejected(string name, string logo, string wide, int days)
        => await Assert.ThrowsAsync<BadRequestException>(() => CreateAsync(Event(name, days, logo, wide)));

    [Fact]
    public async Task ANameTooLongForTheColumnIsRejected()
        => await Assert.ThrowsAsync<BadRequestException>(() => CreateAsync(Event(new string('x', 101))));

    [Fact]
    public async Task RemovingDeletesIt()
    {
        var created = await CreateAsync(Event());

        await using (var db = fixture.CreateContext())
            await new DeleteEventLogoCommandHandler(db).Handle(new DeleteEventLogoCommand(created.Id), default);

        Assert.DoesNotContain(await ListAsync(), e => e.Id == created.Id);
    }

    [Fact]
    public async Task RemovingOneThatIsNotThereIsNotFound()
    {
        await using var db = fixture.CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(() => new DeleteEventLogoCommandHandler(db).Handle(new DeleteEventLogoCommand(Guid.NewGuid()), default));
    }
}

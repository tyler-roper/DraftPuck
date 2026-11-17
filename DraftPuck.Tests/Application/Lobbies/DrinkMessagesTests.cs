using System.ComponentModel.DataAnnotations;
using DraftPuck.Application.Features.Lobbies.Drinks;

namespace DraftPuck.Tests.Application.Lobbies;

public class DrinkMessagesTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   \n ", null)]
    [InlineData("  Drink up  ", "Drink up")]
    [InlineData("Line\r\nbreaks   and\ttabs", "Line breaks and tabs")]
    public void MessagesAreTidiedUpOrDropped(string? message, string? expected) =>
        Assert.Equal(expected, DrinkMessages.Normalize(message));

    [Fact]
    public void MessagesHaveAMaximumLength()
    {
        Assert.Equal(new string('x', DrinkMessages.MaxLength), DrinkMessages.Normalize(new string('x', DrinkMessages.MaxLength)));
        Assert.Throws<ValidationException>(() => DrinkMessages.Normalize(new string('x', DrinkMessages.MaxLength + 1)));
    }

    [Fact]
    public void AQuoteIsSafeToShowAsHtmlAndCannotBecomeAToken()
    {
        var quote = DrinkMessages.Quote("<b>{{name}}</b> & co");

        Assert.Contains("“&lt;b&gt;&#123;&#123;name&#125;&#125;&lt;/b&gt; &amp; co”", quote);
        Assert.DoesNotContain("{{", quote);
        Assert.DoesNotContain("<b>", quote);
    }
}

namespace DraftPuck.Tests.Support;

/// <summary>NSubstitute matchers for the tokens a push notification went to.</summary>
public static class PushArgs
{
    /// <summary>Exactly these devices' tokens, in any order.</summary>
    public static IReadOnlyCollection<string> Devices(params string[] tokens) =>
        Arg.Is<IReadOnlyCollection<string>>(sent => sent.Order().SequenceEqual(tokens.Order()));
}

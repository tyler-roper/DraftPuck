using DraftPuck.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Tests.Infrastructure;

/// <summary>The EF Core model builds without warnings, so every app starts with a clean log.</summary>
public class ModelWarningTests
{
    private sealed class WarningCollector : ILoggerProvider, ILogger
    {
        public List<string> Warnings { get; } = [];
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) Warnings.Add($"{eventId.Name}: {formatter(state, exception)}");
        }
        public void Dispose() { }
    }

    [Fact]
    public void TheModelBuildsWithoutWarnings()
    {
        var collector = new WarningCollector();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(collector));
        using var db = new DraftPuckContext(new DbContextOptionsBuilder<DraftPuckContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .UseLoggerFactory(loggerFactory)
            .Options);

        _ = db.Model;

        Assert.True(collector.Warnings.Count == 0, string.Join("\n", collector.Warnings));
    }
}

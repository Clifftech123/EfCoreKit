using Microsoft.Extensions.Logging;

namespace EfCoreKit.Tests.Integration;

public class SlowQueryTests
{
    [Fact]
    public async Task SlowQuery_LogsWarning_WhenQueryExceedsThreshold()
    {
        var logger = new TestLoggerFactory();
        await using var ctx = DbFactory.CreateWithSlowQueryLogging(
            TimeSpan.Zero, // threshold of zero ensures every query triggers
            logger);

        ctx.Products.Add(new Product { Name = "SlowTest", Price = 1m });
        await ctx.SaveChangesAsync();

        // A query that exceeds threshold=0
        _ = await ctx.Products.ToListAsync();

        Assert.True(logger.Warnings.Count > 0,
            "Expected at least one slow query warning to be logged.");
        Assert.Contains(logger.Warnings, w => w.Contains("Slow query detected"));
    }

    [Fact]
    public async Task SlowQuery_DoesNotLog_WhenThresholdNotExceeded()
    {
        var logger = new TestLoggerFactory();
        await using var ctx = DbFactory.CreateWithSlowQueryLogging(
            TimeSpan.FromMinutes(10), // high threshold — nothing is slow
            logger);

        ctx.Products.Add(new Product { Name = "FastTest", Price = 1m });
        await ctx.SaveChangesAsync();

        _ = await ctx.Products.ToListAsync();

        var slowWarnings = logger.Warnings.Where(w => w.Contains("Slow query detected")).ToList();
        Assert.Empty(slowWarnings);
    }

    [Fact]
    public async Task SlowQuery_DoesNotThrow_WhenNoLoggerProvided()
    {
        await using var ctx = DbFactory.CreateWithSlowQueryLogging(
            TimeSpan.Zero, // threshold zero, no logger
            loggerFactory: null);

        ctx.Products.Add(new Product { Name = "NoLogger", Price = 1m });
        await ctx.SaveChangesAsync();

        // Should not throw even though threshold is exceeded
        var products = await ctx.Products.ToListAsync();
        Assert.Single(products);
    }
}

// ── Test logger infrastructure ──────────────────────────────────────────────

public class TestLoggerFactory : ILoggerFactory
{
    public List<string> Warnings { get; } = [];

    public ILogger CreateLogger(string categoryName) => new TestLogger(this);
    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }

    private class TestLogger(TestLoggerFactory factory) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
                factory.Warnings.Add(formatter(state, exception));
        }
    }
}

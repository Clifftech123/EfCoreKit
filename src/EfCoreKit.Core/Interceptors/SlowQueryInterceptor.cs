using System.Data.Common;
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Interceptor that logs queries exceeding the configured slow query threshold.
/// </summary>
/// <remarks>
/// <para>
/// Hooks into <c>ReaderExecuted</c>, <c>ScalarExecuted</c>, and <c>NonQueryExecuted</c>
/// (both sync and async) to compare the command duration
/// against <see cref="EfCoreOptions.SlowQueryThreshold"/>.
/// </para>
/// <para>
/// When the threshold is exceeded, a warning is emitted via <see cref="ILogger{TCategoryName}"/>
/// including the duration and the SQL command text.
/// </para>
/// </remarks>
/// <example>
/// Register standalone (without <see cref="EfCoreDbContext{TContext}"/>):
/// <code>
/// services.AddDbContext&lt;MyDbContext&gt;((sp, options) =&gt;
/// {
///     var kitOpts = new EfCoreOptions().EnableSlowQueryLogging(TimeSpan.FromSeconds(2));
///     var logger = sp.GetService&lt;ILogger&lt;SlowQueryInterceptor&gt;&gt;();
///     options.UseSqlServer(connectionString)
///            .AddInterceptors(new SlowQueryInterceptor(kitOpts, logger));
/// });
/// </code>
/// </example>
internal sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly EfCoreOptions _options;
    private readonly ILogger<SlowQueryInterceptor>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlowQueryInterceptor"/> class.
    /// </summary>
    /// <param name="options">The EfCoreKit configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public SlowQueryInterceptor(EfCoreOptions options, ILogger<SlowQueryInterceptor>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogIfSlow(command, eventData.Duration);
        return base.ReaderExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogIfSlow(command, eventData.Duration);
        return base.ScalarExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogIfSlow(command, eventData.Duration);
        return base.NonQueryExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogIfSlow(DbCommand command, TimeSpan duration)
    {
        if (_options.SlowQueryThreshold is null || duration < _options.SlowQueryThreshold.Value)
            return;

        _logger?.LogWarning(
            "Slow query detected ({Duration}ms, threshold {Threshold}ms): {CommandText}",
            duration.TotalMilliseconds,
            _options.SlowQueryThreshold.Value.TotalMilliseconds,
            command.CommandText);
    }
}

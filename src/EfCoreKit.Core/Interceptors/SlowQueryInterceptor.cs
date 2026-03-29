using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Interceptor that logs queries exceeding the configured slow query threshold.
/// </summary>
internal sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly EfCoreKitOptions _options;
    private readonly ILogger<SlowQueryInterceptor>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlowQueryInterceptor"/> class.
    /// </summary>
    /// <param name="options">The EfCoreKit configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public SlowQueryInterceptor(EfCoreKitOptions options, ILogger<SlowQueryInterceptor>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    // TODO: Override ReaderExecutedAsync / ScalarExecutedAsync / NonQueryExecutedAsync
    // to measure command duration and log when it exceeds _options.SlowQueryThreshold
}

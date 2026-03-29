using System.Collections.Concurrent;
using System.Reflection;

namespace EfCoreKit.Core.Internal;

/// <summary>
/// Thread-safe cache for reflection metadata to avoid repeated lookups.
/// </summary>
internal static class ReflectionCache
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>
    /// Gets the public instance properties for the given type, using a cache.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The public instance properties.</returns>
    public static PropertyInfo[] GetProperties(Type type) =>
        PropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
}

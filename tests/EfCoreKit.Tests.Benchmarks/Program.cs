using BenchmarkDotNet.Running;

namespace EfCoreKit.Tests.Benchmarks;

/// <summary>
/// Entry point for running EfCoreKit benchmarks.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

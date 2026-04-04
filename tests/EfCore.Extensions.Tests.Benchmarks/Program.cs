using BenchmarkDotNet.Running;

namespace EfCore.Extensions.Tests.Benchmarks;

/// <summary>
/// Entry point for running EfCore.Extensions benchmarks.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

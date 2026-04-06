using ArchiForge.Persistence.Data.Infrastructure;

using BenchmarkDotNet.Attributes;

namespace ArchiForge.Benchmarks;

/// <summary>String fragments used after ORDER BY in list queries (see repositories using <see cref="SqlPagingSyntax"/>).</summary>
[MemoryDiagnoser]
public class SqlPagingSyntaxMicroBenchmarks
{
    [Params(200, 500, 1000)]
    public int RowCount { get; set; }

    [Benchmark]
    public string FirstRowsOnlyFragment()
    {
        return SqlPagingSyntax.FirstRowsOnly(RowCount);
    }
}

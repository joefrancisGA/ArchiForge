using BenchmarkDotNet.Attributes;

namespace ArchiForge.Benchmarks;

/// <summary>
/// Illustrates wall-clock gain from parallelizing independent I/O-bound work (same shape as four LLM calls).
/// Not a substitute for production load tests; see <c>scripts/load/</c>.
/// </summary>
[MemoryDiagnoser]
public class SimulatedParallelBatchBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task SequentialFourDelays()
    {
        await Task.Delay(1);
        await Task.Delay(1);
        await Task.Delay(1);
        await Task.Delay(1);
    }

    [Benchmark]
    public async Task ParallelFourDelays()
    {
        await Task.WhenAll(
            Task.Delay(1),
            Task.Delay(1),
            Task.Delay(1),
            Task.Delay(1));
    }
}
